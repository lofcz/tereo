using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.JSInterop;
using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Code;
using TeReoLocalizer.Shared.Code.Services;

namespace TeReoLocalizer.Shared.Components.Pages;

public partial class Localize
{
    async Task DeleteLanguage()
    {
        if (langCode.IsNullOrWhiteSpace())
        {
            await Js.Toast(ToastTypes.Error, "Kód jazyka nemůže být prázdný");
            return;
        }
        
        string normalized = langCode.ToUpper().Trim();

        if (!Enum.TryParse(normalized, true, out Languages _))
        {
            await Js.Toast(ToastTypes.Error, $"Kód jazyka '{normalized}' není platný");
            return;
        }
        
        if (File.Exists($"{basePath}/TeReo/lang_{normalized}.json"))
        {
            File.Delete($"{basePath}/TeReo/lang_{normalized}.json");
            langCode = string.Empty;
            await ShowLanguages(false);
            StateHasChanged();
        }
        else
        {
            await Js.Toast(ToastTypes.Error, $"Jazyk '{normalized}' není přidán");
        }
    }

    async Task AddLanguage()
    {
        if (langCode.IsNullOrWhiteSpace())
        {
            await Js.Toast(ToastTypes.Error, "Kód jazyka nemůže být prázdný");
        }

        string normalized = langCode.ToUpper().Trim();

        if (!Enum.TryParse(normalized, true, out Languages _))
        {
            await Js.Toast(ToastTypes.Error, $"Kód jazyka '{normalized}' není platný");
            return;
        }
        
        if (!File.Exists($"{basePath}/TeReo/lang_{normalized}.json"))
        {
            await File.WriteAllTextAsync($"{basePath}/TeReo/lang_{normalized}.json", "{}");
            langCode = string.Empty;
            await ShowLanguages(false);
            StateHasChanged();
        }
        else
        {
            await Js.Toast(ToastTypes.Error, $"Jazyk '{normalized}' je již přidán");
        }
    }

    async Task ShowLanguages(bool showLanguages)
    {
        if (showLanguages)
        {
            showLangs = true;
        }

        if (!Directory.Exists($"{basePath}/TeReo"))
        {
            return;
        }

        existingFiles = Directory.GetFiles($"{basePath}/TeReo").ToList();
        LangsData.Langs.Clear();

        await Parallel.ForEachAsync(existingFiles, async (str, ctx) =>
        {
            string lang = Path.GetFileName(str).Replace(".json", string.Empty).Replace("lang_", string.Empty);

            if (!Enum.TryParse(lang, out Languages result))
            {
                return;
            }

            string data = await File.ReadAllTextAsync(str, ctx);
            LangData parsed = JsonSerializer.Deserialize<LangData>(data) ?? new LangData();
            parsed.PersistedData = new ConcurrentDictionary<string, string>(parsed.Data);
            LangsData.Langs.TryAdd(result, parsed);
        });

        if (showLanguages)
        {
            StateHasChanged();
        }
    }
    
    public async Task SaveLanguage(Languages language)
    {
        LangData source = LangsData.Langs[language];
        source.PersistedData = new ConcurrentDictionary<string, string>(source.Data);
        source.UncommitedChanges.Clear();
        StateHasChanged();

        string data = LangsData.Langs[language].ToJson();
        await File.WriteAllTextAsync($"{basePath}/TeReo/lang_{language}.json", data);
    }

    public async Task SaveLanguages()
    {
        await Parallel.ForEachAsync(LangsData.Langs, async (pair, token) =>
        {
            await SaveLanguage(pair.Key);
        });
    }
    
    void ToggleLangsSelection()
    {
        showLangsSelection = !showLangsSelection;
        StateHasChanged();
    }

    void ToggleLangsAdd()
    {
        showAddLangs = !showAddLangs;
        StateHasChanged();
    }
}