using Ghost.Core.Attributes;
using System.Reflection;

namespace Ghost.Editor.Core.Utilities;

public static class TypeCache
{
    private static readonly TypeInfo[] s_types;

    static TypeCache()
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

        s_types = loadableTypes.Select(t => t.GetTypeInfo()).ToArray();
    }

    internal static void Init()
    {
        // Intentionally left blank.
        // This method exists to force the static constructor to run.
    }

    public static Type[] GetTypes()
    {
        return s_types;
    }
}