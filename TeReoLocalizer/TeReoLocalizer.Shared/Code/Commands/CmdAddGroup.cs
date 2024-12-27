namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Adds a new group to the grouplist.
/// </summary>
public class CmdAddGroup : BaseCommand
{
    private string GroupName { get; set; }
    private string? GroupId { get; set; }
    private string? SelectedDeclId { get; set; }

    public CmdAddGroup(string name)
    {
        GroupName = name.Trim();
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        if (GroupName.IsNullOrWhiteSpace())
        {
            return new DataOrException<bool>(new Exception("Název skupiny nemůže být prázdný"));
        }

        Decl? dupe = Project.Decls.FirstOrDefault(x => string.Equals(x.Name, GroupName, StringComparison.OrdinalIgnoreCase));

        if (dupe is not null)
        {
            return new DataOrException<bool>(new Exception($"Skupina s názvem {GroupName} již existuje"));
        }
        
        if (firstTime)
        {
            GroupId = General.IIID();
            SelectedDeclId = Project.SelectedDecl.Id;
        }

        Decl newDecl = new Decl
        {
            Name = GroupName,
            Id = GroupId!,
            Keys = []
        };

        Project.Decls.Add(newDecl);
        Project.SelectedDecl = newDecl;
        await Owner.SaveProject();

        Settings.SelectedDecl = GroupId;
        await Owner.SaveUserSettings();

        Owner.RecomputeVisibleKeys();
        
        return new DataOrException<bool>(true);
    }

    public override async Task Undo()
    {
        Decl? groupToRemove = Project.Decls.FirstOrDefault(x => x.Id == GroupId);
        
        if (groupToRemove is not null)
        {
            Project.Decls.Remove(groupToRemove);
        }

        Decl? originalDecl = Project.Decls.FirstOrDefault(x => x.Id == SelectedDeclId);

        if (originalDecl is not null)
        {
            Project.SelectedDecl = originalDecl;   
        }
        else if (Project.Decls.Count > 0)
        {
            Project.SelectedDecl = Project.Decls[0];
        }
        
        await Owner.SaveProject();
        await Owner.SaveUserSettings();
        
        Owner.RecomputeVisibleKeys();
    }
}