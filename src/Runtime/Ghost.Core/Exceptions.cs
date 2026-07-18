namespace Ghost.Core;

public class InvalidResourceHandleException : Exception
{
    public InvalidResourceHandleException(string message)
        : base(message)
    {

    }

    public static InvalidResourceHandleException Create<T>(T handle)
    {
        return new InvalidResourceHandleException($"Not able to locate the underlying resource. The handle {handle} is not valid.");
    }
}
