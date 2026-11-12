using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.PoC.Drawers;

public sealed class PropertyDrawerRegistry
{
    private readonly Dictionary<Type, PropertyDrawer> _drawers = new();
    private readonly PropertyDrawer _defaultDrawer = new DefaultPropertyDrawer();

    public PropertyDrawerRegistry()
    {
        RegisterDrawer(typeof(float3), new Float3PropertyDrawer());
        RegisterDrawer(typeof(int), new IntPropertyDrawer());
        RegisterDrawer(typeof(float), new FloatPropertyDrawer());
        RegisterDrawer(typeof(bool), new BoolPropertyDrawer());
    }

    public void RegisterDrawer(Type type, PropertyDrawer drawer)
    {
        _drawers[type] = drawer;
    }

    public PropertyDrawer GetDrawer(Type type)
    {
        if (_drawers.TryGetValue(type, out var drawer))
        {
            return drawer;
        }

        return _defaultDrawer;
    }
}
