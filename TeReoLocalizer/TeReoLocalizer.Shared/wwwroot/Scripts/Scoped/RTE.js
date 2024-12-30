var TextareaTypes;
(function (TextareaTypes) {
    TextareaTypes[TextareaTypes["Native"] = 0] = "Native";
    TextareaTypes[TextareaTypes["RTE"] = 1] = "RTE";
    TextareaTypes[TextareaTypes["CodeEditor"] = 2] = "CodeEditor";
    TextareaTypes[TextareaTypes["NativeLargeText"] = 3] = "NativeLargeText";
})(TextareaTypes || (TextareaTypes = {}));
var ClientInputEvents;
(function (ClientInputEvents) {
    ClientInputEvents[ClientInputEvents["Input"] = 0] = "Input";
    ClientInputEvents[ClientInputEvents["Change"] = 1] = "Change";
    ClientInputEvents[ClientInputEvents["Blur"] = 2] = "Blur";
})(ClientInputEvents || (ClientInputEvents = {}));
var ClientTransferProtocols;
(function (ClientTransferProtocols) {
    ClientTransferProtocols[ClientTransferProtocols["Unknown"] = 0] = "Unknown";
    ClientTransferProtocols[ClientTransferProtocols["Plaintext"] = 1] = "Plaintext";
    ClientTransferProtocols[ClientTransferProtocols["Patch"] = 2] = "Patch";
})(ClientTransferProtocols || (ClientTransferProtocols = {}));
export function SyncValue(options = {
    id: "",
    value: "",
    silent: false
}) {
    if (!options.value) {
        options.value = "";
    }
    if (window[`textareaApi_${options.id}`]) {
        window[`textareaApi_${options.id}`].ignoreEvents = true;
        window[`textareaApi_${options.id}`].lastVal = options.value;
        window[`textareaApi_${options.id}`].editor.setContent(options.value);
        window[`textareaApi_${options.id}`].ignoreEvents = false;
    }
}
export async function SetValueStreaming(pars = {
    id: "",
    stream: {},
    silent: false
}) {
    if (!window[`textareaApi_${pars.id}`]) {
        return;
    }
    var bytes = await pars.stream["arrayBuffer"]();
    var utf8Decoder = new TextDecoder();
    var decodedVal = utf8Decoder.decode(bytes);
    SyncValue({
        id: pars.id,
        value: decodedVal,
        silent: pars.silent
    });
    return true;
}
export async function Init(pars = {
    id: "",
    net: {},
    sendOnEnter: false,
    autogrow: true,
    hasButton: false,
    defaultContent: "",
    type: TextareaTypes.Native
}) {
    var el = document.getElementById(pars.id);
    await mcf.requireLibArrAsync(["react", "react-dom", "tiptap-island", "diffpatch_full"]);
    await mcf.requireCssAsync("tiptap-island");
    window[`textareaApi_${pars.id}`] = {
        dispose: () => {
            window[`textareaApi_${pars.id}`].abortController.abort();
            if (window[`textareaApi_${pars.id}`].options.autogrow) {
                mcf.requireLib("autogrow", () => {
                    window["autosize"].destroy(el);
                });
            }
            window[`textareaApi_${pars.id}`] = undefined;
            delete window[`textareaApi_${pars.id}`];
        },
        setValue: (val) => {
            if (!window[`textareaApi_${pars.id}`].el) {
                return "";
            }
            window[`textareaApi_${pars.id}`].el.value = val;
            if (window[`textareaApi_${pars.id}`].options.autogrow) {
                mcf.requireLib("autogrow", () => {
                    window["autosize"].update(el);
                });
            }
        },
        getValue: () => {
            return window[`textareaApi_${pars.id}`].lastVal;
        },
        editor: null,
        el: el,
        options: pars,
        abortController: new AbortController(),
        isStreaming: false,
        streamThreshold: 100,
        ignoreEvents: false,
        differ: null,
        msgCounter: 0,
        diffs: [],
        removeDiff: (diff) => {
            var index = window[`textareaApi_${pars.id}`].diffs.find(x => x.id === diff.id);
            if (index) {
                window[`textareaApi_${pars.id}`].diffs.splice(index, 1);
            }
        },
        storeDiff: (oldText, newText, callback = (patchObj) => { }) => {
            window[`textareaApi_${pars.id}`].msgCounter++;
            var msgIndex = window[`textareaApi_${pars.id}`].msgCounter;
            if (!window[`textareaApi_${pars.id}`].differ) {
                window[`textareaApi_${pars.id}`].differ = new window["diff_match_patch"]();
            }
            var diff = window[`textareaApi_${pars.id}`].differ;
            var patch = diff.patch_make(oldText || "", newText || "");
            var patchText = diff.patch_toText(patch);
            var patchObj = {
                id: msgIndex,
                content: patchText
            };
            window[`textareaApi_${pars.id}`].diffs.push(patchObj);
            callback(patchObj);
        },
        restoreBookmark: (bookmark) => {
            window[`textareaApi_${pars.id}`].el.selectionStart = bookmark.start;
            window[`textareaApi_${pars.id}`].el.selectionEnd = bookmark.end;
        },
        storeBookmark: () => {
            return {
                start: window[`textareaApi_${pars.id}`].el.selectionStart,
                end: window[`textareaApi_${pars.id}`].el.selectionEnd
            };
        },
        setStreaming: (streaming) => {
            if (window[`textareaApi_${pars.id}`].isStreaming !== streaming) {
                window[`textareaApi_${pars.id}`].isStreaming = streaming;
                if (streaming) {
                    window[`textareaApi_${pars.id}`].options.net["invokeMethodAsync"]('SetStreaming');
                }
            }
        },
        onchange: (val, sendIfStreaming = false, eventName = ClientInputEvents.Input) => {
            if (window[`textareaApi_${pars.id}`].lastVal === val) {
                return;
            }
            var storedLastVal = window[`textareaApi_${pars.id}`].lastVal || "";
            window[`textareaApi_${pars.id}`].lastVal = val;
            if (val.length >= window[`textareaApi_${pars.id}`].streamThreshold) {
                window[`textareaApi_${pars.id}`].storeDiff(storedLastVal, val, (patchObj) => {
                    if (patchObj.content.length < window[`textareaApi_${pars.id}`].streamThreshold) {
                        window[`textareaApi_${pars.id}`].setStreaming(false);
                        window[`textareaApi_${pars.id}`].options.net["invokeMethodAsync"]('UpdateValJs', patchObj.content, eventName, ClientTransferProtocols.Patch);
                        window[`textareaApi_${pars.id}`].removeDiff(patchObj);
                    }
                    else {
                        window[`textareaApi_${pars.id}`].setStreaming(true);
                        window[`textareaApi_${pars.id}`].options.net["invokeMethodAsync"]('GetValueJs', eventName, patchObj.id);
                    }
                });
            }
            else {
                window[`textareaApi_${pars.id}`].setStreaming(false);
                window[`textareaApi_${pars.id}`].options.net["invokeMethodAsync"]('UpdateValJs', val, eventName, ClientTransferProtocols.Plaintext);
            }
        }
    };
    let editor = window["TipTapIsland"].create(pars.id, {
        content: pars.defaultContent,
        onUpdate: (html) => {
            if (!window[`textareaApi_${pars.id}`].ignoreEvents) {
                window[`textareaApi_${pars.id}`].onchange(html, false, ClientInputEvents.Input);
            }
        }
    });
    window[`textareaApi_${pars.id}`].editor = editor;
}
export function GetValueStreaming(pars = {
    id: "",
    index: 0
}) {
    if (!window[`textareaApi_${pars.id}`]) {
        return;
    }
    if (pars.index && pars.index > 0) {
        var storedDiff = window[`textareaApi_${pars.id}`].diffs.find(x => x.id === pars.index);
        if (storedDiff) {
            window[`textareaApi_${pars.id}`].removeDiff(storedDiff);
        }
    }
    var utf8Encoder = new TextEncoder();
    var encodedTextValue = utf8Encoder.encode(window[`textareaApi_${pars.id}`].getValue());
    if (encodedTextValue.byteLength >= 10000000) {
        mcf.toast("error", "Délka textu přesáhla podporované maximum 10M znaků.");
        return null;
    }
    return encodedTextValue;
}
export function Destroy(pars = {
    id: ""
}) {
    if (window[`textareaApi_${pars.id}`]) {
        window[`textareaApi_${pars.id}`].dispose();
    }
}
//# sourceMappingURL=RTE.js.map