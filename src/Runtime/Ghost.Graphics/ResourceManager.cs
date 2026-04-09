using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.Numerics;

namespace Ghost.Graphics;

public sealed partial class ResourceManager : IDisposable
{
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
    private UnsafeList<Shader> _shaders; // TODO: Use SlotMap?

    private readonly MaterialPaletteStore _materialPalettes;

    private ulong _submittedFrame;

    private bool _disposed;

    public ResourceManager(IRenderDevice renderDevice, IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase)
    {
        _renderDevice = renderDevice;
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;

        _meshes = new UnsafeSlotMap<Mesh>(64, Allocator.Persistent);
        _materials = new UnsafeSlotMap<Material>(64, Allocator.Persistent);
        _shaders = new UnsafeList<Shader>(16, Allocator.Persistent);

        _materialPalettes = new MaterialPaletteStore();
    }

    ~ResourceManager()
    {
        Dispose();
    }

    internal void BeginFrame(ulong submittedFrame)
    {
        Debug.Assert(!_disposed);
        _submittedFrame = submittedFrame;
    }

    internal void EndFrame(ulong completedFrame)
    {
        Debug.Assert(!_disposed);
        EndFramePool(completedFrame);
    }

    /// <summary>
    /// Creates a new mesh from the specified vertex and index data.
    /// </summary>
    /// <param name="vertices">A UnsafeList containing the vertices that define the geometry of the mesh. Must contain at least one vertex.</param>
    /// <param name="indices">A UnsafeList containing the indices that specify how vertices are connected to form primitives. Must contain at least one index.</param>
    /// <returns>An <see cref="Identifier{Mesh}"/> representing the newly created mesh.</returns>
    public unsafe Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices)
    {
        Debug.Assert(!_disposed);

        var vertexBufferDesc = new BufferDesc
        {
            Size = (uint)(vertices.Count * sizeof(Vertex)),
            Stride = (uint)sizeof(Vertex),
            Usage = BufferUsage.Vertex | BufferUsage.ShaderResource | BufferUsage.Raw,
            HeapType = HeapType.Default,
        };

        var indexBufferDesc = new BufferDesc
        {
            Size = (uint)(indices.Count * sizeof(uint)),
            Stride = sizeof(uint),
            Usage = BufferUsage.Index | BufferUsage.ShaderResource | BufferUsage.Raw,
            HeapType = HeapType.Default,
        };

        var objectBufferDesc = new BufferDesc
        {
            Size = (uint)sizeof(MeshData),
            Stride = (uint)sizeof(MeshData),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Default,
        };

        var vertexBuffer = _resourceAllocator.CreateBuffer(in vertexBufferDesc, "VertexBuffer");
        var indexBuffer = _resourceAllocator.CreateBuffer(in indexBufferDesc, "IndexBuffer");
        var objectBuffer = _resourceAllocator.CreateBuffer(in objectBufferDesc, "ObjectBuffer");

        var mesh = new Mesh
        {
            Vertices = vertices,
            Indices = indices,
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
            ObjectDataBuffer = objectBuffer,
        };

        var id = _meshes.Add(mesh, out var generation);
        return new Handle<Mesh>(id, generation);
    }

    /// <summary>
    /// Creates a new material instance using the specified shader.
    /// </summary>
    /// <param name="shader">The identifier of the shader to associate with the new material.</param>
    /// <returns>An <see cref="Identifier{Material}"/> representing the newly created material.</returns>
    public Handle<Material> CreateMaterial(Identifier<Shader> shader)
    {
        Debug.Assert(!_disposed);

        var material = new Material();
        if (material.SetShader(shader, this, _resourceDatabase, _resourceAllocator) != Error.None)
        {
            return Handle<Material>.Invalid;
        }

        var id = _materials.Add(material, out var generation);
        return new Handle<Material>(id, generation);
    }

    /// <summary>
    /// Creates a new shader and returns its unique identifier.
    /// </summary>
    /// <returns>An <see cref="Identifier{Shader}"/> representing the newly created shader.</returns>
    /// <param name="descriptor">The viewGroup containing the shader's properties and passes.</param>
    public Identifier<Shader> CreateGraphicsShader(GraphicsShaderDescriptor descriptor, ref readonly GraphicsCompiledResult compiledResult)
    {
        Debug.Assert(!_disposed);

        var shader = new Shader(descriptor, in compiledResult);

        var id = _shaders.Count;
        _shaders.Add(shader);
        return new Identifier<Shader>(id);
    }

    /// <summary>
    /// Determines whether a mesh with the specified Handle exists.
    /// </summary>
    /// <param name="handle">The handle of the mesh to check for existence. Cannot be null.</param>
    /// <returns>true if a mesh with the specified Handle exists; otherwise, false.</returns>
    public bool HasMesh(Handle<Mesh> handle)
    {
        Debug.Assert(!_disposed);
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
        Debug.Assert(!_disposed);

        ref var mesh = ref _meshes.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return;
        }

        _meshes.Remove(handle.ID, handle.Generation);
        mesh.ReleaseResource(_resourceDatabase);
    }

    /// <summary>
    /// Determines whether a material with the specified handle exists in the collection.
    /// </summary>
    /// <param name="handle">The handle of the material to check for existence.</param>
    /// <returns>true if a material with the specified handle exists; otherwise, false.</returns>
    public bool HasMaterial(Handle<Material> handle)
    {
        Debug.Assert(!_disposed);
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
        Debug.Assert(!_disposed);

        ref var material = ref _materials.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return;
        }

        _materials.Remove(handle.ID, handle.Generation);
        material.ReleaseResource(_resourceDatabase);
    }

    /// <summary>
    /// Returns an existing material palette index for the specified material sequence or creates a new one.
    /// </summary>
    /// <param name="materials">The ordered material list for the palette.</param>
    /// <returns>The palette index. Index 0 represents an empty palette.</returns>
    public int GetOrCreateMaterialPalette(ReadOnlySpan<Handle<Material>> materials)
    {
        Debug.Assert(!_disposed);

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
        Debug.Assert(!_disposed);
        return _materialPalettes.IsValid(paletteID);
    }

    /// <summary>
    /// Gets metadata for a material palette entry.
    /// </summary>
    /// <param name="paletteID">The palette index to query.</param>
    public MaterialPalette GetMaterialPaletteInfo(Identifier<MaterialPalette> paletteID)
    {
        Debug.Assert(!_disposed);
        return _materialPalettes.GetInfo(paletteID);
    }

    /// <summary>
    /// Gets a material handle from a palette entry by local material index.
    /// </summary>
    /// <param name="paletteID">The palette index to query.</param>
    /// <param name="localMaterialIndex">The material slot inside the palette.</param>
    public Handle<Material> GetMaterialPaletteMaterial(Identifier<MaterialPalette> paletteID, int localMaterialIndex)
    {
        Debug.Assert(!_disposed);
        return _materialPalettes.GetMaterial(paletteID, localMaterialIndex);
    }

    /// <summary>
    /// Releases a material palette reference previously returned by <see cref="GetOrCreateMaterialPalette(ReadOnlySpan{Handle{Material}})"/>.
    /// </summary>
    /// <param name="paletteID">The palette index to release.</param>
    public void ReleaseMaterialPalette(Identifier<MaterialPalette> paletteID)
    {
        Debug.Assert(!_disposed);
        _materialPalettes.Release(paletteID);
    }

    /// <summary>
    /// Determines whether a shader with the specified identifier exists in the collection.
    /// </summary>
    /// <param name="id">The identifier of the shader to check for existence.</param>
    /// <returns>true if a shader with the specified identifier exists; otherwise, false.</returns>
    public bool HasShader(Identifier<Shader> id)
    {
        Debug.Assert(!_disposed);
        return id.Value >= 0 && id.Value < _shaders.Count;
    }

    /// <summary>
    /// Returns a reference to the shader associated with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier of the shader to retrieve. Must refer to a valid shader.</param>
    /// <returns>A result containing a reference to the shader corresponding to the specified identifier, or an error status if the identifier is invalid.</returns>
    public RefResult<Shader, Error> GetShaderReference(Identifier<Shader> id)
    {
        if (!HasShader(id))
        {
            return Error.NotFound;
        }

        return RefResult<Shader, Error>.Success(ref _shaders[id.Value]);
    }

    /// <summary>
    /// Releases the shader associated with the specified identifier, freeing any resources allocated to it.
    /// </summary>
    /// <param name="id">The identifier of the shader to release. Must refer to a valid, previously created shader.</param>
    public void ReleaseShader(Identifier<Shader> id)
    {
        Debug.Assert(!_disposed);

        if (!HasShader(id))
        {
            return;
        }

        ref var shader = ref _shaders[id.Value];
        shader.ReleaseResource(_resourceDatabase);
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
        _materialPalettes.Dispose();

        DisposePool();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
