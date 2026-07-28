using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Services;

public partial class ResourceManager
{
    private UnsafeHashMap<ResourceDesc, UnsafeStack<Handle<GPUResource>>> _resourcePool = new(128, AllocationHandle.Persistent);

    public Handle<GPUResource> CreatePooledResource(scoped in ResourceDesc desc)
    {
        ref var stack = ref _resourcePool.GetValueRefOrAddDefault(desc, out var exists);
        if (!exists)
        {
            stack = new UnsafeStack<Handle<GPUResource>>(4, AllocationHandle.Persistent);
        }

        if (stack.TryPop(out var handle))
        {
            Logger.DebugAssert(handle.IsValid, $"Resource handle for {desc} is invalid.");
            Logger.DebugAssert(_resourceDatabase.GetResourceDescription(handle).Value == desc);

            return handle;
        }

        return desc.Type switch
        {
            ResourceType.Texture => _resourceAllocator.CreateTexture(in desc.TextureDescriptor).AsResource(),
            ResourceType.Buffer => _resourceAllocator.CreateBuffer(in desc.BufferDescriptor).AsResource(),
            _ => throw new NotSupportedException($"Resource type {desc.Type} is not supported."),
        };
    }

    public Handle<GPUBuffer> CreatePooledBuffer(scoped in BufferDesc desc)
    {
        var resourceDesc = ResourceDesc.Buffer(desc);
        var handle = CreatePooledResource(resourceDesc);
        return handle.AsBuffer();
    }

    public Handle<GPUTexture> CreatePooledTexture(scoped in TextureDesc desc)
    {
        var resourceDesc = ResourceDesc.Texture(desc);
        var handle = CreatePooledResource(resourceDesc);
        return handle.AsTexture();
    }

    public void ReleasePooledResource(Handle<GPUResource> handle)
    {
        var (desc, error) = _resourceDatabase.GetResourceDescription(handle);
        if (error.IsFailure)
        {
            return;
        }

        ref var stack = ref _resourcePool.GetValueRefOrAddDefault(desc, out var exists);
        if (!exists)
        {
            stack = new UnsafeStack<Handle<GPUResource>>(4, AllocationHandle.Persistent);
        }

        stack.Push(handle);
    }

    private void DisposePersistentPool()
    {
        foreach (var kvp in _resourcePool)
        {
            var stack = kvp.Value;
            foreach (var handle in stack)
            {
                _resourceDatabase.ReleaseResourceImmediately(handle);
            }

            stack.Dispose();
        }

        _resourcePool.Dispose();
    }
}