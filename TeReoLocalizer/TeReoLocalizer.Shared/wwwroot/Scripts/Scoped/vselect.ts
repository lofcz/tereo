enum OptionModes {
    Single = 0,
    Groups = 1
}

enum SelectDesignTypes
{
    Unknown,
    Default,
    InlineDescription
}

var fullOptions = {
    id: "",
    options: [],
    multiple: false,
    placeholder: "",
    net: {},
    announce: "",
    optionsMode: OptionModes.Single,
    enableClear: true,
    optionDescription: "",
    extraSearchIndicies: [],
    firstRender: true,
    destroyOld: false,
    enableSearch: true,
    modalRef: null,
    smartSearch: true,
    allowNewOption: false,
    defaultCustomOption: null,
    newOptionTitle: "",
    design: SelectDesignTypes.Default,
    searchPlaceholder: ""
};

export function SetPlaceholder(pars = {
    id: "",
    placeholder: ""
}) {
    if (window[`vselectApi_${pars.id}`] && window[`vselectApi_${pars.id}`].element) {
        window[`vselectApi_${pars.id}`].element.placeholder = pars.placeholder;

        if (document.getElementById(`${pars.id}`)) {

            var placeholderEl = document.querySelector(`#${pars.id} div[data-intent='placeholder']`);

            placeholderEl.innerHTML = pars.placeholder;

            if (placeholderEl) {
                setTimeout(() => {
                    placeholderEl.innerHTML = pars.placeholder;
                }, 10);
            }
        }
    }
}

export function SetData(pars = fullOptions) {

    if (!window[`vselectApi_${pars.id}`] || !window[`vselectApi_${pars.id}`].element) {
        mcf.registerCall(SetData, pars, `vselectApi_${pars.id}_ready`);

        console.log(pars.id);

        return;
    }

    if (window[`vselectApi_${pars.id}`] && window[`vselectApi_${pars.id}`].element) {
        var data = PrepareData(pars);

        window[`vselectApi_${pars.id}`].lastValue = null;
        window[`vselectApi_${pars.id}`].element.placeholder = pars.placeholder;
        window[`vselectApi_${pars.id}`].element.labelRenderer = data.labelRenderer;

        // @ts-ignore
        document.querySelector(`#${pars.id}`).setOptions(data.finalPars);

        if (data.selectedOptions.length > 0) {
            // @ts-ignore
            document.querySelector(`#${pars.id}`).setValue(data.selectedOptions);
        }
    }
}

export function Clear(pars = {
    id: "",
    net: {},
}) {
    if (window[`vselectApi_${pars.id}`] && document.getElementById(pars.id)) {
        window[`vselectApi_${pars.id}`].lastValue = null;
        document.getElementById(pars.id)["reset"]();
    }
}

export function SetValue(pars = {
    id: "",
    net: {},
    value: null,
}) {
    if (window[`vselectApi_${pars.id}`] && document.getElementById(pars.id)) {
        window[`vselectApi_${pars.id}`].lastValue = null;
        document.getElementById(pars.id)["setValue"](pars.value);
    }
}

export function SetCustomValue(pars = {
    id: "",
    value: null,
}) {
    if (window[`vselectApi_${pars.id}`] && document.getElementById(pars.id)) {
        window[`vselectApi_${pars.id}`].setValue(pars.value);
    }
}

export function Destroy(pars = {
    id: ""
}) {
    if (window[`vselectApi_${pars.id}`]) {
        window[`vselectApi_${pars.id}`].dispose(pars.id);
        // @ts-ignore
        document.querySelector(`#${pars.id}`).destroy();
        window[`vselectApi_${pars.id}`].element.destroy();
        window[`vselectApi_${pars.id}`] = undefined;
    }
}

