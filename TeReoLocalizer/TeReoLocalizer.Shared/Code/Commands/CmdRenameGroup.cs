namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Renames active group.
/// </summary>
public class CmdRenameGroup : BaseCommand
{
    private string OldName { get; set; }
    private string NewName { get; set; }
    
    public CmdRenameGroup(string newName)
    {
        NewName = newName.Trim();
    }
    
    public override Task<DataOrException<bool>> Do(bool firstTime)
    {
        if (firstTime)
        {
            OldName = Decl.Name ?? "Nepojmenovaná skupina";
        }
        
        if (NewName.IsNullOrWhiteSpace())
        {
            return Task.FromResult(new DataOrException<bool>(new Exception("Název skupiny nemůže být prázdný")));
        }
        
        Decl.Name = NewName;
        return Task.FromResult(new DataOrException<bool>(true));
    }

    public override Task Undo()
    {
        Decl.Name = OldName;
        return Task.FromResult(new DataOrException<bool>(true));
    }

    public override string GetName()
    {
        return $"Změna názvu skupiny <code>{OldName}</code> \u2192 <code>{NewName}</code>";
    }
}