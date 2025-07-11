using Ghost.Engine.Models;
using Ghost.Engine.Services;
using Ghost.Graphics;

namespace Ghost.Engine;

internal class EngineCore
{
    public void Start(LaunchArgument args)
    {
        ActivationHandler.Handle(args);

        GraphicsPipeline.Initialize();
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