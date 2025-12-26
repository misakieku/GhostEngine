namespace Ghost.Shader.Concept;

/// <summary>
/// Material instance pool for efficient reuse and memory management.
/// Reduces GC pressure for frequently created/destroyed materials.
/// </summary>
public sealed class MaterialPool
{
    private readonly Dictionary<ShaderProgram, Stack<Material>> _pools = new();
    private readonly object _lock = new();

    public Material Rent(ShaderProgram shaderProgram)
    {
        lock (_lock)
        {
            if (_pools.TryGetValue(shaderProgram, out var pool) && pool.Count > 0)
            {
                var material = pool.Pop();
                return material;
            }
        }

        return new Material(shaderProgram);
    }

    public void Return(Material material)
    {
        if (material == null)
            return;

        lock (_lock)
        {
            if (!_pools.TryGetValue(material.ShaderProgram, out var pool))
            {
                pool = new Stack<Material>();
                _pools[material.ShaderProgram] = pool;
            }

            pool.Push(material);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    var material = pool.Pop();
                    material.Dispose();
                }
            }
            _pools.Clear();
        }
    }
}
