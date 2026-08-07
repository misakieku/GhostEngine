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
    /// Validates the scheduler state and reserves storage for an upcoming sequence of submissions.
    /// </summary>
    /// <remarks>
    /// This method does not transfer command-buffer ownership. After it succeeds, the specified number of valid
    /// <see cref="Submit"/> calls can be made without growing the scheduler's internal submission storage.
    /// </remarks>
    /// <param name="additionalSubmissionCount">Number of submissions that will be appended to the pending frame.</param>
    void PrepareSubmissions(int additionalSubmissionCount);

    /// <summary>
    /// Starts an atomic sequence of submissions and explicit dependencies.
    /// </summary>
    /// <remarks>
    /// The scheduler snapshots its pending-frame state and reserves all requested storage before ownership transfer begins.
    /// The returned token must be committed or rolled back exactly once. Submission handles created by the transaction must
    /// not escape until the transaction commits.
    /// </remarks>
    /// <param name="additionalSubmissionCount">Maximum number of submissions appended by the transaction.</param>
    /// <param name="additionalDependencyCount">Maximum number of explicit dependencies appended by the transaction.</param>
    /// <returns>A token identifying the active transaction.</returns>
    SubmissionTransaction BeginSubmissionTransaction(int additionalSubmissionCount, int additionalDependencyCount);

    /// <summary>
    /// Commits an active submission transaction.
    /// </summary>
    /// <param name="transaction">The active transaction token.</param>
    void CommitSubmissionTransaction(SubmissionTransaction transaction);

    /// <summary>
    /// Rolls back an active submission transaction and returns every command buffer transferred by it.
    /// </summary>
    /// <param name="transaction">The active transaction token.</param>
    void RollbackSubmissionTransaction(SubmissionTransaction transaction);

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
