namespace Ghost.RenderGraph.Concept;

public class RenderGraphBlackboard
{
    private readonly Dictionary<Type, object> _data = new();

    public void Add<T>(T data) where T : class
    {
        _data[typeof(T)] = data;
    }

    public T Get<T>() where T : class
    {
        if (_data.TryGetValue(typeof(T), out var data))
        {
            return (T)data;
        }
        throw new KeyNotFoundException($"Data of type {typeof(T).Name} not found in blackboard.");
    }

    public bool TryGet<T>(out T? data) where T : class
    {
        if (_data.TryGetValue(typeof(T), out var obj))
        {
            data = (T)obj;
            return true;
        }
        data = null;
        return false;
    }

    public void Clear()
    {
        _data.Clear();
    }
}
