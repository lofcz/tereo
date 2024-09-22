using Microsoft.Extensions.Caching.Memory;
using TeReoLocalizerWeb;

namespace TeReoLocalizer;

public static class Program
{
     public static WebApplication Web;
        
        public static async Task Main(string[] args)
        {
            await BuildWeb(args);
        }

        public static WebApplication CreateWebHostBuilder(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            WebConfig startup = new WebConfig();
            startup.ConfigureServices(builder.Services);

            builder.Logging.AddConsole();
            
            builder.WebHost.ConfigureKestrel(x =>
            {
                x.AddServerHeader = false;
                x.Limits.MaxRequestBodySize = 524288000; //500MB
                x.Limits.MaxRequestBufferSize = 524288000;
                x.Limits.MaxRequestLineSize = 524288000;
            });
            
            WebApplication app = builder.Build();
            IWebHostEnvironment env = app.Services.GetService<IWebHostEnvironment>()!;
            IMemoryCache cache = app.Services.GetService<IMemoryCache>()!;
            IConfiguration config = app.Services.GetService<IConfiguration>()!;

            WebConfig.Configure(app, env, cache, config);
            return app;
            
            /*int z = 0;
            
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) => { logging.AddConsole(); })
                .ConfigureKestrel(x =>
                {
                    x.AddServerHeader = false;
                    x.Limits.MaxRequestBodySize = 524288000; //500MB
                    x.Limits.MaxRequestBufferSize = 524288000;
                    x.Limits.MaxRequestLineSize = 524288000;
                })
                //.UseStaticWebAssets()
                .UseStartup<WebConfig>();*/
        }
        
        public static async Task BuildWeb(string[] args)
        {
            WebApplication builder = CreateWebHostBuilder(args);
            Web = builder;//.Build();
            await Web.RunAsync();
        }
}