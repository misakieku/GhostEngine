using Ghost.Engine.Components;

namespace Ghost.Studio.Drawers;

public sealed class ComponentDrawerRegistry
{
    private readonly Dictionary<Type, ComponentDrawer> _drawers = new();
    private readonly ComponentDrawer _defaultDrawer = new DefaultComponentDrawer();

    public static ComponentDrawerRegistry Default { get; } = CreateDefault();

    public void Register<T>(ComponentDrawer drawer) => _drawers[typeof(T)] = drawer;

    public ComponentDrawer GetDrawer(Type type)
    {
        if (_drawers.TryGetValue(type, out var drawer))
        {
            return drawer;
        }

        return _defaultDrawer;
    }

    private static ComponentDrawerRegistry CreateDefault()
    {
        var registry = new ComponentDrawerRegistry();
        registry.Register<LocalTransform>(new TransformComponentDrawer());
        return registry;
    }
}
