using Ghost.Core.Attributes;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.Utilities;

public static class TypeCache
{
    private static TypeInfo[] s_types;
    private static Dictionary<nint, List<MethodInfo>> s_attributeMethodCache;

    static TypeCache()
    {
        s_types = LoadTypes();
        s_attributeMethodCache = FindMethodWithAttribute();
    }

    private static TypeInfo[] LoadTypes()
    {
        var loadableTypes = new List<Type>(512);
        var assembliesToScan = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetCustomAttribute<EngineAssemblyAttribute>() != null);

        foreach (var assembly in assembliesToScan)
        {
            try
            {
                loadableTypes.AddRange(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException ex)
            {
                var types = ex.Types.Where(t => t != null);
                loadableTypes.AddRange(types!);
            }
        }

        return loadableTypes.Select(t => t.GetTypeInfo()).ToArray();
    }

    private static Dictionary<nint, List<MethodInfo>> FindMethodWithAttribute()
    {
        var dict = new Dictionary<nint, List<MethodInfo>>();
        foreach (var type in s_types)
        {
            foreach (var method in type.DeclaredMethods)
            {
                var attrs = method.GetCustomAttributes<DiscoverableAttributeBase>(false);
                foreach (var attr in attrs)
                {
                    var key = attr.GetType().TypeHandle.Value;
                    ref var methodList = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out var exist);
                    if (!exist)
                    {
                        methodList = new List<MethodInfo>();
                    }

                    methodList!.Add(method);
                }
            }
        }

        return dict;
    }

    internal static void Initialize()
    {
        // Intentionally left blank.
        // This method exists to force the static constructor to run.
    }

    internal static void Reload()
    {
        s_types = LoadTypes();
        s_attributeMethodCache = FindMethodWithAttribute();
    }

    public static IReadOnlyCollection<TypeInfo> GetTypes()
    {
        return s_types;
    }

    public static IReadOnlyCollection<MethodInfo>? GetMethodsWithAttribute<T>()
        where T : DiscoverableAttributeBase
    {
        var key = typeof(T).TypeHandle.Value;
        if (s_attributeMethodCache.TryGetValue(key, out var methods))
        {
            return methods;
        }

        return null;
    }
}