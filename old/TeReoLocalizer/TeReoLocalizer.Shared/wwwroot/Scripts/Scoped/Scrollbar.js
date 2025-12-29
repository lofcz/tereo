export function Init(options = {
    id: "elementId"
}) {
    mcf.requireCss("simplebar", () => {
        mcf.requireLib("simplebar", () => {
            var el = document.getElementById(options.id);
            if (!el) {
                return;
            }
            el.style.overflow = "auto";
            var sb = new window["SimpleBar"](document.getElementById(options.id));
            var sbObj = {
                sb: sb,
                sbCanLoad: true,
                sbLoading: false
            };
            var scrollEl = sbObj.sb.getScrollElement();
            if (scrollEl) {
                scrollEl.addEventListener('scroll', (e) => {
                    if (sbObj.sbCanLoad && !sbObj.sbLoading) {
                    }
                });
            }
        });
    });
}
//# sourceMappingURL=Scrollbar.js.map