using Ghost.Engine.Models;
using Ghost.Engine.Services;

namespace Ghost.Engine;

internal class EngineCore : IDisposable, IAsyncDisposable
{
    public async Task StartAsync(LaunchArgument args)
    {
        ActivationHandler.Handle(args);

        Logger.LogInfo("Engine started successfully.");

        await Task.CompletedTask;
    }

    public async Task ShutDownAsync()
    {
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        ShutDownAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await ShutDownAsync();
    }
}