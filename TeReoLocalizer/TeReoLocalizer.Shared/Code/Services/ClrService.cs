using System.Collections.Concurrent;
using System.Linq.Expressions;
using Mapster;
using Microsoft.Extensions.Caching.Memory;
using MemberInfo = System.Reflection.MemberInfo;

namespace TeReoLocalizer.Shared.Code.Services;

public static class ClrService
{
    private static IMemoryCache Cache => GlobalServices.Cache;
    private static readonly ConcurrentDictionary<MemberInfo, bool> JsExposedMembers = [];
    private static readonly ConcurrentDictionary<Type, HashSet<string>> MapsterIgnoreCache = [];
    private static readonly ConcurrentDictionary<Type, TypeAdapterConfig> MapsterConfigCache = [];

    public static HashSet<string> GetIgnoredProperties(Type type)
    {
        return MapsterIgnoreCache.GetOrAdd(type, t =>
        {
            return t.GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(MapsterIgnoreAttribute), true).Length is not 0)
                .Select(p => p.Name)
                .ToHashSet();
        });
    }

    public static TypeAdapterConfig GetTypeConfig<T>()
    {
        return MapsterConfigCache.GetOrAdd(typeof(T), t =>
        {
            TypeAdapterConfig config = new TypeAdapterConfig();
            HashSet<string> ignoredProperties = GetIgnoredProperties(t);

            if (ignoredProperties.Count > 0)
            {
                TypeAdapterSetter<T, T> typeConfig = config.ForType<T, T>();
                
                foreach (string propertyName in ignoredProperties)
                {
                    ParameterExpression parameter = Expression.Parameter(typeof(T), "dest");
                    MemberExpression propertyAccess = Expression.Property(parameter, propertyName);
                    UnaryExpression convertedAccess = Expression.Convert(propertyAccess, typeof(object));
                    Expression<Func<T, object>> lambda = Expression.Lambda<Func<T, object>>(convertedAccess, parameter);
                    typeConfig.Ignore(lambda);
                }
            }

            return config;
        });
    }
    
    public static bool TypeImplementsInterface(Type type, Type interfaceType)
    {
        if (Cache.TryGetValue($"clrTypeImplementsInterface_${type}_{interfaceType}", out bool implements))
        {
            return implements;
        } 
        
        bool impls = interfaceType.IsAssignableFrom(type);
        Cache.Forever($"clrTypeImplementsInterface_${type}_{interfaceType}", impls);
        
        return impls;
    }

    public static T? TypeGetAttribute<T>(Type declType) where T : Attribute
    {
        Type type = typeof(T);
        
        if (Cache.TryGetValue($"memberInfoHasAttribute{declType.FullName}_{type.FullName}", out T? tVal))
        {
            return tVal;
        }

        object[] attrs = declType.GetCustomAttributes(typeof(T), false);
        
        if (attrs.Length is 0)
        {
            Cache.Forever($"memberInfoHasAttribute{declType.FullName}_{type.FullName}", null);
            return null;
        }

        if (attrs[0] is T t)
        {
            Cache.Forever($"memberInfoHasAttribute{declType.FullName}_{type.FullName}", t);
            return t;
        }
      
        Cache.Forever($"memberInfoHasAttribute{declType.FullName}_{type.FullName}", null);
        return null;
    }

    public static T? MemberInfoGetAttribute<T>(MemberInfo info) where T : Attribute
    {
        Type type = typeof(T);
        
        if (Cache.TryGetValue($"memberInfoHasAttribute{info.ReflectedType?.FullName}_{info.Name}_{type.FullName}", out T? tVal))
        {
            return tVal;
        }

        object[] attrs = info.GetCustomAttributes(typeof(T), false);

        if (attrs.Length is 0)
        {
            Cache.Forever($"memberInfoHasAttribute{info.ReflectedType?.FullName}_{info.Name}_{type.FullName}", null);
            return null;
        }

        if (attrs[0] is T t)
        {
            Cache.Forever($"memberInfoHasAttribute{info.ReflectedType?.FullName}_{info.Name}_{type.FullName}", t);
            return t;
        }
      
        Cache.Forever($"memberInfoHasAttribute{info.ReflectedType?.FullName}_{info.Name}_{type.FullName}", null);
        return null;
    }
    
    public static bool EnumValueHasAttribute<TEnum, TAttr>(TEnum val) where TEnum : struct, IConvertible
    {
        if (Cache.TryGetValue($"clrEnumValHasAttr_${typeof(TEnum)}_{typeof(TAttr)}_{val.ToString()}", out bool has))
        {
            return has;
        } 
        
        bool hasAttr = typeof(TEnum).GetField(val.ToString())?.GetCustomAttributes(typeof(TAttr), false).Length > 0;
        Cache.Forever($"clrEnumValHasAttr_${typeof(TEnum)}_{typeof(TAttr)}_{val.ToString()}", hasAttr);

        return hasAttr;
    }

    public static TAttr? EnumValueGetAttribute<TEnum, TAttr>(TEnum val)
    {
        if (Cache.TryGetValue($"clrEnumValAttr_${typeof(TEnum)}_{typeof(TAttr)}_{val.ToString()}", out TAttr? tattr))
        {
            return tattr;
        } 
        
        TAttr? fetched = (TAttr?)typeof(TEnum).GetField(val.ToString()).GetCustomAttributes(typeof(TAttr), false).FirstOrDefault();
        Cache.Forever($"clrEnumValAttr_${typeof(TEnum)}_{typeof(TAttr)}_{val.ToString()}", fetched);

        return fetched;
    }
    
    public static TAttr? EnumGetAttribute<TEnum, TAttr>()
    {
        if (Cache.TryGetValue($"clrEnumValAttr_${typeof(TEnum)}_{typeof(TAttr)}", out TAttr? tattr))
        {
            return tattr;
        } 
        
        TAttr? fetched = (TAttr?)typeof(TEnum).GetCustomAttributes(typeof(TAttr), false).FirstOrDefault();
        Cache.Forever($"clrEnumValAttr_${typeof(TEnum)}_{typeof(TAttr)}", fetched);

        return fetched;
    }
}