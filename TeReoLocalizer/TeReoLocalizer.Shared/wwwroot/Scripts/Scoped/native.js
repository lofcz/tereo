var NativeCommands;
(function (NativeCommands) {
    NativeCommands[NativeCommands["Unknown"] = 0] = "Unknown";
    NativeCommands[NativeCommands["SetTextareaHeight"] = 1] = "SetTextareaHeight";
})(NativeCommands || (NativeCommands = {}));
var RenderModes;
(function (RenderModes) {
    RenderModes[RenderModes["Unknown"] = 0] = "Unknown";
    RenderModes[RenderModes["Input"] = 1] = "Input";
    RenderModes[RenderModes["Textarea"] = 2] = "Textarea";
})(RenderModes || (RenderModes = {}));
export async function Init(pars = {
    id: "",
    net: {},
    settings: {}
}) {
    const nativeProcessor = {
        abortController: new AbortController(),
        processCommands: async (cmds) => {
            for (var cmd of cmds) {
                await nativeProcessor.processCommand(cmd);
            }
        },
        processCommand: async (cmd) => {
            if (cmd.type === NativeCommands.SetTextareaHeight) {
                const rows = document.querySelectorAll('tr');
                function cacheScrollTops(el) {
                    const arr = [];
                    while (el && el.parentNode && el.parentNode instanceof Element) {
                        if (el.parentNode.scrollTop) {
                            arr.push([el.parentNode, el.parentNode.scrollTop]);
                        }
                        el = el.parentNode;
                    }
                    return () => arr.forEach(([node, scrollTop]) => {
                        node.style.scrollBehavior = 'auto';
                        node.scrollTop = scrollTop;
                        node.style.scrollBehavior = null;
                    });
                }
                for (const row of rows) {
                    const textareas = row.querySelectorAll('textarea');
                    if (textareas.length === 0) {
                        continue;
                    }
                    const adjustRowHeight = () => {
                        let maxHeight = 0;
                        textareas.forEach(textarea => {
                            const computed = window.getComputedStyle(textarea);
                            const restoreScrollTops = cacheScrollTops(textarea);
                            textarea.style.height = '';
                            let height;
                            if (computed.boxSizing === 'content-box') {
                                height = textarea.scrollHeight - (parseFloat(computed.paddingTop) +
                                    parseFloat(computed.paddingBottom));
                            }
                            else {
                                height = textarea.scrollHeight +
                                    parseFloat(computed.borderTopWidth) +
                                    parseFloat(computed.borderBottomWidth);
                            }
                            if (computed.maxHeight !== 'none' && height > parseFloat(computed.maxHeight)) {
                                height = parseFloat(computed.maxHeight);
                            }
                            if (height > maxHeight) {
                                maxHeight = height;
                            }
                            restoreScrollTops();
                        });
                        textareas.forEach(textarea => {
                            textarea.style.overflow = 'hidden';
                            textarea.style.height = `${maxHeight}px`;
                        });
                    };
                    textareas.forEach(textarea => {
                        textarea.addEventListener('input', adjustRowHeight);
                        textarea.style.overflowX = 'hidden';
                        textarea.style.wordWrap = 'break-word';
                    });
                    adjustRowHeight();
                    if (window["scheduler"] && window["scheduler"].yield) {
                        await window["scheduler"].yield();
                    }
                }
            }
        },
        updateTextareas: async () => {
            await nativeProcessor.processCommand({
                type: NativeCommands.SetTextareaHeight
            });
        },
        updateTableHeight: () => {
            const table = document.querySelector('.tableSticky');
            if (!table) {
                return;
            }
            const topMenuBar = document.querySelector('.topMenuBar');
            const bottomMenuBar = document.querySelector('.bottomMenuBar');
            const contentArticle = document.querySelector('article.content');
            const tableTop = table.getBoundingClientRect().top;
            const windowHeight = window.innerHeight;
            const offset = 20;
            const topMenuStyle = topMenuBar ? window.getComputedStyle(topMenuBar) : null;
            const bottomMenuStyle = bottomMenuBar ? window.getComputedStyle(bottomMenuBar) : null;
            const contentStyle = contentArticle ? window.getComputedStyle(contentArticle) : null;
            const topMenuBarHeight = topMenuBar ? (topMenuBar.offsetHeight +
                parseFloat(topMenuStyle?.marginTop || '0') +
                parseFloat(topMenuStyle?.marginBottom || '0')) : 0;
            const bottomMenuBarHeight = bottomMenuBar ? (bottomMenuBar.offsetHeight +
                parseFloat(bottomMenuStyle?.marginTop || '0') +
                parseFloat(bottomMenuStyle?.marginBottom || '0')) : 0;
            const contentPadding = contentArticle ? (parseFloat(contentStyle?.paddingTop || '0') +
                parseFloat(contentStyle?.paddingBottom || '0')) : 0;
            const calculatedHeight = windowHeight - offset - topMenuBarHeight - bottomMenuBarHeight - contentPadding;
            table.parentNode.style.maxHeight = `${calculatedHeight}px`;
        },
        setupResizeObserver: () => {
            const resizeObserver = new ResizeObserver(entries => {
                for (let entry of entries) {
                    nativeProcessor.updateTableHeight();
                }
            });
            resizeObserver.observe(document.documentElement);
            const topMenuBar = document.querySelector('.topMenuBar');
            if (topMenuBar) {
                resizeObserver.observe(topMenuBar);
            }
            const bottomMenuBar = document.querySelector('.bottomMenuBar');
            if (bottomMenuBar) {
                resizeObserver.observe(bottomMenuBar);
            }
            nativeProcessor.updateTableHeight();
        }
    };
    window["nativeProcessor"] = nativeProcessor;
    nativeProcessor.setupResizeObserver();
    if (pars.settings && pars.settings["renderMode"] === 2) {
        await nativeProcessor.updateTextareas();
    }
    window.addEventListener("resize", () => {
        nativeProcessor.processCommand({
            type: NativeCommands.SetTextareaHeight
        });
    }, { signal: nativeProcessor.abortController.signal });
}
export async function ProcessCommands(pars = {
    id: "",
    commands: []
}) {
    window["nativeProcessor"]?.processCommands(pars.commands).then(x => { });
}
export function Destroy(pars = {
    id: ""
}) {
}
//# sourceMappingURL=native.js.map