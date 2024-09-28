var NativeCommands;
(function (NativeCommands) {
    NativeCommands[NativeCommands["Unknown"] = 0] = "Unknown";
    NativeCommands[NativeCommands["SetTextareaHeight"] = 1] = "SetTextareaHeight";
})(NativeCommands || (NativeCommands = {}));
export async function Init(pars = {
    id: "",
    net: {}
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