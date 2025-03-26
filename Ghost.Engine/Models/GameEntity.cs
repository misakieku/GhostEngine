using System.Collections.ObjectModel;

namespace Ghost.Engine.Models;

public abstract class GameEntity
{
    private ObservableCollection<Component> _components = new();

    public GameEntity()
    {
        //AddComponent(new Transform());
    }

    public void AddComponent(Component component)
    {
        _components.Add(component);
    }
}