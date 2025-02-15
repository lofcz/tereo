using System.Text.Json;
using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Code;
using TeReoLocalizer.Shared.Code.Services;
using TeReoLocalizer.Shared.Components.Pages.Owned;

namespace TeReoLocalizer.Shared.Components.Pages;

public partial class Localize
{
    public async Task SaveUserSettings()
    {
        await File.WriteAllTextAsync($"{basePath}\\TeReo\\userSettings.json", Settings.ToJson());
    }
    
    async Task UpgradeProjectIfNeeded()
    {
        if (Project.NeedsUpgrade)
        {
            await SaveProject();
        }
    }

    async Task ForceSave()
    {
        await SaveProject();
        await SaveLanguages();
        await Js.Toast(ToastTypes.Success, "Projekt uložen");
    }
    
    public async Task SaveProject()
    {
        if (Project.NeedsUpgrade)
        {
            await Js.Toast(ToastTypes.Success, $"Schéma projektu aktualizováno z verze {Project.SchemaVersion} na verzi {Project.LatestVersion}");
            Project.SchemaVersion = Project.LatestVersion;
            Project.VersionMajor = Project.LatestVersionMajor;
            Project.VersionMinor = Project.LatestVersionMinor;
            Project.VersionPatch = Project.VersionPatch;
        }

        string data = Project.ToJson(true);
        await File.WriteAllTextAsync($"{basePath}/TeReo/project.json", data);
    }
    
    void Panic(string message, bool render = true)
    {
        showLoadErrors = render;
        projectLoadingFinished = true;
        loadErrors ??= [];
        loadErrors.Add(new ProjectError(message));
        ready = true;
        StateHasChanged();
    }
    
    async Task LoadProject()
    {
        ObservedUser data = User.Data();
        
        if (!data.ProjectId.IsNullOrWhiteSpace())
        {
            openProject = await BootService.GetProject(data.ProjectId);
            
            if (openProject is not null)
            {
                basePath = openProject.Path;
            }
        }

        CommandManager.OnBeforeJump = () =>
        {
            JumpingInHistory = true;
            EnableRendering = false;
            return Task.CompletedTask;
        };

        CommandManager.OnAfterJump = () =>
        {
            JumpingInHistory = false;
            EnableRendering = true;
            StateHasChanged();
            return Task.CompletedTask;
        };

        CommandManager.OnBeforeRewindProgressCommand = (action, cmd) =>
        {
            if (RewindModalRef is not null)
            {
                RewindModalRef.Close();
                RewindModalRef = null;
            }
            
            RewindModalRef = Md.ShowModal<LocalizeActionRewindModal>(new
            {
                Owner = this,
                Action = action,
                Command = cmd
            });

            return Task.CompletedTask;
        };

        CommandManager.OnAfterRewindProgressCommand = (action, cmd) =>
        {
            if (RewindModalRef is not null)
            {
                RewindModalRef.Close();
                RewindModalRef = null;
            }
            
            return Task.CompletedTask;
        };
        
        await ShowLanguages(false);

        if (!Directory.Exists(Path.Join(basePath, "TeReo")))
        {
            Nm.NavigateTo("/setup");
            Panic("Projekt nenalezen", false);
            return;
        }

        if (File.Exists($"{basePath}/TeReo/project.json"))
        {
            string currentData = await File.ReadAllTextAsync($"{basePath}/TeReo/project.json");
            Project = JsonSerializer.Deserialize<Project>(currentData) ?? new Project();
        }
        else if (File.Exists($"{basePath}/TeReo/decl.json"))
        {
            // old structure
            string currentData = await File.ReadAllTextAsync($"{basePath}/TeReo/decl.json");
            Decl oldDecl = JsonSerializer.Deserialize<Decl>(currentData) ?? new Decl();
            Project.Decls.Add(oldDecl);
        }
        else
        {
            Nm.NavigateTo("/setup");
            Panic("Projekt nenalezen", false);
            return;
        }

        if (Project.SchemaVersion != Project.LatestVersion)
        {
            string[] parts = Project.SchemaVersion.Split('.');

            if (parts.Length is not 3)
            {
                Panic($"Verze projektu {Project.SchemaVersion} není v platném formátu X.Y.Z");
                return;
            }

            if (!int.TryParse(parts[0], out int major))
            {
                Panic($"Část verze projektu {parts[0]} není platné číslo");
            }

            Project.VersionMajor = major;

            if (!int.TryParse(parts[1], out int minor))
            {
                Panic($"Část verze projektu {parts[1]} není platné číslo");
            }

            Project.VersionMinor = minor;

            if (!int.TryParse(parts[2], out int patch))
            {
                Panic($"Část verze projektu {parts[2]} není platné číslo");
            }

            Project.VersionPatch = patch;

            if (Panicked)
            {
                return;
            }

            if (major > Project.LatestVersionMajor || minor > Project.LatestVersionMinor)
            {
                Panic($"Verze TeReo, kterou používáte je zastaralá. Aktualizujte prosím z {Project.LatestVersionMajor}.{Project.LatestVersionMinor}.{Project.LatestVersionPatch} na ^{major}.{minor}.0 pro načtení tohoto projektu");
                return;
            }
        }
        else
        {
            Project.VersionMajor = Project.LatestVersionMajor;
            Project.VersionMinor = Project.LatestVersionMinor;
            Project.VersionPatch = Project.LatestVersionPatch;
        }

        if (Panicked)
        {
            return;
        }

        projectLoadingFinished = true;
        Project.SelectedDecl = Project.Decls.FirstOrDefault() ?? new Decl();

        if (File.Exists($"{basePath}/TeReo/userSettings.json"))
        {
            Settings = (await File.ReadAllTextAsync($"{basePath}/TeReo/userSettings.json")).JsonDecode<UserSettings>() ?? Settings;

            if (Settings.ShowLangs is not null)
            {
                foreach (KeyValuePair<Languages, LangData> x in LangsData.Langs)
                {
                    x.Value.Visible = Settings.ShowLangs.Contains(x.Key);
                }
            }
        }

        if (Consts.Cfg.Experimental)
        {
            synchronizingIndex = true;

            AsyncService.Fire(async () =>
            {
                Index.SynchronizeIndex(Decl.Keys.Select(x => new IndexDocument
                {
                    Id = x.Value.Id,
                    Content = x.Value.Name
                }).ToList());

                synchronizingIndex = false;
                StateHasChanged();
            });
        }

        foreach (Decl x in Project.Decls)
        {
            foreach (KeyValuePair<string, Key> key in x.Keys)
            {
                key.Value.Owner = x;
            }
        }

        if (!Settings.SelectedDecl.IsNullOrWhiteSpace())
        {
            Decl? matchingDecl = Project.Decls.FirstOrDefault(x => x.Id == Settings.SelectedDecl);

            if (matchingDecl is not null)
            {
                Project.SelectedDecl = matchingDecl;
            }
        }

        ApplySearch(true, false);
        ready = true;
    }
}