import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

const source = (await readFile(new URL('../../Dima.Web/wwwroot/js/session-monitor.js', import.meta.url), 'utf8'))
    .replaceAll('export ', '') + '\nglobalThis.monitor = { initialize, setAuthenticated, dispose };';
const settle = async () => { for (let i = 0; i < 8; i++) await Promise.resolve(); };
function fixture(BroadcastChannel) {
    let now = Date.UTC(2026, 8, 29), sequence = 0;
    const timers = new Map(), listeners = new Map(), requests = [], endings = [], messages = [];
    const state = { status: 200, id: 'first', offline: false, expiry: now + 900000, skew: 0, hold: null };
    const context = {
        AbortSignal,
        BroadcastChannel,
        Date: class extends Date { static now() { return now + state.skew; } },
        window: { addEventListener: (n, f) => listeners.set(n, f), removeEventListener: n => listeners.delete(n) },
        document: { visibilityState: 'visible', addEventListener: (n, f) => listeners.set(n, f), removeEventListener: n => listeners.delete(n) },
        localStorage: { setItem: (_, message) => messages.push(JSON.parse(message)) },
        setTimeout: (f, delay) => { const id = ++sequence; timers.set(id, { f, at: now + delay }); return id; },
        clearTimeout: id => timers.delete(id),
        fetch: async (url, options) => {
            requests.push({ url, ...options });
            if (state.hold) await state.hold;
            if (state.offline) throw new Error('offline');
            if (options.method === 'POST' && state.status === 200) state.expiry = now + 900000;
            return { status: state.status, ok: state.status === 200, json: async () => ({
                sessionId: state.id, expiresUtc: new Date(state.expiry).toISOString(), serverUtc: new Date(now).toISOString()
            }) };
        }
    };
    vm.runInNewContext(source, context);
    const monitor = context.monitor;
    monitor.initialize('/api/', { invokeMethodAsync: async (_, reason) => endings.push(reason) });
    const advance = async milliseconds => {
        const target = now + milliseconds;
        for (;;) {
            const next = [...timers].filter(([, t]) => t.at <= target).sort((a, b) => a[1].at - b[1].at)[0];
            if (!next) break;
            now = next[1].at; timers.delete(next[0]); next[1].f(); await settle();
        }
        now = target; await settle();
    };
    const event = async (name, value = {}) => { listeners.get(name)?.(value); await settle(); };
    return { monitor, state, context, requests, endings, messages, timers, listeners, advance, event,
        activity: () => event('keydown', { isTrusted: true }), now: () => now };
}

test('idle tab has no polling and expires locally at the server deadline', async () => {
    const f = fixture(); await f.monitor.setAuthenticated(true);
    await f.advance(899999);
    assert.equal(f.requests.length, 1); assert.deepEqual(f.endings, []);
    await f.advance(1);
    assert.deepEqual(f.endings, ['expired']); assert.equal(f.requests.length, 1);
    f.monitor.dispose(); assert.equal(f.timers.size, 0); assert.equal(f.listeners.size, 0);
});

test('trusted interactions are coalesced into one trailing activity per minute', async () => {
    const f = fixture(); await f.monitor.setAuthenticated(true);
    await f.event('keydown', { isTrusted: false }); await f.advance(60000);
    assert.equal(f.requests.length, 1);
    await f.activity(); await f.advance(0);
    assert.equal(f.requests.at(-1).method, 'POST');
    assert.equal(f.requests.at(-1).headers['X-Dima-Session'], 'first');
    await f.advance(10000); await f.activity(); await f.activity();
    await f.advance(49999); assert.equal(f.requests.length, 2);
    await f.advance(1); assert.equal(f.requests.length, 3);
    await f.advance(120000); assert.equal(f.requests.length, 3);
});

test('last interaction inside the minute is delivered even without another event', async () => {
    const f = fixture(); await f.monitor.setAuthenticated(true);
    await f.advance(10000); await f.activity();
    await f.advance(50000);
    assert.equal(f.requests.length, 2);
    await f.advance(899999); assert.deepEqual(f.endings, []);
    await f.advance(1); assert.deepEqual(f.endings, ['expired']);
});

test('client clock offset does not change the fifteen minute duration', async () => {
    const f = fixture(); f.state.skew = 7200000; await f.monitor.setAuthenticated(true);
    await f.advance(899999); assert.deepEqual(f.endings, []);
    await f.advance(1); assert.deepEqual(f.endings, ['expired']);
});

