using Ghost.Core;

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
    public BarrierLayout layout;
    public BarrierAccess access;
    public BarrierSync sync;

    public ResourceBarrierData(BarrierLayout layout, BarrierAccess access, BarrierSync sync)
    {
        this.layout = layout;
        this.access = access;
        this.sync = sync;
    }
}

public enum BindlessAccess
{
    ShaderResource,
    ConstantBuffer,
    UnorderedAccess,
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
    Result<ResourceBarrierData, Error> GetResourceBarrierData(Handle<GPUResource> handle);

    /// <summary>
    /// Sets the barrier data of the specified resource handle.
    /// </summary>
    /// <param name="handle">The handle that identifies the resource.</param>
    /// <param name="data">The new barrier data.</param>
    /// <returns>An Error indicating the success or failure of the operation.</returns>
    Error SetResourceBarrierData(Handle<GPUResource> handle, ResourceBarrierData data);

    /// <summary>
    /// Retrieves the description of a GPU resource associated with the specified handle.
    /// </summary>
    /// <param name="handle">A handle that identifies the GPU resource for which to obtain the description. Must reference a valid resource.</param>
    /// <returns>A ResourceDesc structure containing details about the specified GPU resource.</returns>
    Result<ResourceDesc, Error> GetResourceDescription(Handle<GPUResource> handle);

    /// <summary>
    /// Retrieves the bindless index associated with the specified GPU resource handle.
    /// </summary>
    /// <param name="handle">A handle to the GPU resource for which to obtain the bindless index. Must reference a valid, currently registered resource.</param>
    /// <param name="access">The type of bindless access for which to obtain the index.</param>
    /// <returns>The bindless index corresponding to the specified GPU resource handle. ~0 if the resource does not support bindless access or is not found.</returns>
    uint GetBindlessIndex(Handle<GPUResource> handle, BindlessAccess access = BindlessAccess.ShaderResource);

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
    /// Releases the GPU resource associated with the specified handle, freeing any resources allocated to it.
    /// </summary>
    /// <param name="handle">The handle of the resource to be removed.</param>
    void ScheduleReleaseResource(Handle<GPUResource> handle);

    /// <summary>
    /// Releases the GPU resource associated with the specified handle immediately, freeing any resources allocated to it.
    /// </summary>
    /// <param name="handle">The handle of the resource to be removed.</param>
    void ReleaseResourceImmediately(Handle<GPUResource> handle);

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
    /// Releases the sampler associated with the specified identifier and frees any resources allocated to it.
    /// </summary>
    /// <param name="id">The identifier of the sampler to release. Must reference a valid, existing sampler.</param>
    void ReleaseSampler(Identifier<Sampler> id);
}
