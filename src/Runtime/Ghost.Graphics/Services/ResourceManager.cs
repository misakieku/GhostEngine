using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics.Geometry;

namespace Ghost.Graphics.Services;

public sealed partial class ResourceManager : IDisposable
{
    private const uint _PALETTE_BUFFER_INITIAL_CAPACITY = 64;

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

    // Persistent GPU buffers for the two-buffer material palette indirection.
    private Handle<GPUBuffer> _paletteOffsetBuffer;
    private Handle<GPUBuffer> _materialIndexBuffer;
    private uint _paletteOffsetCapacity;
    private uint _materialIndexCapacity;

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

        _meshWriteLock = new Lock();
        _materialWriteLock = new Lock();
        _shaderWriteLock = new Lock();
        _computeShaderWriteLock = new Lock();

        // Create initial GPU palette buffers. These grow on demand in UploadMaterialPaletteData.
        _paletteOffsetCapacity = _PALETTE_BUFFER_INITIAL_CAPACITY;
        _materialIndexCapacity = _PALETTE_BUFFER_INITIAL_CAPACITY * 4;
        _paletteOffsetBuffer = CreatePaletteBuffer(_paletteOffsetCapacity, "PaletteOffsetBuffer");
        _materialIndexBuffer = CreatePaletteBuffer(_materialIndexCapacity, "MaterialIndexBuffer");
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

