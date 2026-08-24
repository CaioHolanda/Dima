// DT-22.1
// Transitional Service Worker used to remove the legacy PWA cache
// and unregister the Service Worker itself.

self.addEventListener('install', () => {
    console.info('Service worker cleanup: Install');

    // Activate this Service Worker immediately,
    // without waiting for the previous one to stop.
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    console.info('Service worker cleanup: Activate');

    event.waitUntil(cleanup());
});

async function cleanup() {
    // Take control of currently open pages as soon as possible.
    await self.clients.claim();

    // Remove all Cache Storage entries created for this application.
    const cacheKeys = await caches.keys();

    await Promise.all(
        cacheKeys.map(cacheKey => caches.delete(cacheKey))
    );

    console.info('Service worker cleanup: Cache cleared');

    // Remove this Service Worker registration.
    await self.registration.unregister();

    console.info('Service worker cleanup: Unregistered');

    // Reload all open application windows so they return to the network
    // and load the current published version.
    const clients = await self.clients.matchAll({
        type: 'window',
        includeUncontrolled: true
    });

    for (const client of clients) {
        client.navigate(client.url);
    }
}