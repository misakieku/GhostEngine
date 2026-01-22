using Ghost.Core;
using Ghost.Graphics.Core;

namespace Ghost.Graphics.RHI;

public interface IResourceReleasable
{
    /// <summary>
    /// A method to release GPU resources.
    /// </summary>
    void ReleaseResource(IResourceDatabase database);
}

public struct ResourceBarrierData
{
    public BarrierLayout Layout;
    public BarrierAccess Access;
    public BarrierSync Sync;

    public ResourceBarrierData(BarrierLayout layout, BarrierAccess access, BarrierSync sync)
    {
        Layout = layout;
        Access = access;
        Sync = sync;
    }
}

// TODO: Consider adding methods for resource enumeration, statistics, and bulk operations.
// TODO: Consider adding async resource loading and streaming support.
// TODO: Mesh, Material, Shader management could be separated into their own interfaces for better modularity because they are not bound to specific graphics API.
public interface IResourceDatabase : IDisposable
{
    /*
    /// <summary>
    /// Imports an external unmanaged resource and returns a handle for use within the resource management system.
    /// </summary>
    /// <typeparam name="T">The space of the unmanaged resource pointer to import.</typeparam>
    /// <param name="resourcePtr">A pointer to the external unmanaged resource to be imported. Must remain valid for the duration of the resource's usage.</param>
    /// <param name="initialState">The initial state to assign to the imported resource.</param>
    /// <returns>A handle representing the imported resource, which can be used for subsequent operations.</returns>
    unsafe Handle<GPUResource> ImportExternalResource<T>(T resourcePtr, ResourceState initialState, string? name = null)
        where T : unmanaged;
    */

    /// <summary>
    /// Checks if a resource with the specified handle exists in the database.
    /// </summary>
    /// <param name="handle">The handle of the resource to check for existence.</param>
    bool HasResource(Handle<GPUResource> handle);

    /// <summary>
    /// Retrieves the current barrier data of the specified resource.
    /// </summary>
    /// <param name="handle">The handle that uniquely identifies the resource.</param>
    /// <returns>A ResourceBarrierData value representing the current barrier state.</returns>
    Result<ResourceBarrierData, ErrorStatus> GetResourceBarrierData(Handle<GPUResource> handle);

    /// <summary>
    /// Sets the barrier data of the specified resource handle.
    /// </summary>
    /// <param name="handle">The handle that identifies the resource.</param>
    /// <param name="data">The new barrier data.</param>
    /// <returns>An ErrorStatus indicating the success or failure of the operation.</returns>
    ErrorStatus SetResourceBarrierData(Handle<GPUResource> handle, ResourceBarrierData data);

    /// <summary>
    /// Retrieves the description of a GPU resource associated with the specified handle.
    /// </summary>
    /// <param name="handle">A handle that identifies the GPU resource for which to obtain the description. Must reference a valid resource.</param>
    /// <returns>A ResourceDesc structure containing details about the specified GPU resource.</returns>
    Result<ResourceDesc, ErrorStatus> GetResourceDescription(Handle<GPUResource> handle);

    /// <summary>
    /// Retrieves the bindless index associated with the specified GPU resource handle.
    /// </summary>
    /// <param name="handle">A handle to the GPU resource for which to obtain the bindless index. Must reference a valid, currently registered resource.</param>
    /// <returns>The bindless index corresponding to the specified GPU resource handle. ~0 if the resource does not support bindless access or is not found.</returns>
    uint GetBindlessIndex(Handle<GPUResource> handle);

    /// <summary>
    /// Retrieves the name of the GPU resource associated with the specified handle.
    /// </summary>
    /// <remarks>
    /// You should only use this method in debug builds or inside engine editor.
    /// </remarks>
    /// <param name="handle">A handle to the GPU resource for which to obtain the name. Must reference a valid resource.</param>
    /// <returns>The name of the GPU resource associated with the specified handle, or null if the resource does not have a name.</returns>
    string? GetResourceName(Handle<GPUResource> handle);

    /// <summary>
    /// Removes a resource from the database using its handle.
    /// </summary>
    /// <param name="handle">The handle of the resource to be removed.</param>
    void ReleaseResource(Handle<GPUResource> handle);

    /// <summary>
    /// Retrieves an existing sampler identifier that matches the specified description, or creates a new one if none
    /// exists.
    /// </summary>
    /// <param name="desc">A read-only reference to a <see cref="SamplerDesc"/> structure that defines the properties of the sampler to retrieve or create.</param>
    /// <param name="id">An integer identifier to associate with the sampler.</param>
    /// <returns>An <see cref="Identifier{Sampler}"/> representing the sampler that matches the specified description.
    ///     If a matching sampler does not exist, a new sampler is created and its identifier is returned.</returns>
    Identifier<Sampler> CreateSampler(ref readonly SamplerDesc desc, int id);

    /// <summary>
    /// Determines whether a sampler with the specified identifier exists.
    /// </summary>
    /// <param name="id">The identifier of the sampler to check for existence.</param>
    /// <returns>true if a sampler with the given identifier exists; otherwise, false.</returns>
    bool TryGetSampler(ref readonly SamplerDesc desc, out Identifier<Sampler> id);

    /// <summary>
    /// Adds a mesh to the resource database and returns its handle.
    /// </summary>
    /// <param name="mesh">The mesh data to be added to the database.</param>
    /// <returns>The <see cref="Handle{Mesh}"/> representing the newly added mesh.</returns>"/>
    Handle<Mesh> AddMesh(ref readonly Mesh mesh);

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
    RefResult<Mesh, ErrorStatus> GetMeshReference(Handle<Mesh> handle);

    /// <summary>
    /// Releases the mesh resource associated with the specified handle, freeing any resources held by it. Includes both CPU and GPU resources.
    /// </summary>
    /// <param name="handle">The handle of the mesh to release. Must refer to a mesh that was previously created and not already released.</param>
    void ReleaseMesh(Handle<Mesh> handle);

    /// <summary>
    /// Adds a new material to the collection and returns its unique handle.
    /// </summary>
    /// <param name="material">The material to add. The material must be fully initialized before calling this method.</param>
    /// <returns>The <see cref="Handle{Material}"/> representing the newly added material.</returns>
    Handle<Material> AddMaterial(ref readonly Material material);

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
    RefResult<Material, ErrorStatus> GetMaterialReference(Handle<Material> handle);

    /// <summary>
    /// Releases the material associated with the specified handle, making it available for reuse or disposal.
    /// </summary>
    /// <param name="handle">The handle of the material to release. Must refer to a material that has been previously acquired.</param>
    void ReleaseMaterial(Handle<Material> handle);

    /// <summary>
    /// Adds the specified shader to the collection and returns its unique identifier.
    /// </summary>
    /// <param name="shader">The shader to add. The shader is passed by read-only reference and will not be modified.</param>
    /// <returns>The <see cref="Identifier{Shader}"/> representing the newly added shader.</returns>
    Identifier<Shader> AddShader(Shader shader);

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
    RefResult<Shader, ErrorStatus> GetShaderReference(Identifier<Shader> id);

    /// <summary>
    /// Releases the shader associated with the specified identifier, freeing any resources allocated to it.
    /// </summary>
    /// <param name="id">The identifier of the shader to release. Must refer to a valid, previously created shader.</param>
    void ReleaseShader(Identifier<Shader> id);
}
