namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Deletes given group.
/// </summary>
public class CmdDeleteGroup : BaseCommand
{
    private string? SelectedDeclId { get; set; }
    private Decl? DeletedDecl { get; set; }

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
        }
        
        Project.Decls.Remove(Decl);
        Project.SelectedDecl = Project.Decls.FirstOrDefault() ?? new Decl();
        await Owner.SaveProject();

        Settings.SelectedDecl = Project.SelectedDecl.Id;
        await Owner.SaveUserSettings();

        Owner.RecomputeVisibleKeys();
        return new DataOrException<bool>(true);
    }

    public override async Task Undo()
    {
        if (DeletedDecl is null)
        {
            return;
        }
        
        Project.Decls.Add(DeletedDecl);
        
        if (SelectedDeclId is not null)
        {
            Project.SelectedDecl = Project.Decls.FirstOrDefault(d => d.Id == SelectedDeclId) ?? Project.Decls.FirstOrDefault() ?? new Decl();
            Settings.SelectedDecl = Project.SelectedDecl.Id;
        }

        await Owner.SaveProject();
        await Owner.SaveUserSettings();
        
        Owner.RecomputeVisibleKeys();
    }

    public override string GetName()
    {
        return $"Odstranění skupiny <code>{(DeletedDecl?.Name ?? "Nepojmenovaná skupina")}</code>";
    }
}