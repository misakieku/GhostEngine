using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using TerraFX.Interop.Windows;

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

public static class Exceptions
{
    [SupportedOSPlatform("windows")]
    public static void TerminateIfFailed(HRESULT hr, [CallerArgumentExpression(nameof(hr))] string? op = null)
    {
        if (hr.FAILED)
        {
            var message = $"Operation {op} failed with HRESULT {hr}.";
            Logger.Error(message);
            Environment.FailFast(message);
        }
    }

    public static void TerminateIfFailed(Error error, [CallerArgumentExpression(nameof(error))] string? op = null)
    {
        if (error != Error.None)
        {
            var message = $"Operation {op} failed with error {error}.";
            Logger.Error(message);
            Environment.FailFast(message);
        }
    }
}