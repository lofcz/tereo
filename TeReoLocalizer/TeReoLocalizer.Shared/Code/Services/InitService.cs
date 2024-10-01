using System.Text;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace TeReoLocalizer.Shared.Code.Services;

public static class InitService
{
    public static LiveSyncMSBuildWorkspace? Workspace { get; set; }
    
    public static async Task Init()
    {
        EncodingProvider provider = CodePagesEncodingProvider.Instance;
        Encoding.RegisterProvider(provider);
        MSBuildLocator.RegisterDefaults();

        string solutionPath = Consts.Cfg.Sln;

        if (false && File.Exists(solutionPath))
        {
            Workspace = new LiveSyncMSBuildWorkspace();
            await Workspace.OpenSolutionAsync(solutionPath);
        }
    }
}