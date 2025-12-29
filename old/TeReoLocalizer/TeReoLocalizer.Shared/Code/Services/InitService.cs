using System.Text;
using Microsoft.Build.Locator;

namespace TeReoLocalizer.Shared.Code.Services;

public static class InitService
{
    public static LiveSyncMSBuildWorkspace? Workspace { get; set; }
    
    public static async Task Init()
    {
        EncodingProvider provider = CodePagesEncodingProvider.Instance;
        Encoding.RegisterProvider(provider);
        MSBuildLocator.RegisterDefaults();
    }
}