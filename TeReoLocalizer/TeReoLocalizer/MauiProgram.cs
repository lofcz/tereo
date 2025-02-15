using System.Security.Claims;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
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
using TeReoLocalizer.Shared.Components;

namespace TeReoLocalizer;

public static class MauiProgram
{
	public static Microsoft.UI.Xaml.Window Window { get; set; }

	public static void Maximize()
	{
#if WINDOWS
			IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(Window);  
			WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);  
			AppWindow? appWindow = AppWindow.GetFromWindowId(myWndId);
			appWindow?.SetPresenter(AppWindowPresenterKind.Overlapped);

			HWND hwnd = new HWND(hWnd);
			PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_MAXIMIZE);
#endif
	}
	
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
					Window = window;
					SharedProxy.Maximize = Maximize;
#if WINDOWS
					window.ExtendsContentIntoTitleBar = false;  
					IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);  
					WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);  
					AppWindow? appWindow = AppWindow.GetFromWindowId(myWndId);
					appWindow?.SetPresenter(AppWindowPresenterKind.Overlapped);

					HWND hwnd = new HWND(hWnd);
					
					int screenWidth = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
					int screenHeight = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);
					
					PInvoke.GetWindowRect(hwnd, out RECT rect);
					int windowWidth = rect.right - rect.left;
					int windowHeight = rect.bottom - rect.top;
					
					int x = (screenWidth - windowWidth) / 2;
					int y = (screenHeight - windowHeight) / 2;
					
					PInvoke.SetWindowPos(hwnd, HWND.Null, x, y, windowWidth, windowHeight, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

					PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOW);
#endif
				});
			});  
		});  
		
		MauiApp app = builder.Build();
		return app;
	}
}
