using System.Collections.Concurrent;
using System.Reflection;
using TeReoLocalizer.Annotations;

namespace TeReoLocalizer.Shared.Code;

public class Key
{
    public string Name { get; set; }
}

public class Decl
{
    public ConcurrentDictionary<string, Key> Keys { get; set; } = [];
}

public class LangsData
{
    public ConcurrentDictionary<Languages, LangData> Langs { get; set; } = [];
}

public class LangData
{
    public ConcurrentDictionary<string, string> Data { get; set; } = [];
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