using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace TeReoLocalizer.Shared.Code;

public static class Extensions
{
    public static bool IsNullOrWhiteSpace([NotNullWhen(returnValue: false)] this string? value)
    {
        return value == null || value.All(char.IsWhiteSpace);
    }
    
    public static T? JsonDecode<T>(this string? s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(s);
        }
        catch (Exception e)
        {
            return default;
        }
    }
}