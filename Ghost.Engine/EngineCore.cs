using Ghost.Engine.Models;
using Ghost.Engine.Services;
using Ghost.Graphics;
using Ghost.Graphics.Data;

namespace Ghost.Engine;

internal class EngineCore
{
    public async Task StartAsync(LaunchArgument args)
    {
        ActivationHandler.Handle(args);
        GraphicsPipeline.Initialize(GraphicsAPI.DX12);
        GraphicsPipeline.Start();

        Logger.LogInfo("Engine started successfully.");

        await Task.CompletedTask;
    }

    public async Task ShutDownAsync()
    {
        GraphicsPipeline.Shutdown();
        await Task.CompletedTask;
    }
}