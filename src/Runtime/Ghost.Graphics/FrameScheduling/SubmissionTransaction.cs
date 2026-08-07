namespace Ghost.Graphics.FrameScheduling;

/// <summary>
/// Identifies an active scheduler transaction that can be committed or rolled back atomically.
/// </summary>
/// <remarks>
/// Submission handles created inside a transaction must not escape until the transaction commits.
/// </remarks>
public readonly struct SubmissionTransaction
{
    internal readonly int SchedulerId;
    internal readonly uint SchedulerGeneration;
    internal readonly uint TransactionId;

    /// <summary>
    /// Gets whether this token identifies a scheduler transaction.
    /// </summary>
    public bool IsValid => SchedulerId != 0 && SchedulerGeneration != 0 && TransactionId != 0;

    internal SubmissionTransaction(int schedulerId, uint schedulerGeneration, uint transactionId)
    {
        SchedulerId = schedulerId;
        SchedulerGeneration = schedulerGeneration;
        TransactionId = transactionId;
    }
}
