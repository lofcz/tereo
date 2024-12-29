enum TextareaTypes {
    Native,
    RTE,
    CodeEditor,
    NativeLargeText
}

enum ClientInputEvents
{
    Input,
    Change,
    Blur
}

enum ClientTransferProtocols
{
    Unknown,
    Plaintext,
    Patch
}

export function SyncValue(options = {
    id: "",
    value: "",
    silent: false
}) {

    if (!options.value) {
        options.value = "";
    }

    window[`textareaApi_${options.id}`].lastVal = options.value;

    if (window[`textareaApi_${options.id}`]) {
        window[`textareaApi_${options.id}`].ignoreEvents = true;
        var bm = window[`textareaApi_${options.id}`].storeBookmark();
        window[`textareaApi_${options.id}`].setValue(options.value);
        window[`textareaApi_${options.id}`].restoreBookmark(bm);
        window[`textareaApi_${options.id}`].ignoreEvents = false;
    }
}


export async function SetValueStreaming(pars =  {
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

export function Init(pars = {
    id: "",
    net:  {},
    sendOnEnter: false,
    autogrow: true,
    hasButton: false,
    defaultContent: "",
    type: TextareaTypes.Native
}) {
    var el = document.getElementById(pars.id) as HTMLTextAreaElement;

    if (el) {
        var c = new AbortController();

        // only present for textarea with large text support
        if (pars.defaultContent && pars.defaultContent.length) {
            el.value = pars.defaultContent;
        }

        if (pars.sendOnEnter) {
            el.addEventListener("keypress", (e) => {
                applyStyles();
                if (e.keyCode === 13 && !e.shiftKey) {
                    e.preventDefault();
                    pars.net["invokeMethodAsync"]('AttemptSubmitJs');
                    return false;
                }
            }, { signal: c.signal });
        }

        el.addEventListener('paste', (e) => {
            const pastedText = e.clipboardData?.getData('text') || '';
            const val = el.value.slice(0, el.selectionStart) + pastedText + el.value.slice(el.selectionEnd);

            if (!window[`textareaApi_${pars.id}`].ignoreEvents) {
                window[`textareaApi_${pars.id}`].onchange(val, false, ClientInputEvents.Input);
            }
        }, { signal: c.signal });

        el.addEventListener('input', (e) => {
            var val = el.value;

            if (!window[`textareaApi_${pars.id}`].ignoreEvents) {
                window[`textareaApi_${pars.id}`].onchange(val, false, ClientInputEvents.Input);
            }
        }, { signal: c.signal });

        el.addEventListener('change', (e) => {
            var val = el.value;

            if (!window[`textareaApi_${pars.id}`].ignoreEvents) {
                window[`textareaApi_${pars.id}`].onchange(val, true, ClientInputEvents.Change);
            }
        }, { signal: c.signal });

        el.addEventListener('blur', (e) => {
            var val = el.value;

            if (!window[`textareaApi_${pars.id}`].ignoreEvents) {
                window[`textareaApi_${pars.id}`].onchange(val, true, ClientInputEvents.Blur);
            }
        }, { signal: c.signal });


        if (pars.hasButton) {
            var buttonEl = document.getElementById(`${pars.id}_button_icon`);

            if (buttonEl) {
                el.addEventListener("focus", (e) => {
                    applyStyles();
                }, { signal: c.signal });

                el.addEventListener("focusout", (e) => {
                    applyStyles(true);
                }, { signal: c.signal });

                el.addEventListener("keyup", (e) => {
                    applyStyles();
                }, { signal: c.signal });
            }
        }

        function applyStyles(focusOut = false) {

            if (!buttonEl) {
                return;
            }

            var empty = el["value"].trim().length <= 0;

            if (empty) {
                if (focusOut) {
                    buttonEl.style.fill = "#6c6c7b";
                }
                else {
                    buttonEl.style.fill = "#565662";
                }
            }
            else {
                buttonEl.style.fill = "#f0ab2a";
            }
        }

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
            setValue: (val : string) => {
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
            getValue: () : string => {

                if (!window[`textareaApi_${pars.id}`].el) {
                    return "";
                }

                return window[`textareaApi_${pars.id}`].el.value;
            },
            el: el,
            options: pars,
            abortController: c,
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
            storeDiff: (oldText, newText, callback = (patchObj) => {}) => {
                window[`textareaApi_${pars.id}`].msgCounter++;
                var msgIndex = window[`textareaApi_${pars.id}`].msgCounter;

                mcf.requireLib("diffpatch_full", () => {

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
                });
            },
            restoreBookmark: (bookmark : {
                start: 0,
                end: 0
            }) => {
                window[`textareaApi_${pars.id}`].el.selectionStart = bookmark.start;
                window[`textareaApi_${pars.id}`].el.selectionEnd = bookmark.end;
            },
            storeBookmark: () => {
                return {
                    start: window[`textareaApi_${pars.id}`].el.selectionStart,
                    end: window[`textareaApi_${pars.id}`].el.selectionEnd
                }
            },
            setStreaming: (streaming : boolean) => {
                if (window[`textareaApi_${pars.id}`].isStreaming !== streaming) {
                    window[`textareaApi_${pars.id}`].isStreaming = streaming;

                    if (streaming) {
                        window[`textareaApi_${pars.id}`].options.net["invokeMethodAsync"]('SetStreaming');
                    }
                }
            },
            onchange: (val : string, sendIfStreaming = false, eventName = ClientInputEvents.Input) => {

                if (window[`textareaApi_${pars.id}`].lastVal === val) { //  && (eventName !== ClientInputEvents.Blur && eventName !== ClientInputEvents.Change)
                    return
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
        }

        if (pars.autogrow) {
            mcf.requireLib("autogrow", () => {
                window["autosize"](el);
            });
        }

    }
}

export function GetValueStreaming(pars =  {
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

    if (encodedTextValue.byteLength >= 10_000_000) {
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