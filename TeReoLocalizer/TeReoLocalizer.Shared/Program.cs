using Microsoft.AspNetCore.Diagnostics;
using TeReoLocalizer.Shared.Components;

namespace TeReoLocalizer.Shared;

public class Program
{
    public static void Main(string[] args)
    {
        string appType = "WEB";
        
        foreach (var arg in args)
        {
            if (arg.StartsWith("--appType="))
            {
                appType = arg.Substring("--appType=".Length);
            }
        }

        // Použití appType pro podmíněnou logiku
        if (appType == "MAUI")
        {
            SharedProxy.IsMaui = true;
        }
        else
        {
            SharedProxy.IsMaui = false;
        }
        
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents(x =>
            {
                x.DetailedErrors = true;
            });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/html";

                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;

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

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}