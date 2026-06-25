#define PLATFORM_WINDOWNS

using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using SDL;
using System.Runtime.CompilerServices;
using static SDL.SDL3;

namespace Ghost.Engine;

public struct WindowDesc
{
    public required string Title
    {
        get; set;
    }

    public required int Width
    {
        get; set;
    }

    public required int Height
    {
        get; set;
    }

    public bool Borderless
    {
        get; set;
    }
}

public unsafe class EngineWindow : IDisposable
{
    // TODO: Linux can run on either X11 or Wayland, so we need to detect which one is being used and use the appropriate property name.

#if PLATFORM_WINDOWNS
    private static ReadOnlySpan<byte> HANDLE_PROPERTY_NAME => SDL_PROP_WINDOW_WIN32_HWND_POINTER;
#elif PLATFORM_MACOS
    private static ReadOnlySpan<byte> HANDLE_PROPERTY_NAME => SDL_PROP_WINDOW_COCOA_WINDOW_POINTER;
#endif

    private readonly SwapChainManager _swapChainManager;

    private readonly SDL_Window* _window;
    private readonly SDL_PropertiesID _propID;

    private readonly ISwapChain _swapChain;
    private readonly int _swapChainIndex;

    private bool _isRunning;

    public IntPtr Handle => SDL_GetPointerProperty(_propID, HANDLE_PROPERTY_NAME, 0);
    public int SwapChainIndex => _swapChainIndex;

    public bool IsRunning => _isRunning;

    public EngineWindow(SwapChainManager swapChainManager, WindowDesc desc)
    {
        _swapChainManager = swapChainManager;

        var windowFlags = SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
        if (desc.Borderless)
        {
            windowFlags |= SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
        }

        var window = SDL_CreateWindow(desc.Title, desc.Width, desc.Height, windowFlags);
        if (window == null)
        {
            throw new Exception($"Failed to create window: {SDL_GetError()}");
        }

        _window = window;
        _propID = SDL_GetWindowProperties(_window);

        var swapChainDesc = new SwapChainDesc
        {
            Width = (uint)desc.Width,
            Height = (uint)desc.Height,
            Format = TextureFormat.B8G8R8A8_UNorm,
            ScaleX = 1.0f,
            ScaleY = 1.0f,
            Target = SwapChainTarget.FromWindowHandle(Handle)
        };

        swapChainManager.CreateSwapChain(swapChainDesc, out _swapChain, out _swapChainIndex);

        _isRunning = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PollEvents(Action<SDL_Event>? callback = null)
    {
        SDL_Event e;
        while (SDL_PollEvent(&e))
        {
            switch (e.Type)
            {
                case SDL_EventType.SDL_EVENT_QUIT:
                    _isRunning = false;
                    break;
                case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                    var resizeEvent = e.window;
                    _swapChain.Resize((uint)resizeEvent.data1, (uint)resizeEvent.data2);
                    break;
            }

            callback?.Invoke(e);
        }
    }

    public void Dispose()
    {
        var refCount = _swapChainManager.ReleaseSwapChain(_swapChainIndex);
        Logger.DebugAssert(refCount == 0);

        SDL_DestroyWindow(_window);
    }
}
