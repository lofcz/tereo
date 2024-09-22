
using System.Collections.Concurrent;
using System.Reflection;
using TeReoLocalizer.Shared.Code;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class StringValueAttribute : Attribute
{
    public StringValueAttribute(string? value, string key = "")
    {
        Value = value;
        Key = key;
    }

    public StringValueAttribute(object? value, string key = "")
    {
        Value = value?.ToString();
        Key = key;
    }

    public string? Value { get; }
    public string? Key { get; }
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class TypeValueAttribute : Attribute
{
    public TypeValueAttribute(object value, string key)
    {
        Value = value;
        Key = key;
    }

    public TypeValueAttribute(object value)
    {
        Value = value;
    }

    public object Value { get; }
    public string? Key { get; }
}

public class StringEnum
{
    #region Instance implementation

    private readonly Type _enumType;
    private static readonly ConcurrentDictionary<Enum, string?> StringValues = new();
    private static readonly ConcurrentDictionary<Enum, ConcurrentDictionary<string, string?>> StringValuesWithKeys = new();
    private static readonly ConcurrentDictionary<Enum, object?> TypeValues = new();
    private static readonly ConcurrentDictionary<Enum, ConcurrentDictionary<string, object?>> TypeValuesWithKeys = new();
    
    public StringEnum(Type enumType)
    {
        if (!enumType.IsEnum)
            throw new ArgumentException($"Supplied type must be an Enum.  Type was {enumType.ToString()}");

        _enumType = enumType;
    }

    public static T? GetTypeValue<T>(Enum? value, string? key = null)
    {
        if (value is null)
        {
            return default;
        }

        T? output = default;
        Type type = value.GetType();
        FieldInfo? fi;

        if (key.IsNullOrWhiteSpace())
        {
            if (TypeValues.TryGetValue(value, out object? value2))
            {
                if (value2 is T tVal)
                {
                    return tVal;
                }

                return default;
            }

            fi = type.GetField(value.ToString());
            if (fi?.GetCustomAttributes(typeof(TypeValueAttribute), false) is not TypeValueAttribute[] { Length: > 0 } attrs)
            {
                return default;
            }

            if (TypeValues.ContainsKey(value))
            {
                return default;
            }

            try
            {
                object? s = attrs[0].Value;

                if (s is not null)
                {
                    TypeValues.TryAdd(value, s);
                    object obj = attrs[0].Value;

                    if (obj is T tVal)
                    {
                        return tVal;
                    }
                }
            }
            catch (Exception e)
            {
            }

            return output;
        }

        if (TypeValuesWithKeys.TryGetValue(value, out ConcurrentDictionary<string, object?>? withKey))
        {
            if (withKey.TryGetValue(key, out object? value2))
            {
                if (value2 is T tVal)
                {
                    return tVal;
                }

                return default;
            }
        }

        fi = type.GetField(value.ToString());
        if (fi?.GetCustomAttributes(typeof(TypeValueAttribute), false) is not TypeValueAttribute[] { Length: > 0 } attrs2)
        {
            return default;
        }

        try
        {
            TypeValueAttribute? attr = attrs2.FirstOrDefault(x => x.Key == key);
            if (attr is null)
            {
                return default;
            }

            if (key is not null)
            {
                if (TypeValuesWithKeys.TryGetValue(value, out ConcurrentDictionary<string, object?>? value2))
                {
                    value2.TryAdd(key, attr.Value);
                }
                else
                {
                    ConcurrentDictionary<string, object> cd = new ConcurrentDictionary<string, object>();
                    cd.TryAdd(key, attr.Value);
                    TypeValuesWithKeys.TryAdd(value, cd);
                }
            }

            if (attr.Value is T tVal)
            {
                return tVal;
            }
        }
        catch (Exception e)
        {
        }

        return output;
    }

    public static string? GetStringValue(Enum? value, string? key = null)
    {
        if (value is null)
        {
            return string.Empty;
        }

        string? output = null;
        Type type = value.GetType();
        FieldInfo? fi;

        if (key.IsNullOrWhiteSpace())
        {
            if (StringValues.TryGetValue(value, out string? value2))
            {
                return value2;
            }

            fi = type.GetField(value.ToString());

            if (fi?.GetCustomAttributes(typeof(StringValueAttribute), false) is not StringValueAttribute[] { Length: > 0 } attrs)
            {
                StringValues.TryAdd(value, null);
                return null;
            }

            if (StringValues.ContainsKey(value))
            {
                StringValues.TryAdd(value, null);
                return null;
            }

            try
            {
                string? s = attrs[0].Value;

                if (s is not null)
                {
                    StringValues.TryAdd(value, s);
                    output = attrs[0].Value;
                }
            }
            catch (Exception e)
            {
            }

            return output;
        }

        if (StringValuesWithKeys.TryGetValue(value, out ConcurrentDictionary<string, string>? withKey))
        {
            if (withKey.TryGetValue(key!, out string? value2))
            {
                return value2;
            }
        }

        fi = type.GetField(value.ToString());
        if (fi?.GetCustomAttributes(typeof(StringValueAttribute), false) is not StringValueAttribute[] { Length: > 0 } attrs2)
        {
            return null;
        }

        try
        {
            StringValueAttribute? attr = attrs2.FirstOrDefault(x => x.Key == key);
            if (attr == null)
            {
                return null;
            }

            if (key != null)
            {
                if (StringValuesWithKeys.TryGetValue(value, out ConcurrentDictionary<string, string?>? value2))
                {
                    value2.TryAdd(key, attr.Value);
                }
                else
                {
                    ConcurrentDictionary<string, string> cd = new ConcurrentDictionary<string, string>();
                    cd.TryAdd(key, attr.Value);
                    StringValuesWithKeys.TryAdd(value, cd);
                }
            }

            output = attr.Value;
        }
        catch (Exception e)
        {
        }

        return output;
    }

    public static object Parse(Type type, string stringValue, bool ignoreCase = false)
    {
        object output = null;
        string enumStringValue = null;

        if (!type.IsEnum)
        {
            throw new ArgumentException($"Supplied type must be an Enum.  Type was {type.ToString()}");
        }

        foreach (FieldInfo fi in type.GetFields())
        {
            if (fi.GetCustomAttributes(typeof(StringValueAttribute), false) is StringValueAttribute[] { Length: > 0 } attrs)
            {
                enumStringValue = attrs[0].Value;
            }

            if (string.Compare(enumStringValue, stringValue, ignoreCase) == 0)
            {
                output = Enum.Parse(type, fi.Name);
                break;
            }
        }

        return output;
    }

    public static bool IsStringDefined(Type enumType, string stringValue)
    {
        return Parse(enumType, stringValue) != null;
    }

    public static bool IsStringDefined(Type enumType, string stringValue, bool ignoreCase)
    {
        return Parse(enumType, stringValue, ignoreCase) != null;
    }

    #endregion
}