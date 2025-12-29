using System.Xml.Linq;

namespace TeReoLocalizer.Shared.Code.Services;

public static class MsBuildService
{
    /// <summary>
    /// Attempts to resolve RootNamespace of a given project.
    /// This is designed after https://github.com/dotnet/msbuild/blob/9b15d8bf992c29ea610d525435088c9127e623d6/src/Build/Evaluation/Evaluator.cs#L1184
    /// and https://github.com/dotnet/sdk/blob/5fc518d421255218cf575a517ee403eb5ed38398/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.Sdk.props#L42
    /// </summary>
    /// <param name="projectFile"></param>
    /// <returns></returns>
    public static async Task<string?> GetRootNamespace(string projectFile)
    {
        if (!File.Exists(projectFile))
        {
            return null;
        }
        
        await using FileStream stream = File.OpenRead(projectFile);
        XDocument projectXml = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
        string? rootNs = projectXml
            .Descendants("PropertyGroup")
            .Descendants("RootNamespace")
            .FirstOrDefault()?.Value;
        
        if (!string.IsNullOrEmpty(rootNs))
        {
            return rootNs;
        }
        
        string projectName = Path.GetFileNameWithoutExtension(projectFile);
        return projectName.Replace(" ", string.Empty).Replace("-", string.Empty);
    }
}