    /// <summary>
    /// Creates a new mesh from the specified vertex and index data.
    /// </summary>
    /// <param name="vertices">A UnsafeList containing the vertices that define the geometry of the mesh.</param>
    /// <param name="indices">A UnsafeList containing the indices that specify how vertices are connected to form primitives.</param>
    /// <param name="dynamic">Indicates whether the mesh is expected to be updated frequently. If true, the underlying GPU buffers will be created with upload heap type for better CPU write performance.</param>
    /// <param name="name">The name of the mesh.</param>
    /// <returns>An <see cref="Identifier{Mesh}"/> representing the newly created mesh.</returns>
    public unsafe Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices, bool dynamic = false, string? name = null)
    {
        Logger.DebugAssert(!_disposed);

        var vertexBufferDesc = new BufferDesc
        {
            Size = (uint)(vertices.Count * sizeof(Vertex)),
            Stride = (uint)sizeof(Vertex),
            Usage = BufferUsage.Vertex | BufferUsage.ShaderResource | BufferUsage.Raw,
            HeapType = dynamic ? HeapType.Upload : HeapType.Default,
        };

        var indexBufferDesc = new BufferDesc
        {
            Size = (uint)(indices.Count * sizeof(uint)),
            Stride = sizeof(uint),
            Usage = BufferUsage.Index | BufferUsage.ShaderResource | BufferUsage.Raw,
            HeapType = dynamic ? HeapType.Upload : HeapType.Default,
        };

        var meshDataBufferDesc = new BufferDesc
        {
            Size = (uint)sizeof(MeshData),
            Stride = (uint)sizeof(MeshData),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = dynamic ? HeapType.Upload : HeapType.Default,
        };

        var hasName = name != null;
        var vertexBuffer = _resourceAllocator.CreateBuffer(in vertexBufferDesc, hasName ? $"{name}_VertexBuffer" : "VertexBuffer");
        var indexBuffer = _resourceAllocator.CreateBuffer(in indexBufferDesc, hasName ? $"{name}_IndexBuffer" : "IndexBuffer");
        var meshDataBuffer = _resourceAllocator.CreateBuffer(in meshDataBufferDesc, hasName ? $"{name}_MeshDataBuffer" : "MeshDataBuffer");

        var mesh = new Mesh
        {
            Vertices = vertices,
            Indices = indices,
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
            MeshDataBuffer = meshDataBuffer,
        };

        lock (_meshWriteLock)
        {

            var id = _meshes.Add(mesh, out var generation);
            return new Handle<Mesh>(id, generation);
        }
    }

    public Handle<Mesh> CreateEmptyMesh(string? name = null)
    {
        Logger.DebugAssert(!_disposed);

        lock (_meshWriteLock)
        {
            var id = _meshes.Add(new Mesh(), out var generation);
            return new Handle<Mesh>(id, generation);
        }
    }

    public Handle<Mesh> CreateUploadedMesh(
        Handle<GPUBuffer> vertexBuffer,
        Handle<GPUBuffer> indexBuffer,
        Handle<GPUBuffer> meshletBuffer,
        Handle<GPUBuffer> meshletVerticesBuffer,
        Handle<GPUBuffer> meshletTrianglesBuffer,
        Handle<GPUBuffer> meshletGroupBuffer,
        Handle<GPUBuffer> meshletHierarchyBuffer,
        Handle<GPUBuffer> meshDataBuffer,
        int vertexCount,
        int indexCount,
        int meshletCount,
        int lodLevelCount,
        int materialSlotCount,
        AABB boundingBox)
    {
        Logger.DebugAssert(!_disposed);

        var mesh = new Mesh
        {
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
            MeshLetBuffer = meshletBuffer,
            MeshletVerticesBuffer = meshletVerticesBuffer,
            MeshletTrianglesBuffer = meshletTrianglesBuffer,
            MeshletGroupBuffer = meshletGroupBuffer,
            MeshletHierarchyBuffer = meshletHierarchyBuffer,
            MeshDataBuffer = meshDataBuffer,
            BoundingBox = boundingBox,
        };
        mesh.SetCounts(vertexCount, indexCount);
        mesh.SetMeshletSummary(meshletCount, lodLevelCount, materialSlotCount);

        lock (_meshWriteLock)
        {
            var id = _meshes.Add(mesh, out var generation);
            return new Handle<Mesh>(id, generation);
        }
    }

    public Handle<Mesh> ReplaceMesh(Handle<Mesh> dst, Handle<Mesh> src)
    {
        Logger.DebugAssert(!_disposed);

        lock (_meshWriteLock)
        {
            ref var dstMesh = ref _meshes.GetElementReferenceAt(dst.ID, dst.Generation, out var dstExists);
            ref var srcMesh = ref _meshes.GetElementReferenceAt(src.ID, src.Generation, out var srcExists);
            if (!dstExists || !srcExists)
            {
                return Handle<Mesh>.Invalid;
            }

            var oldMesh = dstMesh;
            dstMesh = srcMesh;
            _meshes.Remove(src.ID, src.Generation);

            oldMesh.ReleaseResource(_resourceDatabase);
            return dst;
        }
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

        var material = new Material();
        if (material.SetShader(shader, this, _resourceDatabase, _resourceAllocator) != Error.None)
        {
            return Handle<Material>.Invalid;
        }

        lock (_materialWriteLock)
        {
            var id = _materials.Add(material, out var generation);
            return new Handle<Material>(id, generation);
        }
    }

    /// <summary>
    /// Creates a new shader and returns its unique identifier.
    /// </summary>
    /// <returns>An <see cref="Handle{Shader}"/> representing the newly created shader.</returns>
    /// <param name="descriptor">The viewGroup containing the shader's properties and passes.</param>
    public Handle<Shader> CreateGraphicsShader(GraphicsShaderDescriptor descriptor)
    {
        Logger.DebugAssert(!_disposed);

        var shader = new Shader(descriptor);

        lock (_shaderWriteLock)
        {
            var id = _shaders.Add(shader, out var generation);
            return new Handle<Shader>(id, generation);
        }
    }

    public Handle<ComputeShader> CreateComputeShader(ComputeShaderDescriptor descriptor)
    {
        Logger.DebugAssert(!_disposed);

        var computeShader = new ComputeShader(descriptor);

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
            ref var mesh = ref _meshes.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
            if (!exist)
            {
                return;
            }

            _meshes.Remove(handle.ID, handle.Generation);
            mesh.ReleaseResource(_resourceDatabase);
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
            ref var material = ref _materials.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
            if (!exist)
            {
                return;
            }

            _materials.Remove(handle.ID, handle.Generation);
            material.ReleaseResource(_resourceDatabase);
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

        // Resolve material handles → bindless CBuffer indices.
        _materialPalettes.ResolveMaterialIndices(static (materialHandle, state) =>
        {
            var self = (ResourceManager)state!;
            var r = self.GetMaterialReference(materialHandle);
            if (r.IsFailure || !r.Value._cBufferCache.IsCreated)
            {
                return 0u;
            }

            return self._resourceDatabase.GetBindlessIndex(r.Value._cBufferCache.GpuResource.AsResource());
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
            ref var shader = ref _shaders.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
            if (!exist)
            {
                return;
            }

            _shaders.Remove(handle.ID, handle.Generation);
            shader.ReleaseResource(_resourceDatabase);
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
            ref var computeShader = ref _computeShaders.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
            if (!exist)
            {
                return;
            }

            _computeShaders.Remove(handle.ID, handle.Generation);
            computeShader.ReleaseResource(_resourceDatabase);
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

        _resourceDatabase.ReleaseResource(_paletteOffsetBuffer.AsResource());
        _resourceDatabase.ReleaseResource(_materialIndexBuffer.AsResource());

        DisposePool();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
