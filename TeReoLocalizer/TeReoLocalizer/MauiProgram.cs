using System.Security.Claims;
using Windows.Win32;
using Windows.Win32.Foundation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using TeReoLocalizer.Shared;
using TeReoLocalizer.Shared.Code;
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
		
		        
		builder.Services.AddSingleton<IWebHostEnvironment>(x =>
		{
			return new MauiHostingEnvironment
			{
               
			};
		});
		
		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddScoped<AuthenticationStateProvider, MauiAuthenticationStateProvider>();
		
		Program.AddSharedServices(builder.Services);
		
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif
		
		builder.ConfigureLifecycleEvents(events =>  
		{  
			events.AddWindows(wndLifeCycleBuilder =>  
			{  
				wndLifeCycleBuilder.OnWindowCreated(window =>  
				{  
					#if WINDOWS
					window.ExtendsContentIntoTitleBar = false;  
					IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);  
					WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);  
					AppWindow? appWindow = AppWindow.GetFromWindowId(myWndId);
					appWindow?.SetPresenter(AppWindowPresenterKind.Overlapped);

					HWND hwnd = new HWND(hWnd);
					PInvoke.ShowWindow(hwnd, Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD.SW_MAXIMIZE);
					
					#endif
				});
			});  
		});  
		
		MauiApp app = builder.Build();
		return app;
	}
}
