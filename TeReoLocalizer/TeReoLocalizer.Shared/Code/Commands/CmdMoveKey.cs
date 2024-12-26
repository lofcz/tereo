namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Moves given key from active group to a given group.
/// </summary>
public class CmdMoveKey : BaseCommand
{
    private string Key { get; set; }
    private Decl NewDecl { get; set; }
    private string? OldDeclName { get; set; }
    
    public CmdMoveKey(string key, Decl newDecl)
    {
        Key = key;
        NewDecl = newDecl;
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        if (firstTime)
        {
            OldDeclName = Decl.Name;
        }
        
        if (!Decl.Keys.TryRemove(Key, out Key? key))
        {
            return new DataOrException<bool>(false);    
        }
        
        NewDecl.Keys.TryAdd(Key, key);

        await Owner.SaveProject();
        Owner.RecomputeVisibleKeys();

        return new DataOrException<bool>(true);
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

    public override string GetName()
    {
        return $"Přesun klíče <code>{Key}</code> ze skupiny <code>{(OldDeclName ?? "výchozí skupina")}</code> do skupiny <code>{Decl.Name}</code>";
    }
}