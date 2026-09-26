using Ghost.Engine.Input;
using Ghost.Engine.Streaming;
using Misaki.HighPerformance.LowLevel.Buffer;
using SDL;

namespace Ghost.Engine;

public interface IEngineLanunchProfile
{
    EngineDesc GetEngineDesc();
    GraphicsDesc GetGraphicsDesc();
    IContentProvider GetContentProvider();

    void OnEngineInitialized(EngineCore engine);
    void OnEngineShutdown(EngineCore engine);
}

public static class EngineRunner
{
    /// <summary>
    /// Runs the engine with a specified launch profile.
    /// </summary>
    /// <typeparam name="T">The type of the launch profile to use. Must implement IEngineLanunchProfile.</typeparam>
    /// <param name="profile">The launch profile to use for running the engine.</param>
    /// <exception cref="Exception">Thrown if SDL initialization fails.</exception>
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
                var userHandler = profile is IInputHandler inputHandler ? inputHandler : null;
                using var window = new EngineWindow(engineCore.RenderEngine, engineDesc.WindowDesc);
                window.AttachToInputManager(engineCore.InputManager);

                engineCore.Start();

                while (window.IsRunning)
                {
                    window.PollEvents(engineCore.InputManager, userHandler);
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

    /// <summary>
    /// Runs the engine with a specified launch profile type.
    /// </summary>
    /// <typeparam name="T">The type of the launch profile to use. Must implement IEngineLanunchProfile and have a parameterless constructor.</typeparam>
    public static void Run<T>()
        where T : IEngineLanunchProfile, new()
    {
        Run(new T());
    }
}