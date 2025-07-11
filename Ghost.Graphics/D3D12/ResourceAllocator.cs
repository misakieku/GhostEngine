using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

internal unsafe class ResourceAllocator
{
    private readonly struct TempResourceAllocInfo
    {
        public readonly GraphicsResource resource;
        public readonly uint cpuFenceValue;

        public TempResourceAllocInfo(GraphicsResource resource, uint cpuFenceValue)
        {
            this.resource = resource;
            this.cpuFenceValue = cpuFenceValue;
        }

        public TempResourceAllocInfo(GraphicsResource resource)
            : this(resource, GraphicsPipeline.CPUFenceValue + 1)
        {
        }
    }

    private const ResourceStates _INITIALCOPYTARGETSTATE = ResourceStates.Common;
    private const ResourceStates _INITIALREADTARGETSTATE = ResourceStates.Common;
    private const ResourceStates _INITIALUAVTARGETSTATE = ResourceStates.Common;

    private const uint _MAX_BYTES = D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_A_TERM * 1024u * 1024u;

    private readonly Queue<TempResourceAllocInfo> _temResources = new();

    //public static ID3D12Resource CreateStaticBuffer<T>(
    //    ID3D12Device device,
    //    D3D12ResourceUploadBatch resourceUpload,
    //    T[] data, ResourceStates afterState,
    //    ResourceFlags flags = ResourceFlags.None)
    //    where T : unmanaged
    //{
    //    Span<T> span = data;
    //    return CreateStaticBuffer(device, resourceUpload, span, afterState, flags);
    //}

    //public static ID3D12Resource CreateStaticBuffer<T>(
    //    ID3D12Device device,
    //    D3D12ResourceUploadBatch resourceUpload,
    //    Span<T> data,
    //    ResourceStates afterState,
    //    ResourceFlags flags = ResourceFlags.None)
    //    where T : unmanaged
    //{
    //    var sizeInBytes = (uint)(sizeof(T) * data.Length);
    //    if (sizeInBytes > _MAX_BYTES)
    //    {
    //        throw new InvalidOperationException($"ERROR: Resource size too large for DirectX 12 (size {sizeInBytes})");
    //    }

    //    var buffer = device.CreateCommittedResource(
    //        HeapType.Default,
    //        HeapFlags.None,
    //        ResourceDescription.Buffer(sizeInBytes, flags),
    //        _INITIALCOPYTARGETSTATE
    //    );

    //    fixed (T* dataPtr = data)
    //    {
    //        SubresourceData initData = new()
    //        {
    //            pData = dataPtr,
    //        };

    //        resourceUpload.Upload(buffer, 0, &initData, 1);
    //        resourceUpload.Transition(buffer, ResourceStates.CopyDest, afterState);

    //        return buffer;
    //    }
    //}

    //public static ID3D12Resource CreateUploadBuffer<T>(
    //    ID3D12Device device,
    //    T[] data,
    //    ResourceFlags flags = ResourceFlags.None)
    //    where T : unmanaged
    //{
    //    var sizeInBytes = (uint)(sizeof(T) * data.Length);
    //    fixed (T* dataPtr = data)
    //    {
    //        return CreateUploadBuffer(device, sizeInBytes, dataPtr, flags);
    //    }
    //}

    //public static ID3D12Resource CreateUploadBuffer<T>(
    //    ID3D12Device device,
    //    Span<T> data,
    //    ResourceFlags flags = ResourceFlags.None)
    //    where T : unmanaged
    //{
    //    var sizeInBytes = (uint)(sizeof(T) * data.Length);
    //    fixed (T* dataPtr = data)
    //    {
    //        return CreateUploadBuffer(device, sizeInBytes, dataPtr, flags);
    //    }
    //}

    //public static ID3D12Resource CreateUploadBuffer(
    //    ID3D12Device device,
    //    uint sizeInBytes,
    //    void* data = default,
    //    ResourceFlags flags = ResourceFlags.None)
    //{
    //    if (sizeInBytes > _MAX_BYTES)
    //    {
    //        throw new InvalidOperationException($"ERROR: Resource size too large for DirectX 12 (size {sizeInBytes})");
    //    }

    //    var buffer = device.CreateCommittedResource(
    //        HeapType.Upload,
    //        HeapFlags.None,
    //        ResourceDescription.Buffer(sizeInBytes, flags),
    //        ResourceStates.GenericRead
    //    );

    //    if (data is not null)
    //    {
    //        void* mappedPtr = default;
    //        buffer.Map(0, null, &mappedPtr).CheckError();
    //        Unsafe.CopyBlock(data, mappedPtr, sizeInBytes);
    //        buffer.Unmap(0, null);
    //    }

    //    return buffer;
    //}

    //public static ID3D12Resource CreateReadbackBuffer(
    //    ID3D12Device device,
    //    uint sizeInBytes,
    //    ResourceFlags flags = ResourceFlags.None)
    //{
    //    if (sizeInBytes > _MAX_BYTES)
    //    {
    //        throw new InvalidOperationException($"ERROR: Resource size too large for DirectX 12 (size {sizeInBytes})");
    //    }
    //    var buffer = device.CreateCommittedResource(
    //        HeapType.Readback,
    //        HeapFlags.None,
    //        ResourceDescription.Buffer(sizeInBytes, flags),
    //        _INITIALREADTARGETSTATE
    //    );
    //    return buffer;
    //}

