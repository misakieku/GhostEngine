using Ghost.Entities;

namespace Ghost.Engine.Services;

internal static class PlayerLoopService
{
    private static bool _isRunning = false;

    // TODO: Implement the actual time system
    public static float fixedDeltaTime = 0.02f;

    public static void Start()
    {
        if (_isRunning)
        {
            return;
        }

        for (var i = 0; i < World.WorldCount; i++)
        {
            var world = World.GetWorld(i);

            foreach (var script in world.QueryScript())
            {
                script.Start();
            }
        }
    }

    public static void Update()
    {
        for (var i = 0; i < World.WorldCount; i++)
        {
            var world = World.GetWorld(i);
            world.SystemStorage.UpdateSystems();

            foreach (var script in world.QueryScript())
            {
                script.Update();
            }
        }
    }

    public static void Shutdown()
    {
        for (var i = 0; i < World.WorldCount; i++)
        {
            var world = World.GetWorld(i);
            foreach (var script in world.QueryScript())
            {
                script.OnDestroy();
            }
        }

        _isRunning = false;
    }
}