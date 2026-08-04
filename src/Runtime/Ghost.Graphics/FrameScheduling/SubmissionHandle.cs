using Ghost.Graphics.RHI;

namespace Ghost.Graphics.FrameScheduling;

/// <summary>
/// Identifies one deferred command-buffer submission without exposing its native fence value.
/// </summary>
public readonly struct SubmissionHandle : IEquatable<SubmissionHandle>
{
    internal int SchedulerId
    {
        get;
    }

    internal int SubmissionIndex
    {
        get;
    }

    internal uint Generation
    {
        get;
    }

    internal CommandQueueType QueueType
    {
        get;
    }

    internal ulong FenceValue
    {
        get;
    }

    /// <summary>
    /// Gets whether this handle identifies a submission.
    /// </summary>
    public bool IsValid => SchedulerId != 0 && FenceValue != 0;

    internal SubmissionHandle(int schedulerId, int submissionIndex, uint generation, CommandQueueType queueType, ulong fenceValue)
    {
        SchedulerId = schedulerId;
        SubmissionIndex = submissionIndex;
        Generation = generation;
        QueueType = queueType;
        FenceValue = fenceValue;
    }

    /// <inheritdoc />
    public bool Equals(SubmissionHandle other)
    {
        return SchedulerId == other.SchedulerId && QueueType == other.QueueType && FenceValue == other.FenceValue;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SubmissionHandle other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(SchedulerId, QueueType, FenceValue);
    }

    /// <summary>
    /// Determines whether two submission handles identify the same submission.
    /// </summary>
    public static bool operator ==(SubmissionHandle left, SubmissionHandle right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two submission handles identify different submissions.
    /// </summary>
    public static bool operator !=(SubmissionHandle left, SubmissionHandle right)
    {
        return !left.Equals(right);
    }
}
