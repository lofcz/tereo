using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Web.WebView2.Core;
using TeReoLocalizer.Shared;
using TeReoLocalizer.Shared.Code.Services;

namespace TeReoLocalizer;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		AsyncService.Fire(async () =>
		{
			await InitService.Init();
		});
		
		Console.WriteLine(typeof(MauiProgram).Assembly);
		
		MauiAppBuilder builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		Program.AddSharedServices(builder.Services);
		
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
