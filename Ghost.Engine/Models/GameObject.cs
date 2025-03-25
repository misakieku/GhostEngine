using Ghost.Engine.Components;

namespace Ghost.Engine.Models;

public class GameObject
{
    private List<Component> _components = new();

    public GameObject()
    {
        AddComponent(new Transform());
    }

    public void AddComponent(Component component)
    {
        _components.Add(component);
    }
}