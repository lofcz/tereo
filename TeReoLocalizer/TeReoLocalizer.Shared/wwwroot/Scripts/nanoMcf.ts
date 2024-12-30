/* Autor: Matěj "lof" Štágl
 * Verze: 2.0
 */
enum ClientInfoResults {
    Crash,
    Priv,
    Public
}

enum ClientTransferProtocols
{
    Unknown,
    Plaintext,
    Patch
}

interface CompressedMessage {
    data: string,
    protocol: ClientTransferProtocols
}

var moduleCommonFunctions = {
    version: 2.0,
    switchFullscreen: false,
    loadedLibs: [],
    loadingLibs: [],
    loadedCss: [],
    tempBinds: [],
    binds: [],
    modalBinds: [],
    loadedAudioFiles: [],
    windowHistoryXhr: null,
    isInLcTab: true,
    commonUniversalModalOpen: false,
    prefDarkTheme: false,
    prefFriendsPanel: false,
    universalModalMode: "url",
    templates: [],
    evSignalReadyFn: [],
    signalIsReady: false,
    registeredFunctions: [],
    registeredScripts: [],
    loadingPromises: [],
    minTemplates: true,
    /**
     * @type {{name: string, callback: function, args: object, argsPacked: bool}[]}
     */
    signalsToDispatch: [],
    tempTitleOn: false,
    tempTitleTimer: null,
    tempTitleMode: false,
    uniqueEvents: [],
    tooltips: [],
    observedNodes: [],
    observerInitialized: false,
    callsQue: [],
    disposeQue: [],
    debugObservers: [],
    eventAborter: new AbortController(),
    lastBodyScroll: {x: 0, y: 0},
    differ: null,
    ini: function(name) {
        //mcf.toast("info", "cau cau");
    },
    requireLibCompleted: function(libName) {
        for (var i = 0; i < moduleCommonFunctions["requireLibCompletedCallbacks_" + libName].length; i++) {
            moduleCommonFunctions["requireLibCompletedCallbacks_" + libName][i]();
        }

        moduleCommonFunctions["requireLibCompletedCallbacks_" + libName] = [];
    },
    clientHash: () => {
        return new Promise((resolve) => {
            mcf.requireLib("game", () => {
                try {
                    window["FingerprintJS"].load().then(x => x.get()).then(x => {
                        resolve(x["visitorId"]);
                    })
                }
                catch (e) {
                    resolve("");
                    return
                }
            });
        });
    },
    getUserAgent: () => {
        return navigator.userAgent;
    },
    clientStorage: () => {
        var hash = localStorage.getItem("clientHash");

        if (!hash || hash.length <= 0) {
            hash = mcf.iiid();
            localStorage.setItem("clientHash", hash);
            return hash;
        }

        return hash;
    },
    clientInfo: () => {
        return new Promise((resolve) => {
            mcf.requireLib("info", () => { 
                try {
                    window["getInfo"]().then((result) => {
                        if (result && result.isPrivate) {
                            resolve(ClientInfoResults.Priv);
                            return;
                        }
    
                        resolve(ClientInfoResults.Public);
                        return;
                    });
            } catch (e) {
                resolve(ClientInfoResults.Crash);
                return
            }
            });
        });
    },
    callFnIfDefined : (fnName : string, ...params) => {
        if (fnName === "") {
            return;
        }

        if (typeof(window[fnName]) === "function") {
            // @ts-ignore
            window[fnName](...params);
        }
    },
    displayInfo: () => {
        return {
            width: Math.round(window.visualViewport.width),
            height: Math.round(window.visualViewport.height)
        }
    },
    eval: (code) => {
        try {
            var f = new Function(`return (() => { ${code} })();`);
            f();
        }
        catch (e) {

        }
    },
    /**
     * Načte zadané CSS styly. V jednu chvíli je načítáno tolik stylů, kolik je volných slotů v download frontě.
     * @param cssNames názvy stylů, buď jako CSV string "css1,css2", nebo pole ["css1", "css2"], soubory bez .css koncovky
     * @param callback funkce, která bude zavolána po načtení všech stylů
     */
    requireCssArr: (cssNames, callback = () => {}) => {
        if (!Array.isArray(cssNames)) {
            cssNames = cssNames.trim().replaceAll(" ", "").split(",");
        }

        var promiseGuid = mcf.iiid();
        var callbacks = [];
        for (var i = 0; i < cssNames.length; i++) {
            callbacks[i] = false;
        }

        window[promiseGuid + "_data"] = callbacks;
        window[promiseGuid + "_fn"] = (index) => {
            window[promiseGuid + "_data"][index] = true;
            if (window[promiseGuid + "_data"].find(x => x === false) === undefined) {
                callback();
            }
        };

        for (var i = 0; i < cssNames.length; i++) {
            let cI = i;
            mcf.requireCss(cssNames[i], () => {
                window[promiseGuid + "_fn"](cI);
            });
        }
    },
    debugCancelAll: () => {
        // @ts-ignore
        for (var observer of mcf.debugObservers) {
            observer.disconnect();
        }
        
        mcf.debugObservers = [];
    },
    /**
     * Přidá breakpoint jakmile dojde ke změně dané vlastnosti zadného elementu
     * @param el
     * @param property
     */
    debugObserveProperty: (el : HTMLElement, property : string) => {
        var observer = new MutationObserver(function(mutations) {
                mutations.forEach(function(mutation) {
                    if (mutation.attributeName == property) {
                        console.log('Debug observe property: ', 'old value', mutation.oldValue, 'new value', (mutation.target as HTMLElement).getAttribute(property), 'mutation', mutation);
                        debugger;
                    }
                });
            }
        );
    
        var config = {
            attributes: true,
            attributeOldValue: true
        }
    
        observer.observe(el, config);
        mcf.debugObservers.push(observer);
        return observer;
    },
    /**
     * Loads an array of libraries in parallel.
     * @param libNames
     * @param callback
     * @param forceLoad
     * @param autoPath
     */
    requireLibArrAsync: async (libNames: string | string[], callback = () => {}, forceLoad = false, autoPath = true) => {
        if (!Array.isArray(libNames)) {
            // @ts-ignore
            libNames = libNames.trim().replaceAll(" ", "").split(",");
        }

        var v = window.localStorage.getItem("appVersion");

        if (window["mcfDebug"]) {
            v = mcf.guid();
        }
        
        const promises = [];

        // @ts-ignore
        for (const libName of libNames) {
            if (moduleCommonFunctions.loadedLibs.includes(libName)) {
                callback();
            } else {
                if (!moduleCommonFunctions.loadingLibs.includes(libName) && !forceLoad) {
                    moduleCommonFunctions.loadingLibs.push(libName);
                    moduleCommonFunctions["requireLibCompletedCallbacks_" + libName] = [callback];

                    let path = '/Scripts/' + libName + `.js?v=${v}`;

                    if (!autoPath) {
                        path = libName;
                    }

                    const scriptPromise = new Promise((resolve, reject) => {
                        const script = document.createElement('script');
                        document.body.appendChild(script);
                        script.onload = resolve;
                        script.onerror = reject;
                        script.async = false;
                        script.src = path;
                    });

                    mcf.loadingPromises[libName] = scriptPromise;
                    
                    promises.push({
                        promise: scriptPromise,
                        callback: () => {
                            moduleCommonFunctions.loadedLibs.push(libName);
                            moduleCommonFunctions.requireLibCompleted(libName);
                        }
                    });
                }
                else if (!forceLoad) {
                    moduleCommonFunctions["requireLibCompletedCallbacks_" + libName].push(callback);

                    if (moduleCommonFunctions.loadingPromises[libName]) {
                        await moduleCommonFunctions.loadingPromises[libName];
                    }
                }
                else {
                    moduleCommonFunctions["requireLibCompletedCallbacks_" + libName] = [callback];
                    let path = '/Scripts/' + libName + `.js?v=${v}`;

                    if (!autoPath) {
                        path = libName;
                    }

                    const scriptPromise = new Promise((resolve, reject) => {
                        const script = document.createElement('script');
                        document.body.appendChild(script);
                        script.onload = resolve;
                        script.onerror = reject;
                        script.async = false;
                        script.src = path;
                    });

                    mcf.loadingPromises[libName] = scriptPromise;

                    promises.push({
                        promise: scriptPromise,
                        callback: () => {
                            moduleCommonFunctions.loadedLibs.push(libName);
                            moduleCommonFunctions.requireLibCompleted(libName);
                        }
                    });
                }
            }   
        }
        
        if (promises.length > 0) {
            const promisesArr = promises.map(x => x.promise);
            // @ts-ignore
            await Promise.allSettled(promisesArr);
            
            // @ts-ignore
            for (const promise of promises) {
                promise.callback();
            }
        }
    },
    /**
     * Načte zadané JS knihovny. V jednu chvíli je načítáno tolik knihoven, kolik je volných slotů v download frontě.
     * @param libNames názvy knihoven, buď jako CSV string "knihovna1,knihovna2", nebo pole ["knihovna1", "knihovna2"], soubory bez .js koncovky
     * @param callback funkce, která bude zavolána po načtení všech knihoven
     */
    requireLibArr: (libNames, callback = () => {}) => {

        if (!Array.isArray(libNames)) {
            libNames = libNames.trim().replaceAll(" ", "").split(",");
        }

        var promiseGuid = mcf.iiid();
        var callbacks = [];
        for (var i = 0; i < libNames.length; i++) {
            callbacks[i] = false;
        }

        window[promiseGuid + "_data"] = callbacks;
        window[promiseGuid + "_fn"] = (index) => {
            window[promiseGuid + "_data"][index] = true;
            if (window[promiseGuid + "_data"].find(x => x === false) === undefined) {
                callback();
            }
        };

        for (var i = 0; i < libNames.length; i++) {
            let cI = i;
            mcf.requireLib(libNames[i], () => {
                window[promiseGuid + "_fn"](cI);
            });
        }
    },
    bodyToggleScroll: (enabled: boolean) => {
        var hasVScroll = window.innerWidth > document.documentElement.clientWidth;
        var cStyle = document.body.style || window.getComputedStyle(document.body, "");
        hasVScroll = hasVScroll || cStyle.overflow == "visible" || cStyle.overflowY == "visible" || (hasVScroll && cStyle.overflow == "auto") || (hasVScroll && cStyle.overflowY == "auto");
        if (!hasVScroll) {
            return;
        }
        if (!enabled) {
            mcf.lastBodyScroll = { x: document.documentElement.scrollLeft || document.body.scrollLeft, y: document.documentElement.scrollTop || document.body.scrollTop };
            document.body.style.top = `-${mcf.lastBodyScroll.y}px`;
            document.body.style.position = "fixed";
            document.body.style.width = "100%";
            document.body.style.overflowY = "scroll";
            return;
        }
        var scrollY = mcf.lastBodyScroll.y;
        var scrollX = mcf.lastBodyScroll.x;
        document.body.style.position = "";
        document.body.style.top = "";
        document.body.style.overflowY = "auto";
        // @ts-ignore
        window.scrollTo({left: scrollX, top: scrollY, behavior: "instant"});
    },
    getErrorsInInterval: (seconds) => {
        return 0; // [todo] fix err.date.getTime(); crashing
        
        var errors = localStorage.getItem("serverErrors");
        var errorsArr = [];
        var inTime = 0;

        if (errors && errors.length > 0) {
            errorsArr = JSON.parse(errors);
        } 
        
        for (var i = errorsArr.length - 1; i >= 0; i--) {
            var err = errorsArr[i];

            console.log(err);
            
            var dif = new Date().getTime() - err.date.getTime();
            
            console.log(dif);
            
            var secs = Math.abs(dif) / 1000;

            mcf.toast("ok", "test" + secs);
            
            if (secs <= seconds) {
                inTime++;
                
                if (inTime >= 10) {
                    break;
                }
            }
            else {
                break;
            }
        }
        
        return inTime;
    },
    getErrors: (remove = false) => {
        var errors = localStorage.getItem("serverErrors");
        var errorsArr = [];

        if (errors && errors.length > 0) {
            errorsArr = JSON.parse(errors);
        }
        
        if (remove) {
            localStorage.setItem("serverErrors", "");   
        }
        
        return errorsArr;
    },
    storeError: (exception) => {
        var errors = localStorage.getItem("serverErrors");
        var errorsArr = [];
        
        if (errors && errors.length > 0) {
            errorsArr = JSON.parse(errors);
        }
        
        errorsArr.push({
            date: new Date(),
            exception: JSON.parse(exception)
        })
        
        localStorage.setItem("serverErrors", JSON.stringify(errorsArr));
        
        var inDelta = mcf.getErrorsInInterval(10);
        
        if (inDelta >= 10) {
            // @ts-ignore
            window.location = "/bugsplash";
        }
    },
    /**
     * Pokud je window['windowKey'] undefined, načte synchronně polyfill, relativně k /Scripts/Polyfills, bez .js
     * @param windowKey
     * @param polyfillLib
     */
    polyfill: (windowKey : string, polyfillLib : string) => {
        
    },
    setAppVersion: (entropyIIID : string) => {
        window.localStorage.setItem("appVersion", entropyIIID);
    },
    refreshStaticAssets: () => {
        window.localStorage.setItem("appVersion", mcf.iiid());
        // @ts-ignore
        location.reload(true);
    },
    log: (data: any) => {
        console.log(data);  
    },
    focus: (id: string): boolean => {
        const el = document.getElementById(id);

        if (el) {
            el.focus();
            
            const getScrollParent = (node) => {
                if (!node || node === document.body) {
                    return document.body;
                }

                const style = getComputedStyle(node);
                const overflowY = style.overflowY;
                const isScrollable = overflowY === 'auto' || overflowY === 'scroll';

                if (isScrollable && node.scrollHeight > node.clientHeight) {
                    return node;
                }

                return getScrollParent(node.parentNode);
            };

            const scrollParent = getScrollParent(el);

            if (scrollParent) {
                const rect = el.getBoundingClientRect();
                const parentRect = scrollParent.getBoundingClientRect();
                const isVisible = (rect.top >= parentRect.top) && (rect.bottom <= parentRect.bottom);

                if (!isVisible) {
                    const scrollTop = rect.top - parentRect.top + scrollParent.scrollTop;
                    scrollParent.scrollTo({
                        top: scrollTop - scrollParent.clientHeight / 2 + rect.height / 2,
                        behavior: 'instant'
                    });
                }
            } else {
                el.scrollIntoView({ behavior: 'instant', block: 'nearest' });
            }

            return true;
        }

        return false;
    },
    select: (id : string) => {
        var el = document.getElementById(id) as HTMLInputElement;

        if (el) {
            el.select();
        }
    },
    copy: (text) => {

        if (window["clipboardData"] && window["clipboardData"].setData) {
            return window["clipboardData"].setData("Text", text);
        } else if (document.queryCommandSupported && document.queryCommandSupported("copy")) {
            var textarea = document.createElement("textarea");
            textarea.textContent = text;
            textarea.style.position = "fixed";
            document.body.appendChild(textarea);
            textarea.select();

            var ok = false;

            try {
                document.execCommand("copy");
                ok = true;
            } catch (ex) {
                ok = false;
            } finally {
                document.body.removeChild(textarea);
            }

            if (ok) {
                return true;
            }

            try {
                navigator.clipboard.writeText(text).then(function() {
                    return true;
                }, function(err) {
                    return false;
                });

                return true;
            }
            catch (e) {
                return false
            }
        }
    },
    requireLibAsync: async function (libName, callback = function () {}, forceLoad = false, autoPath = true) {
        if (Array.isArray(libName) || libName.includes(",")) {
            return await mcf.requireLibArrAsync(libName, callback, forceLoad, autoPath);
        }

        var v = window.localStorage.getItem("appVersion");

        if (window["mcfDebug"]) {
            v = mcf.guid();
        }

        if (moduleCommonFunctions.loadedLibs.includes(libName)) {
            callback();
        } else {
            if (!moduleCommonFunctions.loadingLibs.includes(libName) && !forceLoad) {
                moduleCommonFunctions.loadingLibs.push(libName);
                moduleCommonFunctions["requireLibCompletedCallbacks_" + libName] = [callback];

                var path = '/Scripts/' + libName + `.js?v=${v}`;

                if (!autoPath) {
                    path = libName;
                }

                var scriptPromise = new Promise((resolve, reject) => {
                    const script = document.createElement('script');
                    document.body.appendChild(script);
                    script.onload = resolve;
                    script.onerror = reject;
                    script.async = false;
                    script.src = path;
                });

                mcf.loadingPromises[libName] = scriptPromise;
                await scriptPromise;

                moduleCommonFunctions.loadedLibs.push(libName);
                moduleCommonFunctions.requireLibCompleted(libName);
            }
            else if (!forceLoad) {
                moduleCommonFunctions["requireLibCompletedCallbacks_" + libName].push(callback);

                if (moduleCommonFunctions.loadingPromises[libName]) {
                    await moduleCommonFunctions.loadingPromises[libName];
                }
            }
            else {
                moduleCommonFunctions["requireLibCompletedCallbacks_" + libName] = [callback];
                var path = '/Scripts/' + libName + `.js?v=${v}`;

                if (!autoPath) {
                    path = libName;
                }

                var scriptPromise = new Promise((resolve, reject) => {
                    const script = document.createElement('script');
                    document.body.appendChild(script);
                    script.onload = resolve;
                    script.onerror = reject;
                    script.async = false;
                    script.src = path;
                });

                mcf.loadingPromises[libName] = scriptPromise;
                await scriptPromise;

                moduleCommonFunctions.loadedLibs.push(libName);
                moduleCommonFunctions.requireLibCompleted(libName);
            }
        }
    },
    /**
     * Načte zadanou JS knihovnu, pokud ještě není v paměti
     * @param {string} libName název knihovny, bez .js, relativně k /Scripts/. Může být také pole knihoven jako CSV "knihovna1, knihovna2" nebo pole ["knihovna1", "knihovna2"]
     * @param {function()} callback funkce, která se provede po načtení knihovny
     * @param {boolean} forceLoad zda knihovnu načíst, i když už v paměti je. Užitečné v některých případech, default = false
     * @param {boolean} autoPath pokud false, použije se absolutní cesta místo relativní
     */
    requireLib: function (libName, callback = function () {}, forceLoad = false, autoPath = true) {

        if (Array.isArray(libName) || libName.includes(",")) {
            return mcf.requireLibArr(libName, callback);
        }
        
        var v = window.localStorage.getItem("appVersion");
        var libNameNormalized = libName; // .replace(/[\W_]+/g, "_");
        
        if (window["mcfDebug"]) {
            v = mcf.guid();
        }

        if (moduleCommonFunctions.loadedLibs.includes(libNameNormalized)) {
            callback();
        } else {
            if (!moduleCommonFunctions.loadingLibs.includes(libNameNormalized) && !forceLoad) {
                moduleCommonFunctions.loadingLibs.push(libNameNormalized);
                moduleCommonFunctions["requireLibCompletedCallbacks_" + libNameNormalized] = [callback];

                var path = '/Scripts/' + libName + `.js?v=${v}`;

                if (!autoPath) {
                    path = libName;
                }
                
                var scriptPromise = new Promise((resolve, reject) => {
                    const script = document.createElement('script');
                    document.body.appendChild(script);
                    script.onload = resolve;
                    script.onerror = reject;
                    script.async = true;
                    script.src = path;
                });

                scriptPromise.then(() => {
                    moduleCommonFunctions.loadedLibs.push(libNameNormalized);
                    moduleCommonFunctions.requireLibCompleted(libNameNormalized);
                });

            }
            else if (!forceLoad) {
                moduleCommonFunctions["requireLibCompletedCallbacks_" + libNameNormalized].push(callback);
            }
            else {
                moduleCommonFunctions["requireLibCompletedCallbacks_" + libNameNormalized] = [callback];
                var path = '/Scripts/' + libNameNormalized + `.js?v=${v}`;

                if (!autoPath) {
                    path = libName;
                }

                var scriptPromise = new Promise((resolve, reject) => {
                    const script = document.createElement('script');
                    document.body.appendChild(script);
                    script.onload = resolve;
                    script.onerror = reject;
                    script.async = true;
                    script.src = path;
                });
                
                scriptPromise.then(() => {
                    moduleCommonFunctions.loadedLibs.push(libNameNormalized);
                    moduleCommonFunctions.requireLibCompleted(libNameNormalized);
                });
            }
        }
    },
    openInNewTab: (url, focus = true) => {
        var win = window.open(url, '_blank');

        if (win && focus) {
            win.focus();
        }
    },
    /**
     * Podle délky dat vrací buď původní data, nebo patch, pokud je taková reprezentace úspornější
     * @param {string} dataOld Předešlá data
     * @param {string} dataNew Nová data
     */
    messageCompress: async (dataOld: string, dataNew: string) : Promise<CompressedMessage> => {
        
        if (dataNew.length < 100) {
            return {
                data: dataNew,
                protocol: ClientTransferProtocols.Plaintext
            };
        }
        
        await mcf.requireLibAsync("diffpatch");
        
        if (!mcf.differ) {
            mcf.differ = new window["diff_match_patch"]();
        }

        var diff = mcf.differ;
        var patch = diff.patch_make(dataOld || "", dataNew || "");
        var patchText = diff.patch_toText(patch);
        
        return {
            data: patchText,
            protocol: ClientTransferProtocols.Patch
        }
    },
    fetchText: async (path) => {
        return (await fetch(path)).text();
    },
    /**
     * Načte zadaný CSS stylesheet, pokud ještě není v paměti
     * @param {string} sheetName název css souboru, bez .css, relativně k /Content/. Může být také pole stylů jako CSV "css1, css2" nebo pole ["css1", "css2"]
     * @param {function()} callback funkce, která se provede po načtení csska
     * @param {string} container ID divu, do kterého se styly načtou, bez #
     * @param {string} mode "append|force|replace" (načte, pokud ještě není načteno / vynutí načtení / vynutí načtení a přepíše originál)
     */
    requireCssAsync: async function(sheetName, callback = function() {}, container = "commonCss", mode = "append") {

        if (Array.isArray(sheetName) || sheetName.includes(",")) {
            return mcf.requireCssArr(sheetName, callback);
        }

        var cssPromise;
        
        if (container === "body") {
            if (moduleCommonFunctions.loadedCss.includes(sheetName)) {
                callback();
            } else {
                cssPromise = new Promise((resolve, reject) => {
                    var link = document.createElement('link');
                    link.rel = "stylesheet";
                    link.type = "text/css";
                    link.onload = resolve;
                    link.href = "/Content/" + sheetName + ".css?v=" + window["lcVer"];
                    document.body.appendChild(link);
                    moduleCommonFunctions.loadedCss.push(sheetName);
                });
            }
        }

        if (mode === "force" || mode === "replace") {
            cssPromise = new Promise((resolve, reject) => {
                var link = document.createElement('link');
                link.rel = "stylesheet";
                link.type = "text/css";
                link.onload = resolve;
                link.href = "/Content/" + sheetName + ".css?v=" + window["lcVer"];
                document.getElementById(container).innerHTML = link.outerHTML;
                moduleCommonFunctions.loadedCss.push(sheetName);
            });
        }

        if (moduleCommonFunctions.loadedCss.includes(sheetName)) {
            callback();
        } else {
            cssPromise = new Promise((resolve, reject) => {
                var link = document.createElement('link');
                link.rel = "stylesheet";
                link.type = "text/css";
                link.onload = resolve;
                link.href = "/Content/" + sheetName + ".css?v=" + window["lcVer"];
                document.getElementById(container).appendChild(link);
                moduleCommonFunctions.loadedCss.push(sheetName);
            });
        }

        if (cssPromise) {
            await cssPromise.then(() => {
                callback();
            });   
        }
    },
    /**
     * Načte zadaný CSS stylesheet, pokud ještě není v paměti
     * @param {string} sheetName název css souboru, bez .css, relativně k /Content/. Může být také pole stylů jako CSV "css1, css2" nebo pole ["css1", "css2"]
     * @param {function()} callback funkce, která se provede po načtení csska
     * @param {string} container ID divu, do kterého se styly načtou, bez #
     * @param {string} mode "append|force|replace" (načte, pokud ještě není načteno / vynutí načtení / vynutí načtení a přepíše originál)
     */
    requireCss: function(sheetName, callback = function() {}, container = "commonCss", mode = "append") {

        if (Array.isArray(sheetName) || sheetName.includes(",")) {
            return mcf.requireCssArr(sheetName, callback);
        }

        if (container === "body") {
            if (moduleCommonFunctions.loadedCss.includes(sheetName)) {
                callback();
            } else {
                var link = document.createElement('link');
                link.rel = "stylesheet";
                link.type = "text/css";
                link.onload = () => {callback();};
                link.href = "/Content/" + sheetName + ".css?v=" + window["lcVer"];
                document.body.appendChild(link);
                moduleCommonFunctions.loadedCss.push(sheetName);
            }
        }

        if (mode === "force" || mode === "replace") {
            var link = document.createElement('link');
            link.rel = "stylesheet";
            link.type = "text/css";
            link.onload = () => {callback();};
            link.href = "/Content/" + sheetName + ".css?v=" + window["lcVer"];
            document.getElementById(container).innerHTML = link.outerHTML;
            moduleCommonFunctions.loadedCss.push(sheetName);
            return;
        }

        if (moduleCommonFunctions.loadedCss.includes(sheetName)) {
            callback();
        } else {
            var link = document.createElement('link');
            link.rel = "stylesheet";
            link.type = "text/css";
            link.onload = () => {callback();};
            link.href = "/Content/" + sheetName + ".css?v=" + window["lcVer"];
            document.getElementById(container).appendChild(link);
            moduleCommonFunctions.loadedCss.push(sheetName);
        }
    },
    /**
     * Načte modul, pokud ještě není načtený
     * @param {string} moduleName název modulu, bez cesty, bez Module_ prefixu a bez .js (například CommonFunctions)
     * @param {function()} callback funkce, která se provede po načtení modulu, nebo, pokud už je modul v paměti ihned
     */
    requireOnce: function(moduleName, callback = function() {}) {
        if (window["moduleCore"].isModuleLoaded(moduleName)) {
            callback();
        } else {
            window["moduleCore"].registerModule(moduleName, callback);
        }
    },
    execFn: (name, ...args) => {

        console.log("exec fn called");
        console.log(name);
        
        var a = moduleCommonFunctions.registeredFunctions.filter(x => x.Name === name);
        
        console.log(a);
        
        if (a.length > 0) {
            // @ts-ignore
            return a[0].Fn(...args);
        }

        return 0;
    },
    canSignal: () => {
        return window["signalConnection"] !== undefined && window["signalConnection"].state === "Connected";
    },
    execFnCallback: (name, callback = () => {}, ...args) => {
        var a = moduleCommonFunctions.registeredFunctions.filter(x => x.Name === name);
        if (a.length > 0) {
            // @ts-ignore
            a[0].Fn(...args);
            callback();
        }
    },
    preregisterFn: (name) => {
        if (!moduleCommonFunctions.registeredFunctions.some(x => x.Name === name)) {
            moduleCommonFunctions.registeredFunctions.push({Name: name, Fn: () => {}, Ready: false, ReadyQue: []});
        }
    },
    registerFn: (name, fn) => {
        var a = moduleCommonFunctions.registeredFunctions.filter(x => x.Name === name);
        if (a.length > 0) {
            a[0].Fn = fn;
            a[0].Ready = true;

            if (a[0].ReadyQue.length > 0) {
                for (var i = 0; i < a[0].ReadyQue.length; i++) {
                    a[0].ReadyQue[i]();
                }
            }
        }
    },
    getElementValue: (elId) => {
        let el = document.getElementById(elId) as HTMLInputElement;
        return el?.value;
    },
    /**
     * Dekóduje string
     * @param encodedString enkódovaný string
     * @returns {string} dekódovaný text
     */
    decode: (encodedString) => {
        var txt = document.createElement("textarea");
        txt.innerHTML = encodedString;
        return txt.value;
    },
    hideAllTooltips: () => {
        mcf.requireLib("popper2", () => {
            mcf.requireLib("tippy", () => {
                window["tippy"].hideAll({duration: 0});
            });
        });
    },
    setupObserver: () => {
        mcf.observerInitialized = true;
        
        var observer = new MutationObserver(function(mutations_list) {
            mutations_list.forEach(function(mutation) {
                mutation.removedNodes.forEach(function(removed_node) {

                    //@ts-ignore
                    if (!removed_node.id) {
                        return;
                    }
                    
                    for (var i = 0; i < mcf.observedNodes.length; i++) {
                        var node = mcf.observedNodes[i];

                        //@ts-ignore
                        if (removed_node.id.includes("_container")) {
                            //@ts-ignore
                            mcf.toast("info",removed_node.id);
                        }
                        
                        // @ts-ignore
                        if (removed_node.id === node.nodeId) {
                            node.onDelete(node.data);
                            mcf.observedNodes.splice(i, 1);
                        }   
                    }
                });
            });
        });

        observer.observe(document.getElementById("mcfRoot"), { subtree: true, childList: true });
    },
    watchNode: (nodeId, data = {}, onDelete = (data: {}) => {}) => {
        return;
        if (!mcf.observerInitialized) {
            mcf.setupObserver();
        }
        
        mcf.observedNodes.push({
            nodeId: nodeId,
            data: data,
            onDelete: onDelete
        });
    },
    disposeAllTooltips: () => {
        // @ts-ignore
        for (var matchedTooltip of mcf.tooltips) {
            matchedTooltip.inst.destroy();
            window[`tooltipInst_${matchedTooltip.elId}`] = undefined;
            delete window[`tooltipInst_${matchedTooltip.elId}`];
        }
        
        mcf.tooltips = [];
    },
    tooltipDispose: (elId) => {
        // @ts-ignore
        var matchedTooltip = mcf.tooltips.find(x => x.elId === elId);
    
        if (matchedTooltip) {
            matchedTooltip.inst.destroy();
            window[`tooltipInst_${elId}`] = undefined;
            delete window[`tooltipInst_${elId}`];
            
            var index = mcf.tooltips.indexOf(matchedTooltip);
            mcf.tooltips = mcf.tooltips.splice(index, 1);
            return true;
        }    
        
        return false;
    },
    tooltip: async (elId, text, hideOnClick = false, position = "top", persistent = false) => {
        
        await mcf.requireLibAsync("popper2");
        await mcf.requireLibAsync("tippy");

        var oldTooltips = mcf.tooltips.filter(x => x.elId === elId);

        oldTooltips.forEach(x => {
            if (x && x.inst && x.inst.setContent) {
                x.inst.setContent(text);
            }
        });

        // @ts-ignore
        if (!mcf.tooltips.find(x => x.elId === elId)) {
            window[`tooltipInst_${elId}`] = window["tippy"](document.getElementById(elId),
                {
                    content: text,
                    allowHTML: true,
                    hideOnClick: !hideOnClick,
                    //delay: [100, 200000],
                    placement: position.toLowerCase()
                });

            window[`tooltipInst_${elId}`].show();

            var inst = {
                inst: window[`tooltipInst_${elId}`],
                elId: elId,
                persistent: persistent
            };
            
            mcf.tooltips.push(inst);
            return inst;
        }
    },
    navigateTo: (path = '/') => {
        var win: Window = window;
        // @ts-ignore
        win.location = path;  
    },
    reload: () => {
        location.reload();
    },
    delay: async (ms: number) => {
        await new Promise(x => setTimeout(x, ms));
    },
    execScript: (name, callback = (args = {}) => {}) => {
        var script = document.createElement('script');
        script.async = true;
        script.src = '/scripts/owned/' + name + ".js";
        script.onload = () => {
            fetch('/scripts/owned/' + name + ".js")
                .then(response => response.text())
                .then((response) => {
                    console.log(response);

                    var func = new Function(response);
                    var result = func.call(null, 1, 2);
                    
                    callback(result);
                })
        };

        document.body.appendChild(script);
    },
    requireFn: (name, callback = (fnResult) => {}, argsObj) => {
        window[`fnArgs`] = argsObj;
        if (!moduleCommonFunctions.registeredFunctions.some(x => x.Name === name)) {
            moduleCommonFunctions.preregisterFn(name);
            var scriptPromise = new Promise((resolve, reject) => {
                var script = document.createElement('script');
                script.onerror = reject;
                script.async = true;
                script.src = '/scripts/owned/' + name + ".js";
                script.onload = () => {
                    setTimeout(() => {
                        window[`fnArgs`] = argsObj;
                        console.log("eval:");
                        console.log(script.innerHTML);
                        var result = eval(script.innerHTML);
                        
                        console.log("eval result");
                        console.log(result);
                        
                        var obj = {
                            key: name,
                            script: "",
                            scriptLoaded: false
                        };
                        
                        mcf.registeredScripts.push(obj);

                        fetch('/scripts/owned/' + name + ".js")
                            .then(response => response.text())
                            .then((response) => {
                                obj.script = response;
                                obj.scriptLoaded = true;
                            })
                        
                        callback(result);
                    })
                };

                document.body.appendChild(script);
            });
        }
        else {
            // @ts-ignore
            var fn = moduleCommonFunctions.registeredFunctions.find(x => x.Name === name);
            if (fn !== undefined) {
                if (fn.Ready) {
                    callback({});
                }
                else {
                    fn.ReadyQue.push(callback);
                }
            }
            
            // @ts-ignore
            var script = mcf.registeredScripts.find(x => x.key === name);
            if (script !== undefined) {
                console.log("fn ------------------ backup");
                console.log(script);
                window["fnArgs"] = argsObj;
                eval(script.script);
            }
        }
    },
    download: (path, name) => {
        var link = document.createElement("a");
        link.setAttribute('download', name);
        link.href = path;
        document.body.appendChild(link);
        link.click();
        link.remove();
    },
    progressStart: () => {
        window["NProgress"].start();
    },
    progressEnd: () => {
        window["NProgress"].done();
    },
    fn: (name, ...args) => {
        moduleCommonFunctions.requireFn(name, () => {
            // @ts-ignore
            moduleCommonFunctions.execFn(name, ...args);
        }, args)
    },
    fnCallback: (name, callback = () => {}, ...args) => {
        moduleCommonFunctions.requireFn(name, () => {
            // @ts-ignore
            moduleCommonFunctions.execFnCallback(name, callback, ...args);
        }, args);
    },
    ready: function(callback = function() {}) {
        if (document.readyState !== 'loading'){
            callback();
        } else {
            document.addEventListener('DOMContentLoaded', callback);
        }
    },
    scrollToTop: (smooth: true, id = "") => {
        if (id.length < 1) {
            window.scrollTo({ left: 0, top: 0, behavior: smooth ? "smooth" : "auto" });
        }
        else {
            var el = document.getElementById(id);
            
            if (el) {
                el.scrollIntoView({behavior: smooth ? "smooth" : "auto" });
            }
        }
    },
    callIfDefined: (ident = "", ...pars) => {
        var identArr = ident.split('.');
        var lastObj = window;
        
        for (var i = 0; i < identArr.length; i++) {
            var part = identArr[i];
            
            if (lastObj[part]) {
                if (i === identArr.length - 1) {
                    // @ts-ignore
                    lastObj[part](...pars);
                }
                else {
                    lastObj = lastObj[part];
                }
            }
            else {
                break;
            }
        }
    },
    dispose: (obj = "") => {
        if (window[obj] && window[obj]["dispose"]) {
            window[obj]["dispose"](obj);
        }
    },
    scrollToOffset: (elId = "", offset = 100) =>{
        var element = document.getElementById(elId);
        var headerOffset = offset;
        var elementPosition = element.getBoundingClientRect().top;
        var offsetPosition = elementPosition + window.pageYOffset - headerOffset;

        setTimeout(() => {
            window.scrollTo({
                top: offsetPosition,
                behavior: "smooth"
            });
        }, 50);
    },
    scrollToElement: (elId: string) => {
        
        var el = document.getElementById(elId);
        
        if (!el) {
            return;
        }

        var headerOffset = 45;
        var elementPosition = el.getBoundingClientRect().top;
        var offsetPosition = elementPosition + window.pageYOffset - headerOffset;
        
        window.scrollTo({ left: 0, top: offsetPosition });
    },
    scrollToBottom: () => {
        window.scrollTo({ left: 0, top: document.body.scrollHeight, behavior: "smooth" });
    },
    postAsync: async (url, data) => {
        let hdrs = new Headers();
        hdrs.append('X-Requested-With', 'XMLHttpRequest');
        hdrs.append('LcRequest', 'inlinePost');

        var fetchResult = await fetch(url, {
            body: new URLSearchParams(data),
            headers: hdrs,
            method: "POST"
        });
        
        return await fetchResult.json();
    },
    post: function(url, data, callback = (data) => {}, antiforgery = "", async = true, cType = "") {

        function isJson(str) {
            try {
                JSON.parse(str);
            } catch (e) {
                return false;
            }
            return true;
        }

        let hdrs = new Headers();
        hdrs.append('X-Requested-With', 'XMLHttpRequest');
        hdrs.append('LcRequest', 'inlinePost');

        if (cType !== "") {
            hdrs.append("Content-Type", cType);
        }

        if (antiforgery === "") {
            fetch(url,
                {
                    body: new URLSearchParams(data),
                    headers: hdrs,
                    method: "POST",
                }).then(x => x.text()).then(data => {
                if (isJson(data)) {
                    callback(JSON.parse(data));
                }
                else {
                    mcf.toast("error",`Požadavek na server, adresa ${url} skončil neošetřenou chybou:<br/>${data}<br/>Kontaktuj prosím podporu / nahlas chybu v našem úkolovacím systému (obojí z bočního menu). Děkujeme!`);
                }
            });
        }
        else {

            if (window[antiforgery] === undefined || window[antiforgery] === false) {
                window[antiforgery] = true;

                if (async) {
                    fetch(url,
                        {
                            body: new URLSearchParams(data),
                            headers: hdrs,
                            method: "POST",
                        }).then(x => x.text()).then(data => {
                        if (isJson(data)) {
                            callback(JSON.parse(data));
                        }
                        else {
                            mcf.toast("error",`Požadavek na server, adresa ${url} skončil neošetřenou chybou:<br/>${data}<br/>. Kontaktuj prosím podporu / nahlas chybu v našem úkolovacím systému. Děkujeme!`);
                        }
                        window[antiforgery] = false;
                    });
                }
                else {

                }
            }
        }
    },
    /**
     * Vytvoří POST požadavek pomocí fetch backendu a vrátí JSON
     * @param {string} url Endpoint /kontroler/akce
     * @param {Object<FormData>} formdata Kolekce dat
     * @param {function()} callback Funkce, která se spustí po dokončení požadavku
     * @param {string} antiforgery Token autorizující požadavek
     * @returns {}
     */
    formdata: function(url, formdata, callback = (data) => {}, antiforgery = "") {
        let hdrs = new Headers();
        hdrs.append('X-Requested-With', 'XMLHttpRequest');

        if (antiforgery === "") {
            fetch(url,
                {
                    body: formdata,
                    headers: hdrs,
                    method: "POST"
                }).then(x => x.json()).then(data => {
                callback(data);
            });

            return;
        }

        if (window[antiforgery] === undefined || window[antiforgery] === false) {
            window[antiforgery] = true;
            fetch(url,
                {
                    body: formdata,
                    headers: hdrs,
                    method: "POST"
                }).then(x => x.json()).then(data => {
                callback(data);
                window[antiforgery] = false;
            });
        }
    },
    /**
     * Načte pole modulů, pokud ještě nejsou načteny. Jakmile jsou všechny moduly připravené, spustí se callback
     * @param {Array<string>} modules pole modulů k načtení, např: ["modul1", "modul2"]
     * @param {function()} callback funkce, která se spustí po načtení všech modulů
     */
    requireOnceMultiple: function(modules, callback = function() {}) {

        var toLoad = [];

        modules.forEach(function(item, index) {
            toLoad.push(1);

            moduleCommonFunctions.requireOnce(item, function() {
                toLoad[index] = 0;

                if (toLoad.every(item => item === 0)) {
                    callback();
                }
            });
        });
    },
    /**
     * Vrátí ID klienta pro WebSocket připojení.
     */
    getSocketId: () => {
        var socketId = localStorage.getItem("websocketId");
        // @ts-ignore
        var idValid = socketId && socketId.startsWith("ua_") && socketId.length === 39; // 37 + ua

        if (!idValid) {
            socketId = `ua_${mcf.iiid()}`;
            localStorage.setItem("websocketId", socketId);
        }

        return socketId;
    },
    /**
     * Odstraní všechny aktivní notifikace v pravém horním rohu.
     */
    toastClear: () => {
        mcf.requireLib("toastr", () => {
            window["toastr"].clear();
        });
    },
    /**
     * Vytvoří upozornění v pravém horním rohu, které automaticky zmizí po několika sekundách.
     * @param {string} type "[ok|success]|info|[err|error]"
     * @param {string} text HTML notifikace
     * @param {string} title Nadpis notifikace, pokud prázdné, nezobrazí se
     */
    toast: (type : "info" | "ok" | "error" | "err" | "warning" | "success" = "info", text : string = "notifikace", title : string = null) => {

        if (type === "ok") {
            type = "success";
        }
        else if (type === "err" ) {
            type = "error";
        }

        mcf.requireCss("toastr", () => {
            mcf.requireLib("toastr", () => {
                if (typeof window["toastr"][type] === "function") {
                    if (title) {
                        window["toastr"][type](text, title);
                    }
                    else {
                        window["toastr"][type](text);
                    }
                }
                else {
                    window["toastr"]["error"]("mcf.toast -> první parametr očekávan jako info|ok|success|warning|error|err. Poskytnuto: " + type);
                }
            });
        });
    },
    /**
     * Vrátí náhodnou možnost ze vstupního pole
     * @param {Array<any>} options vstupní možnosti
     */
    choose: function(options) {
        var randomNumber = Math.floor(Math.random() * options.length);
        return options[randomNumber];
    },
    /**
     * Vytvoří náhodný GUID podle RFC4122 v4 (shodné s c# guid)
     * @returns {string} RFC4122 v4 GUID
     */
    guid: function() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    },
    iiid: function() {
        return 'xxxxxxxx_xxxx_4xxx_yxxx_xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    },
    /**
     * Spustí celostránkové načítání, které překryje obsah. Užitečné pro akce, které můžou trvat delší dobu.
     * @param {string} textToDisplay html, které se má zobrazit při načítaní
     * @param {number} fadeInTimeMs čas v milisekundách, po který bude trvat fadeIn
     */
    fullscreenLoadStart: function (textToDisplay, fadeInTimeMs = 300) {
        // @ts-ignore
        $("#globalOverlayText").html(textToDisplay);
        // @ts-ignore
        $("#overlay").fadeIn(fadeInTimeMs);
    },
    /**
     * Ukončí celostránkové načítání, tuto funkci je potřeba spustit po použití {@link fullscreenLoadStart}, jinak nebude moci uživatel interagovat se stránkou
     * @param {number} fadeOutTimeMs čas v milisekundách, po který bude trvat fadeOut
     * @param {function()} callback funkce, která se spustí po dokončení fadeOut efektu
     */
    fullscreenLoadEnd: function (fadeOutTimeMs = 300, callback = function () {}) {
        // @ts-ignore
        $("#overlay").fadeOut(fadeOutTimeMs, callback);
    },
    evalScripts: (html) => {
        let container = document.createElement('div');
        container.innerHTML = html;

        let scripts = container.querySelectorAll('script');
        let nodes = container.childNodes;

        for (let i = 0; i < scripts.length; i++) {
            let script = document.createElement('script');
            script.type = scripts[i].type || 'text/javascript';

            if (scripts[i].hasAttribute('src')) {
                script.src = scripts[i].src;
            }

            script.innerHTML = scripts[i].innerHTML;
            document.head.appendChild(script);
            document.head.removeChild(script);
        }
    },
    clearCalls: () => {
      mcf.callsQue = [];  
    },
    registerDispose: (obj: {}, id: string) => {
        if (!obj || !obj["dispose"]) {
            return;
        }
        
        mcf.disposeQue.push({
            obj: obj,
            id: id
        });
    },
    checkForCall: (key : string) => {
       // @ts-ignore
        var call = mcf.callsQue.find(x => x.key === key);
       
       if (call) {
            var index = mcf.callsQue.indexOf(call);
            
            if (index >= 0) {
                mcf.callsQue.splice(index, 1);
            }
            
            call["fn"](call.data);
       }
    },
    registerCall: (fn, data = {}, key : string) => {

        // @ts-ignore
        var call = mcf.callsQue.find(x => x.key === key);

        if (call) {
            var index = mcf.callsQue.indexOf(call);

            if (index >= 0) {
                mcf.callsQue.splice(index, 1);
            }
        }
        
        data["mcfRegisteredCall"] = true;
        
        mcf.callsQue.push({
            fn: fn,
            data: data,
            key: key
        });
    },
    disposePersistentTooltips: () => {
        mcf.tooltips.forEach(x => {
            if (x && x.inst && x.inst.destroy) {
                if (!x.persistent) {
                    return;
                }

                x.inst.destroy();
            }

            window[`tooltipInst_${x.elId}`] = undefined;
            delete window[`tooltipInst_${x.elId}`];
        });
    },
    disposeSafeEvents: () => {
        
        mcf.eventAborter.abort();
        mcf.uniqueEvents.forEach(x => {
          removeEventListener(x.name, x.fn);  
        });
        mcf.uniqueEvents = [];

        mcf.tooltips.forEach(x => {
            if (x && x.inst && x.inst.destroy) {
                if (x.persistent) {
                    return;
                }

                x.inst.destroy();
            }
            
            window[`tooltipInst_${x.elId}`] = undefined;
            delete window[`tooltipInst_${x.elId}`];
        });
        
        mcf.tooltips = [];
        mcf.eventAborter = new AbortController();
        mcf.clearCalls();
        
        // @ts-ignore
        for (var x of mcf.disposeQue) {
            if (x && x.obj && x.obj["dispose"]) {
                x.obj["dispose"](x.id);
            }
        }
        
        mcf.disposeQue = [];
        
        return true;
    },
    toggleCheckbox: (id) => {
        document.getElementById(id)["checked"] = !document.getElementById(id)["checked"];  
    },
    addSafeEventListener: (elementId, eventName, eventFn = (eventArgs) => {}) => {

        // @ts-ignore
        if (mcf.uniqueEvents.find(x => x.name === `mcfUniqueEvent_${elementId}_${eventName}`)) {
            var eventsToRemove = mcf.uniqueEvents.filter(x => x.name === `mcfUniqueEvent_${elementId}_${eventName}`);
            
            for (var i = 0; i < eventsToRemove.length; i++) {
                document.getElementById(elementId).removeEventListener(eventName, eventsToRemove[i].fn);
            }

            mcf.uniqueEvents = mcf.uniqueEvents.filter(x => x.name !== `mcfUniqueEvent_${elementId}_${eventName}`)
        }
        
        // @ts-ignore
        if (!mcf.uniqueEvents.find(x => x.name === `mcfUniqueEvent_${elementId}_${eventName}`)) {
            
            if (!document.getElementById(elementId)) {
                return false;
            }
            
            try {
                var finalFn = (e) => {
                    eventFn(e);
                };
               
                // @ts-ignore
                document.getElementById(elementId).addEventListener(eventName, finalFn, { signal: mcf.eventAborter.signal });

                mcf.uniqueEvents.push({
                    name: `mcfUniqueEvent_${elementId}_${eventName}`,
                    fn: finalFn
                })
                
                return true;
            }
            catch (e) {
                return false; 
            }
        }
        
        return false;
    },
    insertHTML: function(html, dest, append= false, prepend = false){
        
        if (typeof dest === 'string') {
            dest = document.getElementById(dest);
        }
        
        if (!append && !prepend) {
            if (dest !== null && dest !== undefined) {
                dest.innerHTML = '';
            }
        }

        if (!(dest !== null && dest !== undefined)) {
            return;
        }

        var mode = "";
        if (dest.nodeName === "TBODY") {
            mode = "table";
        }

        let container = document.createElement(mode === "" ? "div" : "tbody");
        container.innerHTML = html;

        let scripts = container.querySelectorAll('script');
        let nodes = container.childNodes;

        for (let i = 0; i < nodes.length; i++) {
            if (!prepend) {
                dest.appendChild(nodes[i].cloneNode(true));
            } else {
                dest.insertBefore(nodes[i].cloneNode(true), dest.firstChild);
            }
        }

        for (let i = 0; i < scripts.length; i++) {
            let script = document.createElement('script');
            script.type = scripts[i].type || 'text/javascript';

            if (scripts[i].hasAttribute('src')) {
                script.src = scripts[i].src;
            }

            script.innerHTML = scripts[i].innerHTML;
            document.head.appendChild(script);
            document.head.removeChild(script);
        }
        return true;
    },
    setCookie: (name, value, days = 365) => {
        var expires = "";
        if (days) {
            var date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = "; expires=" + date.toUTCString();
        }
        document.cookie = name + "=" + (value || "") + expires + "; path=/;SameSite=Lax";
    },
    getCookie: (name) => {
        var nameEQ = name + "=";
        var ca = document.cookie.split(';');
        for (var i = 0; i < ca.length; i++) {
            var c = ca[i];
            while (c.charAt(0) == ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
        }
        return null;
    },
    eraseCookie: (name) => {
        document.cookie = name + '=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;SameSite=Lax';
    }
};
var mcf = moduleCommonFunctions;