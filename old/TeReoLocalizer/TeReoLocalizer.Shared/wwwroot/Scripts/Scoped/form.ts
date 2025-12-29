
export function Init(pars = {
    id: "",
    net: {}
}) {
    window[`formApi_${pars.id}`] = {
        init: () => {
            var el = document.getElementById(pars.id);

            if (!el) {
                return;
            }

            if (false) {
                window[`formApi_${pars.id}`].observer.observe(document.getElementById(pars.id), {
                    childList: true,
                    subtree: true,
                    characterDataOldValue: false
                });
            }
        },
        observer: new MutationObserver((data) => {
            // @ts-ignore
            for (var entry of data) {
                for (var i = 0; i < entry.addedNodes.length; i++) {
                    var el = entry.addedNodes[i];

                    if (false && el.nodeName === "input") {
                        console.log("přidána node input");
                        console.log(el);
                    }
                }
            }
        }),
        dispose: () => {
            window[`formApi_${pars.id}`].observer.disconnect();
            window[`formApi_${pars.id}`] = undefined;
            delete window[`formApi_${pars.id}`];
        }
    }

    window[`formApi_${pars.id}`].init();
}

export function Destroy(pars = {
    id: ""
}) {
    if (window[`formApi_${pars.id}`]) {
        window[`formApi_${pars.id}`].dispose();
    }
}