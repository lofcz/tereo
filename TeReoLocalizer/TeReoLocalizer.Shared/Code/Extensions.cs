using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlazingModal;
using BlazingModal.Services;
using EnumsNET;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using TeReoLocalizer.Annotations;
using TeReoLocalizer.Shared.Code.Services;
using TeReoLocalizer.Shared.Components.Shared;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
    
    public static T? JsonDecode<T>(this JsonElement el)
    {
        return el.GetRawText().JsonDecode<T>();
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
    
    [return: NotNullIfNotNull(nameof(t))]
    public static T? DeepClone<T>(this T? t)
    {
        return FastCloner.FastCloner.DeepClone(t);
    }
    
    public static bool ContainsOnlyWhitelistChars(this string s, HashSet<char> chars)
    {
        return s.All(chars.Contains);
    }
    
    public static bool IsValidEmail(this string str)
    {
        return new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(str);
    }
    
    public static MemoryStream ToMemoryStream(this string s)
    {
        MemoryStream stream = new MemoryStream();
        StreamWriter writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
    
    public static object? ChangeType(this object? value, Type conversion) 
    {
        Type? t = conversion;

        if (t.IsEnum && value != null)
        {
            if (Enums.TryParse(t, value.ToString(), true, out object? x))
            {
                return x;
            }
        }
            
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) 
        {
            if (value == null) 
            { 
                return null; 
            }

            t = Nullable.GetUnderlyingType(t);
        }

        if (t == typeof(int) && value?.ToString() == "")
        {
            return 0;
        }
            
        if (t == typeof(int) && ((value?.ToString()?.Contains('.') ?? false) || (value?.ToString()?.Contains(',') ?? false)))
        {
            if (double.TryParse(value.ToString()?.Replace(",", "."), out double x))
            {
                return (int)x;
            }
        }

        if (value != null && t is {IsGenericType: true} && value.GetType().IsGenericType)
        {
            Type destT = t.GetGenericArguments()[0];
            Type sourceT = value.GetType().GetGenericArguments()[0];

            if (destT.IsEnum && sourceT == typeof(int))
            {
                IList? instance = (IList?)Activator.CreateInstance(t);

                foreach (object? x in (IList) value)
                {
                    instance?.Add(x);
                }

                return instance;
            }
        }

        return t != null ? System.Convert.ChangeType(value, t) : null;
    }

    
    public static IModalReference? ShowModal<T>(this IModalService? service, IDictionary<string, object?>? pars)
    {
        ModalParameters mp = new ModalParameters
        {
            { nameof(GenericModal.RenderFragmentType), typeof(T) }
        };

        if (pars is not null)
        {
            mp.Add(nameof(GenericModal.RenderFragmentParams), pars);
        }

        return service?.Show<GenericModal>("", mp, CustomLayoutOpts);
    }
    
    public static IModalReference? ShowModal<T>(this IModalService? service, object? pars)
    {
        return service.ShowModal<T>(pars?.ToDictionary());
    }
        
    public static IModalReference? ShowModal<T>(this IModalService? service)
    {
        return service.ShowModal<T>(null);
    }

    public static IDictionary<string, object?>? ToDictionary(this object? obj)
    {
        return obj is null ? null : HtmlHelper.ObjectToDictionary(obj);
    }
    
    public static List<int> FromCsv(this string? str, string separator = ",")
    {
        return str == null ? [] : str.Split(separator).Where(m => int.TryParse(m, out int _)).Select(int.Parse).ToList();
    }
    
    public static IList InstantiateList(this Type t)
    {
        Type genericListType = typeof(List<>).MakeGenericType(t);
        return (IList)Activator.CreateInstance(genericListType)!;
    }
    
    static void AddJsonElementStringIfValid<T>(this IList<T> values, JsonElement value)
    {
        string stringVal = value.GetString() ?? "";

        if (typeof(T).IsEnum || typeof(T) == typeof(int) || typeof(T) == typeof(int?))
        {
            if (int.TryParse(stringVal, out int tvalInt))
            {
                values.Add((T)(dynamic)tvalInt);
            }
        }
    }

    static void AddJsonElementNumberIfValid<T>(this IList<T> values, JsonElement value)
    {
        bool isInt = value.TryGetInt32(out int tintVal);

        if (isInt)
        {
            if (typeof(T).IsEnum || typeof(T) == typeof(int) || typeof(T) == typeof(int?))
            {
                values.Add((T)(dynamic)tintVal);
            }

            return;
        }

        bool isDouble = value.TryGetDouble(out double tdoubleVal);

        if (isDouble)
        {
            if (typeof(T) == typeof(double))
            {
                values.Add((T)(dynamic)tdoubleVal);
            }
            else if (typeof(T) == typeof(float))
            {
                values.Add((T)(dynamic)(float)tdoubleVal);
            }
        }
    }
    
    static void AddJsonElementStringIfValid<T>(this IList<T> values, JsonElement value, bool allowDuplicates = true)
    {
        string stringVal = value.GetString() ?? "";

        if (typeof(T).IsEnum || typeof(T) == typeof(int) || typeof(T) == typeof(int?) || typeof(T) == typeof(object))
        {
            if (int.TryParse(stringVal, out int tvalInt))
            {
                T tval = (T)(dynamic)tvalInt;

                if (allowDuplicates)
                {
                    values.Add(tval);   
                }
                else
                {
                    if (!values.Contains(tval))
                    {
                        values.Add(tval);
                    }
                }
            }
        }
        else if (typeof(T) == typeof(string))
        {
            string strVal = value.GetString() ?? string.Empty;
            T tval = (T)(dynamic)strVal;
            
            if (allowDuplicates)
            {
                values.Add(tval);   
            }
            else
            {
                if (!values.Contains(tval))
                {
                    values.Add(tval);
                }
            }
        }
    }
    
    static void AddJsonElementNumberIfValid<T>(this IList<T> values, JsonElement value, bool allowDuplicates = true)
    {
        bool isInt = value.TryGetInt32(out int tintVal);

        if (isInt)
        {
            if (typeof(T).IsEnum || typeof(T) == typeof(int) || typeof(T) == typeof(int?) || typeof(T) == typeof(object))
            {
                T tInt = (T)(dynamic)tintVal;

                if (allowDuplicates)
                {
                    values.Add(tInt);   
                }
                else
                {
                    if (!values.Contains(tInt))
                    {
                        values.Add(tInt);
                    }
                }
            }

            return;
        }

        bool isDouble = value.TryGetDouble(out double tdoubleVal);

        if (isDouble)
        {
            if (typeof(T) == typeof(double))
            {
                values.Add((T)(dynamic)tdoubleVal);
            }
            else if (typeof(T) == typeof(float))
            {
                values.Add((T)(dynamic)(float)tdoubleVal);
            }
        }
    }

    public static void AddJsonElement<T>(this IList<T> values, JsonElement value, bool allowDuplicates = true)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
            {
                AddJsonElementStringIfValid(values, value, allowDuplicates);
                break;
            }
            case JsonValueKind.Number:
            {
                AddJsonElementNumberIfValid(values, value, allowDuplicates);
                break;
            }
            case JsonValueKind.Array:
            {
                foreach (JsonElement el in value.EnumerateArray())
                {
                    AddJsonElement(values, el, allowDuplicates);
                }

                break;
            }
        }
    }
    
    /// <summary>
    /// Creates options usable by <see cref="EdSelect{TValue}"/>.
    /// </summary>
    /// <param name="ignoreFirst">Whether to ignore the first value in the enum</param>
    /// <param name="selected">Which options are preselected</param>
    /// <param name="subset">Limits options to a given subset of source enum options</param>
    /// <param name="includeDescription">Whether to include <see cref="DescriptionAttribute"/> in the options</param>
    /// <param name="ignoreLast">Whether to ignore the last value in the enum</param>
    /// <param name="orderBy">Orders on source enum level</param>
    /// <param name="optionsOrderBy">Orders materialized options</param>
    /// <typeparam name="T">Enum from which the options are sourced</typeparam>
    /// <returns></returns>
    public static List<ISelectOption> EnumerateAsSelectOptions<T>(bool ignoreFirst = false, Func<T, bool>? selected = null, List<T>? subset = null, bool includeDescription = false, bool ignoreLast = false, Func<T, int>? orderBy = null, Expression<Func<ISelectOption, object>>? optionsOrderBy = null) where T : struct, IConvertible
    {
        bool isLocalized = ClrService.EnumGetAttribute<T, LocalizedAttribute>() is not null;
        
        List<ISelectOption> options = Enum.GetValues(typeof(T)).Cast<T>()
            .Where(x => (subset == null || subset.Contains(x)) && (!ignoreFirst || x.ToInt32(null) > 0) && (!ignoreLast || x.ToInt32(null) < Enum.GetValues(typeof(T)).Length - 1))
            .OrderBy(x => orderBy?.Invoke(x) ?? (ClrService.EnumValueHasAttribute<T, OrderIndexAttribute>(x) ? ClrService.EnumValueGetAttribute<T, OrderIndexAttribute>(x)?.Index : x.ToInt32(null)))
            .Select(x => 
            {
                string? stringValue = (x as Enum).GetStringValue();
                string name = stringValue is null ? string.Empty : isLocalized ? "Reo.GetString(stringValue)" : stringValue;
                
                return includeDescription
                    ? new DescriptionSelectOption 
                    { 
                        Selected = selected?.Invoke(x) ?? false, 
                        Name = name, 
                        Value = x.ToInt32(null), 
                        Description = ClrService.EnumValueHasAttribute<T, DescriptionAttribute>(x) ? ClrService.EnumValueGetAttribute<T, DescriptionAttribute>(x)?.Description : null 
                    }
                    : new NativeSelectOption 
                    { 
                        Selected = selected?.Invoke(x) ?? false, 
                        Name = name, 
                        Value = x.ToInt32(null) 
                    };
            })
            .Cast<ISelectOption>().ToList();

        if (optionsOrderBy is not null)
        {
            Func<ISelectOption, object> orderByFunc = optionsOrderBy.Compile();
            return options.OrderBy(orderByFunc).ToList();
        }
        
        return options;
    }
    
    public static void Forever(this IMemoryCache cache, string key, object? value)
    {
        cache.Set(key, value, DateTime.MaxValue);
    }
}