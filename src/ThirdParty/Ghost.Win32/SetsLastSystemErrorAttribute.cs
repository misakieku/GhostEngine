using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ghost.Win32;

/// <summary>Specifies that the given method sets the last system error and it can be retrieved via <see cref="Marshal.GetLastSystemError" />.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
[Conditional("DEBUG")]
public sealed partial class SetsLastSystemErrorAttribute : Attribute
{
    public SetsLastSystemErrorAttribute()
    {
    }
}
