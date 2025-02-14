using System.CommandLine;
using System.Text;
using BlazingModal;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using TeReoLocalizer.Shared.Code;
using TeReoLocalizer.Shared.Code.Services;
using TeReoLocalizer.Shared.Components;

namespace TeReoLocalizer.Shared;

public class MauiHostingEnvironment : IWebHostEnvironment
{
    public MauiHostingEnvironment()
    {
    }

    public string EnvironmentName { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

public class Program
{
    public static InvertedIndex Index;
    public static IMemoryCache Cache;
    public static IWebHostEnvironment Env;
    
    public static void AddSharedServices(IServiceCollection services)
    {
        services.AddScoped<ILoginService, LoginService>();
        
        services.AddMemoryCache();
        services.AddMvc(x => x.EnableEndpointRouting = false);
        services.AddRazorPages();
        services.AddHttpContextAccessor();
        services.AddBlazingModal();

        services.AddAuthentication(x =>
        {
            x.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        }).AddCookie(x =>
        {
            x.Cookie.Name = "reoSession";
            x.SlidingExpiration = true;
            x.ExpireTimeSpan = TimeSpan.FromDays(365);
            x.LoginPath = "/home/index";
            x.LogoutPath = "/home/index";
            x.Cookie.SameSite = SameSiteMode.None;
            x.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            x.Cookie.MaxAge = TimeSpan.FromDays(365);
            x.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = async (CookieValidatePrincipalContext ctx) =>
                {

                }
            };
        });
        
        DirectoryInfo path = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys"));

        if (!path.Exists)
        {
            path.Create();
        }
            
        services.AddDataProtection()
            .PersistKeysToFileSystem(path)
            .AddKeyManagementOptions(options =>
            {
                options.NewKeyLifetime = new TimeSpan(360, 0, 0, 0);
                options.AutoGenerateKeys = true;
            })
            .SetApplicationName("Reo")
            .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration { EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC, ValidationAlgorithm = ValidationAlgorithm.HMACSHA256 });
        
        services.AddOptions();
        services.AddAuthorizationCore();
        
        services.ConfigureExternalCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.None;
        });

        services.AddAntiforgery();
    }
    
    public static async Task Main(string[] args)
    {
        await InitService.Init();

        if (Consts.Cfg.Experimental)
        {
            Index = new InvertedIndex($"{Consts.Cfg.Repository}/.reoindex");   
        }
        
        Option<string> appTypeOption = new Option<string>(
            name: "--appType",
            description: "Shell to use, supported values are: WEB,MAUI",
            getDefaultValue: () => "WEB");

        Option<FileInfo?> repositoryOption = new Option<FileInfo?>(
            name: "--repository",
            description: "Repository path",
            getDefaultValue: () => null);

        Option<FileInfo?> slnOption = new Option<FileInfo?>(
            name: "--sln",
            description: "Solution file path",
            getDefaultValue: () => null);

        RootCommand rootCommand = new RootCommand("TeReo Localizer")
        {
            TreatUnmatchedTokensAsErrors = false
        };
        
        rootCommand.AddOption(appTypeOption);
        rootCommand.AddOption(repositoryOption);
        rootCommand.AddOption(slnOption);

        rootCommand.SetHandler(ctx =>
        {
            string? appType = ctx.ParseResult.GetValueForOption(appTypeOption);
            FileInfo? repository = ctx.ParseResult.GetValueForOption(repositoryOption);
            FileInfo? sln = ctx.ParseResult.GetValueForOption(slnOption);
            
            SharedProxy.IsMaui = appType is "MAUI";
            SharedProxy.Repository = repository?.FullName;
            SharedProxy.Sln = sln?.FullName;

            Consts.UpdateConfigFromShared();
        });

        await rootCommand.InvokeAsync(args);
        
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents(static x =>
            {
                x.DetailedErrors = true;
            });

        AddSharedServices(builder.Services);

        WebApplication app = builder.Build();
        IHostApplicationLifetime lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        IMemoryCache cache = app.Services.GetRequiredService<IMemoryCache>();
        IWebHostEnvironment env = app.Services.GetRequiredService<IWebHostEnvironment>();
        Cache = cache;
        Env = env;

        lifetime.ApplicationStopping.Register(() =>
        {
            Index.Dispose();

            if (InitService.Workspace is not null)
            {
                InitService.Workspace.Dispose();
            }
        });
        
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/html";

                    IExceptionHandlerPathFeature? exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    Exception? exception = exceptionHandlerPathFeature?.Error;

                    await context.Response.WriteAsync("<html><body>\n");
                    await context.Response.WriteAsync("<h2>Error:</h2>\n");
                    await context.Response.WriteAsync($"<p>{exception?.Message}</p>\n");
                    await context.Response.WriteAsync("<h3>Stack trace:</h3>\n");
                    await context.Response.WriteAsync($"<pre>{exception?.StackTrace}</pre>\n");
                    await context.Response.WriteAsync("</body></html>\n");
                });
            });

            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.UseMvcWithDefaultRoute();
        
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        await app.RunAsync();
    }
}