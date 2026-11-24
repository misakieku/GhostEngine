using System.Diagnostics;

namespace Ghost.Win32;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
[Conditional("DEBUG")]
public sealed partial class NativeTypeNameAttribute : Attribute
{
    private readonly string _name;

    public NativeTypeNameAttribute(string name)
    {
        _name = name;
    }

    public string Name => _name;
}
