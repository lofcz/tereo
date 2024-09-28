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
        processCommands: async (cmds) => {
            for (var cmd of cmds) {
                await nativeProcessor.processCommand(cmd);
            }
        },
        processCommand: async (cmd) => {
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
                            const height = textarea.scrollHeight + 2;
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
        });
    }
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