using Ghost.Engine.Models;

namespace Ghost.Engine;

internal class EngineCore
{
    public async Task StartAsync()
    {
        ActivationHandler.Handle(new LaunchArgument());
        await Task.CompletedTask;
    }

    public async Task ShutDownAsync()
    {
        await Task.CompletedTask;
    }
}