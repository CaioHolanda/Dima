import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

test('automatic checks, trusted activity, suspended tabs and account changes', async () => {
    const listeners = new Map();
    globalThis.window = {
        addEventListener: (name, action) => listeners.set(name, action),
        removeEventListener: name => listeners.delete(name)
    };
    globalThis.document = { ...window, visibilityState: 'visible' };
    globalThis.localStorage = { setItem() {} };
    let tick;
    globalThis.setInterval = action => { tick = action; return 1; };
    globalThis.clearInterval = () => {};
    let status = 200, id = 'first', offline = false;
    const requests = [], endings = [];
    globalThis.fetch = async (url, options) => {
        requests.push({ url, ...options });
        if (offline) throw new Error('offline');
        return { status, ok: status === 200, json: async () => ({ sessionId: id, expiresUtc: new Date().toISOString() }) };
    };
    const source = await readFile(new URL('../../Dima.Web/wwwroot/js/session-monitor.js', import.meta.url), 'utf8');
    const monitor = await import('data:text/javascript;base64,' + Buffer.from(source).toString('base64'));
    const settle = () => new Promise(resolve => setImmediate(resolve));
    monitor.initialize('https://example.test/api/', { invokeMethodAsync: async (_, reason) => endings.push(reason) });
    await monitor.setAuthenticated(true);
    assert.equal(requests.at(-1).method, 'GET');
    tick(); await settle();
    assert.equal(requests.at(-1).method, 'GET');
    const count = requests.length;
    listeners.get('pointerdown')({ isTrusted: false }); await settle();
    assert.equal(requests.length, count);
    listeners.get('pointerdown')({ isTrusted: true }); await settle();
    assert.equal(requests.at(-1).method, 'POST');
    assert.equal(requests.at(-1).headers['X-Dima-Session'], 'first');
    const touched = requests.length;
    listeners.get('keydown')({ isTrusted: true }); await settle();
    assert.equal(requests.length, touched);
    status = 403; tick(); await settle();
    assert.deepEqual(endings, []);
    offline = true; tick(); await settle();
    assert.deepEqual(endings, []);
    offline = false; status = 200; id = 'second';
    listeners.get('storage')({ key: 'dima.session.changed' }); await settle();
    assert.deepEqual(endings, ['changed']);
    await monitor.setAuthenticated(false);
    await monitor.setAuthenticated(true);
    status = 401;
    listeners.get('visibilitychange')(); await settle();
    assert.deepEqual(endings, ['changed', 'expired']);
    monitor.dispose();
    assert.equal(listeners.size, 0);
});
