let api, callback, activityTimer, expiryTimer, channel, sessionId;
let active = false, busy = false, pending = false, needsValidation = false;
let generation = 0, lastAttempt = 0, deadline = 0, serverExpiry = 0;
let lastValidationAttempt = -Infinity;
const key = 'dima.session.changed';
const interval = 60000;
const events = ['pointerdown', 'keydown', 'wheel', 'touchstart'];

export function initialize(base, reference) {
    api = base;
    callback = reference;
    events.forEach(name => window.addEventListener(name, interaction, { passive: true }));
    window.addEventListener('storage', changed);
    window.addEventListener('focus', resumed);
    document.addEventListener('visibilitychange', resumed);
    try {
        channel = new BroadcastChannel(key);
        channel.onmessage = event => receive(event.data);
    } catch { /* Storage events remain available where supported. */ }
}

function broadcast(message) {
    const value = { ...message, sentAt: Date.now(), nonce: Math.random() };
    try { channel?.postMessage(value); } catch { /* Try storage too. */ }
    try { localStorage.setItem(key, JSON.stringify(value)); } catch { /* Revalidate on resume. */ }
}

function stop() {
    active = false;
    generation++;
    busy = pending = needsValidation = false;
    clearTimeout(activityTimer);
    clearTimeout(expiryTimer);
    activityTimer = expiryTimer = undefined;
}

export async function setAuthenticated(value) {
    if (!value) {
        if (active) broadcast({ type: 'logout', sessionId });
        stop();
        sessionId = undefined;
        return;
    }
    if (active) return;
    active = true;
    lastAttempt = Date.now();
    serverExpiry = deadline = 0;
    needsValidation = true;
    await check(false);
}

async function end(reason) {
    if (!active) return;
    stop();
    await callback.invokeMethodAsync('EndSession', reason);
}

function setDeadline(expiry, remaining) {
    // An older response/notification must not undo a newer activity acknowledgement.
    if (expiry < serverExpiry) return;
    serverExpiry = expiry;
    deadline = Date.now() + Math.max(0, remaining);
    clearTimeout(expiryTimer);
    expiryTimer = setTimeout(() => {
        // A suspended tab is validated when it becomes visible again.
        if (document.visibilityState === 'visible') end('expired');
    }, Math.max(0, remaining));
}

async function check(activity) {
    if (!active || busy) return;
    busy = true;
    const version = generation;
    const started = Date.now();
    if (!activity) lastValidationAttempt = started;
    if (activity) {
        lastAttempt = started;
        pending = false;
    }
    try {
        const response = await fetch(api + 'v1/identity/session' + (activity ? '/activity' : ''), {
            method: activity ? 'POST' : 'GET', credentials: 'include', cache: 'no-store',
            signal: AbortSignal.timeout(15000),
            headers: { 'X-Requested-With': 'XMLHttpRequest', ...(activity ? { 'X-Dima-Session': sessionId } : {}) }
        });
        if (!active || version !== generation) return;
        if (response.status === 401) { await end('expired'); return; }
        if (response.status === 409) { await end('changed'); return; }
        if (!response.ok) { pending = false; return; }
        const status = await response.json();
        if (!active || version !== generation) return;
        if (sessionId && sessionId !== status.sessionId) { await end('changed'); return; }
        const expiry = Date.parse(status.expiresUtc);
        const serverNow = Date.parse(status.serverUtc);
        if (!Number.isFinite(expiry) || !Number.isFinite(serverNow)) return;
        sessionId = status.sessionId;
        needsValidation = false;
        // Use server time, conservatively subtracting the request duration.
        setDeadline(expiry, expiry - serverNow - (Date.now() - started));
        broadcast({ type: 'status', sessionId, expiry: serverExpiry, remaining: deadline - Date.now() });
    } catch {
        pending = false; // No background retry loop when offline.
    } finally {
        if (version === generation) {
            busy = false;
            // A failed resume check must not discard the last known local deadline.
            if (active && deadline && expiryTimer === undefined)
                setDeadline(serverExpiry, deadline - Date.now());
            scheduleActivity();
        }
    }
}

function scheduleActivity() {
    if (!active || !pending || busy || needsValidation || activityTimer !== undefined) return;
    activityTimer = setTimeout(() => {
        activityTimer = undefined;
        if (!active || !pending) return;
        if (Date.now() >= deadline) { end('expired'); return; }
        check(true);
    }, Math.max(0, lastAttempt + interval - Date.now()));
}

function interaction(event) {
    if (!event.isTrusted || !active || document.visibilityState !== 'visible') return;
    pending = true;
    if (needsValidation || !sessionId) {
        if (Date.now() - lastValidationAttempt >= interval) check(false);
        return;
    }
    scheduleActivity();
}

function resumed() {
    if (!active || document.visibilityState !== 'visible') return;
    needsValidation = true;
    clearTimeout(activityTimer);
    activityTimer = undefined;
    // Do not let an old local deadline sign out a session renewed in another tab.
    clearTimeout(expiryTimer);
    expiryTimer = undefined;
    check(false);
}

function receive(message) {
    if (!active || !message || !sessionId) return;
    if (message.type === 'logout' && message.sessionId === sessionId) { end('expired'); return; }
    if (message.type !== 'status') return;
    if (message.sessionId !== sessionId) { end('changed'); return; }
    if (Number.isFinite(message.expiry) && Number.isFinite(message.remaining) && Number.isFinite(message.sentAt)) {
        setDeadline(message.expiry, message.remaining - Math.max(0, Date.now() - message.sentAt));
    }
}

function changed(event) {
    if (event.key !== key || !event.newValue) return;
    try { receive(JSON.parse(event.newValue)); } catch { /* Ignore malformed notifications. */ }
}

export function dispose() {
    stop();
    channel?.close();
    channel = undefined;
    sessionId = undefined;
    events.forEach(name => window.removeEventListener(name, interaction));
    window.removeEventListener('storage', changed);
    window.removeEventListener('focus', resumed);
    document.removeEventListener('visibilitychange', resumed);
}
