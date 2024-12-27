namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Updates settings of a given key.
/// </summary>
public class CmdSetKeySettings : BaseCommand
{
    private Key OldKey { get; set; }
    private Key NewKey { get; set; }
    
    public CmdSetKeySettings(Key oldKey, Key newKey)
    {
        OldKey = oldKey;
        NewKey = newKey;
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        Owner.Project.SelectedDecl.Keys[OldKey.Name] = NewKey;
        Owner.RecomputeVisibleKeys();
        
        await Owner.SaveProject();
        return new DataOrException<bool>(true);
    }

    public override async Task Undo()
    {
        Owner.Project.SelectedDecl.Keys[OldKey.Name] = OldKey;
        Owner.RecomputeVisibleKeys();
        
        await Owner.SaveProject();
    }

    public override string GetName()
    {
        return $"Změna nastavení klíče <code>{OldKey.Name}</code>";
    }
}