using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.Services;

public sealed partial class ResourceManager : IDisposable
{
    private const uint PALETTE_BUFFER_INITIAL_CAPACITY = 64;

    private readonly struct ResourceReturnEntry
    {
        public readonly Handle<GPUResource> handle;
        public readonly ulong returnFrame;

        public ResourceReturnEntry(Handle<GPUResource> handle, ulong returnFrame)
        {
            this.handle = handle;
            this.returnFrame = returnFrame;
        }
    }

    private readonly IRenderDevice _renderDevice;
    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;

    private UnsafeSlotMap<Mesh> _meshes;
    private UnsafeSlotMap<Material> _materials;
    private UnsafeSlotMap<Shader> _shaders;
    private UnsafeSlotMap<ComputeShader> _computeShaders;

    private readonly MaterialPaletteStore _materialPalettes;
    private readonly StaticSampler _staticSampler;

    // Persistent GPU buffers for the two-buffer material palette indirection.
    private Handle<GPUBuffer> _paletteOffsetBuffer;
    private Handle<GPUBuffer> _materialIndexBuffer;
    private uint _paletteOffsetCapacity;
    private uint _materialIndexCapacity;

    // Global Material Buffer Pool
    private const uint MATERIAL_POOL_INITIAL_CAPACITY = 4 * 1024 * 1024; // 4 MB
    private const uint MATERIAL_BLOCK_ALIGNMENT = 16u;

    private struct MaterialPoolFreeBlock
    {
        public uint offset;
        public uint size;
    }

    private Handle<GPUBuffer> _materialPoolBuffer;
    private uint _materialPoolCapacity;
    private uint _materialPoolAllocatedBytes;
    private UnsafeList<MaterialPoolFreeBlock> _materialPoolFreeList;
    private UnsafeList<Handle<Material>> _dirtyMaterials;
    private UnsafeHashSet<int> _dirtyMaterialSet;

    // TODO: Any better way? System.Threading.Lock is very fast though, it use spin lock before entering kernel.
    // rw lock slim is an option but it has more overhead on read. Because more than 90% of the time we are reading, it may not be a good option.
    // Plus UnsafeSlotMap use jagged array internally, which means we can have concurrent read and write, but not add and remove, on different slots without any issue, so we only need to lock when writing to those slots.
    private readonly Lock _meshWriteLock;
    private readonly Lock _materialWriteLock;
    private readonly Lock _shaderWriteLock;
    private readonly Lock _computeShaderWriteLock;

    private ulong _submittedFrame;

    private bool _disposed;

    /// <summary>
    /// Returns the bindless descriptor heap index for the palette offset GPU buffer.
    /// Valid after the first <see cref="UploadMaterialPaletteData"/> call.
    /// </summary>
    public uint PaletteOffsetBufferBindlessIndex => _resourceDatabase.GetBindlessIndex(_paletteOffsetBuffer.AsResource());

    /// <summary>
    /// Returns the bindless descriptor heap index for the material index GPU buffer.
    /// Valid after the first <see cref="UploadMaterialPaletteData"/> call.
    /// </summary>
    public uint MaterialIndexBufferBindlessIndex => _resourceDatabase.GetBindlessIndex(_materialIndexBuffer.AsResource());

    /// <summary>
    /// Returns the bindless descriptor heap index for the global material GPU buffer.
    /// </summary>
    public uint MaterialBufferBindlessIndex => _resourceDatabase.GetBindlessIndex(_materialPoolBuffer.AsResource());

    /// <summary>
    /// Returns the global material GPU buffer handle.
    /// </summary>
    public Handle<GPUBuffer> MaterialPoolBuffer => _materialPoolBuffer;

    public IResourceAllocator ResourceAllocator => _resourceAllocator;
    public StaticSampler StaticSampler => _staticSampler;

