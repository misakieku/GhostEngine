using Ghost.Core.Attributes;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Describes a single editable field within an ECS component.
/// Knows how to read/write a specific field directly from/to unmanaged memory.
/// </summary>
public sealed class PropertyDescriptor
{
    public string Name { get; }
    public string DisplayName { get; }
    public Type FieldType { get; }
    public int OffsetInComponent { get; }
    public int FieldSize { get; }
    public bool IsReadOnly { get; }
    
    // For nested structs (e.g. float4x4 -> float4 -> float)
    public PropertyDescriptor[]? Children { get; }

    internal PropertyDescriptor(FieldInfo fieldInfo, int parentOffset)
    {
        Name = fieldInfo.Name;
        FieldType = fieldInfo.FieldType;
        OffsetInComponent = parentOffset + (int)Marshal.OffsetOf(fieldInfo.DeclaringType!, fieldInfo.Name);
        FieldSize = Marshal.SizeOf(FieldType);
        
        IsReadOnly = fieldInfo.GetCustomAttribute<ReadOnlyInInspectorAttribute>() != null;
        
        var nameAttr = fieldInfo.GetCustomAttribute<InspectorNameAttribute>();
        DisplayName = nameAttr?.Name ?? FormatName(Name);
        
        // Handle nested structs if this is an unmanaged struct that is not a primitive or common vector type we have custom drawers for.
        // We will refine nested struct decomposition later if needed.
    }

    internal PropertyDescriptor(string name, Type type, int offset, int size, bool isReadOnly, PropertyDescriptor[]? children = null)
    {
        Name = name;
        DisplayName = FormatName(name);
        FieldType = type;
        OffsetInComponent = offset;
        FieldSize = size;
        IsReadOnly = isReadOnly;
        Children = children;
    }

    private static string FormatName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.StartsWith("_")) name = name.Substring(1);
        if (name.Length == 0) return name;
        
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    public unsafe object ReadBoxed(void* pComponent)
    {
        var src = (byte*)pComponent + OffsetInComponent;
        return Marshal.PtrToStructure((nint)src, FieldType)!;
    }

    public unsafe void WriteBoxed(void* pComponent, object value)
    {
        if (IsReadOnly) return;
        var dst = (byte*)pComponent + OffsetInComponent;
        Marshal.StructureToPtr(value, (nint)dst, false);
    }

    public unsafe ref T Read<T>(void* pComponent) where T : unmanaged
    {
        return ref *(T*)((byte*)pComponent + OffsetInComponent);
    }

    public unsafe void Write<T>(void* pComponent, in T value) where T : unmanaged
    {
        if (IsReadOnly) return;
        *(T*)((byte*)pComponent + OffsetInComponent) = value;
    }
}
