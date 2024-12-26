namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Moves given key from active group to a given group.
/// </summary>
public class CmdMoveKey : BaseCommand
{
    private string Key { get; set; }
    private Decl NewDecl { get; set; }
    
    public CmdMoveKey(string key, Decl newDecl)
    {
        Key = key;
        NewDecl = newDecl;
    }
    
    public override async Task<bool> Do(bool firstTime)
    {
        if (!Decl.Keys.TryRemove(Key, out Key? key))
        {
            return false;    
        }
        
        NewDecl.Keys.TryAdd(Key, key);

        await Owner.SaveProject();
        Owner.RecomputeVisibleKeys();

        return true;
    }

    public override async Task Undo()
    {
        if (NewDecl.Keys.TryRemove(Key, out Key? key))
        {
            Decl.Keys.TryAdd(Key, key);
            
            await Owner.SaveProject();
            Owner.RecomputeVisibleKeys();
        }
    }
}