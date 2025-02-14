using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Code;
using TeReoLocalizer.Shared.Components.Pages.Owned;

namespace TeReoLocalizer.Shared.Components.Pages;

public partial class Localize
{
    ProjectCtx GetCtx()
    {
        return new ProjectCtx(Project, Project.SelectedDecl, Settings, LangsData, Js, this);
    }

    public async Task<DataOrException<bool>> Execute(ICommand cmd)
    {
        cmd.Ctx = GetCtx();
        DataOrException<bool> result = await CommandManager.Execute(cmd);
        StateHasChanged();
        return result;
    }
    
    void ToggleGroupSettings()
    {
        showGroupSettings = !showGroupSettings;
        StateHasChanged();
    }
    
    async Task HandleKeyAddKeyPressed(KeyboardEventArgs args)
    {
        if (args.Code == "Enter")
        {
            await AddKey();
        }
    }

    async Task HandleEditorClick(Languages language, string key, string value)
    {
        string? freshValue = null;
        
        try
        {
            // the value reported here is possibly stale, if the value has been edited prior to clicking the RTE icon without blur event happening interim
            freshValue = await Js.InvokeAsync<string?>("mcf.getElementValue", $"input_{language}_{key}");
        }
        catch (Exception e)
        {
            
        }
        
        Md.ShowModal<LocalizeKeyEditorModal>(new
        {
            Owner = this,
            Language = language,
            Key = key,
            Value = freshValue ?? value
        });
    }
    
    public void ApplySearch(bool recomputeKeysAfter = false, bool resetPagingOnRecompute = true)
    {
        string searchTerm = Settings.KeySearchLang is null ? KeySearch.ToBaseLatin() : KeySearch;
        searchTerm = searchTerm.TrimStart();
        
        Parallel.ForEach(Settings.KeySearchAllGroups ? Project.Decls.SelectMany(x => x.Keys) : Decl.Keys, FullDop, item =>
        {
            item.Value.IsVisible = false;
            item.Value.SearchPriority = 0;

            string? keyToCompare = null;

            if (Settings.KeySearchLang is null)
            {
                keyToCompare = item.Key;
            }
            else
            {
                if (LangsData.Langs.TryGetValue(Settings.KeySearchLang.Value, out LangData? data))
                {
                    if (data.Data.TryGetValue(item.Key, out string? translatedKey))
                    {
                        keyToCompare = translatedKey;
                    }
                }
            }

            if (keyToCompare is null)
            {
                return;
            }

            if (searchTerm.Length is 0)
            {
                item.Value.IsVisible = true;
                item.Value.SearchPriority = 0;
                
                // when search is empty but "search in all groups" is enabled, consider as matches only keys from the active decl
                if (Settings.KeySearchAllGroups)
                {
                    if (item.Value.Owner != Decl)
                    {
                        item.Value.IsVisible = false;
                    }
                }
                
                return;
            }

            (bool contains, bool startsWith, bool exactMatch) = CompareStrings(keyToCompare, searchTerm, true);

            bool isMatch = Settings.KeySearchMode switch
            {
                KeySearchModes.Unknown or KeySearchModes.Contains => contains,
                KeySearchModes.Exact => exactMatch,
                _ => false
            };

            if (isMatch)
            {
                item.Value.IsVisible = true;
                item.Value.SearchPriority = exactMatch ? 2 : startsWith ? 1 : 0;
            }
        });

        if (recomputeKeysAfter)
        {
            RecomputeVisibleKeys(resetPagingOnRecompute);   
        }
    }

    void ToggleSearchModeSelection()
    {
        renderSearchModeSelection = !renderSearchModeSelection;
        StateHasChanged();
    }

    void ShowAppSettings()
    {
        Md.ShowModal<AppSettingsModal>(new
        {
            Owner = this
        });
    }

    void ShowHistory()
    {
        Md.ShowModal<LocalizeHistoryModal>(new
        {
            Owner = this
        });
    }

    async Task Undo()
    {
        if (CommandManager.CanUndo)
        {
            await CommandManager.Undo();
            StateHasChanged();
        }
    }

    async Task Redo()
    {
        if (CommandManager.CanRedo)
        {
            await CommandManager.Redo();
            StateHasChanged();
        }
    }

    public async Task JumpTo(HistoryItem item)
    {
        await CommandManager.Jump(item);
        StateHasChanged();
    }
    
    async Task AfterLanguageToggle(KeyValuePair<Languages, LangData> local, bool visible)
    {
        local.Value.Visible = visible;
        Settings.ShowLangs = LangsData.Langs.Where(x => x.Value.Visible).Select(x => x.Key).ToList();
        await SaveUserSettings();
    }

    async Task AfterLimitUpdate(int value)
    {
        Settings.LimitRender = value;
        RecomputeVisibleKeys(true);
        StateHasChanged();
        await SaveUserSettings();
    }
    
    public void ConditionallyClearSearch(string currentVal)
    {
        if (!KeySearch.IsNullOrWhiteSpace() && !KeySearch.Contains(currentVal))
        {
            KeySearch = string.Empty;
        }
    }

    public void SetSearch(string val)
    {
        KeySearch = val;
    }
    
    async Task CycleTranslationMode()
    {
        if (Settings.TranslationMode < TranslationModes.Invalidate)
        {
            Settings.TranslationMode++;
        }
        else
        {
            Settings.TranslationMode = TranslationModes.Default;
        }

        await SaveUserSettings();
        StateHasChanged();
    }

    async Task CycleInputMode()
    {
        if (Settings.RenderMode < RenderModes.Textarea)
        {
            Settings.RenderMode++;
            nativeCommands.Add(new NativeCommand
            {
                Type = NativeCommands.SetTextareaHeight
            });
        }
        else
        {
            Settings.RenderMode = RenderModes.Input;
        }

        StateHasChanged();
        await SaveUserSettings();
    }
}