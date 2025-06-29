namespace Ghost.Graphics.Contracts;

/// <summary>
/// Defines the contract for a render view in the graphics pipeline.
/// </summary>
internal interface IRenderView : IDisposable
{
    /// <summary>
    /// Requests a resize of the render view.
    /// </summary>
    /// <param name="width">The new width of the render view.</param>
    /// <param name="height">The new height of the render view.</param>
    /// <remarks>This only submits a resize request without executing it. May overwrite last request if next request issued before next frame.</remarks>
    public void RequestResize(uint width, uint height);
    /// <summary>
    /// Executes any pending resize operations for the current context.
    /// </summary>
    public void ExecutePendingResize();

    /// <summary>
    /// Begins a render operation.
    /// </summary>
    /// <returns>An ICommandBuffer instance to manage render commands.</returns>
    public ICommandBuffer BeginRender();
    /// <summary>
    /// Renders the current content to the output target.
    /// </summary>
    public void Render();
    /// <summary>
    /// Ends the current rendering operation and finalizes any pending rendering tasks.
    /// </summary>
    public void EndRender();

    /// <summary>
    /// Waits for the next frame to be ready for rendering.
    /// </summary>
    public void WaitNextFrame();
    /// <summary>
    /// Waits for the rendering operations to complete and the GPU to be idle.
    /// </summary>
    public void WaitIdle();
}