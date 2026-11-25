using System.Diagnostics;

namespace Ghost.Win32;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
[Conditional("DEBUG")]
public sealed partial class VtblIndexAttribute : Attribute
{
    private readonly uint _index;

    public VtblIndexAttribute(uint index)
    {
        _index = index;
    }

    public uint Index => _index;
}
