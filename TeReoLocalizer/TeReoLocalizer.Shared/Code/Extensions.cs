using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlazingModal;
using BlazingModal.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TeReoLocalizer.Shared.Components.Shared;

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
    
    public static IEnumerable<string> GetFiles(this string path, IEnumerable<string> searchPatterns, SearchOption searchOption = SearchOption.TopDirectoryOnly, Func<string, bool>? filter = null)
    {
        return searchPatterns.AsParallel().SelectMany(searchPattern => Directory.EnumerateFiles(path, searchPattern, searchOption).Where(x => filter is not null && filter(x)));
    }
    
    private static JsonSerializerOptions IndendedSerializer = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
        
    private static JsonSerializerOptions UnindendedSerializer = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string ToJson(this object data, bool prettify = false)
    {
        return JsonSerializer.Serialize(data, prettify ? IndendedSerializer : UnindendedSerializer);
    }
    
    public static string FirstLetterToLower(this string? str)
    {
        if (str == null)
        {
            return "";
        }

        if (str.Length > 1)
        {
            return char.ToLower(str[0]) + str[1..];
        }

        return str.ToLower();
    }
    
    private static readonly Regex HtmlHeuristicRegex = new Regex("<\\s*([a-z][a-z0-9]*)\\b[^>]*>(.*?<\\s*\\/\\s*\\1\\s*>)?", RegexOptions.Compiled);

    public static bool ProbablyContainsHtml(this string? str)
    {
        return str is not null && HtmlHeuristicRegex.IsMatch(str);
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
    
    public static bool Implements<T>(this Type source)
    {
        return source.IsAssignableTo(typeof(T));
    }
    
    public static void AddOrUpdate<TKey, TVal>(this Dictionary<TKey, TVal> dict, TKey key, TVal val) where TKey : notnull
    {
        dict[key] = val;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="service"></param>
    /// <param name="title"></param>
    /// <param name="descriptionViewParams"></param>
    /// <param name="confirmAction"></param>
    /// <param name="size"></param>
    /// <typeparam name="T">A <see cref="IDescriptionModal"/> modal</typeparam>
    public static void ShowConfirmActionModal<T>(this IModalService? service, string title, Dictionary<string, object?> descriptionViewParams, Func<Task> confirmAction, ModalSizes size = ModalSizes.Medium) where T : IDescriptionModal
    {
        ShowModal<ConfirmActionModal>(service, new Dictionary<string, object?> {{"Title", title}, {"BodyComponent", new DynamicComponentInfo { Type = typeof(T), Params = new Dictionary<string, object?> { {"Params", descriptionViewParams} }}}, {"ConfirmAction", confirmAction}, {"Size", size}});
    }
    
    public static void ShowSelectActionModal(this IModalService? service, string title, string description, IEnumerable<ModalAction> actions, ModalSizes size = ModalSizes.Medium)
    {
        ShowModal<SelectActionModal>(service, new Dictionary<string, object?> {{"Title", title}, {"Description", description}, {"Actions", actions}, {"Size", size}});
    }
    
    public static void ShowSelectActionModal<T>(this IModalService? service, string title, Dictionary<string, object?> descriptionViewParams, IEnumerable<ModalAction> actions, ModalSizes size = ModalSizes.Medium, bool autoScroll = true) where T : IDescriptionModal
    {
        ShowModal<SelectActionModal>(service, new Dictionary<string, object?> {{"DescriptionAutoScroll", autoScroll}, {"Title", title}, {"BodyComponent", new DynamicComponentInfo { Type = typeof(T), Params = new Dictionary<string, object?> { {"Params", descriptionViewParams} }}}, {"Description", ""}, {"Actions", actions}, {"Size", size}});
    }
    
    public static void ShowSelectActionModal<T>(this IModalService? service, string title, Dictionary<string, object?> descriptionViewParams, Func<string, object?, Task> notificationAction, IEnumerable<ModalAction> actions, ModalSizes size = ModalSizes.Medium, bool autoScroll = true) where T : IDescriptionModal
    {
        ShowModal<SelectActionModal>(service, new Dictionary<string, object?> {{"DescriptionAutoScroll", autoScroll}, {"Title", title}, {"NotificationAction", notificationAction}, {"BodyComponent", new DynamicComponentInfo { Type = typeof(T), Params = new Dictionary<string, object?> { {"Params", descriptionViewParams} }}}, {"Description", ""}, {"Actions", actions}, {"Size", size}});
    }
    
    public static void ShowConfirmActionModal(this IModalService? service, string title, string description, Func<Task> confirmAction, ModalSizes size = ModalSizes.Medium)
    {
        ShowModal<ConfirmActionModal>(service, new Dictionary<string, object?> {{"Title", title}, {"Description", description}, {"ConfirmAction", confirmAction}, {"Size", size}});
    }
    
    public static void ShowConfirmActionModal(this IModalService? service, string title, Func<Task> confirmAction, ModalSizes size = ModalSizes.Medium)
    {
        ShowModal<ConfirmActionModal>(service, new Dictionary<string, object?> {{"Title", title}, {"ConfirmAction", confirmAction}, {"Size", size}});
    }

    
    public static void ShowConfirmActionModal(this IModalService? service, string title, Func<Task<string>> description, Func<Task> confirmAction, ModalSizes size = ModalSizes.Medium)
    {
        ShowModal<ConfirmActionModal>(service, new Dictionary<string, object?> {{"Title", title}, {"DescriptionFn", description}, {"ConfirmAction", confirmAction}, {"Size", size}});
    }
    
    public static void ShowConfirmActionModal(this IModalService? service, string title, string description, Func<Task> confirmAction, Func<Task> cancelAction, ModalSizes size = ModalSizes.Medium, Button? confirmButton = null, Button? cancelButton = null)
    {
        ShowModal<ConfirmActionModal>(service, new Dictionary<string, object?> {{"Title", title}, {"Description", description}, {"ConfirmAction", confirmAction}, {"CancelAction", cancelAction}, {"Size", size}, {"ConfirmButton", confirmButton}, {"CancelButton", cancelButton}});
    }
    
    public static void ShowPromptModal(this IModalService? service, string title, string? description, Func<string, Task> confirmAction, Func<Task>? cancelAction = null, ModalSizes size = ModalSizes.Medium, Button? confirmButton = null, Button? cancelButton = null, string? defaultText = null, string? inputPlaceholder = null)
    {
        ShowModal<PromptModal>(service, new Dictionary<string, object?> {{"Title", title}, {"Description", description}, {"ConfirmAction", confirmAction}, {"CancelAction", cancelAction}, {"Size", size}, {"ConfirmButton", confirmButton}, {"CancelButton", cancelButton}, {"DefaultText", defaultText}, {"Placeholder", inputPlaceholder}});
    }
    
    public static string ToProgressPercent(this double value)
    {
        return value.ToString(Math.Abs(value - Math.Floor(value)) < 0.0001d ? "0" : "0.##", CultureInfo.InvariantCulture);
    }
    
    private static readonly ModalOptions CustomLayoutOpts = new ModalOptions
    {
        UseCustomLayout = true,
        AnimationType = ModalAnimationType.None
    };
    
    public static void ShowModal<T>(this IModalService? service, IDictionary<string, object?>? pars = null)
    {
        ModalParameters mp = new ModalParameters()
        {
            { nameof(GenericModal.RenderFragmentType), typeof(T) }
        };

        if (pars != null)
        {
            mp.Add(nameof(GenericModal.RenderFragmentParams), pars);
        }

        service?.Show<GenericModal>("", mp, CustomLayoutOpts);
    }
    
    public static void ShowModal<T>(this IModalService? service, object? pars)
    {
        service.ShowModal<T>(pars?.ToDictionary());
    }
        
    public static void ShowModal<T>(this IModalService? service)
    {
        service.ShowModal<T>(null);
    }

    public static IDictionary<string, object?>? ToDictionary(this object? obj)
    {
        return obj is null ? null : HtmlHelper.ObjectToDictionary(obj);
    }
}