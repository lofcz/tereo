using System.Diagnostics.CodeAnalysis;

namespace TeReoLocalizer.Shared.Code.Services;

public static class GitService
{
    public static bool IsRepository(string path, [NotNullWhen(returnValue: true)] out string? rootPath)
    {
        if (Directory.Exists(Path.Combine(path, ".git")))
        {
            rootPath = path;
            return true;
        }
        
        DirectoryInfo directory = new DirectoryInfo(path);
        
        while (directory.Parent is not null)
        {
            if (Directory.Exists(Path.Combine(directory.Parent.FullName, ".git")))
            {
                rootPath = directory.Parent.FullName;
                return true;
            }
        
            directory = directory.Parent;
        }

        rootPath = null;
        return false;
    }
}