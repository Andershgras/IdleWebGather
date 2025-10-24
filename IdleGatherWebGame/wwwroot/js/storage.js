window.storage = {
    get: (key) => Promise.resolve(localStorage.getItem(key)),
    set: (key, value) => { localStorage.setItem(key, value); return Promise.resolve(); },
    remove: (key) => { localStorage.removeItem(key); return Promise.resolve(); }
};