namespace Ghost.Graphics.Contracts;

/// <summary>
/// Defines the contract for a render view in the graphics pipeline.
/// </summary>
internal interface IRenderer : IDisposable
{
    public ReadOnlySpan<IRenderPass> RenderPasses
    {
        get;
    }

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
    /// Renders the current content to the output target.
    /// </summary>
    public void Render();

    /// <summary>
    /// Waits for the next frame to be ready for rendering.
    /// </summary>
    public void WaitNextFrame();
    /// <summary>
    /// Waits for the render view to become idle, ensuring all previous commands have been executed and resources are ready for the next frame.
    /// </summary>
    public void WaitIdle();
}