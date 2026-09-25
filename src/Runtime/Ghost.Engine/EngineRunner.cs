using Ghost.Engine.Streaming;
using Misaki.HighPerformance.LowLevel.Buffer;
using SDL;

namespace Ghost.Engine;

public interface IEngineLanunchProfile
{
    Action<SDL_Event>? OnWindowEvent { get; }

    EngineDesc GetEngineDesc();
    GraphicsDesc GetGraphicsDesc();
    IContentProvider GetContentProvider();

    void OnEngineInitialized(EngineCore engine);
    void OnEngineShutdown(EngineCore engine);
}

public static class EngineRunner
{
    public static void Run<T>(T profile)
        where T : IEngineLanunchProfile
    {
        var engineDesc = profile.GetEngineDesc();

        AllocationManager.Initialize(engineDesc.AllocationManagerDesc);

        if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD))
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
                window.AttachToInputManager(engineCore.InputManager);

                engineCore.Start();

                while (window.IsRunning)
                {
                    window.PollEvents(engineCore.InputManager.ProcessEvent, profile.OnWindowEvent);
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