    public ResourceManager(IRenderDevice renderDevice, IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase)
    {
        _renderDevice = renderDevice;
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;

        _meshes = new UnsafeSlotMap<Mesh>(64, AllocationHandle.Persistent);
        _materials = new UnsafeSlotMap<Material>(64, AllocationHandle.Persistent);
        _shaders = new UnsafeSlotMap<Shader>(16, AllocationHandle.Persistent);
        _computeShaders = new UnsafeSlotMap<ComputeShader>(16, AllocationHandle.Persistent);

        _materialPalettes = new MaterialPaletteStore();
        _staticSampler = new StaticSampler(resourceAllocator, resourceDatabase);

        _meshWriteLock = new Lock();
        _materialWriteLock = new Lock();
        _shaderWriteLock = new Lock();
        _computeShaderWriteLock = new Lock();
        _deferredPoolReturns = new UnsafeQueue<ResourceReturnEntry>(32, AllocationHandle.Persistent);

        // Create initial GPU palette buffers. These grow on demand in UploadMaterialPaletteData.
        _paletteOffsetCapacity = PALETTE_BUFFER_INITIAL_CAPACITY;
        _materialIndexCapacity = PALETTE_BUFFER_INITIAL_CAPACITY * 4;
        _paletteOffsetBuffer = CreatePaletteBuffer(_paletteOffsetCapacity, "PaletteOffsetBuffer");
        _materialIndexBuffer = CreatePaletteBuffer(_materialIndexCapacity, "MaterialIndexBuffer");

        // Initialize Global Material Buffer Pool
        _materialPoolCapacity = MATERIAL_POOL_INITIAL_CAPACITY;
        _materialPoolAllocatedBytes = 0;
        _materialPoolBuffer = CreateMaterialBuffer(_materialPoolCapacity, "GlobalMaterialBuffer");
        _materialPoolFreeList = new UnsafeList<MaterialPoolFreeBlock>(16, AllocationHandle.Persistent);
        _dirtyMaterials = new UnsafeList<Handle<Material>>(32, AllocationHandle.Persistent);
        _dirtyMaterialSet = new UnsafeHashSet<int>(32, AllocationHandle.Persistent);

        InitializeTransientPool();
    }

    ~ResourceManager()
    {
        Dispose();
    }

    internal void BeginFrame(ulong submittedFrame)
    {
        Logger.DebugAssert(!_disposed);
        _submittedFrame = submittedFrame;
    }

    internal void EndFrame(ulong completedFrame)
    {
        Logger.DebugAssert(!_disposed);
        _materialPalettes.EndFrame(_submittedFrame, completedFrame);
        EndFramePool(completedFrame);
    }

    public Handle<Mesh> RegisterMesh([Owner] ref Mesh mesh)
    {
        Logger.DebugAssert(!_disposed);

        lock (_meshWriteLock)
        {
            var id = _meshes.Add(mesh, out var generation);
            return new Handle<Mesh>(id, generation);
        }
    }

    private uint AllocateMaterialBlock(uint size)
    {
        if (size == 0)
        {
            return 0;
        }

        var alignedSize = (size + (MATERIAL_BLOCK_ALIGNMENT - 1u)) & ~(MATERIAL_BLOCK_ALIGNMENT - 1u);

        for (var i = 0; i < _materialPoolFreeList.Count; i++)
        {
            ref var block = ref _materialPoolFreeList[i];
            if (block.size >= alignedSize)
            {
                var offset = block.offset;
                if (block.size == alignedSize)
                {
                    _materialPoolFreeList.RemoveAtSwapBack(i);
                }
                else
                {
                    block.offset += alignedSize;
                    block.size -= alignedSize;
                }
                return offset;
            }
        }

        var newOffset = _materialPoolAllocatedBytes;
        _materialPoolAllocatedBytes += alignedSize;
        return newOffset;
    }

