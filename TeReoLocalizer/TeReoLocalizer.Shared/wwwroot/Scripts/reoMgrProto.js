"use strict";
(function (root, factory) {
    if (typeof window["define"] === 'function' && window["define"].amd) {
        window["define"]([], factory);
    }
    else if (typeof window["module"] === 'object' && window["module"].exports) {
        window["module"].exports = factory();
    }
    else {
        root.reo = factory();
    }
}(typeof self !== 'undefined' ? self : this, function () {
    const defaultLang = 'cs';
    class Reo {
        constructor() {
            this.translations = {};
            this.moduleStates = new Map();
            this.currentLang = this.getCurrentLang();
            this.loadingMapPromise = null;
            this.translationMap = null;
            this.isDevelopment = window["mcfDebug"] || true;
            if (this.isDevelopment) {
                return new Proxy(this, {
                    get: (target, prop) => {
                        if (prop in target) {
                            const value = target[prop];
                            return typeof value === 'function' ? value.bind(target) : value;
                        }
                        if (!(target.translations[prop]?.[target.currentLang])) {
                            target.loadTranslationMap()
                                .then(() => {
                                const owner = target.findModuleForKey(prop);
                                if (owner) {
                                    console.warn(`Missing translation key: ${prop} for language: ${target.currentLang}. ` +
                                        `To resolve this, please load module '${owner}' using reo.require('${owner}')`);
                                }
                                else {
                                    console.warn(`Missing translation key: ${prop} for language: ${target.currentLang}. ` +
                                        `Key not found in any known module`);
                                }
                            });
                            return `[[${prop}]]`;
                        }
                        return target.translations[prop][target.currentLang];
                    }
                });
            }
        }
        async loadTranslationMap() {
            if (this.translationMap !== null) {
                return Promise.resolve();
            }
            if (this.loadingMapPromise !== null) {
                return this.loadingMapPromise;
            }
            this.loadingMapPromise = new Promise(async (resolve, reject) => {
                try {
                    const response = await fetch('/scripts/reo/reo.map.json');
                    if (!response.ok) {
                        console.warn('Failed to load translation map file');
                        this.translationMap = {};
                        resolve();
                        return;
                    }
                    this.translationMap = await response.json();
                    resolve();
                }
                catch (error) {
                    console.warn('Error loading translation map:', error);
                    this.translationMap = {};
                    resolve();
                }
                finally {
                    this.loadingMapPromise = null;
                }
            });
            return this.loadingMapPromise;
        }
        findModuleForKey(key) {
            if (!this.translationMap)
                return null;
            for (const [module, keys] of Object.entries(this.translationMap)) {
                if (keys.includes(key)) {
                    return module;
                }
            }
            return null;
        }
        get devMode() {
            return this.isDevelopment;
        }
        async require(module, lang = this.currentLang) {
            const moduleKey = module.toLowerCase();
            let state = this.moduleStates.get(moduleKey);
            if (!state) {
                state = { isLoaded: false, isLoading: false, languages: new Set() };
                this.moduleStates.set(moduleKey, state);
            }
            if (state.languages.has(lang)) {
                return Promise.resolve();
            }
            if (state.isLoading && state.promise) {
                return state.promise;
            }
            state.isLoading = true;
            state.promise = new Promise(async (resolve, reject) => {
                try {
                    let v = window["mcf"] ? window["mcf"]["getAppVersion"]() : "v1";
                    const url = `/scripts/reo/${moduleKey}.${lang}.json?v=${v}`;
                    const response = await fetch(url);
                    if (!response.ok) {
                        console.log(`Failed to load translations for module '${moduleKey}' and language '${lang}': ${response.status}`);
                        return;
                    }
                    const newTranslations = await response.json();
                    for (const key in newTranslations) {
                        if (!this.translations[key]) {
                            this.translations[key] = {};
                        }
                        this.translations[key][lang] = newTranslations[key];
                    }
                    state.languages.add(lang);
                    state.isLoaded = true;
                    state.isLoading = false;
                    resolve();
                }
                catch (error) {
                    state.isLoading = false;
                    state.promise = undefined;
                    console.log(`Translation loading error: ${error}`);
                    reject(error);
                }
            });
            return state.promise;
        }
        getCurrentLang() {
            return window["mcfUiLang"] || defaultLang;
        }
        get(key, lang) {
            const targetLang = lang || this.currentLang;
            return this.translations[key]?.[targetLang] || `[[${key}]]`;
        }
        async setLanguage(newLang) {
            this.currentLang = newLang;
            const loadPromises = [];
            for (const [moduleKey, state] of this.moduleStates.entries()) {
                if (!state.languages.has(newLang)) {
                    loadPromises.push(this.require(moduleKey, newLang));
                }
            }
            await Promise.all(loadPromises);
        }
        getLoadedModules() {
            return Array.from(this.moduleStates.entries())
                .filter(([_, state]) => state.isLoaded)
                .map(([key, state]) => ({
                module: key,
                languages: Array.from(state.languages)
            }));
        }
        getLoadingState(module) {
            return this.moduleStates.get(module.toLowerCase());
        }
    }
    return new Reo();
}));
//# sourceMappingURL=reoMgrProto.js.map