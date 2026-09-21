let api, callback, timer, sessionId, active = false, busy = false, lastSent = 0;
const key = 'dima.session.changed';
const events = ['pointerdown', 'keydown', 'wheel', 'touchstart'];

export function initialize(base, reference) {
    api = base;
    callback = reference;
    events.forEach(name => window.addEventListener(name, interaction, { passive: true }));
    window.addEventListener('storage', changed);
    window.addEventListener('focus', resumed);
    document.addEventListener('visibilitychange', resumed);
    timer = setInterval(() => check(false), 15000);
}

function broadcast(value) {
    try { localStorage.setItem(key, JSON.stringify({ value, nonce: Math.random() })); } catch { /* Polling still synchronizes tabs. */ }
}

export async function setAuthenticated(value) {
    if (!value) {
        if (active) broadcast('logout');
        active = false;
        sessionId = undefined;
        return;
    }
    const starting = !active;
    active = true;
    await check(false);
    if (starting && sessionId) broadcast(sessionId);
}

async function end(reason) {
    active = false;
    await callback.invokeMethodAsync('EndSession', reason);
}

async function check(activity) {
    if (!active || busy) return;
    busy = true;
    try {
        // Always consult the server on resume, before reporting new activity.
        const response = await fetch(api + 'v1/identity/session' + (activity ? '/activity' : ''), {
            method: activity ? 'POST' : 'GET', credentials: 'include', cache: 'no-store',
            headers: { 'X-Requested-With': 'XMLHttpRequest', ...(activity ? { 'X-Dima-Session': sessionId } : {}) }
        });
        if (response.status === 401) { await end('expired'); return; }
        if (response.status === 409) { await end('changed'); return; }
        // A 403 is an authorization failure, not proof of session expiration.
        if (!response.ok) return;
        const status = await response.json();
        if (sessionId && sessionId !== status.sessionId) { await end('changed'); return; }
        sessionId = status.sessionId;
        if (activity) lastSent = Date.now();
    } catch {
        // Connection failures must not be presented as expired credentials.
    } finally { busy = false; }
}

function interaction(event) {
    if (!event.isTrusted || !active || !sessionId || document.visibilityState !== 'visible') return;
    if (Date.now() - lastSent >= 30000) check(true);
}

function resumed() {
    if (document.visibilityState === 'visible') check(false);
}

function changed(event) {
    if (event.key !== key) return;
    if (active && event.newValue) {
        try {
            const message = JSON.parse(event.newValue);
            if (message.value === 'logout') { end('expired'); return; }
            if (sessionId && message.value !== sessionId) { end('changed'); return; }
        } catch { /* Verify malformed notifications against the server. */ }
    }
    check(false);
}

export function dispose() {
    clearInterval(timer);
    events.forEach(name => window.removeEventListener(name, interaction));
    window.removeEventListener('storage', changed);
    window.removeEventListener('focus', resumed);
    document.removeEventListener('visibilitychange', resumed);
    active = false;
}