    private void FreeMaterialBlock(uint offset, uint size)
    {
        if (size == 0)
        {
            return;
        }

        var alignedSize = (size + (MATERIAL_BLOCK_ALIGNMENT - 1u)) & ~(MATERIAL_BLOCK_ALIGNMENT - 1u);
        _materialPoolFreeList.Add(new MaterialPoolFreeBlock { offset = offset, size = alignedSize });
    }

    /// <summary>
    /// Creates a new material instance using the specified shader.
    /// </summary>
    /// <param name="shader">The identifier of the shader to associate with the new material.</param>
    /// <param name="name">The name of the material.</param>
    /// <returns>An <see cref="Handle{Material}"/> representing the newly created material.</returns>
    public Handle<Material> CreateMaterial(Handle<Shader> shader, string? name = null)
    {
        Logger.DebugAssert(!_disposed);

        var shaderRef = GetShaderReference(shader);
        if (shaderRef.IsFailure)
        {
            return Handle<Material>.Invalid;
        }

        uint poolOffset;
        lock (_materialWriteLock)
        {
            poolOffset = AllocateMaterialBlock(shaderRef.Value.PropertyBufferSize);
        }

        var material = new Material();
        if (material.SetShader(shader, poolOffset, this, _resourceDatabase, _resourceAllocator) != Error.None)
        {
            lock (_materialWriteLock)
            {
                FreeMaterialBlock(poolOffset, shaderRef.Value.PropertyBufferSize);
            }
            return Handle<Material>.Invalid;
        }

        lock (_materialWriteLock)
        {
            var id = _materials.Add(material, out var generation);
            var handle = new Handle<Material>(id, generation);
            if (_dirtyMaterialSet.Add(handle.ID))
            {
                _dirtyMaterials.Add(handle);
            }
            return handle;
        }
    }

