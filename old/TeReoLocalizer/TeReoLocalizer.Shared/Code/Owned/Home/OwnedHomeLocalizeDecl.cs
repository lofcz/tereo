using Microsoft.AspNetCore.Components;
using TeReoLocalizer.Shared.Code;
using TeReoLocalizer.Shared.Code.Commands;
using TeReoLocalizer.Shared.Code.Services;
using TeReoLocalizer.Shared.Components.Pages.Owned;

namespace TeReoLocalizer.Shared.Components.Pages;

public partial class Localize
{
    async Task HandleActiveDeclChange(ChangeEventArgs args)
    {
        if (args.Value is string str)
        {
            Decl? matchingDecl = Project.Decls.FirstOrDefault(x => x.Id == str);

            if (matchingDecl is not null)
            {
                Project.SelectedDecl = matchingDecl;
                Settings.SelectedDecl = str;
                await SaveUserSettings();

                ApplySearch(true);
                StateHasChanged();
            }
        }
    }
    
    Task EditDecl()
    {
        Md.ShowModal<LocalizeGroupSettingsModal>(new
        {
            Owner = this
        });
        
        return Task.CompletedTask;
    }

    Task RenameDecl()
    {
        Md.ShowPromptModal($"Nový název skupiny {Decl.Name}", string.Empty, async (str) =>
        {
            await Execute(new CmdRenameGroup(str), ExecuteErrorHandleTypes.Toast);
            StateHasChanged();
        }, defaultText: Decl.Name.IsNullOrWhiteSpace() ? string.Empty : Decl.Name);

        return Task.CompletedTask;
    }

    Task AddDecl()
    {
        Md.ShowPromptModal("Přidat skupinu", "Zadejte název nové skupiny", async (str) =>
        {
            await Execute(new CmdAddGroup(str), ExecuteErrorHandleTypes.Toast);
        });

        return Task.CompletedTask;
    }

    async Task DeleteDecl()
    {
        if (Project.Decls.Count <= 1)
        {
            await Js.Toast(ToastTypes.Error, "Skupinu je možné odstranit pouze pokud projekt obsahuje více než jednu skupinu");
            return;
        }
        
        Md.ShowConfirmActionModal($"Potvrďte odstranění skupiny '{Decl.Name}' (klíčů: {Decl.Keys.Count})", async () =>
        {
            await Execute(new CmdDeleteGroup(Decl), ExecuteErrorHandleTypes.Toast);
            StateHasChanged();
        });
    }

}