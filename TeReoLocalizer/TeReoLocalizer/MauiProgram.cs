using Microsoft.Extensions.Logging;

namespace TeReoLocalizer;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		Console.WriteLine(typeof(MauiProgram).Assembly);
		
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
