using Ghost.Core;
using Ghost.Engine.Input;
using Ghost.Engine.Streaming;
using Misaki.HighPerformance.LowLevel.Buffer;
using SDL;

namespace Ghost.Engine;

/// <summary>
/// Defines the interface for an engine launch profile, which provides the necessary configuration and setup for launching the engine.
/// Implementations of this interface should provide the engine description, graphics description, content provider, and handle engine initialization and shutdown events.
/// </summary>
public interface IEngineLaunchProfile
{
    /// <summary>
    /// Gets the engine description that defines the configuration for the engine, including allocation manager settings, window settings, and job scheduler settings.
    /// </summary>
    /// <returns>The engine description.</returns>
    EngineDesc GetEngineDesc();
    /// <summary>
    /// Gets the graphics description that defines the configuration for the graphics engine.
    /// </summary>
    /// <returns>The graphics description.</returns>
    GraphicsDesc GetGraphicsDesc();
    /// <summary>
    /// Gets the content provider that supplies game assets and resources.
    /// </summary>
    /// <returns>The content provider.</returns>
    IContentProvider GetContentProvider();

    /// <summary>
    /// Called when the engine has been initialized. This method allows for any additional setup or configuration that needs to be performed after the engine is ready to run.
    /// </summary>
    /// <param name="engine">The initialized engine.</param>
    void OnEngineInitialized(EngineCore engine);
    /// <summary>
    /// Called when the engine is about to shut down. This method allows for any cleanup or finalization that needs to be performed before the engine is terminated.
    /// </summary>
    /// <param name="engine">The engine to shut down.</param>
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
        where T : IEngineLaunchProfile
    {
        var executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("Failed to get the executable path.");
        Environment.CurrentDirectory = Path.GetDirectoryName(executablePath) ?? throw new InvalidOperationException("Failed to get the executable directory.");

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
        catch (Exception ex)
        {
            // Exception should be either handled or not expected. If an unhandled exception goes all the way up to this point, it indicates a critical failure in the engine execution.
            // Log the error and terminate the application.

            Logger.Error($"An unhandled exception occurred during engine execution: {ex}");
            Environment.FailFast("An unhandled exception occurred during engine execution.", ex);
        }
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
        where T : IEngineLaunchProfile, new()
    {
        Run(new T());
    }
}