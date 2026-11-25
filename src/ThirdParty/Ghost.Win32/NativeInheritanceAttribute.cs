using System.Diagnostics;

namespace Ghost.Win32;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
[Conditional("DEBUG")]
public sealed partial class NativeInheritanceAttribute : Attribute
{
    private readonly string _name;

    public NativeInheritanceAttribute(string name)
    {
        _name = name;
    }

    public string Name => _name;
}
