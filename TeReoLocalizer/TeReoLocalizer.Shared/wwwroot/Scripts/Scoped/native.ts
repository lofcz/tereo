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
        processCommands: async (cmds: any) => {
            for (var cmd of cmds) {
                await nativeProcessor.processCommand(cmd);
            }
        },
        processCommand: async (cmd: any) => {
            if (cmd.type === NativeCommands.SetTextareaHeight) {
                const rows = document.querySelectorAll('tr');

                for (const row of rows) {
                    const textareas = row.querySelectorAll('textarea');

                    if (textareas.length === 0) {
                        continue;
                    }

                    const adjustRowHeight = () => {
                        let maxHeight = 0;
                        
                        textareas.forEach(textarea => {
                            textarea.style.height = 'auto';
                            const height = textarea.scrollHeight + 2; // small addition to compensate scrollbars
                            if (height > maxHeight) {
                                maxHeight = height;
                            }
                        });
                        
                        textareas.forEach(textarea => {
                            textarea.style.height = `${maxHeight}px`;
                        });
                    };

                    textareas.forEach(textarea => {
                        textarea.addEventListener('input', adjustRowHeight);
                    });
                    
                    adjustRowHeight();

                    if (window["scheduler"] && window["scheduler"].yield) {
                        await window["scheduler"].yield();
                    }
                }
            }
        }
    };
    
    window["nativeProcessor"] = nativeProcessor;
    
    if (pars.settings && pars.settings["renderMode"] === 2) {
        await nativeProcessor.processCommand({
            type: NativeCommands.SetTextareaHeight
        })
    }
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