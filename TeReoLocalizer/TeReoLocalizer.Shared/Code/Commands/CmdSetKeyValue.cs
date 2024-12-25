using TeReoLocalizer.Annotations;

namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Sets value of a given key in a given language.
/// </summary>
public class CmdSetKeyValue : BaseCommand
{
    private Languages Language { get; set; }
    private string Key { get; set; }
    private string Value { get; set; }

    private readonly Dictionary<Languages, string?> originalValues = [];
    private TranslationModes translationMode = TranslationModes.Default;

    public CmdSetKeyValue(Languages language, string key, string value)
    {
        Language = language;
        Key = key;
        Value = value;
    }
    
    public override async Task<bool> Do(bool firstTime)
    {
        bool performed = Ctx.LangsData.SetKey(Language, Key, Value);
        
        if (!performed)
        {
            return false;
        }
     
        translationMode = Ctx.Settings.TranslationMode;

        if (firstTime)
        {
            if (translationMode is TranslationModes.Invalidate)
            {
                foreach (KeyValuePair<Languages, LangData> lang in LangsData.Langs)
                {
                    if (lang.Value.FocusData.TryGetValue(Key, out string? originalValue))
                    {
                        originalValues[lang.Key] = originalValue;
                    }
                }
            }
            else
            {
                if (LangsData.Langs[Language].FocusData.TryGetValue(Key, out string? originalValue))
                {
                    originalValues[Language] = originalValue;
                }
            }
        }

        if (translationMode is TranslationModes.Default)
        {
            await Ctx.Owner.SaveLanguage(Language);
        }
        else
        {
            foreach (KeyValuePair<Languages, LangData> x in Ctx.LangsData.Langs.Where(x => x.Key != Language))
            {
                x.Value.Data.TryRemove(Key, out _);
            }

            await Ctx.Owner.SaveLanguages();
        }
        
        return true;
    }

    public override async Task Undo()
    {
        foreach (KeyValuePair<Languages, string?> originalValue in originalValues)
        {
            if (originalValue.Value is not (null or ""))
            {
                LangData lang = LangsData.Langs[Language];
                lang.Data[Key] = originalValue.Value;
                lang.FocusData[Key] = originalValue.Value;
            }
            else
            {
                LangsData.Langs[originalValue.Key].Data.TryRemove(Key, out _);
                LangsData.Langs[originalValue.Key].FocusData.TryRemove(Key, out _);
            }
        }
        
        if (translationMode is TranslationModes.Default)
        {
            await Ctx.Owner.SaveLanguage(Language);
        }
        else
        {
            await Ctx.Owner.SaveLanguages();
        }
    }
}