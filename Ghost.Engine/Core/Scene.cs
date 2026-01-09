using Ghost.Entities;

namespace Ghost.Engine.Core;

public partial class Scene
{
    private static short s_nextSceneID = 0;
}

public partial class Scene : IDisposable
{
    private readonly World _world;
    private readonly short _id;

    private bool _isDisposed;

    public World World => _world;
    public short ID => _id;

    public Scene(World world)
    {
        _world = world;
        _id = s_nextSceneID++;
    }

    ~Scene()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
