using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Code;
using TeReoLocalizer.Shared.Code.Commands;
using TeReoLocalizer.Shared.Code.Services;
using TeReoLocalizer.Shared.Components.Pages.Owned;

namespace TeReoLocalizer.Shared.Components.Pages;

public partial class Localize
{
    async Task HandleKeySearchValueChange(ChangeEventArgs? args)
    {
        if (args?.Value is string str)
        {
            if (str is "null")
            {
                Settings.KeySearchLang = null;
            }
            else if (Enum.TryParse(str, true, out Languages parsed))
            {
                Settings.KeySearchLang = parsed;
            }
        }
        
        await UpdateSearchResults();
        await SaveUserSettings();
    }
    
    void HandleKeyFocus(Languages language, string key, string value)
    {
        if (JumpingInHistory)
        {
            return;
        }
        
        LangsData.Langs[language].FocusData[key] = value;
    }

    async Task HandleKeyUpdate(Languages language, string key, string value)
    {
        if (JumpingInHistory)
        {
            return;
        }
        
        await Execute(new CmdSetKeyValue(language, key, value));
    }
    
    async Task AddKey()
    {
        if (JumpingInHistory)
        {
            return;
        }
        
        if (NewKey.IsNullOrWhiteSpace())
        {
            return;
        }

        DataOrException<bool> result = await Execute(new CmdAddKey(NewKey));

        if (result.Exception is not null)
        {
            await Js.Toast(ToastTypes.Error, result.Exception.Message);
        }
    }
    
    void SetKeyValue(Languages language, string key, string value)
    {
        LangData lang = LangsData.Langs[language];
        lang.Data[key] = value;
    }

    public async Task SetKey(Languages language, string key, string value)
    {
        SetKeyValue(language, key, value);
        
        if (Settings.TranslationMode is TranslationModes.Default)
        {
            await SaveLanguage(language);
        }
        else
        {
            foreach (KeyValuePair<Languages, LangData> x in LangsData.Langs.Where(x => x.Key != language))
            {
                x.Value.Data.TryRemove(key, out _);
            }

            await SaveLanguages();
        }
    }

    public void RecomputeVisibleKeys(bool resetPaging = false, string? keyToFocus = null)
    {
        visibleKeys.Clear();

        if (resetPaging)
        {
            keySelectedPage = 1;
            keyPages = 0;
        }

        IOrderedEnumerable<KeyValuePair<string, Key>> source = (Settings.KeySearchAllGroups ? Project.Decls.SelectMany(x => x.Keys) : Decl.Keys).OrderByDescending(x => x.Value.SearchPriority).ThenBy(x => x.Key, StringComparer.Ordinal);

        if (Settings.LimitRender <= 0)
        {
            foreach (KeyValuePair<string, Key> x in source)
            {
                if (x.Value.IsVisible)
                {
                    visibleKeys.Add(x);
                }
            }
        }
        else
        {
            List<KeyValuePair<string, Key>> keys = source.Where(x => x.Value.IsVisible).ToList();
            keyPages = (int)Math.Ceiling(keys.Count / (double)Settings.LimitRender);
            int take = Settings.LimitRender;

            if (keyToFocus is not null)
            {
                if (Decl.Keys.TryGetValue(keyToFocus, out Key? match))
                {
                    int index = keys.BinarySearch(new KeyValuePair<string, Key>(keyToFocus, match), Comparer<KeyValuePair<string, Key>>.Create((a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal)));

                    if (index < 0)
                    {
                        index = ~index;
                    }

                    int indexPage = (int)Math.Ceiling((index + 1) / (double)take);
                    keySelectedPage = Math.Max(1, Math.Min(indexPage, keyPages));
                }
            }

            int skip = Settings.LimitRender * (keySelectedPage - 1);

            foreach (KeyValuePair<string, Key> x in keys.Skip(skip).Take(take))
            {
                visibleKeys.Add(x);
            }

            RecomputeVisibleInputHeights();
        }
    }
    
    async Task MoveKey(Key key)
    {
        Md.ShowModal<LocalizeMoveKeyModal>(new
        {
            Key = key,
            Owner = this
        });
    }

