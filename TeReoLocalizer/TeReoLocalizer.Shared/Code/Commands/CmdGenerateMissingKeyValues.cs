using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using DeepL;
using DeepL.Model;
using TeReoLocalizer.Annotations;

namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Generates missing translations for all keys using selected translations provider.
/// </summary>
public partial class CmdGenerateMissingKeyValues : BaseCommand
{
    static readonly Regex DeeplRegex = DeeplRegexImpl();
    static readonly Regex UndeeplRegex = UndeeplRegexImpl();

    [GeneratedRegex("\\{([^}]+)\\}", RegexOptions.Compiled)]
    private static partial Regex DeeplRegexImpl();
    [GeneratedRegex("\\(!\\d+\\)", RegexOptions.Compiled)]
    private static partial Regex UndeeplRegexImpl();

    readonly Dictionary<(Languages Lang, string Key), string> originalValues = [];
    readonly ConcurrentDictionary<(Languages Lang, string Key), string> newValues = [];

    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        if (Settings.ApiKeys.DeepL.IsNullOrWhiteSpace())
        {
            return new DataOrException<bool>(new Exception("Pro generování překladů je potřeba nastavit DeepL API klíč v Nastavení (dolní lišta)."));
        }
        
        Owner.Translated = 0;
        Owner.ToTranslate.Clear();
        Owner.StateHasChanged();

        if (firstTime)
        {
            Translator translator = new Translator(Settings.ApiKeys.DeepL);

            foreach (KeyValuePair<string, Key> keys in Ctx.Decl.Keys.Where(static x => x.Value.AutoTranslatable))
            {
                foreach (KeyValuePair<Languages, LangData> x in Ctx.LangsData.Langs)
                {
                    if (x.Key == Owner.Project.Settings.PrimaryLanguage)
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
                        
                        if (Ctx.LangsData.Langs[Owner.Project.Settings.PrimaryLanguage].Data.TryGetValue(keys.Key, out string? csValue) && !csValue.IsNullOrWhiteSpace())
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

            if (Owner.ToTranslate.Count is 0)
            {
                return new DataOrException<bool>(new Exception("Není potřeba doplnit žádné překlady."));
            }

            TextTranslateOptions opts = new TextTranslateOptions
            {
                Context = Project.Settings.TranslationProviders.DeepL.Context,
                TagHandling = "xml"
            };
            opts.IgnoreTags.Add("ignore");
            opts.IgnoreTags.Add("x");

            await Parallel.ForEachAsync(Owner.ToTranslate, async (input, token) =>
            {
                try
                {
                    DeeplifiedText deeplifyResult = DeeplifyText(input.PrimaryValue);
                    TextResult result = await translator.TranslateTextAsync(deeplifyResult.Text, DeeplCode(Owner.Project.Settings.PrimaryLanguage), DeeplCode(input.Source.Key), opts, token);
                    string translatedText = UndeeplifyText(result.Text, deeplifyResult.Placeholders);
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

    static DeeplifiedText DeeplifyText(string text)
    {
        Dictionary<string, string> placeholders = [];
        int counter = 0;
    
        string result = DeeplRegex.Replace(text, x =>
        {
            string placeholder = x.Groups[1].Value;
            string tag = $"(!{counter})";
            placeholders[tag] = $"{{{placeholder}}}";
            counter++;
            return tag;
        });

        return new DeeplifiedText
        {
            Text = result,
            Placeholders = placeholders
        };
    }


    static string UndeeplifyText(string text, Dictionary<string, string> placeholders)
    {
        return placeholders.Aggregate(text, (current, placeholder) => current.Replace(placeholder.Key, placeholder.Value));
    }

    static string DeeplCode(Languages lang)
    {
        return lang switch
        {
            Languages.AR => LanguageCode.Arabic,
            Languages.BG => LanguageCode.Bulgarian,
            Languages.CS => LanguageCode.Czech,
            Languages.DA => LanguageCode.Danish,
            Languages.DE => LanguageCode.German,
            Languages.EL => LanguageCode.Greek,
            Languages.EN => LanguageCode.EnglishAmerican,
            Languages.ES => LanguageCode.Spanish,
            Languages.ET => LanguageCode.Estonian,
            Languages.FI => LanguageCode.Finnish,
            Languages.FR => LanguageCode.French,
            Languages.HU => LanguageCode.Hungarian,
            Languages.IN => LanguageCode.Indonesian,
            Languages.IT => LanguageCode.Italian,
            Languages.JA => LanguageCode.Japanese,
            Languages.KO => LanguageCode.Korean,
            Languages.LT => LanguageCode.Lithuanian,
            Languages.LV => LanguageCode.Latvian,
            Languages.NO => LanguageCode.Norwegian,
            Languages.NL => LanguageCode.Dutch,
            Languages.PL => LanguageCode.Polish,
            Languages.PT => LanguageCode.Portuguese,
            Languages.RO => LanguageCode.Romanian,
            Languages.RU => LanguageCode.Russian,
            Languages.SK => LanguageCode.Slovak,
            Languages.SL => LanguageCode.Slovenian,
            Languages.SV => LanguageCode.Swedish,
            Languages.TR => LanguageCode.Turkish,
            Languages.UK => LanguageCode.Ukrainian,
            Languages.ZH => LanguageCode.Chinese,
            _ => LanguageCode.EnglishAmerican
        };
    }
}