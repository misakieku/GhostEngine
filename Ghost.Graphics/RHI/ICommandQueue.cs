namespace Ghost.Graphics.RHI;

/// <summary>
/// Command queue interface
/// </summary>
public interface ICommandQueue : IDisposable
{
    /// <summary>
    /// Type of commands this queue can execute
    /// </summary>
    public CommandQueueType Type
    {
        get;
    }

    /// <summary>
    /// Submits a single command buffer for execution
    /// </summary>
    /// <param name="commandBuffer">Command buffer to submit</param>
    public void Submit(ICommandBuffer commandBuffer);

    /// <summary>
    /// Submits multiple command buffers for execution
    /// </summary>
    /// <param name="commandBuffers">Command buffers to submit</param>
    public void Submit(params ReadOnlySpan<ICommandBuffer> commandBuffers);

    /// <summary>
    /// Signals a fence with the specified value
    /// </summary>
    /// <param name="value">Value to signal</param>
    /// <returns>The fence value that was signaled</returns>
    public ulong Signal(ulong value);

    /// <summary>
    /// Waits for the fence to reach the specified value
    /// </summary>
    /// <param name="value">Value to wait for</param>
    public void WaitForValue(ulong value);

    /// <summary>
    /// Gets the last completed fence value
    /// </summary>
    /// <returns>Last completed fence value</returns>
    public ulong GetCompletedValue();

    /// <summary>
    /// Waits until all submitted commands have finished executing
    /// </summary>
    public void WaitIdle();
}

/// <summary>
/// Command queue types
/// </summary>
public enum CommandQueueType
{
    Graphics,
    Compute,
    Copy
}
