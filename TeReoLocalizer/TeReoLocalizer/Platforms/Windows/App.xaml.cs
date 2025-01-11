using Microsoft.UI.Xaml;
using TeReoLocalizer.Shared.Components;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TeReoLocalizer.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		base.OnLaunched(args);

		string[] args2 = Environment.GetCommandLineArgs();
		
		// Zpracování argumentů příkazového řádku
		string appType = "WEB"; // Default value

		if (args.Arguments != null)
		{
			string[]? arguments = args.Arguments.Split(' ');
			foreach (string? arg in arguments)
			{
				if (arg.StartsWith("--appType="))
				{
					appType = arg.Substring("--appType=".Length);
				}
			}
		}

		// Použití appType pro podmíněnou logiku
		if (appType == "MAUI")
		{
			SharedProxy.IsMaui = true;
		}
		else
		{
			// Specifická logika pro WEB
		}
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

