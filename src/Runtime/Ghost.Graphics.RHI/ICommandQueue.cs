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
    /// <param name="value">Value to signal</param>
    /// <returns>The fence Value that was signaled</returns>
    ulong Signal(ulong value);

    /// <summary>
    /// Waits for the fence to reach the specified Value
    /// </summary>
    /// <param name="value">Value to wait for</param>
    void WaitForValue(ulong value);

    /// <summary>
    /// Gets the last completed fence Value
    /// </summary>
    /// <returns>Last completed fence Value</returns>
    ulong GetCompletedValue();

    /// <summary>
    /// Waits until all submitted commands have finished executing
    /// </summary>
    void WaitIdle();

    /// <summary>
    /// Waits asynchronously until all submitted commands have finished executing
    /// </summary>
    /// <returns>Task that completes when the queue is idle</returns>
    Task WaitAsync();
}