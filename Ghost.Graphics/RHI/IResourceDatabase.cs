using Ghost.Graphics.Data;

namespace Ghost.Graphics.RHI;

public interface IResourceDatabase
{
    /// <summary>
    /// Get the raw gpu resource pointer from a resource handle
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="handle">Resource handle</param>
    /// <returns>Pointer to the resource</returns>
    public unsafe T* GetResource<T>(ResourceHandle handle)
        where T: unmanaged;

    /// <summary>
    /// Retrieves the current state of the specified resource.
    /// </summary>
    /// <param name="handle">The handle that uniquely identifies the resource whose state is to be retrieved. Must not be null.</param>
    /// <returns>A ResourceState value representing the current state of the resource associated with the specified handle.</returns>
    public ResourceState GetResourceState(ResourceHandle handle);

    /// <summary>
    /// Sets the state of the specified resource handle to the given value.
    /// </summary>
    /// <param name="handle">The handle that identifies the resource whose state will be updated. Cannot be null.</param>
    /// <param name="state">The new state to assign to the resource represented by <paramref name="handle"/>.</param>
    public void SetResourceState(ResourceHandle handle, ResourceState state);

    /// <summary>
    /// Removes a resource from the database using its handle.
    /// </summary>
    /// <param name="handle">The handle of the resource to be removed.</param>
    public void RemoveResource(ResourceHandle handle);
}