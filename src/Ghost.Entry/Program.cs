using System.Diagnostics;

namespace Ghost.Entry;

internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var engineContext = new EngineContext();

        var windowDesc = new WindowDesc
        {
            Width = 800,
            Height = 600,
            Title = "Ghost Engine"
        };

        using var window = new EngineWindow(engineContext.EngineCore.RenderSystem.GraphicsEngine, windowDesc);

        engineContext.EngineCore.Start();

        while (window.IsRunning)
        {
            window.PollEvents();

            Debug.WriteLine("Frame started");
            engineContext.EngineCore.RenderSystem.SignalCPUReady();
            Debug.WriteLine("Frame submitted to GPU");
            engineContext.EngineCore.RenderSystem.WaitForGPUReady();
            Debug.WriteLine("Frame rendered");
        }
    }
}