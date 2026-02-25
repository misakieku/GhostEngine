using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics;

public interface IResourceManager
{
    IResourceAllocator ResourceAllocator
    {
        get;
    }

    IResourceDatabase ResourceDatabase
    {
        get;
    }

    /// <summary>
    /// Creates a new mesh from the specified vertex and index data.
    /// </summary>
    /// <param name="vertices">A UnsafeList containing the vertices that define the geometry of the mesh. Must contain at least one vertex.</param>
    /// <param name="indices">A UnsafeList containing the indices that specify how vertices are connected to form primitives. Must contain at least one index.</param>
    /// <returns>An <see cref="Identifier{Mesh}"/> representing the newly created mesh.</returns>
    Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices);

    /// <summary>
    /// Creates a new material instance using the specified shader.
    /// </summary>
    /// <param name="shader">The identifier of the shader to associate with the new material.</param>
    /// <returns>An <see cref="Identifier{Material}"/> representing the newly created material.</returns>
    Handle<Material> CreateMaterial(Identifier<Shader> shader);

    /// <summary>
    /// Creates a new shader and returns its unique identifier.
    /// </summary>
    /// <returns>An <see cref="Identifier{Shader}"/> representing the newly created shader.</returns>
    /// <param name="descriptor">The viewGroup containing the shader's properties and passes.</param>
    Identifier<Shader> CreateGraphicsShader(ShaderDescriptor descriptor);

    /// <summary>
    /// Determines whether a mesh with the specified Handle exists.
    /// </summary>
    /// <param name="handle">The handle of the mesh to check for existence. Cannot be null.</param>
    /// <returns>true if a mesh with the specified Handle exists; otherwise, false.</returns>
    bool HasMesh(Handle<Mesh> handle);

    /// <summary>
    /// Returns a reference to the mesh associated with the specified handle.
    /// </summary>
    /// <param name="handle">The handle of the mesh to retrieve. Must refer to a valid mesh; otherwise, the behavior is undefined.</param>
    /// <returns>A result containing a reference to the mesh corresponding to the specified handle, or an error status if the handle is invalid.</returns>
    RefResult<Mesh, Error> GetMeshReference(Handle<Mesh> handle);

    /// <summary>
    /// Releases the mesh resource associated with the specified handle, freeing any resources held by it. Includes both CPU and GPU resources.
    /// </summary>
    /// <param name="handle">The handle of the mesh to release. Must refer to a mesh that was previously created and not already released.</param>
    void ReleaseMesh(Handle<Mesh> handle);

    /// <summary>
    /// Determines whether a material with the specified handle exists in the collection.
    /// </summary>
    /// <param name="handle">The handle of the material to check for existence.</param>
    /// <returns>true if a material with the specified handle exists; otherwise, false.</returns>
    bool HasMaterial(Handle<Material> handle);

    /// <summary>
    /// Gets a reference to the material associated with the specified handle.
    /// </summary>
    /// <param name="handle">The handle of the material to retrieve. Must refer to a valid material.</param>
    /// <returns>A result containing a reference to the material corresponding to the specified handle, or an error status if the handle is invalid.</returns>
    RefResult<Material, Error> GetMaterialReference(Handle<Material> handle);

    /// <summary>
    /// Releases the material associated with the specified handle, making it available for reuse or disposal.
    /// </summary>
    /// <param name="handle">The handle of the material to release. Must refer to a material that has been previously acquired.</param>
    void ReleaseMaterial(Handle<Material> handle);

    /// <summary>
    /// Determines whether a shader with the specified identifier exists in the collection.
    /// </summary>
    /// <param name="id">The identifier of the shader to check for existence.</param>
    /// <returns>true if a shader with the specified identifier exists; otherwise, false.</returns>
    bool HasShader(Identifier<Shader> id);

    /// <summary>
    /// Returns a reference to the shader associated with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier of the shader to retrieve. Must refer to a valid shader.</param>
    /// <returns>A result containing a reference to the shader corresponding to the specified identifier, or an error status if the identifier is invalid.</returns>
    RefResult<Shader, Error> GetShaderReference(Identifier<Shader> id);

    /// <summary>
    /// Releases the shader associated with the specified identifier, freeing any resources allocated to it.
    /// </summary>
    /// <param name="id">The identifier of the shader to release. Must refer to a valid, previously created shader.</param>
    void ReleaseShader(Identifier<Shader> id);
}

