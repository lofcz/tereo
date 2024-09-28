using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using TeReoLocalizer.Annotations;

namespace TeReoLocalizer.Shared.Code;

public class Key
{
    public string Name { get; set; }
    public string Id { get; set; }
    [JsonIgnore]
    public bool DefaultLangContainsHtml { get; set; }
    [JsonIgnore] 
    public bool IsVisible { get; set; } = true;
}

public class Decl
{
    [JsonConverter(typeof(SortedDictionaryConverter<string, Key>))]
    public ConcurrentDictionary<string, Key> Keys { get; set; } = [];
}

public class LangsData
{
    [JsonConverter(typeof(SortedDictionaryConverter<Languages, LangData>))]
    public ConcurrentDictionary<Languages, LangData> Langs { get; set; } = [];
}

public class LangData
{
    [JsonConverter(typeof(SortedDictionaryConverter<string, string>))]
    public ConcurrentDictionary<string, string> Data { get; set; } = [];
    [JsonIgnore] 
    public bool Visible { get; set; } = true;
}

public enum ToastTypes
{
    [StringValue("ok")]
    Success,
    [StringValue("info")]
    Info,
    [StringValue("warning")]
    Warning,
    [StringValue("error")]
    Error
}