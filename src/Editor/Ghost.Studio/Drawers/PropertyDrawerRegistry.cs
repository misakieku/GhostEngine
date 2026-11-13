using Ghost.Core;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio.Drawers;

public sealed class PropertyDrawerRegistry
{
    private readonly Dictionary<Type, PropertyDrawer> _drawers = new();
    private readonly PropertyDrawer _enumDrawer = new EnumPropertyDrawer();
    private readonly PropertyDrawer _defaultDrawer = new DefaultPropertyDrawer();

    public static PropertyDrawerRegistry Default { get; } = CreateDefault();

    public void Register<T>(PropertyDrawer drawer) => _drawers[typeof(T)] = drawer;

    public PropertyDrawer GetDrawer(Type type)
    {
        if (_drawers.TryGetValue(type, out var drawer))
        {
            return drawer;
        }

        if (type.IsEnum)
        {
            return _enumDrawer;
        }

        return _defaultDrawer;
    }

    private static PropertyDrawerRegistry CreateDefault()
    {
        var registry = new PropertyDrawerRegistry();
        registry.Register<float2>(new Float2PropertyDrawer());
        registry.Register<float3>(new Float3PropertyDrawer());
        registry.Register<quaternion>(new QuaternionPropertyDrawer());
        registry.Register<float>(new FloatPropertyDrawer());
        registry.Register<int>(new IntPropertyDrawer());
        registry.Register<uint>(new UIntPropertyDrawer());
        registry.Register<bool>(new BoolPropertyDrawer());
        return registry;
    }
}
