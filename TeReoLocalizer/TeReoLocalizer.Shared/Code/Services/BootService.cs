namespace TeReoLocalizer.Shared.Code.Services;

public static class BootService
{
    public static async Task<bool> RemoveProject(string id)
    {
        BootData data = await GetBootData();
        BootDataProject? project = data.Projects.FirstOrDefault(x => x.Id == id);

        if (project is not null)
        {
            data.Projects.Remove(project);
            await SaveBootData(data);
            return true;
        }
        
        return false;
    }
    
    public static async Task<BootDataProject?> GetProject(string id)
    {
        BootData data = await GetBootData();
        BootDataProject? project = data.Projects.FirstOrDefault(x => x.Id == id);
        return project;
    }
    
    public static async Task<BootData> GetBootData()
    {
        bool created = false;

        if (!File.Exists("reoBoot.json"))
        {
            created = true;
            File.Create("reoBoot.json");
        }

        string bootDataStr = await File.ReadAllTextAsync("reoBoot.json"); 
        BootData data = bootDataStr.JsonDecode<BootData>() ?? new BootData();

        if (created)
        {
            await SaveBootData(data);
        }

        return data;
    }

    public static async Task SaveBootData(BootData data)
    {
        await File.WriteAllTextAsync("reoBoot.json", data.ToJson(true));
    }

    static string MoveUp(string path, int levels = 1)
    {
        string parentPath = path.TrimEnd('/', '\\');
        
        for (int i = 0; i < levels; i++)
        {
            DirectoryInfo? up = Directory.GetParent(parentPath);

            if (up is not null)
            {
                parentPath = up.ToString();   
            }
        }
        
        return parentPath;
    }
    
    public static async Task<BootDataProject> LogProjectOpened(string csprojPath)
    {
        BootData data = await GetBootData();
        BootDataProject? existingProject = data.Projects.FirstOrDefault(x => x.Csproj == csprojPath);

        if (existingProject is not null)
        {
            existingProject.LastRan = DateTime.Now;
            await SaveBootData(data);
            return existingProject;
        }

        BootDataProject newProject = new BootDataProject
        {
            LastRan = DateTime.Now,
            Csproj = csprojPath,
            Path = MoveUp(csprojPath),
            Id = General.IIID(),
            Name = Path.GetFileName(csprojPath)
        };
        
        data.Projects.Add(newProject);
        await SaveBootData(data);
        return newProject;
    }
}