enum NativeCommands
{
    Unknown,
    SetTextareaHeight
}

enum RenderModes
{
    Unknown,
    Input,
    Textarea
}


export async function Init(pars = {
    id: "",
    net: {},
    settings: {}
}) {
    
    /*await mcf.requireCssAsync("columns");
    await mcf.requireLibAsync("columns");

    new window["validide_resizableTableColumns"].ResizableTableColumns(document.getElementById(`table_${pars.id}`), {
        resizeFromBody: false
    });*/

    const nativeProcessor = {
        abortController: new AbortController(),
        processCommands: async (cmds: any) => {
            for (var cmd of cmds) {
                await nativeProcessor.processCommand(cmd);
            }
        },
        processCommand: async (cmd: any) => {
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
                                height = textarea.scrollHeight - (
                                    parseFloat(computed.paddingTop) +
                                    parseFloat(computed.paddingBottom)
                                );
                            } else {
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
            })
        },
        updateTableHeight: () =>  {
            const table = document.querySelector('.tableSticky') as HTMLTableElement;
            
            if (!table) {
                return;
            }
            
            const topMenuBar = document.querySelector('.topMenuBar') as HTMLElement;
            const bottomMenuBar = document.querySelector('.bottomMenuBar') as HTMLElement;

            const tableTop = table.getBoundingClientRect().top;
            const windowHeight = window.innerHeight;
            const offset = 0;
            
            const topMenuStyle = topMenuBar ? window.getComputedStyle(topMenuBar) : null;
            const bottomMenuStyle = bottomMenuBar ? window.getComputedStyle(bottomMenuBar) : null;
            
            const topMenuBarHeight = topMenuBar ? (
                topMenuBar.offsetHeight +
                parseFloat(topMenuStyle?.marginTop || '0') +
                parseFloat(topMenuStyle?.marginBottom || '0')
            ) : 0;

            const bottomMenuBarHeight = bottomMenuBar ? (
                bottomMenuBar.offsetHeight +
                parseFloat(bottomMenuStyle?.marginTop || '0') +
                parseFloat(bottomMenuStyle?.marginBottom || '0')
            ) : 0;

            const calculatedHeight = windowHeight - tableTop - offset - topMenuBarHeight - bottomMenuBarHeight;
            (table.parentNode as HTMLElement).style.maxHeight = `${calculatedHeight}px`;
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
        })
    }, { signal: nativeProcessor.abortController.signal });
    
    
}

export async function ProcessCommands(pars = {
    id: "",
    commands: []
}) {
    window["nativeProcessor"]?.processCommands(pars.commands).then(x => {});
}

export function Destroy(pars = {
    id: ""
}) {

}