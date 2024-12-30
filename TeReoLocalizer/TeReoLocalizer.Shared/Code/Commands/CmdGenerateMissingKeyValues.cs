using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using DeepL;
using DeepL.Model;
using TeReoLocalizer.Annotations;

namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Generates missing translations for all keys using selected translations provider.
/// </summary>
public class CmdGenerateMissingKeyValues : BaseCommand
{
    private static readonly Regex DeeplRegex = new Regex("\\{([^}]+)\\}", RegexOptions.Compiled);
    private static readonly Regex UndeeplRegex = new Regex("</?ignore>", RegexOptions.Compiled);

    private readonly Dictionary<(Languages Lang, string Key), string> originalValues = [];
    private readonly ConcurrentDictionary<(Languages Lang, string Key), string> newValues = [];

    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        if (Consts.Cfg.DeepL.IsNullOrWhiteSpace())
        {
            return new DataOrException<bool>(new Exception("DeepL API klíč není nastaven v appCfg.json5"));
        }
        
        Owner.Translated = 0;
        Owner.ToTranslate.Clear();
        Owner.StateHasChanged();

        if (firstTime)
        {
            Translator translator = new Translator(Consts.Cfg.DeepL);

            foreach (KeyValuePair<string, Key> keys in Ctx.Decl.Keys.Where(x => x.Value.AutoTranslatable))
            {
                foreach (KeyValuePair<Languages, LangData> x in Ctx.LangsData.Langs)
                {
                    if (x.Key is Languages.CS)
                    {
                        continue;
                    }

                    string? currentVal = null;
                    bool adding = true;

                    if (x.Value.Data.TryGetValue(keys.Key, out string? strValue))
                    {
                        currentVal = strValue;
                        adding = false;
                    }
                    
                    if (currentVal.IsNullOrWhiteSpace())
                    {
                        originalValues[(x.Key, keys.Key)] = strValue ?? string.Empty;
                        
                        if (Ctx.LangsData.Langs[Languages.CS].Data.TryGetValue(keys.Key, out string? csValue) && !csValue.IsNullOrWhiteSpace())
                        {
                            Owner.ToTranslate.Add(new TranslateTaskInput
                            {
                                PrimaryValue = csValue,
                                Source = x,
                                Adding = adding,
                                Key = keys
                            });
                        }
                    }
                }
            }

            TextTranslateOptions opts = new TextTranslateOptions
            {
                Context = "Aplikace pro přípravu podkladů na vyučovací hodiny",
                TagHandling = "xml"
            };
            opts.IgnoreTags.Add("ignore");

            await Parallel.ForEachAsync(Owner.ToTranslate, async (input, token) =>
            {
                try
                {
                    string finalVal = DeeplifyText(input.PrimaryValue);
                    TextResult result = await translator.TranslateTextAsync(finalVal, LanguageCode.Czech, DeeplCode(input.Source.Key), opts, token);

                    string translatedText = UndeeplifyText(result.Text);
                    newValues.TryAdd((input.Source.Key, input.Key.Key), translatedText);
                    Ctx.LangsData.Langs[input.Source.Key].Data[input.Key.Key] = translatedText;
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    Owner.Translated++;
                    Owner.StateHasChanged();
                }
            });
        }
        else
        {
            foreach (KeyValuePair<(Languages Lang, string Key), string> kvp in newValues)
            {
                Ctx.LangsData.Langs[kvp.Key.Lang].Data[kvp.Key.Key] = kvp.Value;
            }
        }
        
        Owner.RecomputeVisibleInputHeights();
        
        Owner.ToTranslate.Clear();
        Owner.Translated = 0;
        Owner.Translating = false;

        await Owner.SaveLanguages();
        return new DataOrException<bool>(true);
    }

    public override async Task Undo()
    {
        foreach (((Languages lang, string key), string? originalValue) in originalValues)
        {
            if (!originalValue.IsNullOrWhiteSpace())
            {
                Ctx.LangsData.Langs[lang].Data[key] = originalValue;
            }
            else
            {
                Ctx.LangsData.Langs[lang].Data.TryRemove(key, out _);
            }
        }

        Owner.RecomputeVisibleInputHeights();
        await Ctx.Owner.SaveLanguages();
    }

    public override string GetName()
    {
        return $"Doplnění chybějících překladů ({newValues.Count} položek)";
    }
    
    private static string DeeplifyText(string text)
    {
        return DeeplRegex.Replace(text, "<ignore>{$1}</ignore>");
    }

    private static string UndeeplifyText(string text)
    {
        return UndeeplRegex.Replace(text, string.Empty);
    }

    private static string DeeplCode(Languages lang)
    {
        return lang switch
        {
            Languages.CS => LanguageCode.Czech,
            Languages.EN => LanguageCode.EnglishAmerican,
            Languages.PL => LanguageCode.Polish,
            Languages.ES => LanguageCode.Spanish,
            Languages.SL => LanguageCode.Slovak,
            Languages.FR => LanguageCode.French,
            Languages.DE => LanguageCode.German,
            Languages.RU => LanguageCode.Russian,
            Languages.UK => LanguageCode.Ukrainian,
            Languages.PT => LanguageCode.Portuguese,
            _ => LanguageCode.English
        };
    }
}