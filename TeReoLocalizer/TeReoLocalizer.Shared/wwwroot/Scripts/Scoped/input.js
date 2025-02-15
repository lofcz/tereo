var InputTypes;
(function (InputTypes) {
    InputTypes[InputTypes["Text"] = 0] = "Text";
    InputTypes[InputTypes["Number"] = 1] = "Number";
    InputTypes[InputTypes["Email"] = 2] = "Email";
    InputTypes[InputTypes["Password"] = 3] = "Password";
    InputTypes[InputTypes["Search"] = 4] = "Search";
    InputTypes[InputTypes["Date"] = 5] = "Date";
    InputTypes[InputTypes["File"] = 6] = "File";
    InputTypes[InputTypes["SmartPassword"] = 7] = "SmartPassword";
})(InputTypes || (InputTypes = {}));
export function Init(pars = {
    id: "",
    net: {},
    type: InputTypes.Text
}) {
    window[`inputApi_${pars.id}`] = {
        el: document.getElementById(pars.id),
        net: pars.net,
        abortController: new AbortController(),
        init: () => {
            const inputEl = window[`inputApi_${pars.id}`].el;
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
    if (pars.type === InputTypes.SmartPassword) {
        if (window[`inputApi_${pars.id}`].el) {
            window[`inputApi_${pars.id}`].el.addEventListener("focus", () => {
                window[`inputApi_${pars.id}`].el.type = "text";
            }, { signal: window[`inputApi_${pars.id}`].abortController.signal });
            window[`inputApi_${pars.id}`].el.addEventListener("blur", () => {
                window[`inputApi_${pars.id}`].el.type = "password";
            }, { signal: window[`inputApi_${pars.id}`].abortController.signal });
        }
    }
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