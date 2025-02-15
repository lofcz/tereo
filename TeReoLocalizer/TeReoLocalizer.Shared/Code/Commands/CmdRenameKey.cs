using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Code.Services;

namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Renames given key and refactors codebase to reflect the change.
/// </summary>
public class CmdRenameKey : BaseCommand
{
    string OldKeyName { get; set; }
    string NewKeyName { get; set; }
    IProgress<CommandProgress>? RenameProgress { get; set; }
    KeyRenameReasons Reason { get; set; }

    public override IProgress<CommandProgress>? Progress
    {
        get => RenameProgress;
        set => RenameProgress = value;
    }

    public CmdRenameKey(string oldKeyName, string newKeyName, KeyRenameReasons reason, IProgress<CommandProgress>? progress = null)
    {
        OldKeyName = oldKeyName;
        NewKeyName = newKeyName;
        RenameProgress = progress;
        Reason = reason;
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        NewKeyName = Localizer.BaseIdentifier(NewKeyName);

        if (NewKeyName.IsNullOrWhiteSpace())
        {
            return new DataOrException<bool>(new Exception("Název klíče nemůže být prázdný"));
        }
        
        if (string.Equals(OldKeyName, NewKeyName, StringComparison.Ordinal))
        {
            return new DataOrException<bool>(new Exception("Název klíče je po normalizaci shodný se současným názvem"));
        }

        if (Decl.Keys.TryGetValue(NewKeyName, out Key? dupe))
        {
            return new DataOrException<bool>(new Exception($"Klíč <code>{dupe.Name}</code> je již ve skupině <code>{(Decl.Name ?? "výchozí skupina")}</code> deklarovaný"));
        }
        
        foreach (KeyValuePair<Languages, LangData> x in LangsData.Langs)
        {
            if (x.Value.Data.TryGetValue(OldKeyName, out string? strValue))
            {
                x.Value.Data.TryAdd(NewKeyName, strValue);
                x.Value.Data.TryRemove(OldKeyName, out _);
            }
        }
        
        await Owner.SaveLanguages();

        if (Decl.Keys.TryGetValue(OldKeyName, out Key? kVal))
        {
            kVal.Name = NewKeyName;
            Decl.Keys.TryRemove(OldKeyName, out _);
            Decl.Keys.TryAdd(NewKeyName, kVal);

            Owner.RecomputeVisibleKeys(true, kVal.Name);
            
            await Owner.SaveProject();
        }

        if (BootedProject.CsprojExists)
        {
            RenameResult renameResult = await SymbolRenamer.RenameSymbol(BootedProject.Path, OldKeyName, NewKeyName, RenameProgress);

            if (renameResult.TotalReplacements > 0)
            {
                await Owner.Generate();
            }
        }

        return new DataOrException<bool>(true);
    }

    public override async Task Undo()
    {
        foreach (KeyValuePair<Languages, LangData> x in LangsData.Langs)
        {
            if (x.Value.Data.TryGetValue(NewKeyName, out string? strValue))
            {
                x.Value.Data.TryAdd(OldKeyName, strValue);
                x.Value.Data.TryRemove(NewKeyName, out _);
            }
        }
        
        await Owner.SaveLanguages();
        
        if (Decl.Keys.TryGetValue(NewKeyName, out Key? kVal))
        {
            kVal.Name = OldKeyName;
            Decl.Keys.TryRemove(NewKeyName, out _);
            Decl.Keys.TryAdd(OldKeyName, kVal);

            Owner.RecomputeVisibleKeys(true, kVal.Name);
        
            await Owner.SaveProject();
        }
        
        if (BootedProject.CsprojExists)
        {
            RenameResult renameResult = await SymbolRenamer.RenameSymbol(BootedProject.Path, NewKeyName, OldKeyName, RenameProgress);

            if (renameResult.TotalReplacements > 0)
            {
                await Owner.Generate();
            }
        }
    }

    public override string GetName()
    {
        if (Reason is KeyRenameReasons.Regenerate)
        {
            return $"Název klíče přegenerován <code>{OldKeyName}</code> \u2192 <code>{NewKeyName}</code>";
        }
        
        return $"Změna názvu klíče <code>{OldKeyName}</code> \u2192 <code>{NewKeyName}</code>";
    }
}