    async Task OpenSettingsKey(Key key)
    {
        Md.ShowModal<LocalizeSettingsKeyModal>(new
        {
            Key = key,
            Owner = this
        });
    }

    Task RegenerateKey(Key key)
    {
        Md.ShowModal<LocalizeRegenerateKeyModal>(new
        {
            Owner = this,
            Key = key
        });

        return Task.CompletedTask;
    }

    public async Task<DataOrException<bool>> DoRegenerateKey(string key, IProgress<CommandProgress>? progress = null)
    {
        if (!LangsData.Langs.TryGetValue(Project.Settings.PrimaryLanguage, out LangData? data))
        {
            return new DataOrException<bool>(new Exception($"Překlady v jazyce <code>{Project.Settings.PrimaryLanguage}</code> nejsou dostupné"));
        }
        
        if (data.Data.TryGetValue(key, out string? primaryValue))
        {
            string newKeyCopy = primaryValue.Trim().FirstLetterToUpper();
            string newKeyName = Localizer.BaseIdentifier(newKeyCopy);
            DataOrException<bool> result =  await RenameAndSaveKey(key, newKeyName, KeyRenameReasons.Regenerate, progress);

            return result;
        }

        return new DataOrException<bool>(new Exception($"Klíč nemá hodnotu v primárním jazyce <code>{Project.Settings.PrimaryLanguage}</code>"));
    }

    public async Task<DataOrException<bool>> RenameAndSaveKey(string key, string newKeyName, KeyRenameReasons reason, IProgress<CommandProgress>? progress = null)
    {
        return await Execute(new CmdRenameKey(key, newKeyName, reason, progress));
    }
    
    async Task RenameKey(Key key)
    {
        Md.ShowModal<LocalizeRenameKeyModal>(new
        {
            Owner = this,
            Key = key
        });
    }

    public async Task DeleteKey(string key)
    {
        foreach (KeyValuePair<Languages, LangData> x in LangsData.Langs)
        {
            if (x.Value.Data.TryGetValue(key, out string? _))
            {
                x.Value.Data.TryRemove(key, out _);
            }
        }

        await SaveLanguages();

        if (Decl.Keys.TryGetValue(key, out Key? _))
        {
            Decl.Keys.TryRemove(key, out _);
            await SaveProject();
        }
    }
    
    async Task SetKeyPage(int page)
    {
        keySelectedPage = page;
        RecomputeVisibleKeys();
        StateHasChanged();
    }

    async Task SetKeyPageJump()
    {
        string jumpToPageValue = await Js.InvokeAsync<string>("eval", $"document.getElementById('pageJumpEl').value");

        if (int.TryParse(jumpToPageValue, out int pageNumber) && pageNumber >= 1 && pageNumber <= keyPages)
        {
            await SetKeyPage(pageNumber);
            await Js.InvokeVoidAsync("eval", $"document.getElementById('pageJumpEl').value = ''");
        }
    }
    
    public async Task TransferKey(Key key, Decl newOwner)
    {
        await Execute(new CmdMoveKey(key.Name, key.Owner, newOwner));
    }

    async Task KeySearchModeSelected(KeySearchModes mode)
    {
        Settings.KeySearchMode = mode;
        await UpdateSearchResults();
        await SaveUserSettings();
    }
    
    public void RecomputeVisibleInputHeights()
    {
        if (Settings.RenderMode is RenderModes.Textarea)
        {
            nativeCommands.Add(new NativeCommand
            {
                Type = NativeCommands.SetTextareaHeight
            });
            
            StateHasChanged();
        }
    }
    
     public void SetInputToFocus(string inputToFocus)
    {
        LastFocusedInput = inputToFocus;
        InputToFocus = inputToFocus;
    }

    async Task Delete(Key key)
    {
        Md.ShowConfirmActionModal($"Potvrďte odstranění klíče <code>{key.Name}</code>", async () =>
        {
            await Execute(new CmdDeleteKey(key.Name));
        });
    }

