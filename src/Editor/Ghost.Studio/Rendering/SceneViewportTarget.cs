using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio.Rendering;

/// <summary>
/// Owns per-frame-in-flight offscreen Scene View render targets and handles deferred resize transitions without stalls.
/// </summary>
internal sealed class SceneViewportTarget : IDisposable
{
    private const uint MIN_DIMENSION = 64;
    private const uint MAX_DIMENSION = 8192;
    private const int RETIRE_DELAY_FRAMES = 3;

    private struct ViewportSlot
    {
        public Handle<GPUTexture> textureHandle;
        public uint bindlessSrvIndex;
        public uint width;
        public uint height;
    }

    private struct RetiredTexture
    {
        public Handle<GPUTexture> handle;
        public int framesLeft;
    }

    private readonly ViewportSlot[] _slots;
    private readonly List<RetiredTexture> _retiredTextures = new();
    private IResourceDatabase? _resourceDatabase;
    private uint _requestedWidth = 1280;
    private uint _requestedHeight = 720;
    private bool _disposed;

    public SceneViewportTarget(int frameBufferCount = 2)
    {
        var count = Math.Max(1, frameBufferCount);
        _slots = new ViewportSlot[count];
        for (var i = 0; i < count; i++)
        {
            _slots[i].textureHandle = Handle<GPUTexture>.Invalid;
        }
    }

    private int GetSlotIndex(int frameIndex)
    {
        return (int)((uint)frameIndex % (uint)_slots.Length);
    }

    public Handle<GPUTexture> GetTextureHandle(int frameIndex = 0)
    {
        return _slots[GetSlotIndex(frameIndex)].textureHandle;
    }

    public uint GetBindlessSrvIndex(int frameIndex = 0)
    {
        return _slots[GetSlotIndex(frameIndex)].bindlessSrvIndex;
    }

    public uint GetWidth(int frameIndex = 0)
    {
        return _slots[GetSlotIndex(frameIndex)].width;
    }

    public uint GetHeight(int frameIndex = 0)
    {
        return _slots[GetSlotIndex(frameIndex)].height;
    }

    public bool IsReady(int frameIndex = 0)
    {
        ref readonly var slot = ref _slots[GetSlotIndex(frameIndex)];
        return slot.textureHandle.IsValid && slot.bindlessSrvIndex != 0 && slot.width > 0 && slot.height > 0;
    }

    public void RequestResize(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return;
        }

        _requestedWidth = Math.Clamp(width, MIN_DIMENSION, MAX_DIMENSION);
        _requestedHeight = Math.Clamp(height, MIN_DIMENSION, MAX_DIMENSION);
    }

    public bool EnsureTexture(RenderEngine renderEngine, int frameIndex = 0)
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

        var slotIndex = GetSlotIndex(frameIndex);
        ref var slot = ref _slots[slotIndex];
        if (slot.textureHandle.IsValid && slot.width == targetW && slot.height == targetH)
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
            Format = TextureFormat.R10G10B10A2_UNorm,
            Usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
        };

        var newTexture = allocator.CreateTexture(in desc, $"EditorSceneViewport_Slot{slotIndex}_{targetW}x{targetH}");
        if (!newTexture.IsValid)
        {
            return false;
        }

        var newSrv = database.GetBindlessIndex(newTexture.AsResource(), BindlessAccess.ShaderResource);
        var oldTexture = slot.textureHandle;

        slot.textureHandle = newTexture;
        slot.bindlessSrvIndex = newSrv;
        slot.width = targetW;
        slot.height = targetH;

        if (oldTexture.IsValid)
        {
            _retiredTextures.Add(new RetiredTexture
            {
                handle = oldTexture,
                framesLeft = RETIRE_DELAY_FRAMES,
            });
        }

        return newSrv != 0;
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

    public uint2 GetCurrentSize(int frameIndex = 0)
    {
        ref readonly var slot = ref _slots[GetSlotIndex(frameIndex)];
        return new uint2(Math.Max(1u, slot.width), Math.Max(1u, slot.height));
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

            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].textureHandle.IsValid)
                {
                    _resourceDatabase.ReleaseResource(_slots[i].textureHandle.AsResource());
                    _slots[i] = default;
                    _slots[i].textureHandle = Handle<GPUTexture>.Invalid;
                }
            }
        }

        _retiredTextures.Clear();
    }
}
