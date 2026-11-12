using Ghost.Editor.PoC.Ecs;
using Ghost.Engine.Components;

namespace Ghost.Editor.PoC.Drawers;

public sealed class ComponentDrawerRegistry
{
    private readonly Dictionary<Type, ComponentDrawer> _drawers = new();
    private readonly ComponentDrawer _defaultDrawer = new DefaultComponentDrawer();

    public ComponentDrawerRegistry()
    {
        RegisterDrawer(typeof(LocalTransform), new TransformComponentDrawer());
        RegisterDrawer(typeof(CounterComponent), new CounterComponentDrawer());
    }

    public void RegisterDrawer(Type type, ComponentDrawer drawer)
    {
        _drawers[type] = drawer;
    }

    public ComponentDrawer GetDrawer(Type type)
    {
        if (_drawers.TryGetValue(type, out var drawer))
        {
            return drawer;
        }

        return _defaultDrawer;
    }
}