test('another tab shares extended deadline without triggering a GET', async () => {
    const f = fixture(); await f.monitor.setAuthenticated(true);
    await f.advance(600000);
    await f.event('storage', { key: 'dima.session.changed', newValue: JSON.stringify({
        type: 'status', sessionId: 'first', expiry: f.now() + 900000, remaining: 900000, sentAt: f.now()
    }) });
    await f.advance(300000); assert.deepEqual(f.endings, []); assert.equal(f.requests.length, 1);
    await f.advance(600000); assert.deepEqual(f.endings, ['expired']);
});

test('hidden tab revalidates on resume before sending pending activity', async () => {
    const f = fixture(); await f.monitor.setAuthenticated(true);
    f.context.document.visibilityState = 'hidden'; await f.advance(960000);
    assert.deepEqual(f.endings, []); assert.equal(f.requests.length, 1);
    f.context.document.visibilityState = 'visible'; f.state.status = 401;
    await f.event('visibilitychange'); await f.activity(); await f.advance(60000);
    assert.deepEqual(f.endings, ['expired']); assert.equal(f.requests.length, 2);
    assert.equal(f.requests.at(-1).method, 'GET');
});

test('403 and network errors are not interpreted as unauthorized and do not retry in background', async () => {
    const f = fixture(); await f.monitor.setAuthenticated(true);
    f.state.status = 403; await f.activity(); await f.advance(60000);
    assert.deepEqual(f.endings, []);
    f.state.offline = true; await f.activity(); await f.advance(60000);
    const count = f.requests.length;
    await f.advance(60000); assert.equal(f.requests.length, count); assert.deepEqual(f.endings, []);
});

test('logout cancels pending activity and stale fetch cannot restore the session', async () => {
    const f = fixture(); let release;
    f.state.hold = new Promise(resolve => { release = resolve; });
    const loading = f.monitor.setAuthenticated(true);
    await f.monitor.setAuthenticated(false); release(); await loading;
    await f.advance(1000000); assert.equal(f.timers.size, 0); assert.equal(f.messages.filter(x => x.type === 'status').length, 0);
});

test('account changes, logout and activity conflicts clear the old state', async () => {
    for (const type of ['status', 'logout', 'conflict']) {
        const f = fixture(); await f.monitor.setAuthenticated(true);
        if (type === 'conflict') {
            f.state.status = 409; await f.activity(); await f.advance(60000);
        } else {
            await f.event('storage', { key: 'dima.session.changed', newValue: JSON.stringify({ type, sessionId: type === 'logout' ? 'first' : 'second' }) });
        }
        assert.deepEqual(f.endings, [type === 'logout' ? 'expired' : 'changed']);
        assert.equal(f.timers.size, 0);
    }
});

test('failed resume keeps the known expiry and storage failure never enables polling', async () => {
    const f = fixture();
    f.context.localStorage.setItem = () => { throw new Error('storage disabled'); };
    await f.monitor.setAuthenticated(true); await f.advance(600000);
    f.state.offline = true; await f.event('focus');
    assert.deepEqual(f.endings, []);
    await f.advance(300000);
    assert.deepEqual(f.endings, ['expired']); assert.equal(f.requests.length, 2);
});

test('in-flight resume gates activity and coalesces focus/visibility notifications', async () => {
    const f = fixture(); await f.monitor.setAuthenticated(true); await f.advance(120000);
    let release; f.state.hold = new Promise(resolve => { release = resolve; });
    await f.event('focus'); await f.event('visibilitychange'); await f.activity();
    assert.equal(f.requests.length, 2); assert.equal(f.requests.at(-1).method, 'GET');
    f.state.status = 401; release(); await settle(); await f.advance(60000);
    assert.deepEqual(f.endings, ['expired']); assert.equal(f.requests.length, 2);
});

test('BroadcastChannel shares activity and closes on disposal when storage is blocked', async () => {
    let channel;
    class Channel {
        constructor() { channel = this; }
        postMessage(value) { this.sent = value; }
        close() { this.closed = true; }
    }
    const f = fixture(Channel);
    f.context.localStorage.setItem = () => { throw new Error('blocked'); };
    await f.monitor.setAuthenticated(true);
    assert.equal(channel.sent.type, 'status');
    await f.advance(600000);
    channel.onmessage({ data: { type: 'status', sessionId: 'first', expiry: f.now() + 900000,
        remaining: 900000, sentAt: f.now() } });
    await f.advance(300000); assert.deepEqual(f.endings, []); assert.equal(f.requests.length, 1);
    f.monitor.dispose(); assert.equal(channel.closed, true); assert.equal(f.timers.size, 0);
});

test('pending activity cannot overrun a short server deadline', async () => {
    const f = fixture(); f.state.expiry = f.now() + 30000;
    await f.monitor.setAuthenticated(true); await f.activity(); await f.advance(60000);
    assert.deepEqual(f.endings, ['expired']); assert.equal(f.requests.length, 1);
});
