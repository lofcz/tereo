using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Code.Services;

namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Deletes given group.
/// </summary>
public class CmdDeleteGroup : BaseCommand
{
    string? SelectedDeclId { get; set; }
    Decl? DeletedDecl { get; set; }
    Dictionary<Languages, Dictionary<string, (string? Data, string? FocusData, string? PersistedData)>>? BackupData { get; set; }

    public CmdDeleteGroup(Decl decl)
    {
        DeletedDecl = decl;
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        if (Project.Decls.Count <= 1)
        {
            return new DataOrException<bool>(new Exception("Poslední skupinu v projektu není možné odstranit"));
        }
        
        if (firstTime)
        {
            SelectedDeclId = Project.SelectedDecl.Id;
            DeletedDecl = Decl;
            BackupData = [];
        }

        foreach (KeyValuePair<Languages, LangData> x in LangsData.Langs)
        {
            if (firstTime)
            {
                BackupData![x.Key] = new Dictionary<string, (string?, string?, string?)>();
            }
            
            foreach (KeyValuePair<string, Key> key in Decl.Keys)
            {
                x.Value.Data.TryRemove(key.Key, out string? removedData);
                x.Value.FocusData.TryRemove(key.Key, out string? removedFocusData);
                x.Value.PersistedData.TryRemove(key.Key, out string? removedPersistedData);

                if (firstTime)
                {
                    BackupData![x.Key][key.Key] = (removedData, removedFocusData, removedPersistedData);
                }
            }
        }

        await Owner.SaveLanguages();
        
        Project.Decls.Remove(Decl);
        Project.SelectedDecl = Project.Decls.FirstOrDefault() ?? new Decl();
        await Owner.SaveProject();

        Settings.SelectedDecl = Project.SelectedDecl.Id;
        await Owner.SaveUserSettings();

        Owner.RecomputeVisibleKeys();
        
        Owner.ScheduleGenerate(true);
        
        return new DataOrException<bool>(true);
    }

    public override async Task Undo()
    {
        if (DeletedDecl is null)
        {
            return;
        }

        if (BackupData is not null)
        {
            foreach (KeyValuePair<Languages, Dictionary<string, (string? Data, string? FocusData, string? PersistedData)>> lang in BackupData)
            {
                if (!LangsData.Langs.TryGetValue(lang.Key, out LangData? langData))
                {
                    continue;
                }
                
                foreach (KeyValuePair<string, (string? Data, string? FocusData, string? PersistedData)> keyData in lang.Value)
                {
                    if (!string.IsNullOrEmpty(keyData.Value.Data))
                    {
                        langData.Data[keyData.Key] = keyData.Value.Data;
                    }

                    if (!string.IsNullOrEmpty(keyData.Value.FocusData))
                    {
                        langData.FocusData[keyData.Key] = keyData.Value.FocusData;
                    }

                    if (!string.IsNullOrEmpty(keyData.Value.PersistedData))
                    {
                        langData.PersistedData[keyData.Key] = keyData.Value.PersistedData;
                    }
                }
            }   
        }
        
        await Owner.SaveLanguages();
        
        Project.Decls.Add(DeletedDecl);
        
        if (SelectedDeclId is not null)
        {
            Project.SelectedDecl = Project.Decls.FirstOrDefault(d => d.Id == SelectedDeclId) ?? Project.Decls.FirstOrDefault() ?? new Decl();
            Settings.SelectedDecl = Project.SelectedDecl.Id;
        }

        await Owner.SaveProject();
        await Owner.SaveUserSettings();
        
        Owner.RecomputeVisibleKeys();
        
        Owner.ScheduleGenerate(true);
    }

    public override string GetName()
    {
        return $"Odstranění skupiny <code>{(DeletedDecl?.Name ?? "Nepojmenovaná skupina")}</code>";
    }
}