function PrepareData(pars = fullOptions) {
    var selectedOptions = [];
    var finalPars = [];
    // @ts-ignore
    var replaceAccent = (x : string) => x === null || x === undefined ? "" : x.toString().normalize("NFD").replace(/[\u0300-\u036f]/g, "");

    if (pars.optionsMode === OptionModes.Single) {
        selectedOptions = pars.options.filter(x => x.selected).map(x => x.value);
    }
    else {
        for (var i = 0; i < pars.options.length; i++) {
            var grp = pars.options[i];

            if (grp.options) {
                var grpSelected = grp.options.filter(x => x.selected).map(x => x.value);
                selectedOptions.push(...grpSelected);
            } else if (grp.value) {
                if (grp.selected) {
                    selectedOptions.push(grp.value);
                }
            }
        }
    }

    var hasDescription = false;

    if (!pars.options) {
        pars.options = [];
    }

    pars.options.forEach(x => {

        var data = {};

        // @ts-ignore
        for (let [key, value] of Object.entries(x)) {
            if (key === "value" || key === "name" || key === "selected" || key === "options") {
                continue;
            }

            data[key] = value;

            if (typeof value === 'string') {
                data[`${key}Normalized`] = replaceAccent(value);
            }
        }

        var preFinal = {
            value: x.value,
            name: x.name,
            selected: x.selected,
            customData: data,
            valueNormalized: replaceAccent(x.value),
            nameNormalized: replaceAccent(x.name)
        };

        if (x.description && x.description.length > 0) {
            preFinal["description"] = x.description;
            hasDescription = true;
        }

        if (x.options && x.options.length > 0) {

            for (var i = 0; i < x.options.length; i++) {
                x.options[i].valueNormalized = replaceAccent(x.options[i].value);
                x.options[i].nameNormalized = replaceAccent(x.options[i].name);
            }

            preFinal["options"] = x.options;
        }

        if (!preFinal.customData) {
            preFinal.customData = {};
        }

        preFinal.customData["groupNestLevel"] = 0;

        finalPars.push(preFinal);
    });

    var counter = 0;

    // nest index
    for (var i = 0; i < finalPars.length; i++) {
        counter++;
        var fp = finalPars[i];
        fp.customData["groupNestLevel"] = 0;
        fp.customData["indexCounter"] = counter;

        if (fp.options) {
            for (var j = 0; j < fp.options.length; j++) {
                solveNestIndex(fp.options[j], fp);
            }
        }
    }

    function solveNestIndex(fp, parent) {
        counter++;
        if (!fp.customData) {
            fp.customData = {};
        }

        fp.customData.indexCounter = counter;

        if (!parent.customData.groupNestLevel) {
            fp.customData.groupNestLevel = 1;
        }
        else {
            fp.customData.groupNestLevel = parent.customData.groupNestLevel + 1;
        }

        if (fp.options) {
            for (var i = 0; i < fp.options.length; i++) {
                solveNestIndex(fp.options[i], fp);
            }
        }
    }

    var labelRenderer = null, selectedLabelRenderer = null;

    if (pars.options && (pars.options.some(x => x.description) || pars.options.some(x => x.image) || (pars.optionDescription && pars.optionDescription.length > 0))) {

        labelRenderer = (data) => {
            var customHtml = "";

            if (data.customData && data.customData.image) {

                var w = 24, h = 24;

                if (data.customData.imageWidth) {
                    w = data.customData.imageWidth;
                }

                if (data.customData.imageHeight) {
                    h = data.customData.imageHeight;
                }

                customHtml += `
                                <div style="width: ${w}px;height: ${h}px;margin-top:auto;margin-bottom:auto; -webkit-transform: translate3d(0, 0, 0); flex-shrink: 0; -webkit-backface-visibility: hidden;background: url('${data.customData.image}'); background-size: cover; margin-right: 8px; background-repeat: no-repeat;"></div>
                            `;
            }

            if (pars.design === SelectDesignTypes.InlineDescription) {

                customHtml = `
                    <div onmouseover="window['vselectApi_${pars.id}'].renderTooltip(this)" data-tooltip-initialized="0" id="${mcf.iiid()}" data-toolip="${data.customData.description.replaceAll("\"", "'")}" style="border-radius: 50%;width: 20px;height: 20px;margin-top:auto;margin-bottom:auto; -webkit-transform: translate3d(0, 0, 0); flex-shrink: 0; -webkit-backface-visibility: hidden;background: url('Images/Svg/info2.svg'); background-size: cover; margin-right: 8px; background-repeat: no-repeat;"></div>
                `

                return `${customHtml}<div style="margin-bottom: auto; margin-top:auto;">${data.label}</div>`;
            }

            var descHtml = "";

            if (data.customData.description && data.customData.description.length > 0) {
                descHtml = `
                                <div title="${data.customData.description.replaceAll("\"", "'")}" class="vscomp-option-description">${data.customData.description}</div>
                            `;
            }

            return `${customHtml}<div style="margin-bottom: auto; margin-top:auto;">${data.label}${(pars.optionDescription && pars.optionDescription.length > 0 ? `<div>${data.customData[pars.optionDescription]}</div>` : "")}</div>${descHtml}`;
        }
    }

    return {
        labelRenderer: labelRenderer,
        selectedLabelRenderer: labelRenderer,
        hasDescription: hasDescription,
        finalPars: finalPars,
        selectedOptions: selectedOptions
    }
}

