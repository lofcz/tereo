using Microsoft.JSInterop;
using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Code.Services;

namespace TeReoLocalizer.Shared.Code.Commands;

/// <summary>
/// Adds a new localization key.
/// </summary>
public class CmdAddKey : BaseCommand
{
    private string NewKey { get; set; }
    private string? Search { get; set; }
    private string? ToFocus { get; set; }
    private string? KeyToFocus { get; set; }
    private string? DeclId { get; set; }
    
    public CmdAddKey(string newKey)
    {
        NewKey = newKey;
    }

    public override string GetName()
    {
        return $"Přidání klíče <code>{NewKey}</code>";
    }
    
    public override async Task<DataOrException<bool>> Do(bool firstTime)
    {
        string newKeyCopy = NewKey.Trim().FirstLetterToUpper();
        string baseKey = NewKey;

        newKeyCopy = Localizer.BaseIdentifier(newKeyCopy);

        if (newKeyCopy.IsNullOrWhiteSpace())
        {
            return new DataOrException<bool>(new Exception("Název klíče nemůže být prázdný"));
        }

        Decl? dupeKey = Project.Decls.FirstOrDefault(x => x.Keys.Any(y => y.Key == newKeyCopy));

        if (dupeKey is not null)
        {
            return new DataOrException<bool>(new Exception($"Klíč <code>{newKeyCopy}</code> již existuje ve skupině <code>{dupeKey.Name}</code>"));
        }
        
        Key localKey = new Key
        {
            Name = newKeyCopy,
            Id = General.IIID()
        };

        if (firstTime)
        {
            DeclId = Decl.Id;
        }

        Decl? match = Project.GetDecl(DeclId!);

        if (match is null)
        {
            return new DataOrException<bool>(new Exception($"Skupina s ID {DeclId} nenalezena, klíč není možné přidat"));
        }
        
        if (match.Keys.TryAdd(localKey.Name, localKey))
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

            KeyToFocus = Owner.LastAddedKey;
            Owner.LastAddedKey = localKey.Name;
            ToFocus = $"input_CS_{newKeyCopy}";
            Owner.NewKey = string.Empty;
            Owner.SetInputToFocus($"input_CS_{newKeyCopy}");
            Owner.RecomputeVisibleKeys(true, newKeyCopy);
            Owner.StateHasChanged();
        }
        else
        {
            return new DataOrException<bool>(new Exception($"Klíč <code>{localKey.Name}</code> již existuje ve skupině <code>{match.Name}</code>"));
        }

        return new DataOrException<bool>(true);
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
        
        Decl? match = Project.GetDecl(DeclId!);

        if (match is null)
        {
            return;
        }
        
        if (match.Keys.TryRemove(localKey.Name, out Key? key))
        {
            foreach (KeyValuePair<Languages, LangData> x in LangsData.Langs)
            {
                x.Value.Data.TryRemove(newKeyCopy, out _);
                x.Value.FocusData.TryRemove(newKeyCopy, out _);
            }

            await Owner.SaveLanguages();
            await Owner.SaveProject();
            
            if (newKeyCopy != NewKey)
            {
                await Owner.DeleteKey(key.Name);
            }

            if (Search is not null)
            {
                Owner.SetSearch(Search);                
            }

            Owner.LastAddedKey = KeyToFocus;
            Owner.LastFocusedInput = $"input_CS_{KeyToFocus}";
            Owner.RecomputeVisibleKeys(true, KeyToFocus);
            Owner.InputToFocus = $"input_CS_{KeyToFocus}";
            Owner.StateHasChanged();
        }
    }
}