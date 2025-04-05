using Ghost.Engine.Models;

namespace Ghost.Engine;

internal class EngineCore
{
    public static EngineCore? Current
    {
        get;
        private set;
    }

    public static async Task StartAsync(LaunchArgument args)
    {
        if (Current != null)
        {
            return;
        }

        Current = new EngineCore();

        ActivationHandler.Handle(args);
        await Task.CompletedTask;
    }

    public async Task ShutDownAsync()
    {
        await Task.CompletedTask;
    }
}