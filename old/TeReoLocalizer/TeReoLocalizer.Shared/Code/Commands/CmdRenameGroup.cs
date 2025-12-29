namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Renames active group.
/// </summary>
public class CmdRenameGroup : BaseCommand
{
    string OldName { get; set; }
    string NewName { get; set; }
    
    public CmdRenameGroup(string newName)
    {
        NewName = newName.Trim();
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        if (firstTime)
        {
            OldName = Decl.Name ?? "Nepojmenovaná skupina";
        }
        
        if (NewName.IsNullOrWhiteSpace())
        {
            return new DataOrException<bool>(new Exception("Název skupiny nemůže být prázdný"));
        }
        
        Decl.Name = NewName;
        await Owner.SaveProject();
        
        return new DataOrException<bool>(true);
    }

    public override async Task Undo()
    {
        Decl.Name = OldName;
        await Owner.SaveProject();
    }

    public override string GetName()
    {
        return $"Změna názvu skupiny <code>{OldName}</code> \u2192 <code>{NewName}</code>";
    }
}