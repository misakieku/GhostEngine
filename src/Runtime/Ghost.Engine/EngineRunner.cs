using Ghost.Engine.Streaming;
using Misaki.HighPerformance.LowLevel.Buffer;
using SDL;

namespace Ghost.Engine;

public interface IEngineLanunchProfile
{
    public EngineDesc GetEngineDesc();
    public GraphicsDesc GetGraphicsDesc();
    public IContentProvider GetContentProvider();

    public void OnEngineInitialized(EngineCore engine);
    public void OnWindowEvent(SDL_Event sdlEvent);
    public void OnEngineShutdown(EngineCore engine);
}

public static class EngineRunner
{
    public static void Run<T>(T profile)
        where T : IEngineLanunchProfile
    {
        var engineDesc = profile.GetEngineDesc();

        AllocationManager.Initialize(engineDesc.AllocationManagerDesc);

        if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
        {
            AllocationManager.Dispose();
            var errorMessage = SDL3.SDL_GetError();
            throw new Exception($"Failed to initialize SDL. {errorMessage}");
        }

        try
        {
            using var engineCore = new EngineCore(engineDesc.JobSchedulerDesc, profile.GetGraphicsDesc(), profile.GetContentProvider());

            profile.OnEngineInitialized(engineCore);

            try
            {
                using var window = new EngineWindow(engineCore.RenderEngine, engineDesc.WindowDesc);

                engineCore.Start();

                while (window.IsRunning)
                {
                    window.PollEvents(null, profile.OnWindowEvent);
                    engineCore.Tick();
                }

                engineCore.Stop();
            }
            finally
            {
                profile.OnEngineShutdown(engineCore);
            }
        }
        // TODO: Log the exception
        finally
        {
            SDL3.SDL_Quit();
            AllocationManager.Dispose();
        }
    }

    public static void Run<T>()
        where T : IEngineLanunchProfile, new()
    {
        Run(new T());
    }
}