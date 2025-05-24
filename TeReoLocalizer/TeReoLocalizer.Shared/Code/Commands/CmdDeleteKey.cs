using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Code.Services;

namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Deletes given key.
/// </summary>
public class CmdDeleteKey : BaseCommand
{
    string Key { get; set; }
    bool DeleteInAllDecls { get; set; }
    Dictionary<Languages, string> OriginalLangValues { get; set; } = [];
    Key? OriginalKeyDeclaration { get; set; }
    
    public CmdDeleteKey(string key, bool deleteInAllDecls)
    {
        Key = key;
        DeleteInAllDecls = deleteInAllDecls;
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

        if (DeleteInAllDecls)
        {
            foreach (Decl decl in Project.Decls)
            {
                decl.Keys.TryRemove(Key, out _);
            }
            
            foreach (KeyValuePair<Languages, LangData> x in LangsData.Langs)
            {
                x.Value.Data.TryRemove(Key, out _);
            }

            await Owner.SaveLanguages();   
        }
        else
        {
            if (Decl.Keys.TryRemove(Key, out _))
            {
                if (!Project.Decls.Any(x => x.Keys.TryGetValue(Key, out _)))
                {
                    foreach (KeyValuePair<Languages, LangData> x in LangsData.Langs)
                    {
                        x.Value.Data.TryRemove(Key, out _);
                    }

                    await Owner.SaveLanguages();   
                }
            }   
        }
        
        await Owner.SaveProject();
        Owner.RecomputeVisibleKeys();
        
        Owner.ScheduleGenerate(true);
        
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
            if (DeleteInAllDecls)
            {
                foreach (Decl decl in Project.Decls)
                {
                    decl.Keys[Key] = OriginalKeyDeclaration;
                }
            }
            else
            {
                Decl.Keys[Key] = OriginalKeyDeclaration;
            }
        }

        await Owner.SaveLanguages();
        await Owner.SaveProject();
        
        Owner.ScheduleGenerate(true);
        
        Owner.RecomputeVisibleKeys();
    }

    public override string GetName()
    {
        return $"Odstranění klíče <code>{Key}</code>";
    }
}