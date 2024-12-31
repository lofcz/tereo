export function Init(pars = {
    id: "",
    net: {}
}) {
    window[`inputApi_${pars.id}`] = {
        net: pars.net,
        abortController: new AbortController(),
        init: () => {
            var inputEl = document.getElementById(pars.id);
            if (inputEl) {
                inputEl.addEventListener("keydown", (evt) => {
                    if (evt.key === "Enter" || evt.code === "Enter" || evt.keyCode === 13) {
                        window[`inputApi_${pars.id}`].net["invokeMethodAsync"]('SubmitJs');
                    }
                }, { signal: window[`inputApi_${pars.id}`].abortController.signal });
            }
        },
        dispose: () => {
            window[`inputApi_${pars.id}`].abortController.abort();
            window[`inputApi_${pars.id}`] = undefined;
            delete window[`inputApi_${pars.id}`];
        }
    };
    window[`inputApi_${pars.id}`].init();
}
export function Destroy(pars = {
    id: ""
}) {
    if (window[`inputApi_${pars.id}`]) {
        window[`inputApi_${pars.id}`].dispose();
    }
}
//# sourceMappingURL=input.js.map