internal sealed class ResourceManager : IResourceManager, IDisposable
{
    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;

    private UnsafeSlotMap<Mesh> _meshes;
    private UnsafeSlotMap<Material> _materials;
    private UnsafeList<Shader> _shaders; // TODO: Use SlotMap?

    private bool _disposed;

    public IResourceAllocator ResourceAllocator => _resourceAllocator;
    public IResourceDatabase ResourceDatabase => _resourceDatabase;

    public ResourceManager(IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase)
    {
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;

        _meshes = new UnsafeSlotMap<Mesh>(64, Allocator.Persistent);
        _materials = new UnsafeSlotMap<Material>(16, Allocator.Persistent);
        _shaders = new UnsafeList<Shader>(16, Allocator.Persistent);
    }

    ~ResourceManager()
    {
        Dispose();
    }

    public unsafe Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var vertexBufferDesc = new BufferDesc
        {
            Size = (uint)(vertices.Count * sizeof(Vertex)),
            Stride = (uint)sizeof(Vertex),
            Usage = BufferUsage.Vertex | BufferUsage.ShaderResource | BufferUsage.Raw,
            MemoryType = ResourceMemoryType.Default,
        };

        var indexBufferDesc = new BufferDesc
        {
            Size = (uint)(indices.Count * sizeof(uint)),
            Stride = sizeof(uint),
            Usage = BufferUsage.Index | BufferUsage.ShaderResource | BufferUsage.Raw,
            MemoryType = ResourceMemoryType.Default,
        };

        var objectBufferDesc = new BufferDesc
        {
            Size = (uint)sizeof(PerObjectData),
            Stride = (uint)sizeof(PerObjectData),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            MemoryType = ResourceMemoryType.Default,
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

    public Handle<Material> CreateMaterial(Identifier<Shader> shader)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var material = new Material();
        if (material.SetShader(shader, this) != Error.None)
        {
            return Handle<Material>.Invalid;
        }

        var id = _materials.Add(material, out var generation);
        return new Handle<Material>(id, generation);
    }

    public Identifier<Shader> CreateGraphicsShader(ShaderDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var shader = new Shader(descriptor);

        var id = _shaders.Count;
        _shaders.Add(shader);
        return new Identifier<Shader>(id);
    }

    public bool HasMesh(Handle<Mesh> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _meshes.Contains(handle.ID, handle.Generation);
    }

    public RefResult<Mesh, Error> GetMeshReference(Handle<Mesh> handle)
    {
        ref var mesh = ref _meshes.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        return RefResult<Mesh, Error>.Success(ref mesh);
    }

    public void ReleaseMesh(Handle<Mesh> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_meshes.TryGetElementAt(handle.ID, handle.Generation, out var mesh))
        {
            return;
        }

        _meshes.Remove(handle.ID, handle.Generation);
        mesh.ReleaseResource(_resourceDatabase);
    }

    public bool HasMaterial(Handle<Material> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _materials.Contains(handle.ID, handle.Generation);
    }

    public RefResult<Material, Error> GetMaterialReference(Handle<Material> handle)
    {
        ref var material = ref _materials.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        return RefResult<Material, Error>.Success(ref material);
    }

    public void ReleaseMaterial(Handle<Material> handle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var material = _materials.GetElementReferenceAt(handle.ID, handle.Generation, out var exist);
        if (!exist)
        {
            return;
        }

        _materials.Remove(handle.ID, handle.Generation);
        material.ReleaseResource(_resourceDatabase);
    }

    public bool HasShader(Identifier<Shader> id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return id.Value >= 0 && id.Value < _shaders.Count;
    }

    public RefResult<Shader, Error> GetShaderReference(Identifier<Shader> id)
    {
        if (!HasShader(id))
        {
            return Error.NotFound;
        }

        return RefResult<Shader, Error>.Success(ref _shaders[id.Value]);
    }

    public void ReleaseShader(Identifier<Shader> id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!HasShader(id))
        {
            return;
        }

        var shader = _shaders[id.Value];
        shader.ReleaseResource(_resourceDatabase);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var mesh in _meshes)
        {
            mesh.ReleaseResource(_resourceDatabase);
        }

        foreach (var material in _materials)
        {
            material.ReleaseResource(_resourceDatabase);
        }

        foreach (var shader in _shaders)
        {
            shader.ReleaseResource(_resourceDatabase);
        }

        _meshes.Dispose();
        _materials.Dispose();
        _shaders.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}