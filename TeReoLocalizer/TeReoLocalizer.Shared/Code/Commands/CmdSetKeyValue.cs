using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.Model;
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
    private DiffResult? Diff { get; set; }
    private string? OldValue { get; set; }

    private readonly Dictionary<Languages, string?> originalValues = [];
    private TranslationModes translationMode = TranslationModes.Default;

    public CmdSetKeyValue(Languages language, string key, string value)
    {
        Language = language;
        Key = key;
        Value = value;
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        bool performed = Ctx.LangsData.SetKey(Language, Key, Value);
        
        if (!performed)
        {
            return new DataOrException<bool>(false);
        }
        
        translationMode = Ctx.Settings.TranslationMode;

        if (firstTime)
        {
            if (translationMode is TranslationModes.Invalidate)
            {
                foreach (KeyValuePair<Languages, LangData> lang in LangsData.Langs)
                {
                    if (lang.Value.FocusData.TryGetValue(Key, out string? old))
                    {
                        originalValues[lang.Key] = old;
                    }
                }
            }
            
            if (LangsData.Langs[Language].FocusData.TryGetValue(Key, out string? originalValue))
            {
                originalValues[Language] = originalValue;
                OldValue = originalValue;
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
        
        return new DataOrException<bool>(true);
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

    public override string GetName()
    {
        if (Diff is not null)
        {
            StringBuilder output = new StringBuilder();
            int posOld = 0;
            int posNew = 0;

            foreach (DiffBlock? block in Diff.DiffBlocks)
            {
                while (posNew < block.InsertStartB)
                {
                    output.Append(Diff.PiecesNew[posNew]);
                    posNew++;
                }
                
                if (block.DeleteCountA > 0)
                {
                    output.Append("<del>");
                    for (int i = 0; i < block.DeleteCountA; i++)
                    {
                        output.Append(Diff.PiecesOld[block.DeleteStartA + i]);
                    }
                    output.Append("</del>");
                }
                
                if (block.InsertCountB > 0)
                {
                    output.Append("<ins>");
                    for (int i = 0; i < block.InsertCountB; i++)
                    {
                        output.Append(Diff.PiecesNew[block.InsertStartB + i]);
                    }
                    output.Append("</ins>");
                }

                posOld = block.DeleteStartA + block.DeleteCountA;
                posNew = block.InsertStartB + block.InsertCountB;
            }
            
            while (posNew < Diff.PiecesNew.Length)
            {
                output.Append(Diff.PiecesNew[posNew]);
                posNew++;
            }

            return output.ToString().TrimEnd();
        }

        if (OldValue.IsNullOrWhiteSpace())
        {
            return $"<code>{Language}</code> <code>{Key}</code>: <ins>{Value}</ins>";
        }
        
        return $"<code>{Language}</code> <code>{Key}</code>: <del>{OldValue}</del> \u2192 <ins>{Value}</ins>";
    }
}