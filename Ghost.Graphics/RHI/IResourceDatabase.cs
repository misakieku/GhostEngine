using Ghost.Core;
using Ghost.Graphics.Data;

namespace Ghost.Graphics.RHI;

public interface IResourceReleasable
{
    /// <summary>
    /// A method to release GPU resources.
    /// </summary>
    void ReleaseResource(IResourceDatabase database);
}

public interface IResourceDatabase
{
    /*
    /// <summary>
    /// Imports an external unmanaged resource and returns a handle for use within the resource management system.
    /// </summary>
    /// <typeparam name="T">The type of the unmanaged resource pointer to import.</typeparam>
    /// <param name="resourcePtr">A pointer to the external unmanaged resource to be imported. Must remain valid for the duration of the resource's usage.</param>
    /// <param name="initialState">The initial state to assign to the imported resource.</param>
    /// <returns>A handle representing the imported resource, which can be used for subsequent operations.</returns>
    unsafe Handle<GPUResource> ImportExternalResource<T>(T resourcePtr, ResourceState initialState, string? name = null)
        where T : unmanaged;
    */

    /// <summary>
    /// Retrieves the current state of the specified resource.
    /// </summary>
    /// <param name="handle">The handle that uniquely identifies the resource whose state is to be retrieved. Must not be null.</param>
    /// <returns>A ResourceState value representing the current state of the resource associated with the specified handle.</returns>
    ResourceState GetResourceState(Handle<GPUResource> handle);

    /// <summary>
    /// Sets the state of the specified resource handle to the given value.
    /// </summary>
    /// <param name="handle">The handle that identifies the resource whose state will be updated. Cannot be null.</param>
    /// <param name="state">The new state to assign to the resource represented by <paramref name="handle"/>.</param>
    void SetResourceState(Handle<GPUResource> handle, ResourceState state);

    /// <summary>
    /// Retrieves the description of a GPU resource associated with the specified handle.
    /// </summary>
    /// <param name="handle">A handle that identifies the GPU resource for which to obtain the description. Must reference a valid resource.</param>
    /// <returns>A ResourceDesc structure containing details about the specified GPU resource.</returns>
    ResourceDesc GetResourceDescription(Handle<GPUResource> handle);

    /// <summary>
    /// Retrieves the bindless index associated with the specified GPU resource handle.
    /// </summary>
    /// <param name="handle">A handle to the GPU resource for which to obtain the bindless index. Must reference a valid, currently registered resource.</param>
    /// <returns>The bindless index corresponding to the specified GPU resource handle. -1 if the resource does not support bindless access or is not found.</returns>
    int GetBindlessIndex(Handle<GPUResource> handle);

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
    /// <returns>A reference to the mesh corresponding to the specified handle.</returns>
    ref Mesh GetMeshReference(Handle<Mesh> handle);

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
    /// <returns>A reference to the material corresponding to the specified handle.</returns>
    ref Material GetMaterialReference(Handle<Material> handle);

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
    /// <returns>A reference to the shader corresponding to the specified identifier.</returns>
    Shader GetShaderReference(Identifier<Shader> id);

    /// <summary>
    /// Releases the shader associated with the specified identifier, freeing any resources allocated to it.
    /// </summary>
    /// <param name="id">The identifier of the shader to release. Must refer to a valid, previously created shader.</param>
    void ReleaseShader(Identifier<Shader> id);

    // TODO: Use xxhash3 to generate passKey from string id.

    /// <summary>
    /// Adds a shader pass to the collection using the specified identifier.
    /// </summary>
    /// <param name="passKey">The unique identifier for the shader pass.</param>
    /// <param name="pass">The shader pass to add. Cannot be null.</param>
    void AddShaderPass(ShaderPassKey passKey, ShaderPass pass);

    /// <summary>
    /// Retrieves the shader pass associated with the specified pass identifier.
    /// </summary>
    /// <param name="passKey">The unique identifier of the shader pass to retrieve.</param>
    /// <returns>The <see cref="ShaderPass"/> corresponding to the specified identifier, or null if no matching shader pass is found.</returns>
    ShaderPass GetShaderPass(ShaderPassKey passKey);
}