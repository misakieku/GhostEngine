using Ghost.Engine.Models;
using Ghost.Engine.Services;
using Ghost.Graphics;
using Ghost.Graphics.Data;

namespace Ghost.Engine;

internal class EngineCore
{
    public void Start(LaunchArgument args)
    {
        ActivationHandler.Handle(args);

        GraphicsPipeline.Initialize(GraphicsAPI.D3D12);
        GraphicsPipeline.Start();

        Logger.LogInfo("Engine started successfully.");
    }

    public void IncrementCPUFenceValue()
    {
        GraphicsPipeline.SignalCPUReady();
    }

    public void ShutDown()
    {
        GraphicsPipeline.SignalCPUReady();
        GraphicsPipeline.Shutdown();
    }
}