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

public unsafe interface IResourceDatabase : IDisposable
{
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
    void ReleaseResource(Handle<GPUResource> handle);

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
    Identifier<Sampler> AddSampler(ref readonly SamplerDesc desc, int id);

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

    /// <summary>
    /// Swaps the resources associated with the two specified handles, effectively exchanging their identities and all associated data.
    /// </summary>
    /// <param name="handleA">The first handle whose associated resource is to be swapped.</param>
    /// <param name="handleB">The second handle whose associated resource is to be swapped.</param>
    /// <returns>An Error indicating the success or failure of the swap operation.</returns>
    Error Swap(Handle<GPUResource> handleA, Handle<GPUResource> handleB);

    /// <summary>
    /// Creates a new GPU resource that is a share of the specified source resource, including all its properties and data.
    /// The new resource will have the same description and content as the source resource, but will be a distinct entity in the resource database with its own handle.
    /// </summary>
    /// <remarks>
    /// The shared resource created by this method will have the same description and content as the source resource, but will be a distinct entity in the resource database with its own handle.
    /// However, it is important to note that modifications to the shared resource through one handle will affect all other handles that reference the same underlying resource, as they all point to the same GPU memory.
    /// </remarks>
    /// <param name="src">The handle to the source resource.</param>
    /// <returns>The handle to the newly created shared resource.</returns>
    Handle<GPUResource> CreateShared(Handle<GPUResource> src);

    /// <summary>
    /// Maps a subresource of a GPU resource for CPU access, specifying read and write ranges.
    /// </summary>
    /// <param name="handle">A handle to the GPU resource to be mapped.</param>
    /// <param name="subResource">The zero-based index of the subresource to map.</param>
    /// <param name="readRange">The range of the resource to be read by the CPU. Specify null to indicate read access to the entire resource.</param>
    /// <returns>A pointer to the mapped subresource data, or null if the mapping operation fails.</returns>
    void* MapResource(Handle<GPUResource> handle, uint subResource, ResourceRange? readRange);

    /// <summary>
    /// Unmaps a previously mapped subresource of a GPU resource, optionally specifying the range of data that was written by the CPU.
    /// </summary>
    /// <param name="handle">A handle to the GPU resource to unmap. Must reference a resource that was previously mapped.</param>
    /// <param name="subResource">The zero-based index of the subresource to unmap.</param>
    /// <param name="writtenRange">The range within the resource that was written to by the CPU. Specify null if no data was written or if the entire resource was modified.</param>
    /// <returns>An Error value indicating the result of the operation. Returns Error.None if the resource was successfully unmapped.</returns>
    Error UnmapResource(Handle<GPUResource> handle, uint subResource, ResourceRange? writtenRange);

    /// <summary>
    /// Gets the total size in bytes of the specified GPU resource, including all its subresources. This method is useful for determining the memory footprint of a resource and can be used for memory management and optimization purposes.
    /// </summary>
    /// <param name="resource">The handle to the GPU resource.</param>
    /// <param name="firstSubResource">The index of the first subresource to include in the size calculation.</param>
    /// <param name="numSubResources">The number of subresources to include in the size calculation.</param>
    /// <returns>The total size in bytes of the specified GPU resource and its subresources.</returns>
    ulong GetIntermediateResourceSize(Handle<GPUResource> resource, uint firstSubResource, uint numSubResources);
}
