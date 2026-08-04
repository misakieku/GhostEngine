using Ghost.Graphics.RHI;

namespace Ghost.Graphics.FrameScheduling;

/// <summary>
/// Coordinates deferred native command-buffer submissions and queue synchronization for a graphics device.
/// </summary>
/// <remarks>
/// This interface is render-thread-only. Resource barriers remain the responsibility of the command recorder or render graph.
/// </remarks>
public interface IFrameScheduler : IDisposable
{
    /// <summary>
    /// Gets the number of frames flushed by this scheduler.
    /// </summary>
    ulong SubmittedFrame
    {
        get;
    }

    /// <summary>
    /// Defers an executable command buffer for submission to the queue matching its type.
    /// </summary>
    /// <remarks>
    /// Ownership transfers to the scheduler until native submission. The scheduler then returns the command buffer to the graphics-engine pool.
    /// </remarks>
    /// <param name="commandBuffer">An ended command buffer ready for native submission.</param>
    /// <returns>An opaque handle used to declare dependencies or query completion.</returns>
    SubmissionHandle Submit(ICommandBuffer commandBuffer);

    /// <summary>
    /// Requires the dependent submission to begin only after the producer submission completes.
    /// </summary>
    /// <param name="producer">The submission producing the dependency.</param>
    /// <param name="dependent">The submission consuming the dependency.</param>
    void AddDependency(SubmissionHandle producer, SubmissionHandle dependent);

    /// <summary>
    /// Makes the next destination-queue submission depend on the latest source-queue submission.
    /// </summary>
    /// <param name="source">The queue producing the dependency.</param>
    /// <param name="destination">The queue that will consume the dependency.</param>
    void Transition(CommandQueueType source, CommandQueueType destination);

    /// <summary>
    /// Gets whether the GPU has completed a submission.
    /// </summary>
    /// <param name="submission">The submission to query.</param>
    bool IsComplete(SubmissionHandle submission);

    /// <summary>
    /// Resolves the pending submission graph and enqueues all native queue operations.
    /// </summary>
    /// <returns>Opaque completion state for the flushed frame.</returns>
    FrameCompletionInfo Flush();

    /// <summary>
    /// Blocks the CPU until every queue represented by a frame completion value has completed.
    /// </summary>
    /// <param name="completion">The frame completion value to wait for.</param>
    void WaitForFrame(scoped in FrameCompletionInfo completion);

    /// <summary>
    /// Submits pending work and blocks until all device queues are idle.
    /// </summary>
    void WaitIdle();
}
