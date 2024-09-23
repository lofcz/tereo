using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace TeReoLocalizer.Shared.Code;

public static class Extensions
{
    public static bool IsNullOrWhiteSpace([NotNullWhen(returnValue: false)] this string? value)
    {
        return value == null || value.All(char.IsWhiteSpace);
    }

    public static string ToBaseLatin(this string str, bool toLower = true)
    {
        byte[] tempBytes = Encoding.GetEncoding("ISO-8859-8").GetBytes(toLower ? str.ToLowerInvariant().Trim() : str);
        return Encoding.UTF8.GetString(tempBytes);
    }

    private static readonly JsonSerializerOptions defaultOptionsPretty = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    private static readonly JsonSerializerOptions defaultOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    public static string? GetStringValue(this Enum e, string key)
    {
        return StringEnum.GetStringValue(e, key);
    }
    
    public static string? GetStringValue(this Enum? e)
    {
        return StringEnum.GetStringValue(e);
    }
    
    public static T? GetTypeValue<T>(this Enum e, string key)
    {
        return StringEnum.GetTypeValue<T>(e, key);
    }
        
    public static T? GetTypeValue<T>(this Enum e)
    {
        return StringEnum.GetTypeValue<T>(e);
    }
    
    public static string FirstLetterToUpper(this string? str)
    {
        if (str is null)
        {
            return string.Empty;
        }

        if (str.Length > 1)
        {
            return char.ToUpper(str[0]) + str[1..];
        }

        return str.ToUpper();
    }
    
    public static string? ToCsv(this IEnumerable? elems, string separator = ",")
    {
        if (elems == null)
        {
            return null;
        }

        StringBuilder sb = new StringBuilder();
        foreach (object elem in elems)
        {
            if (sb.Length > 0)
            {
                sb.Append(separator);
            }

            if (elem is Enum)
            {
                sb.Append((int)elem);
            }
            else
            {
                sb.Append(elem);   
            }
        }

        return sb.ToString();
    }
    
    public static string ToJson(this object? obj, bool prettify = false)
    {
        return obj is null ? "{}" : JsonSerializer.Serialize(obj, prettify ? defaultOptionsPretty : defaultOptions);
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