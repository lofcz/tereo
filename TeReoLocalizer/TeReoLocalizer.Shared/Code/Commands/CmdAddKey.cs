using Microsoft.JSInterop;
using TeReoLocalizer.Annotations;

namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Adds new localization key.
/// </summary>
public class CmdAddKey : BaseCommand
{
    private string NewKey { get; set; }
    private string? Search { get; set; }
    private string? ToFocus { get; set; }
    
    public CmdAddKey(string newKey)
    {
        NewKey = newKey;
    }

    public override string GetName()
    {
        return $"Přidání klíče {NewKey}";
    }
    
    public override async Task<bool> Do()
    {
        string newKeyCopy = NewKey.Trim().FirstLetterToUpper();
        string baseKey = NewKey;

        newKeyCopy = Localizer.BaseIdentifier(newKeyCopy);

        Key localKey = new Key
        {
            Name = newKeyCopy,
            Id = General.IIID()
        };

        if (Decl.Keys.TryAdd(localKey.Name, localKey))
        {
            if (Settings.AutoSave)
            {
                await Owner.SaveProject();   
            }

            if (newKeyCopy != baseKey)
            {
                await Owner.SetKey(Languages.CS, newKeyCopy, baseKey);
            }

            Search = Owner.KeySearch;
            Owner.ConditionallyClearSearch(newKeyCopy);

            foreach (Decl pDecl in Project.Decls)
            {
                foreach (KeyValuePair<string, Key> x in pDecl.Keys)
                {
                    x.Value.IsVisible = true;
                }   
            }

            ToFocus = Owner.LastFocusedInput;
            Owner.NewKey = string.Empty;
            Owner.SetInputToFocus($"input_CS_{newKeyCopy}");
            Owner.RecomputeVisibleKeys(true, newKeyCopy);
            Owner.StateHasChanged();
        }
        else
        {
            await Js.InvokeVoidAsync("alert", $"Klíč '{localKey.Name}' už existuje");
            return false;
        }

        return true;
    }

    public override async Task Undo()
    {
        string newKeyCopy = NewKey.Trim().FirstLetterToUpper();
        newKeyCopy = Localizer.BaseIdentifier(newKeyCopy);

        Key localKey = new Key
        {
            Name = newKeyCopy,
            Id = General.IIID()
        };
        
        if (Decl.Keys.TryRemove(localKey.Name, out Key? key))
        {
            if (Settings.AutoSave)
            {
                await Owner.SaveProject();
            }
            
            if (newKeyCopy != NewKey)
            {
                await Owner.DeleteKey(key.Name);
            }
            
            Owner.SetSearch(Search);
            Owner.RecomputeVisibleKeys(true);
            Owner.InputToFocus = ToFocus;
            Owner.StateHasChanged();
        }
        else
        {
            await Js.InvokeVoidAsync("alert", $"Klíč '{newKeyCopy}' nebyl nalezen pro odstranění");
        }
    }
}