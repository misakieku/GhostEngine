namespace Ghost.MicroTest.Core;

[Serializable]
internal class AssertFailedException : Exception
{
    public AssertFailedException()
    {
    }

    public AssertFailedException(string? message) : base(message)
    {
    }

    public AssertFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}