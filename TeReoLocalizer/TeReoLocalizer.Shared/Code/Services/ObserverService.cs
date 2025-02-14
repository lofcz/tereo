using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace TeReoLocalizer.Shared.Code.Services;

public static class ObserverService
{
    static IMemoryCache Cache => GlobalServices.Cache;

    static readonly ConcurrentDictionary<string, ObservedUser> Data = [];

    public static ObservedUser GetUserData(string id)
    {
        return Data.TryGetValue(id, out ObservedUser? data) ? data : new ObservedUser(string.Empty);
    }

    public static void SetUserData(string id, ObservedUser data)
    {
        Data.TryAdd(id, data);
    }
}