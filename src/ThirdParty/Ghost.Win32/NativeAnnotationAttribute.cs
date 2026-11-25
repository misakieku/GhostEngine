using System.Diagnostics;

namespace Ghost.Win32;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
[Conditional("DEBUG")]
public sealed partial class NativeAnnotationAttribute : Attribute
{
    private readonly string _annotation;

    public NativeAnnotationAttribute(string annotation)
    {
        _annotation = annotation;
    }

    public string Annotation => _annotation;
}