    /// <summary>
    /// Sets property data for a material.
    /// </summary>
    public unsafe Error SetMaterialProperty<T>(Handle<Material> handle, scoped in T data)
        where T : unmanaged
    {
        var span = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in data));
        return SetRawMaterialProperty(handle, span);
    }

    /// <summary>
    /// Sets raw property bytes for a material.
    /// </summary>
    public Error SetRawMaterialProperty(Handle<Material> handle, ReadOnlySpan<byte> data)
    {
        Logger.DebugAssert(!_disposed);

        lock (_materialWriteLock)
        {
            var r = GetMaterialReference(handle);
            if (r.IsFailure)
            {
                return r.Error;
            }

            ref var material = ref r.Value;
            var oldHasAlpha = material.HasAlphaClip;
            var err = material.SetRawPropertyCache(data);
            if (err != Error.None)
            {
                return err;
            }

            if (material.HasAlphaClip != oldHasAlpha)
            {
                _materialPalettes.MarkGpuDirty();
            }

            if (_dirtyMaterialSet.Add(handle.ID))
            {
                _dirtyMaterials.Add(handle);
            }

            return Error.None;
        }
    }

    /// <summary>
    /// Creates a new shader and returns its unique identifier.
    /// </summary>
    /// <returns>An <see cref="Handle{Shader}"/> representing the newly created shader.</returns>
    /// <param name="descriptor">The viewGroup containing the shader's properties and passes. Register a empty shader if descriptor is null.</param>
    public Handle<Shader> CreateShader(GraphicsShaderDescriptor? descriptor)
    {
        Logger.DebugAssert(!_disposed);

        var shader = descriptor == null ? default : new Shader(descriptor);

        lock (_shaderWriteLock)
        {
            var id = _shaders.Add(shader, out var generation);
            return new Handle<Shader>(id, generation);
        }
    }

    /// <summary>
    /// Creates a new compute shader and returns its unique identifier.
    /// </summary>
    /// <returns>An <see cref="Handle{ComputeShader}"/> representing the newly created compute shader.</returns>
    /// <param name="descriptor">The viewGroup containing the compute shader's properties and passes. Register a empty compute shader if descriptor is null.</param>
    public Handle<ComputeShader> CreateComputeShader(ComputeShaderDescriptor? descriptor)
    {
        Logger.DebugAssert(!_disposed);

        var computeShader = descriptor == null ? default : new ComputeShader(descriptor);

        lock (_computeShaderWriteLock)
        {
            var id = _computeShaders.Add(computeShader, out var generation);
            return new Handle<ComputeShader>(id, generation);
        }
    }

    /// <summary>
    /// Determines whether a mesh with the specified Handle exists.
    /// </summary>
    /// <param name="handle">The handle of the mesh to check for existence. Cannot be null.</param>
    /// <returns>true if a mesh with the specified Handle exists; otherwise, false.</returns>
    public bool HasMesh(Handle<Mesh> handle)
    {
        Logger.DebugAssert(!_disposed);
        return _meshes.Contains(handle.ID, handle.Generation);
    }

    /// <summary>
    /// Returns a reference to the mesh associated with the specified handle.
    /// </summary>
    /// <param name="handle">The handle of the mesh to retrieve. Must refer to a valid mesh; otherwise, the behavior is undefined.</param>
    /// <returns>A result containing a reference to the mesh corresponding to the specified handle, or an error status if the handle is invalid.</returns>
    public RefResult<Mesh, Error> GetMeshReference(Handle<Mesh> handle)
    {
        ref var mesh = ref _meshes.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        return RefResult<Mesh, Error>.Success(ref mesh);
    }

    /// <summary>
    /// Releases the mesh heap associated with the specified handle, freeing any resources held by it. Includes both CPU and GPU resources.
    /// </summary>
    /// <param name="handle">The handle of the mesh to release. Must refer to a mesh that was previously created and not already released.</param>
    public void ReleaseMesh(Handle<Mesh> handle)
    {
        Logger.DebugAssert(!_disposed);

        lock (_meshWriteLock)
        {
            if (_meshes.Remove(handle.ID, handle.Generation, out var mesh))
            {
                mesh.ReleaseResource(_resourceDatabase);
            }
        }
    }

    /// <summary>
    /// Determines whether a material with the specified handle exists in the collection.
    /// </summary>
    /// <param name="handle">The handle of the material to check for existence.</param>
    /// <returns>true if a material with the specified handle exists; otherwise, false.</returns>
    public bool HasMaterial(Handle<Material> handle)
    {
        Logger.DebugAssert(!_disposed);
        return _materials.Contains(handle.ID, handle.Generation);
    }

    /// <summary>
    /// Gets a reference to the material associated with the specified handle.
    /// </summary>
    /// <param name="handle">The handle of the material to retrieve. Must refer to a valid material.</param>
    /// <returns>A result containing a reference to the material corresponding to the specified handle, or an error status if the handle is invalid.</returns>
    public RefResult<Material, Error> GetMaterialReference(Handle<Material> handle)
    {
        ref var material = ref _materials.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        return RefResult<Material, Error>.Success(ref material);
    }

    /// <summary>
    /// Releases the material associated with the specified handle, making it available for reuse or disposal.
    /// </summary>
    /// <param name="handle">The handle of the material to release. Must refer to a material that has been previously acquired.</param>
    public void ReleaseMaterial(Handle<Material> handle)
    {
        Logger.DebugAssert(!_disposed);

        lock (_materialWriteLock)
        {
            if (_materials.Remove(handle.ID, handle.Generation, out var material))
            {
                FreeMaterialBlock(material.PoolOffset, material.PropertySize);
                material.ReleaseResource(_resourceDatabase);
                _dirtyMaterialSet.Remove(handle.ID);
            }
        }
    }

    /// <summary>
    /// Returns an existing material palette index for the specified material sequence or creates a new one.
    /// </summary>
    /// <param name="materials">The ordered material list for the palette.</param>
    /// <returns>The palette index. Index 0 represents an empty palette.</returns>
    public int GetOrCreateMaterialPalette(ReadOnlySpan<Handle<Material>> materials)
    {
        Logger.DebugAssert(!_disposed);

        foreach (var material in materials)
        {
            if (material.IsInvalid || !HasMaterial(material))
            {
                return 0;
            }
        }

        return _materialPalettes.InsertOrGet(materials);
    }

    /// <summary>
    /// Determines whether the specified material palette index is valid.
    /// </summary>
    /// <param name="paletteID">The palette index to validate.</param>
    public bool HasMaterialPalette(Identifier<MaterialPalette> paletteID)
    {
        Logger.DebugAssert(!_disposed);
        return _materialPalettes.IsValid(paletteID);
    }

    /// <summary>
    /// Gets metadata for a material palette entry.
    /// </summary>
    /// <param name="paletteID">The palette index to query.</param>
    public MaterialPalette GetMaterialPaletteInfo(Identifier<MaterialPalette> paletteID)
    {
        Logger.DebugAssert(!_disposed);
        return _materialPalettes.GetInfo(paletteID);
    }

    /// <summary>
    /// Gets a material handle from a palette entry by local material index.
    /// </summary>
    /// <param name="paletteID">The palette index to query.</param>
    /// <param name="localMaterialIndex">The material slot inside the palette.</param>
    public Handle<Material> GetMaterialPaletteMaterial(Identifier<MaterialPalette> paletteID, int localMaterialIndex)
    {
        Logger.DebugAssert(!_disposed);
        return _materialPalettes.GetMaterial(paletteID, localMaterialIndex);
    }

    /// <summary>
    /// Uploads all dirty materials to the GPU. Must be called once per frame on the render thread, before any draw calls.
    /// </summary>
    /// <param name="ctx">The render context to use for the upload.</param>
    public void UploadMaterials(RenderContext ctx)
    {
        Logger.DebugAssert(!_disposed);

        lock (_materialWriteLock)
        {
            if (_materialPoolAllocatedBytes > _materialPoolCapacity)
            {
                var newCapacity = Math.Max(_materialPoolCapacity * 2u, _materialPoolAllocatedBytes);
                var newBuffer = CreateMaterialBuffer(newCapacity, "GlobalMaterialBuffer_Resized");

                ctx.CommandBuffer.CopyBuffer(newBuffer, _materialPoolBuffer, 0, 0, _materialPoolCapacity);

                _resourceDatabase.ReleaseResource(_materialPoolBuffer.AsResource());
                _materialPoolBuffer = newBuffer;
                _materialPoolCapacity = newCapacity;
            }

            if (_dirtyMaterials.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _dirtyMaterials.Count; i++)
            {
                var handle = _dirtyMaterials[i];
                var r = GetMaterialReference(handle);
                if (r.IsFailure)
                {
                    continue;
                }

                ref var material = ref r.Value;
                if (!material.IsDirty)
                {
                    continue;
                }

                var rawData = material.GetRawPropertyCache();
                if (rawData.Length > 0)
                {
                    ctx.UploadBufferRange(_materialPoolBuffer, rawData, material.PoolOffset);
                }

                material.ClearDirty();
            }

            _dirtyMaterials.Clear();
            _dirtyMaterialSet.Clear();
        }
    }

    /// <summary>
    /// Resolves dirty material palette data and uploads it to the GPU.
    /// Must be called once per frame on the render thread, before any draw calls.
    /// Handles buffer growth with copy-on-resize semantics (same pattern as GPUScene).
    /// </summary>
    public void UploadMaterialPaletteData(RenderContext ctx)
    {
        Logger.DebugAssert(!_disposed);

        if (!_materialPalettes.IsGpuDirty)
        {
            return;
        }

        // Resolve material handles → packed material indices.
        _materialPalettes.ResolveMaterialIndices(static (materialHandle, state) =>
        {
            var self = (ResourceManager)state!;
            var r = self.GetMaterialReference(materialHandle);
            if (r.IsFailure || !r.Value.IsCreated)
            {
                return 0u;
            }

            return MaterialEncoding.Encode(r.Value.PoolOffset, r.Value.VariantIndex, r.Value.HasAlphaClip);
        }, this);

        var offsets = _materialPalettes.PaletteOffsets;
        var indices = _materialPalettes.MaterialIndices;

        _materialPalettes.GetDirtyRanges(
            out var offsetStart, out var offsetEnd,
            out var indicesStart, out var indicesEnd);

        // ── Resize PaletteOffsetBuffer if needed ──
        if ((uint)offsets.Length > _paletteOffsetCapacity)
        {
            var newCapacity = Math.Max(_paletteOffsetCapacity * 2, (uint)offsets.Length);
            var newBuffer = CreatePaletteBuffer(newCapacity, "PaletteOffsetBuffer_Resized");

            ctx.CommandBuffer.CopyBuffer(newBuffer, _paletteOffsetBuffer, 0, 0, _paletteOffsetCapacity * sizeof(uint));

            _resourceDatabase.ReleaseResource(_paletteOffsetBuffer.AsResource());
            _paletteOffsetBuffer = newBuffer;
            _paletteOffsetCapacity = newCapacity;

            // Full upload needed after resize.
            offsetStart = 0;
            offsetEnd = offsets.Length;
        }

        // ── Resize MaterialIndexBuffer if needed ──
        if ((uint)indices.Length > _materialIndexCapacity)
        {
            var newCapacity = Math.Max(_materialIndexCapacity * 2, (uint)indices.Length);
            var newBuffer = CreatePaletteBuffer(newCapacity, "MaterialIndexBuffer_Resized");

            ctx.CommandBuffer.CopyBuffer(newBuffer, _materialIndexBuffer, 0, 0, _materialIndexCapacity * sizeof(uint));

            _resourceDatabase.ReleaseResource(_materialIndexBuffer.AsResource());
            _materialIndexBuffer = newBuffer;
            _materialIndexCapacity = newCapacity;

            indicesStart = 0;
            indicesEnd = indices.Length;
        }

        // ── Upload dirty ranges ──
        if (offsetEnd > offsetStart)
        {
            var dirtyOffsets = offsets.Slice(offsetStart, offsetEnd - offsetStart);
            ctx.UploadBufferRange(_paletteOffsetBuffer, dirtyOffsets, (uint)(offsetStart * sizeof(uint)));
        }

        if (indicesEnd > indicesStart)
        {
            var dirtyIndices = indices.Slice(indicesStart, indicesEnd - indicesStart);
            ctx.UploadBufferRange(_materialIndexBuffer, dirtyIndices, (uint)(indicesStart * sizeof(uint)));
        }

        _materialPalettes.ClearDirty();
    }

    private Handle<GPUBuffer> CreatePaletteBuffer(uint capacity, string name)
    {
        var desc = new BufferDesc
        {
            Size = capacity * sizeof(uint),
            Stride = sizeof(uint),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };
        return _resourceAllocator.CreateBuffer(in desc, name);
    }

    private Handle<GPUBuffer> CreateMaterialBuffer(uint sizeInBytes, string name)
    {
        var desc = new BufferDesc
        {
            Size = sizeInBytes,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };
        return _resourceAllocator.CreateBuffer(in desc, name);
    }

    /// <summary>
    /// Releases the material palette associated with the specified palette ID.
    /// </summary>
    /// <param name="paletteID">The palette index to release.</param>
    public void ReleaseMaterialPalette(Identifier<MaterialPalette> paletteID)
    {
        Logger.DebugAssert(!_disposed);
        _materialPalettes.Release(paletteID);
    }

    /// <summary>
    /// Determines whether a shader with the specified identifier exists in the collection.
    /// </summary>
    /// <param name="id">The identifier of the shader to check for existence.</param>
    /// <returns>true if a shader with the specified identifier exists; otherwise, false.</returns>
    public bool HasShader(Handle<Shader> id)
    {
        Logger.DebugAssert(!_disposed);
        return _shaders.Contains(id.ID, id.Generation);
    }

    /// <summary>
    /// Returns a reference to the shader associated with the specified identifier.
    /// </summary>
    /// <param name="handle">The identifier of the shader to retrieve. Must refer to a valid shader.</param>
    /// <returns>A result containing a reference to the shader corresponding to the specified identifier, or an error status if the identifier is invalid.</returns>
    public RefResult<Shader, Error> GetShaderReference(Handle<Shader> handle)
    {
        ref var shader = ref _shaders.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        return RefResult<Shader, Error>.Success(ref shader);
    }

    /// <summary>
    /// Releases the shader associated with the specified identifier, freeing any resources allocated to it.
    /// </summary>
    /// <param name="handle">The identifier of the shader to release. Must refer to a valid, previously created shader.</param>
    public void ReleaseShader(Handle<Shader> handle)
    {
        Logger.DebugAssert(!_disposed);

        lock (_shaderWriteLock)
        {
            if (_shaders.Remove(handle.ID, handle.Generation, out var shader))
            {
                shader.ReleaseResource(_resourceDatabase);
            }
        }
    }

    /// <summary>
    /// Determines whether a compute shader with the specified identifier exists in the collection.
    /// </summary>
    /// <param name="id">The identifier of the compute shader to check for existence.</param>
    /// <returns>true if a compute shader with the specified identifier exists; otherwise, false.</returns>
    public bool HasComputeShader(Handle<ComputeShader> id)
    {
        Logger.DebugAssert(!_disposed);
        return _computeShaders.Contains(id.ID, id.Generation);
    }

    /// <summary>
    /// Returns a reference to the compute shader associated with the specified identifier.
    /// </summary>
    /// <param name="handle">The identifier of the compute shader to retrieve. Must refer to a valid ComputeShader.</param>
    /// <returns>A result containing a reference to the compute shader corresponding to the specified identifier, or an error status if the identifier is invalid.</returns>
    public RefResult<ComputeShader, Error> GetComputeShaderReference(Handle<ComputeShader> handle)
    {
        ref var computeShader = ref _computeShaders.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        return RefResult<ComputeShader, Error>.Success(ref computeShader);
    }

    /// <summary>
    /// Releases the compute shader associated with the specified identifier, freeing any resources allocated to it.
    /// </summary>
    /// <param name="handle">The identifier of the compute shader to release. Must refer to a valid, previously created ComputeShader.</param>
    public void ReleaseComputeShader(Handle<ComputeShader> handle)
    {
        Logger.DebugAssert(!_disposed);

        lock (_computeShaderWriteLock)
        {
            if (_computeShaders.Remove(handle.ID, handle.Generation, out var computeShader))
            {
                computeShader.ReleaseResource(_resourceDatabase);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (ref var mesh in _meshes)
        {
            mesh.ReleaseResource(_resourceDatabase);
        }

        foreach (ref var material in _materials)
        {
            material.ReleaseResource(_resourceDatabase);
        }

        foreach (ref var shader in _shaders)
        {
            shader.ReleaseResource(_resourceDatabase);
        }

        _meshes.Dispose();
        _materials.Dispose();
        _shaders.Dispose();
        _computeShaders.Dispose();
        _materialPalettes.Dispose();
        _staticSampler.Dispose();

        _resourceDatabase.ReleaseResource(_paletteOffsetBuffer.AsResource());
        _resourceDatabase.ReleaseResource(_materialIndexBuffer.AsResource());
        _resourceDatabase.ReleaseResource(_materialPoolBuffer.AsResource());
        _materialPoolFreeList.Dispose();
        _dirtyMaterials.Dispose();
        _dirtyMaterialSet.Dispose();

        DisposeTransientPool();
        DisposePersistentPool();
        _deferredPoolReturns.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
