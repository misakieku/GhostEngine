namespace Ghost.Graphics.FrameScheduling;

/// <summary>
/// Opaque completion state for one submitted frame.
/// </summary>
public readonly struct FrameCompletionInfo
{
    internal SubmissionHandle GraphicsSubmission
    {
        get;
    }

    internal SubmissionHandle ComputeSubmission
    {
        get;
    }

    internal SubmissionHandle CopySubmission
    {
        get;
    }

    /// <summary>
    /// Gets the scheduler-assigned frame number.
    /// </summary>
    public ulong FrameNumber
    {
        get;
    }

    /// <summary>
    /// Gets whether this value represents a flushed frame.
    /// </summary>
    public bool IsValid => FrameNumber != 0;

    internal FrameCompletionInfo(
        ulong frameNumber,
        SubmissionHandle graphicsSubmission,
        SubmissionHandle computeSubmission,
        SubmissionHandle copySubmission)
    {
        FrameNumber = frameNumber;
        GraphicsSubmission = graphicsSubmission;
        ComputeSubmission = computeSubmission;
        CopySubmission = copySubmission;
    }
}
