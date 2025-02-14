namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Moves given key from active group to a given group.
/// </summary>
public class CmdMoveKey : BaseCommand
{
    string Key { get; set; }
    Decl NewDecl { get; set; }
    Decl OldDecl { get; set; }
    string? OldDeclName { get; set; }
    
    public CmdMoveKey(string key, Decl oldDecl, Decl newDecl)
    {
        Key = key;
        NewDecl = newDecl;
        OldDecl = oldDecl;
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        if (OldDecl.Equals(NewDecl))
        {
            return new DataOrException<bool>(new Exception("Přesun je možný pouze do jiné skupiny, než ve které klíč aktuálně je."));
        }
        
        if (firstTime)
        {
            OldDeclName = OldDecl.Name;
        }
        
        if (!OldDecl.Keys.TryRemove(Key, out Key? key))
        {
            return new DataOrException<bool>(new Exception("Klíč se nenachází ve skupině, ze které má být přesunut"));    
        }
        
        key.Owner = NewDecl;
        NewDecl.Keys.TryAdd(Key, key);

        await Owner.SaveProject();
        Owner.ApplySearch(true, false);

        return new DataOrException<bool>(true);
    }

    public override async Task Undo()
    {
        if (NewDecl.Keys.TryRemove(Key, out Key? key))
        {
            if (Project.Decls.Contains(Decl))
            {
                key.Owner = OldDecl;
                OldDecl.Keys.TryAdd(Key, key);   
            }
            
            await Owner.SaveProject();
            Owner.ApplySearch(true, false);
        }
    }

    public override string GetName()
    {
        return $"Přesun klíče <code>{Key}</code> ze skupiny <code>{(OldDeclName ?? "výchozí skupina")}</code> do skupiny <code>{NewDecl.Name}</code>";
    }
}