export function Init(pars = fullOptions) {
    var vselect = null;
    var lastValue = null;
    var fuse = null;
    var filteredOptions = null;
    var attempts = 0;
    var maxAttempts = 10;
    var initOk = false;
    var neverTrigger = false;
    // @ts-ignore
    var replaceAccent = (x : string) => x === null || x === undefined ? "" : x.toString().normalize("NFD").replace(/[\u0300-\u036f]/g, "");

    mcf.requireLib("virtual-select", () => {
        mcf.requireCss("virtual-select", () => {

            if (!pars.options) {
                pars.options = [];
                //return;
            }

            var selectData = PrepareData(pars);

            function tryInit() {
                if (!document.getElementById(pars.id) && attempts < maxAttempts) {
                    attempts++;
                    setTimeout(() => {
                        tryInit();
                    }, 10);
                }
                else {
                    initVselect();
                }
            }

            tryInit();

            function initVselect() {

                if (!initOk) {
                    initOk = true;
                }
                else {
                    return;
                }

                if (window[`vselectApi_${pars.id}`] && pars.destroyOld) {
                    var el = document.getElementById(`${pars.id}`);
                    // @ts-ignore
                    if (el && el.destroy) {
                        // @ts-ignore
                        //el.destroy();   
                    }
                }

                // console.log(selectData.finalPars);

                var locale = {
                    cs: {
                        search: "Hledat..",
                        noOptionsText: "Žádné odpovídající možnosti",
                        noSearchResultsText: "Žádné výsledky",
                        allOptionsSelectedText: "Všechny možnosti"
                    },
                    pl: {
                        search: "Szukaj..",
                        noOptionsText: "Brak pasujących opcji",
                        noSearchResultsText: "Brak wyników",
                        allOptionsSelectedText: "Wszystkie opcje"
                    },
                    en: {
                        search: "Search..",
                        noOptionsText: "No matching options",
                        noSearchResultsText: "No results",
                        allOptionsSelectedText: "All options"
                    }
                }

                var uiLang = window["mcfUiLang"]?.toLowerCase() || "cs";

                var vselectConfig = {
                    instanceId: pars.id,
                    ele: `#${pars.id}`,
                    options: selectData.finalPars,
                    multiple: pars.multiple,
                    placeholder: pars.placeholder,
                    hideClearButton: true,
                    disableSelectAll: true,
                    enableDeselectAll: true,
                    enableClearButton: pars.enableClear,
                    labelKey: "name",
                    noOfDisplayValues: 500,
                    searchPlaceholderText: pars.searchPlaceholder?.length > 0 ? pars.searchPlaceholder : locale[uiLang].search,
                    noOptionsText: locale[uiLang].noOptionsText,
                    noSearchResultsText: locale[uiLang].noSearchResultsText,
                    allOptionsSelectedText: locale[uiLang].allOptionsSelectedText,
                    selectedValue: selectData.selectedOptions,
                    optionsCount: 8,
                    disableVirtual: pars.design === SelectDesignTypes.InlineDescription,
                    alwaysShowSelectedOptionsLabel: true,
                    searchDelay: 0,
                    searchIndex: pars.smartSearch,
                    searchIndexValue: null,
                    search: pars.enableSearch,
                    searchGroup: true,
                    eventHandler: new AbortController(),
                    searchNormalize: false,
                    silentInitialValueSet: true,
                    allowNewOption: pars.allowNewOption,
                    newValueTransform: (val) => {

                        if (!val) {
                            return val;
                        }

                        return val.charAt(0).toUpperCase() + val.slice(1);
                    },
                    newOptionTitle: pars.newOptionTitle,
                    zIndex: 3,
                    /*dropboxWrapper: "#testel",
                    zIndex: 2000000000,
                    updatePositionThrottle: 0,*/
                    getSearchIndex: () => {
                        return fuse;
                    },
                    labelRenderer: selectData.labelRenderer,
                    selectedLabelRenderer: selectData.selectedLabelRenderer,
                    hasDescription: selectData.hasDescription,
                    afterFirstRender: () => {
                        if (document.getElementById(`${pars.id}_container`)) {
                            document.getElementById(`${pars.id}_container`).style.display = "block";
                        }
                    },
                };

                if (pars.modalRef && pars.modalRef.length > 0) {
                    vselectConfig["dropboxWrapper"] = "body"; //`#${pars.modalRef}`;
                    vselectConfig["updatePositionThrottle"] = 0;
                    vselectConfig["zIndex"] = 2000000000;
                }

                if (!window[`vselectApi_${pars.id}`]) {

                    var dropdownCloseHandler = () => {
                        if (!window[`vselectApi_${pars.id}`]) {
                            return;
                        }

                        window[`vselectApi_${pars.id}`].disposeAllTooltips();
                    };

                    var changeEvtHandler = () => {

                        if (!window[`vselectApi_${pars.id}`]) {
                            return;
                        }

                        if (window[`vselectApi_${pars.id}`].lastValue === null) {
                            window[`vselectApi_${pars.id}`].lastValue = document.getElementById(`${pars.id}`)["value"];
                            return;
                        }

                        if (document.getElementById(`${pars.id}`)["value"] === window[`vselectApi_${pars.id}`].lastValue) {
                            return;
                        }

                        window[`vselectApi_${pars.id}`].lastValue = document.getElementById(`${pars.id}`)["value"];

                        try {
                            if (window[`vselectApi_${pars.id}`] && window[`vselectApi_${pars.id}`].net) {
                                window[`vselectApi_${pars.id}`].broadcastChange();
                            }
                        }
                        catch (e) {
                            console.log(`error: selhal change event vselectu ${pars.id}; ${e}`);
                        }
                    };

                    vselect = window["VirtualSelect"].init(vselectConfig);

                    window[`vselectApi_${pars.id}`] = {
                        abortController: vselectConfig.eventHandler,
                        disposeIds: [pars.id],
                        dispose: (id : "") => {
                            var ref = window[id];
                            if (ref) {

                                if (ref.disposeAllTooltips) {
                                    ref.disposeAllTooltips();
                                }

                                if (ref.abortController) {
                                    ref.abortController.abort();
                                }

                                if (ref.element) {

                                    if (ref.element.$ele) {
                                        ref.element.$ele.destroy();
                                    }

                                    ref.element = undefined;
                                }

                                window[id] = undefined;
                                delete window[id];
                            }
                            else {
                                console.log(id);
                            }
                        },
                        net: pars.net,
                        lastValue: null,
                        neverTrigger: false,
                        options: selectData.finalPars,
                        element: vselect,
                        activeFilters: [],
                        enableNewOption: pars.allowNewOption,
                        ownedTooltips: [],
                        disposeAllTooltips: () => {
                            mcf.disposeAllTooltips();
                        },
                        renderTooltip: (el) => {
                            if (!el) {
                                return;
                            }

                            var initialized = el.getAttribute("data-tooltip-initialized");

                            if (initialized !== "0") {
                                return;
                            }

                            // el.setAttribute("data-tooltip-initialized", 1);

                            var tooltip = el.getAttribute("data-toolip");
                            var tooltipInst = mcf.tooltip(el.id, tooltip);

                            if (tooltipInst) {
                                window[`vselectApi_${pars.id}`].ownedTooltips.push(tooltipInst);
                            }
                        },
                        setValue: (value, triggerChange: false) => {
                            selectData.selectedOptions = [];
                            selectData.finalPars.forEach(x => {
                                if (Array.isArray(value)) {
                                    x.selected = value.includes(x.value);

                                    if (x.selected) {
                                        selectData.selectedOptions.push(x.value);
                                    }
                                }
                                else {
                                    x.selected = value === x.value;

                                    if (x.selected) {
                                        selectData.selectedOptions.push(x.value);
                                    }
                                }
                            });

                            //vselect.setValue(value, false);
                            document.getElementById(pars.id)["setValue"](value);

                            if (triggerChange) {
                                window[`vselectApi_${pars.id}`].broadcastChange();
                            }
                        },
                        broadcastChange: () => {
                            var val = document.getElementById(`${pars.id}`)["value"];

                            if (window[`vselectApi_${pars.id}`].enableNewOption && val !== '' && !window[`vselectApi_${pars.id}`].options.find(x => x.valueNormalized === val)) {

                                window[`vselectApi_${pars.id}`].options.forEach(x => x.selected = false);
                                window[`vselectApi_${pars.id}`].options.push({
                                    value: val,
                                    valueNormalized: val,
                                    selected: true,
                                    name: val,
                                    nameNormalized: val
                                });

                                window[`vselectApi_${pars.id}`].net["invokeMethodAsync"]('AddOptionJs', document.getElementById(`${pars.id}`)["value"]);
                            }
                            else {
                                window[`vselectApi_${pars.id}`].net["invokeMethodAsync"]('UpdateValue', document.getElementById(`${pars.id}`)["value"]);
                            }
                        },
                        resetFilters: () => {
                            window[`vselectApi_${pars.id}`].activeFilters = [];
                            //vselect.setOptions(selectData.finalPars, false);
                            //vselect.render();

                            window[`vselectApi_${pars.id}`].lastValue = null;
                            // @ts-ignore
                            document.querySelector(`#${pars.id}`).setOptions(selectData.finalPars, false);
                        },
                        resetFilter: (filterName) => {

                            console.log("----------------- cisteni");
                            console.log(filterName);
                            console.log(window[`vselectApi_${pars.id}`].activeFilters);

                            window[`vselectApi_${pars.id}`].activeFilters = window[`vselectApi_${pars.id}`].activeFilters.filter(x => x.field !== filterName);

                            console.log(window[`vselectApi_${pars.id}`].activeFilters);
                            window[`vselectApi_${pars.id}`].applyActiveFilters();
                            console.log(window[`vselectApi_${pars.id}`].activeFilters);
                        },
                        applyActiveFilters: (generateIndex = true) => {
                            var selfRef = window[`vselectApi_${pars.id}`];
                            var finalParsCopy = selectData.finalPars.map((x) => x);

                            for (var i = 0; i < selfRef.activeFilters.length; i++) {
                                var filterPtr = selfRef.activeFilters[i];

                                if (Array.isArray(filterPtr.value)) {
                                    finalParsCopy = finalParsCopy.filter(x => x.customData && filterPtr.value.includes(x.customData[filterPtr.jsProperty]))
                                }
                                else {
                                    finalParsCopy = finalParsCopy.filter(x => x.customData && x.customData[filterPtr.jsProperty] === filterPtr.value)
                                }
                            }

                            filteredOptions = finalParsCopy;
                            //vselect.setOptions(filteredOptions, false);
                            //vselect.render();
                            window[`vselectApi_${pars.id}`].lastValue = null;
                            // @ts-ignore
                            document.querySelector(`#${pars.id}`).setOptions(filteredOptions, false);
                            updateIndex();
                        },
                        clear: () => {
                            selectData.finalPars.forEach(x => x.selected = false);
                            selectData.selectedOptions = [];
                            vselect.lastValue = null;
                            vselect.selectedValues = [];
                            vselect.options.forEach(x => x.isSelected = false);
                            vselect.initialSelectedValue = [];
                            window[`vselectApi_${pars.id}`].lastValue = null;
                            // @ts-ignore
                            document.querySelector(`#${pars.id}`).reset();
                        },
                        applyFilter: (filter = {
                            field: "",
                            jsProperty: "",
                            value: null
                        }, generateIndex = true) => {

                            console.log("filter vselect");
                            console.log(filter);

                            var selfRef = window[`vselectApi_${pars.id}`];

                            if (!selfRef.activeFilters.some(x => x.field === filter.field)) {
                                selfRef.activeFilters.push(filter);
                            }
                            else {
                                var src = selfRef.activeFilters.find(x => x.field === filter.field);
                                src.value = filter.value;
                            }

                            selfRef.applyActiveFilters();
                        }
                    };


                    window[`vselectApi_${pars.id}`].lastValue = "auto";
                    var inputEl = document.getElementById(pars.id) as HTMLInputElement;

                    if (inputEl) {
                        inputEl.addEventListener("change", (e) => {
                            changeEvtHandler();
                        }, { signal: vselectConfig.eventHandler.signal });

                        inputEl.addEventListener("afterClose", (e) => {
                            dropdownCloseHandler();
                        }, { signal: vselectConfig.eventHandler.signal });

                        inputEl.addEventListener("onOptionsScroll", (e) => {
                            dropdownCloseHandler();
                        }, { signal: vselectConfig.eventHandler.signal });
                    }

                    if (pars.defaultCustomOption) {
                        window[`vselectApi_${pars.id}`].setValue(pars.defaultCustomOption);
                    }
                }
                else {
                    window[`vselectApi_${pars.id}`].options = selectData.finalPars;
                }

                mcf.checkForCall(`vselectApi_${pars.id}_ready`);
                mcf.registerDispose(window[`vselectApi_${pars.id}`], pars.id);

                if (pars.announce && pars.announce.length > 0) {
                    try {
                        if (window[`vselectApi_${pars.id}`] && window[`vselectApi_${pars.id}`].net) {
                            window[`vselectApi_${pars.id}`].net["invokeMethodAsync"]('AnnounceReady');
                        }
                    }
                    catch (e) {
                    }
                }
            }

            function updateIndex() {
                if (pars.enableSearch && pars.smartSearch) {
                    mcf.requireLib("fuse", () => {

                        var fuseKeys = ['nameNormalized', 'valueNormalized', 'options.nameNormalized', 'options.valueNormalized'];

                        if (pars.extraSearchIndicies && pars.extraSearchIndicies.length > 0) {
                            pars.extraSearchIndicies.forEach((x) => {
                                fuseKeys.push(`customData.${x}Normalized`);
                            });
                        }

                        fuse = new window["Fuse"](filteredOptions || [], {
                            keys: fuseKeys,
                            threshold: 0.2
                        });
                    });
                }
            }

            updateIndex();
        })
    });
}