namespace Ghost.Core;

/// <summary>
/// Marks a struct as a volume component for automatic blend code generation and registration in the volume system.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class VolumeComponentAttribute : Attribute
{
}

/// <summary>
/// Forces a volume component property to be blended using a hardcut rather than interpolation, even if it is a numeric type.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class HardcutAttribute : Attribute
{
}
