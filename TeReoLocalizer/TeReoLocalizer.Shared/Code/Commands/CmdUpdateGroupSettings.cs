namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Updates settings of the active <see cref="Decl"/>.
/// </summary>
public class CmdUpdateGroupSettings : BaseCommand
{
    DeclSettings newSettings;
    DeclSettings oldSettings;
    string declId, declName;
    
    public CmdUpdateGroupSettings(DeclSettings settings)
    {
        newSettings = settings;
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        if (firstTime)
        {
            oldSettings = Owner.Decl.Settings.DeepClone();
            declId = Owner.Decl.Id;
            declName = Owner.Decl.Name ?? "Nepojmenovaná skupina";
        }

        Owner.Decl.Settings = newSettings;
        await Owner.SaveProject();
        return new DataOrException<bool>(true);
    }

    public override async Task Undo()
    {
        Decl? match = Owner.Project.Decls.FirstOrDefault(x => x.Id == declId);

        if (match is null)
        {
            return;
        }

        match.Settings = oldSettings;
        await Owner.SaveProject();
    }

    public override string GetName()
    {
        return $"Změna nastavení skupiny <code>{declName}</code>";
    }
}