using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace TeReoLocalizerWeb;

public class StartupConfig
{
    public bool Electron { get; set; }
}

public class WebConfig
{
    public static IConfiguration Configuration { get; set; }
    public static IMemoryCache Cache { get; set; }
    public static IWebHostEnvironment Env { get; set; }
    public static bool IsElectron { get; set; }
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSignalR(x =>
        {
            x.EnableDetailedErrors = true;
        });

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddLogging();
        services.AddLocalization();
        
        services.AddMvc(x => x.EnableEndpointRouting = false);
        services.AddRazorPages();
        services.AddHttpContextAccessor();
        services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        services.AddHttpClient<HttpClient>(x =>
        {
            x.Timeout = TimeSpan.FromSeconds(80);
        });
        
        services.AddOptions();
        services.AddAuthorizationCore();
        
        services.ConfigureExternalCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.None;
        });
        
        services.ConfigureApplicationCookie(x =>
        {   
            x.Cookie.Name = "edumap_xs";         
            x.ExpireTimeSpan = TimeSpan.FromDays(365);
            x.SlidingExpiration = true;
            x.Cookie.SameSite = SameSiteMode.None;
        });
        
        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddHubOptions(x =>
            {
                x.MaximumReceiveMessageSize = 32 * 1024;
            });
    }

    public static void Configure(WebApplication app, IWebHostEnvironment env, IMemoryCache cache, IConfiguration config)
    {
        Cache = cache;
        Env = env;
        Configuration = config;
        IsElectron = config.GetValue("ELECTRON", false);
        
        //app.UseResponseCompression();
        
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseCookiePolicy(new CookiePolicyOptions
        {
            MinimumSameSitePolicy = SameSiteMode.Lax
        });
        
        app.UseRequestLocalization();

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/secure", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 404;
                context.Response.ContentLength = 0;
                context.Response.Body = Stream.Null;
                return;
            }
            
            await next.Invoke();
        });
        
        app.UseStaticFiles(new StaticFileOptions
        {
            
        });
        app.UseStaticFiles(new StaticFileOptions // force serve all files in /wwwroot/Uploads/
        {
            FileProvider =  new PhysicalFileProvider(Path.Combine(env.WebRootPath, "Uploads")),
            RequestPath = "/storage",
            ServeUnknownFileTypes = true
        });
        
        /*app.UseDirectoryBrowser(new DirectoryBrowserOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath, "Uploads")),
            RequestPath = "/storage"
        });*/
        
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        //app.UseResponseCompression();
        app.UseMvcWithDefaultRoute();
        
        // todo: figure out the fallback
        /*app.UseEndpoints(x =>
        {
            x.MapHub<MasterHub>("/masterhub", y =>
            {
                y.AllowStatefulReconnects = true;
            });
            x.MapRazorComponents<AppHost>()
                .DisableAntiforgery()
                .AddInteractiveServerRenderMode();
        });*/
        
        app.UseEndpoints(x =>
        {
            x.MapBlazorHub();
            x.MapFallbackToPage("/_Host");
        });
    }
}