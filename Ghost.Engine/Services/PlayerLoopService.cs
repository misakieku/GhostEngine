namespace Ghost.Engine.Services;

internal static class PlayerLoopService
{
    private static Timer? _timer;
    private static bool _isRunning = false;

    // TODO: Implement the actual time system
    public static float fixedDeltaTime = 0.02f;

    public static void Start()
    {
        if (_isRunning)
        {
            return;
        }

        foreach (var gameObject in SceneManager.QueryRootGameObjects())
        {
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
        foreach (var gameObject in SceneManager.QueryRootGameObjects())
        {
            gameObject.Update();
        }

        foreach (var gameObject in SceneManager.QueryRootGameObjects())
        {
            gameObject.LateUpdate();
        }
    }

    private static void FixedUpdate(object? state)
    {
        foreach (var gameObject in SceneManager.QueryRootGameObjects())
        {
            gameObject.FixedUpdate();
        }
    }

    public static void Stop()
    {
        _isRunning = false;
    }
}