    //public static ID3D12Resource CreateUAVBuffer(ID3D12Device device, uint bufferSize,
    //    ResourceStates initialState = ResourceStates.Common,
    //    ResourceFlags flags = ResourceFlags.None)
    //{
    //    if (bufferSize > _MAX_BYTES)
    //    {
    //        throw new InvalidOperationException($"ERROR: Resource size too large for DirectX 12 (size {bufferSize})");
    //    }

    //    var buffer = device.CreateCommittedResource(
    //        HeapType.Default,
    //        HeapFlags.None,
    //        ResourceDescription.Buffer(bufferSize, ResourceFlags.AllowUnorderedAccess | flags),
    //        _INITIALCOPYTARGETSTATE
    //    );

    //    return buffer;
    //}

    //public static ID3D12Resource CreateTexture2D<T>(
    //    ID3D12Device device,
    //    D3D12ResourceUploadBatch resourceUpload,
    //    uint width, uint height, Format format,
    //    Span<T> data,
    //    bool generateMips = false,
    //    ResourceStates afterState = ResourceStates.PixelShaderResource,
    //    ResourceFlags flags = ResourceFlags.None)
    //    where T : unmanaged
    //{
    //    if (width > D3D12.RequestTexture2DUOrVDimension || height > D3D12.RequestTexture2DUOrVDimension)
    //    {
    //        throw new InvalidOperationException($"ERROR: Resource dimensions too large for DirectX 12 (2D: size {width} by {height})");
    //    }

    //    ushort mipLevels = 1;
    //    if (generateMips)
    //    {
    //        generateMips = resourceUpload.IsSupportedForGenerateMips(format);
    //        if (generateMips)
    //        {
    //            mipLevels = (ushort)TextureUtility.CountMips(width, height);
    //        }
    //    }


    //    var texture = device.CreateCommittedResource(
    //        HeapType.Default,
    //        HeapFlags.None,
    //        ResourceDescription.Texture2D(format, width, height, 1, mipLevels, 1, 0, flags),
    //        _INITIALCOPYTARGETSTATE
    //    );

    //    fixed (T* dataPtr = data)
    //    {
    //        FormatHelper.GetSurfaceInfo(format, width, height, out var rowPitch, out var slicePitch);
    //        SubresourceData initData = new()
    //        {
    //            pData = dataPtr,
    //            RowPitch = (nint)rowPitch,
    //            SlicePitch = (nint)slicePitch
    //        };

    //        resourceUpload.Upload(texture, 0, &initData, 1);
    //        resourceUpload.Transition(texture, ResourceStates.CopyDest, afterState);

    //        if (generateMips)
    //        {
    //            resourceUpload.GenerateMips(texture);
    //        }

    //        return texture;
    //    }
    //}

    public GraphicsResource CreateUploadBuffer(uint sizeInBytes, bool tempResource = false, ResourceFlags flags = ResourceFlags.None)
    {
        if (sizeInBytes > _MAX_BYTES)
        {
            throw new InvalidOperationException($"ERROR: Resource size too large for DirectX 12 (size {sizeInBytes})");
        }

        var heapProperties = new HeapProperties(HeapType.Upload);
        var resourceDescription = ResourceDescription.Buffer(sizeInBytes, flags);

        ComPtr<ID3D12Resource> buffer = default;
        GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr->CreateCommittedResource(
            &heapProperties,
            HeapFlags.None,
            &resourceDescription,
            ResourceStates.GenericRead,
            null,
            __uuidof<ID3D12Resource>(),
            buffer.GetVoidAddressOf()
        );

        var resource = new GraphicsResource(buffer.Move(), tempResource);
        if (tempResource)
        {
            _temResources.Enqueue(new(resource));
        }

        return resource;
    }

    public GraphicsResource CreateCopyDestinationBuffer(uint sizeInBytes, bool tempResource = false, ResourceFlags flags = ResourceFlags.None)
    {
        if (sizeInBytes > _MAX_BYTES)
        {
            throw new InvalidOperationException($"ERROR: Resource size too large for DirectX 12 (size {sizeInBytes})");
        }

        var heapProperties = new HeapProperties(HeapType.Default);
        var resourceDescription = ResourceDescription.Buffer(sizeInBytes, flags);

        ComPtr<ID3D12Resource> buffer = default;
        GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr->CreateCommittedResource(
            &heapProperties,
            HeapFlags.None,
            &resourceDescription,
            ResourceStates.Common,
            null,
            __uuidof<ID3D12Resource>(),
            buffer.GetVoidAddressOf()
        );

        var resource = new GraphicsResource(buffer.Move(), tempResource);
        if (tempResource)
        {
            _temResources.Enqueue(new(resource));
        }

        return resource;
    }

    public void ReleaseTempResource()
    {
        while (_temResources.Count > 0)
        {
            var info = _temResources.Peek();
            if (info.cpuFenceValue > GraphicsPipeline.GPUFenceValue)
            {
                break;
            }

            info.resource.DisposeInternal();
            _temResources.Dequeue();
        }
    }

    public void Dispose()
    {
        ReleaseTempResource();
    }
}