using Microsoft.Extensions.Caching.Memory;

namespace TeReoLocalizer.Shared.Code.Services;

public class GlobalServices
{
    public static IMemoryCache Cache => Program.Cache;
    public static IWebHostEnvironment Env => Program.Env;
}