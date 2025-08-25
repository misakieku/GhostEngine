using Ghost.Core.Attributes;
using System.Reflection;

namespace Ghost.Editor.Core.Utilities;

public static class TypeCache
{
    private static readonly TypeInfo[] _types;

    static TypeCache()
    {
        var loadableTypes = new List<Type>();
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

        _types = loadableTypes.Select(t => t.GetTypeInfo()).ToArray();
    }

    public static Type[] GetTypes()
    {
        return _types;
    }
}