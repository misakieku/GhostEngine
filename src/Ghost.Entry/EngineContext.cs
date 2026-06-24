using Ghost.Engine;
using Ghost.Engine.Streaming;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using SDL;
using static SDL.SDL3;

namespace Ghost.Entry;

internal class EngineContext : IDisposable
{
    private readonly EngineCore _engineCore;
    private readonly World _defaultWorld;

    public EngineCore EngineCore => _engineCore;
    public World DefaultWorld => _defaultWorld;

    public EngineContext()
    {
        AllocationManager.Initialize(AllocationManagerDesc.Default);

        if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
        {
            throw new Exception($"Failed to initialize SDL{SDL_GetError()}");
        }

        _engineCore = new EngineCore(new RuntimeContentProvider());
        _defaultWorld = World.Create(_engineCore.JobScheduler, 1024);
    }

    public void Dispose()
    {
        _defaultWorld.Dispose();
        _engineCore.Dispose();
        
        SDL_Quit();
        
        AllocationManager.Dispose();
    }
}
