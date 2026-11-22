namespace Ghost.Entities;

/// <summary>
/// Marks a component struct whose data is allocated and owned transiently at runtime
/// (such as GPU handle indices or runtime scene partitions) and must not be persisted to scene files.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class TransientComponentAttribute : Attribute
{
}
