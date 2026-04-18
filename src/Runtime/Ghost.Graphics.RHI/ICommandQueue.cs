namespace Ghost.Graphics.RHI;

/// <summary>
/// Command queue interface
/// </summary>
public interface ICommandQueue : IRHIObject
{
    /// <summary>
    /// Type of commands this queue can execute
    /// </summary>
    CommandQueueType Type
    {
        get;
    }

    /// <summary>
    /// Submits a single command buffer for execution
    /// </summary>
    /// <param name="commandBuffer">Command buffer to submit</param>
    void Submit(ICommandBuffer commandBuffer);

    /// <summary>
    /// Submits multiple command buffers for execution
    /// </summary>
    /// <param name="commandBuffers">Command buffers to submit</param>
    void Submit(params ReadOnlySpan<ICommandBuffer> commandBuffers);

    /// <summary>
    /// Signals a fence with the specified Value
    /// </summary>
    /// <param name="fence">Fence to signal</param>
    /// <param name="value">Value to signal</param>
    /// <returns>The fence Value that was signaled</returns>
    ulong Signal(IFence fence, ulong value);

    /// <summary>
    /// Insert a GPU wait on the specified fence and value. The GPU will wait until the fence reaches the specified value before executing any further commands.
    /// </summary>
    /// <remarks>
    /// CPU will return immediately.
    /// </remarks>
    /// <param name="fence">Fence to wait on</param>
    /// <param name="value">Value to wait for</param>
    void Wait(IFence fence, ulong value);
}