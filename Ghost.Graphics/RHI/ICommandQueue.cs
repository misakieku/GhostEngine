namespace Ghost.Graphics.RHI;

/// <summary>
/// D3D12-style command queue interface
/// </summary>
public interface ICommandQueue : IDisposable
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
    /// Signals a fence with the specified value
    /// </summary>
    /// <param name="value">Value to signal</param>
    /// <returns>The fence value that was signaled</returns>
    ulong Signal(ulong value);

    /// <summary>
    /// Waits for the fence to reach the specified value
    /// </summary>
    /// <param name="value">Value to wait for</param>
    void WaitForValue(ulong value);

    /// <summary>
    /// Gets the last completed fence value
    /// </summary>
    /// <returns>Last completed fence value</returns>
    ulong GetCompletedValue();
}

/// <summary>
/// Command queue types matching D3D12
/// </summary>
public enum CommandQueueType
{
    Graphics,
    Compute,
    Copy
}
