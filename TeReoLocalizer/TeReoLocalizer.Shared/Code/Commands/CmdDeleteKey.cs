using TeReoLocalizer.Annotations;

namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Deletes given key.
/// </summary>
public class CmdDeleteKey : BaseCommand
{
    string Key { get; set; }
    Dictionary<Languages, string> OriginalLangValues { get; set; } = [];
    Key? OriginalKeyDeclaration { get; set; }
    
    public CmdDeleteKey(string key)
    {
        Key = key;
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        if (firstTime)
        {
            foreach (KeyValuePair<Languages, LangData> x in LangsData.Langs)
            {
                if (x.Value.Data.TryGetValue(Key, out string? value))
                {
                    OriginalLangValues[x.Key] = value;
                }
            }

            if (Decl.Keys.TryGetValue(Key, out Key? keyDecl))
            {
                OriginalKeyDeclaration = keyDecl;
            }
        }

        await Owner.DeleteKey(Key);
        
        Owner.RecomputeVisibleKeys();
        return new DataOrException<bool>(true);
    }

    public override async Task Undo()
    {
        foreach (KeyValuePair<Languages, string> kvp in OriginalLangValues)
        {
            if (LangsData.Langs.TryGetValue(kvp.Key, out LangData? langData))
            {
                langData.Data[Key] = kvp.Value;
            }
        }

        if (OriginalKeyDeclaration is not null)
        {
            Decl.Keys[Key] = OriginalKeyDeclaration;
        }

        await Owner.SaveLanguages();
        await Owner.SaveProject();
        Owner.RecomputeVisibleKeys();
    }

    public override string GetName()
    {
        return $"Odstranění klíče <code>{Key}</code>";
    }
}