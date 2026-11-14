using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio.Rendering;

/// <summary>
/// Owns the offscreen Scene View render target and handles deferred resize transitions without stalls.
/// </summary>
internal sealed class SceneViewportTarget : IDisposable
{
    private const uint MIN_DIMENSION = 64;
    private const uint MAX_DIMENSION = 8192;
    private const int RETIRE_DELAY_FRAMES = 3;

    private struct RetiredTexture
    {
        public Handle<GPUTexture> handle;
        public int framesLeft;
    }

    private readonly List<RetiredTexture> _retiredTextures = new();
    private IResourceDatabase? _resourceDatabase;
    private Handle<GPUTexture> _textureHandle = Handle<GPUTexture>.Invalid;
    private uint _bindlessSrvIndex;
    private uint _width;
    private uint _height;
    private uint _requestedWidth = 1280;
    private uint _requestedHeight = 720;
    private bool _disposed;

    public Handle<GPUTexture> TextureHandle => _textureHandle;
    public uint BindlessSrvIndex => _bindlessSrvIndex;
    public uint Width => _width;
    public uint Height => _height;
    public bool IsReady => _textureHandle.IsValid && _bindlessSrvIndex != 0 && _width > 0 && _height > 0;

    public void RequestResize(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        _requestedWidth = Math.Clamp(width, MIN_DIMENSION, MAX_DIMENSION);
        _requestedHeight = Math.Clamp(height, MIN_DIMENSION, MAX_DIMENSION);
    }

    public bool EnsureTexture(RenderEngine renderEngine)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var database = renderEngine.GraphicsEngine.ResourceDatabase;
        _resourceDatabase = database;
        FlushRetiredTextures(database);

        var targetW = _requestedWidth;
        var targetH = _requestedHeight;
        if (targetW == 0 || targetH == 0)
        {
            return false;
        }

        if (_textureHandle.IsValid && _width == targetW && _height == targetH)
        {
            return true;
        }

        var allocator = renderEngine.GraphicsEngine.ResourceAllocator;
        var desc = new TextureDesc
        {
            Width = targetW,
            Height = targetH,
            Slice = 1,
            MipLevels = 1,
            Dimension = TextureDimension.Texture2D,
            Format = TextureFormat.B8G8R8A8_UNorm,
            Usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
        };

        var newTexture = allocator.CreateTexture(in desc, $"EditorSceneViewport_{targetW}x{targetH}");
        if (!newTexture.IsValid)
        {
            return false;
        }

        var newSrv = database.GetBindlessIndex(newTexture.AsResource(), BindlessAccess.ShaderResource);
        var oldTexture = _textureHandle;

        _textureHandle = newTexture;
        _bindlessSrvIndex = newSrv;
        _width = targetW;
        _height = targetH;

        if (oldTexture.IsValid)
        {
            _retiredTextures.Add(new RetiredTexture
            {
                handle = oldTexture,
                framesLeft = RETIRE_DELAY_FRAMES,
            });
        }

        return _bindlessSrvIndex != 0;
    }

    private void FlushRetiredTextures(IResourceDatabase database)
    {
        for (var i = _retiredTextures.Count - 1; i >= 0; i--)
        {
            var entry = _retiredTextures[i];
            entry.framesLeft--;
            if (entry.framesLeft <= 0)
            {
                database.ReleaseResource(entry.handle.AsResource());
                _retiredTextures.RemoveAt(i);
            }
            else
            {
                _retiredTextures[i] = entry;
            }
        }
    }

    public uint2 GetCurrentSize()
    {
        return new uint2(Math.Max(1u, _width), Math.Max(1u, _height));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_resourceDatabase != null)
        {
            for (var i = 0; i < _retiredTextures.Count; i++)
            {
                if (_retiredTextures[i].handle.IsValid)
                {
                    _resourceDatabase.ReleaseResource(_retiredTextures[i].handle.AsResource());
                }
            }

            if (_textureHandle.IsValid)
            {
                _resourceDatabase.ReleaseResource(_textureHandle.AsResource());
            }
        }

        _retiredTextures.Clear();
        _textureHandle = Handle<GPUTexture>.Invalid;
        _bindlessSrvIndex = 0;
        _width = 0;
        _height = 0;
    }
}
