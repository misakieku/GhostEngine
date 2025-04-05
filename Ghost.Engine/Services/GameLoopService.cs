
namespace Ghost.Engine.Services;

internal static class GameLoopService
{
    private readonly static HashSet<GameObject> _gameObjects = new();

    private static Timer? _timer;
    private static bool _isRunning = false;

    // TODO: Implement the actual time system
    public static float fixedDeltaTime = 0.02f;

    public static void RegisterGameObject(GameObject gameObject)
    {
        _gameObjects.Add(gameObject);
    }

    public static void UnregisterGameObject(GameObject gameObject)
    {
        _gameObjects.Remove(gameObject);
    }

    public static void Start()
    {
        if (_isRunning)
        {
            return;
        }

        foreach (var gameObject in _gameObjects)
        {
            if (!gameObject.isActive)
            {
                continue;
            }

            gameObject.Start();
        }

        _timer ??= new Timer(FixedUpdate, null, 0, (int)(fixedDeltaTime * 1000));

        while (_isRunning)
        {
            Update();
        }
    }

    private static void Update()
    {
        foreach (var gameObject in _gameObjects)
        {
            if (!gameObject.isActive)
            {
                continue;
            }

            gameObject.Update();
            gameObject.LateUpdate();
        }
    }

    private static void FixedUpdate(object? state)
    {
        foreach (var gameObject in _gameObjects)
        {
            if (!gameObject.isActive)
            {
                continue;
            }

            gameObject.FixedUpdate();
        }
    }

    public static void Stop()
    {
        _isRunning = false;
    }
}