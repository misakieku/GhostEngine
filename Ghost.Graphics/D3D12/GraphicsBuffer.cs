using Ghost.Graphics.Data;
using System.Runtime.CompilerServices;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

public unsafe class GraphicsBuffer : GraphicsResource
{
    public enum Usage
    {
        Common,
        Vertex,
        Index,
        CopySource,
        CopyDestination,
        Structured,
        Raw,
        Append,
        Counter,
        Indirect,
        Constant,
    }

    private readonly Usage _usage;

    public Usage BufferUsage => _usage;

    private GraphicsBuffer(Usage usage, in BufferHandle handle, bool tempResource = false)
        : base(handle.ResourceHandle, tempResource)
    {
        _usage = usage;
    }

    public static GraphicsBuffer Create(uint sizeInBytes, Usage usage, bool tempResource = false)
    {
        var heapType = HeapType.Default;
        var state = ResourceStates.Common;
        switch (usage)
        {
            case Usage.Vertex:
                heapType = HeapType.Default;
                state = ResourceStates.VertexAndConstantBuffer;
                break;
            case Usage.Index:
                heapType = HeapType.Default;
                state = ResourceStates.IndexBuffer;
                break;
            case Usage.CopySource:
                heapType = HeapType.Readback;
                state = ResourceStates.CopySource;
                break;
            case Usage.CopyDestination:
                heapType = HeapType.Default;
                state = ResourceStates.CopyDest;
                break;
            case Usage.Structured:
            case Usage.Raw:
            case Usage.Append:
            case Usage.Counter:
                heapType = HeapType.Default;
                state = ResourceStates.AllShaderResource | ResourceStates.UnorderedAccess;
                break;
            case Usage.Indirect:
                heapType = HeapType.Default;
                state = ResourceStates.IndirectArgument;
                break;
            case Usage.Constant:
                heapType = HeapType.Upload;
                state = ResourceStates.GenericRead;
                break;
            default:
                break;
        }

        var handle = GraphicsPipeline.ResourceAllocator.CreateBuffer(sizeInBytes, heapType, initialState: state, tempResource: tempResource);
        return new GraphicsBuffer(usage, in handle, tempResource);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetData<T>(Span<T> data, uint offset)
        where T : unmanaged
    {
        fixed (T* ptr = data)
        {
            SetData(ptr, offset, (uint)data.Length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetData<T>(T* data, uint offset, uint length)
        where T : unmanaged
    {
        var size = (uint)(length * sizeof(T));
        SetData((void*)data, offset, size);
    }

    public unsafe void SetData(void* data, uint offset, uint size)
    {
        ThrowIfDisposed();

        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "Data pointer cannot be null.");
        }

        if (size > Size)
        {
            throw new ArgumentException($"Data size {size} exceeds buffer size {Size}.", nameof(size));
        }

        var range = new Win32.Graphics.Direct3D12.Range(offset, size);

        void* mappedPtr;
        ThrowIfFailed(NativeResource.Ptr->Map(0, &range, &mappedPtr));

        Unsafe.CopyBlock(mappedPtr, data, size);
        NativeResource.Ptr->Unmap(0, &range);
    }
}