    public async Task Generate()
    {
        Localizer l = new Localizer(Project, LangsData);
        CodegenResult code = await l.Generate();

        if (code.Backend is not null)
        {
            string backendDir = $"{basePath}/I18N";
            string backendFile = $"{backendDir}/Reo.cs";
            
            Directory.CreateDirectory(backendDir);
            
            string[] existingBackendFiles = Directory.GetFiles(backendDir);
            
            foreach (string file in existingBackendFiles)
            {
                if (file != backendFile)
                {
                    File.Delete(file);
                }
            }
            
            await File.WriteAllTextAsync(backendFile, code.Backend);
        }

        if (code.Frontend.Decls.Count > 0 || code.Frontend.Declarations is not null)
        {
            string frontendDir = $"{basePath}/wwwroot/Scripts/reo";
            Directory.CreateDirectory(frontendDir);
            
            Dictionary<string, string> requiredFiles = new Dictionary<string, string>();

            if (code.Frontend.Declarations is not null)
            {
                requiredFiles[$"{frontendDir}/reo.d.ts"] = code.Frontend.Declarations;
            }

            if (code.Frontend.AmbientDeclarations is not null)
            {
                requiredFiles[$"{frontendDir}/reoAmbient.d.ts"] = code.Frontend.AmbientDeclarations;
            }

            if (code.Frontend.Mgr is not null)
            {
                requiredFiles[$"{frontendDir}/reolib.ts"] = code.Frontend.Mgr.Ts;
                requiredFiles[$"{frontendDir}/reolib.js"] = code.Frontend.Mgr.Js;
                requiredFiles[$"{frontendDir}/reolib.js.map"] = code.Frontend.Mgr.Map;
            }

            if (code.Frontend.Map is not null)
            {
                requiredFiles[$"{frontendDir}/reo.map.json"] = code.Frontend.Map;
            }

            if (code.Frontend.Tsconfig is not null)
            {
                requiredFiles[$"{frontendDir}/tsconfig.json"] = code.Frontend.Tsconfig;
            }
            
            foreach (KeyValuePair<Languages, string> pair in code.Frontend.Decls)
            {
                requiredFiles[$"{frontendDir}/reo.{pair.Key.ToString().ToLowerInvariant()}.json"] = pair.Value;
            }

            foreach (KeyValuePair<(Languages language, string decl), string> pair in code.Frontend.StandaloneDecls)
            {
                requiredFiles[$"{frontendDir}/{pair.Key.decl}.{pair.Key.language.ToString().ToLowerInvariant()}.json"] = pair.Value;
            }
            
            Dictionary<string, bool> localFiles = Directory.GetFiles(frontendDir, "*.*", SearchOption.AllDirectories).ToDictionary(f => f, f => true);
            
            foreach (string existingFile in localFiles.Keys.Where(x => !requiredFiles.ContainsKey(x)))
            {
                File.Delete(existingFile);
            }
            
            await Parallel.ForEachAsync(requiredFiles, async (pair, token) =>
            {
                await File.WriteAllTextAsync(pair.Key, pair.Value, token);
            });
        }

        await UpgradeProjectIfNeeded();
        await Js.Toast(ToastTypes.Success, "Kód vygenerován a zapsán");
        StateHasChanged();
    }

    async Task GenerateMissing()
    {
        if (Translating)
        {
            return;
        }

        ToTranslate.Clear();
        Translating = true;
        Translated = 0;
        StateHasChanged();

        DataOrException<bool> result = await Execute(new CmdGenerateMissingKeyValues());

        if (result.Exception is not null)
        {
            await Js.Toast(ToastTypes.Error, result.Exception.Message);
        }

        ToTranslate.Clear();
        Translated = 0;
        Translating = false;
        StateHasChanged();
    }
    
    async Task AfterSearchUpdate(string val)
    {
        KeySearch = val;
        await UpdateSearchResults();
    }
    
    
    Task UpdateSearchResults()
    {
        if (Consts.Cfg.Experimental)
        {
            foreach (KeyValuePair<string, Key> x in Decl.Keys)
            {
                x.Value.IsVisible = false;
            }

            List<SearchResult> results = Index.Search(KeySearch.ToBaseLatin());
            return Task.CompletedTask;
        }

        ApplySearch(true);
        StateHasChanged();
        return Task.CompletedTask;
    }
}