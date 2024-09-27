export async function Init(pars = {
    id: "",
    net: {},
    decl: {},
    langs: {}
}) {
    try {
        console.log(pars);
        await mcf.requireCssAsync("tabulator");
        await mcf.requireCssAsync("tabulatorCustom");
        await mcf.requireLibAsync("tabulator");
        var tabledata = [];
        var currentExpandedRow = null;
        var decl = pars.decl;
        var langs = pars.langs;
        var langsData = langs.langs;
        var parsedLangs = [];
        for (const [key, value] of Object.entries(langsData)) {
            parsedLangs.push(key);
        }
        for (const [key, value] of Object.entries(decl.keys)) {
            tabledata.push(solveKey(key));
        }
        window["tabledata"] = tabledata;
        function solveKey(key) {
            var obj = {
                key: key
            };
            for (var lang of parsedLangs) {
                obj[lang] = langsData[lang].data[key] ?? "";
            }
            return obj;
        }
        var columns = [
            {
                title: "Akce",
                formatter: function (cell, formatterParams, onRendered) {
                    return `
                            <button class="btn btn-secondary btn-sm editBtn">Upravit</button> 
                            <button class="btn btn-secondary btn-sm deleteBtn">Odstranit</button>
                           `;
                },
                cellClick: function (e, cell) {
                    if (e.target.classList.contains('editBtn')) {
                        console.log('Upravit řádek:', cell.getRow().getData());
                    }
                    if (e.target.classList.contains('deleteBtn')) {
                        console.log('Odstranit řádek:', cell.getRow().getData());
                    }
                },
                width: 160,
                headerSort: false
            },
            {
                title: "Klíč",
                field: "key",
                sorter: "string",
                width: 200,
                editor: false
            }
        ];
        function calculateLanguageColumnWidth(fixedWidth, languageCount) {
            var totalWidth = window.innerWidth;
            var remainingWidth = totalWidth - fixedWidth;
            return Math.floor(remainingWidth / languageCount);
        }
        function createColumnFormatter(field) {
            return function (cell, formatterParams, onRendered) {
                var value = cell.getValue();
                var cellElement = cell.getElement();
                cellElement.classList.toggle('expanded', cell.getRow().getData()._expanded);
                var textElement = document.createElement('div');
                textElement.textContent = value;
                textElement.style.whiteSpace = cell.getRow().getData()._expanded ? 'normal' : 'nowrap';
                textElement.style.overflow = cell.getRow().getData()._expanded ? 'visible' : 'hidden';
                textElement.style.textOverflow = cell.getRow().getData()._expanded ? 'clip' : 'ellipsis';
                return textElement;
            };
        }
        function collapseCurrentExpandedRow() {
            if (currentExpandedRow) {
                currentExpandedRow.getData()._expanded = false;
                currentExpandedRow.getElement().classList.remove('expanded-row');
                currentExpandedRow.normalizeHeight();
                currentExpandedRow.reformat();
                currentExpandedRow = null;
            }
        }
        function expandRow(row) {
            row.getData()._expanded = true;
            row.getElement().classList.add('expanded-row');
            row.normalizeHeight();
            row.reformat();
            currentExpandedRow = row;
        }
        var customEditor = function (cell, onRendered, success, cancel, editorParams) {
            var row = cell.getRow();
            var editor = document.createElement("textarea");
            editor.style.width = "100%";
            editor.style.boxSizing = "border-box";
            editor.value = cell.getValue();
            onRendered(function () {
                editor.focus();
                editor.style.height = "100%";
            });
            function successFunc() {
                success(editor.value);
            }
            editor.addEventListener("blur", successFunc);
            editor.addEventListener("keydown", function (e) {
                if (e.keyCode == 13) {
                    successFunc();
                }
                if (e.keyCode == 27) {
                    cancel();
                }
            });
            return editor;
        };
        var fixedWidth = 460;
        var languageColumnWidth = calculateLanguageColumnWidth(fixedWidth, parsedLangs.length);
        for (var lang of parsedLangs) {
            columns.push({
                title: lang,
                field: lang,
                editor: customEditor,
                width: languageColumnWidth,
                formatter: createColumnFormatter(lang)
            });
        }
        var el = document.getElementById(pars.id);
        el.style.height = "800px";
        var table = new window["Tabulator"](`#${pars.id}`, {
            data: tabledata,
            columns: columns,
            renderHorizontal: "virtual"
        });
        table.on("rowClick", function (e, row) {
            if (currentExpandedRow === row) {
            }
            else {
            }
        });
        window.addEventListener('resize', function () {
            languageColumnWidth = calculateLanguageColumnWidth(fixedWidth, parsedLangs.length);
            columns.forEach(function (column, index) {
                if (index >= 2) {
                    column.width = languageColumnWidth;
                }
            });
            table.setColumns(columns);
        });
    }
    catch (e) {
        console.log(e);
    }
}
export function Destroy(pars = {
    id: ""
}) {
}
//# sourceMappingURL=tabulator.js.map