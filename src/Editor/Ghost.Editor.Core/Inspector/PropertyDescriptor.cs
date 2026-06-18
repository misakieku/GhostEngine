using Ghost.Core;
using Ghost.Editor.Core.SceneGraph;
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
    public Type ValueType { get; }
    public int OffsetInComponent { get; }
    public bool IsReadOnly { get; }

    // For nested structs (e.g. float4x4 -> float4 -> float)
    public PropertyDescriptor[]? Children { get; }

    // TODO: Use source generators to build these at compile time and avoid all reflection/attributes at runtime.
    internal PropertyDescriptor(FieldInfo fieldInfo, int parentOffset)
    {
        Name = fieldInfo.Name;
        ValueType = fieldInfo.FieldType;
        OffsetInComponent = parentOffset + (int)Marshal.OffsetOf(fieldInfo.DeclaringType!, fieldInfo.Name);

        IsReadOnly = fieldInfo.GetCustomAttribute<ReadOnlyInInspectorAttribute>() != null;

        var nameAttr = fieldInfo.GetCustomAttribute<InspectorNameAttribute>();
        DisplayName = nameAttr?.Name ?? FormatName(Name);

        // Handle nested structs if this is an unmanaged struct that is not a primitive or common vector type we have custom drawers for.
        if (ValueType.IsValueType && !ValueType.IsPrimitive && !ValueType.IsEnum)
        {
            if (!PropertyDrawerRegistry.HasCustomDrawer(ValueType))
            {
                var children = new List<PropertyDescriptor>();
                var fields = ValueType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var nestedField in fields)
                {
                    if (!nestedField.IsPublic &&
                        nestedField.GetCustomAttribute<InspectorGroupAttribute>() == null &&
                        nestedField.GetCustomAttribute<ReadOnlyInInspectorAttribute>() == null)
                    {
                        continue;
                    }
                    children.Add(new PropertyDescriptor(nestedField, OffsetInComponent));
                }
                if (children.Count > 0)
                {
                    Children = children.ToArray();
                }
            }
        }
    }

    internal PropertyDescriptor(string name, Type type, int offset, bool isReadOnly, PropertyDescriptor[]? children = null)
    {
        Name = name;
        DisplayName = FormatName(name);
        ValueType = type;
        OffsetInComponent = offset;
        IsReadOnly = isReadOnly;
        Children = children;
    }

    private static string FormatName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (name.StartsWith('_'))
        {
            name = name.Substring(1);
        }

        if (name.Length == 0)
        {
            return name;
        }

        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    public unsafe object ReadBoxed(void* pComponent)
    {
        var src = (byte*)pComponent + OffsetInComponent;
        return Marshal.PtrToStructure((nint)src, ValueType)!;
    }

    public unsafe void WriteBoxed(void* pComponent, object value)
    {
        if (IsReadOnly)
        {
            return;
        }

        var dst = (byte*)pComponent + OffsetInComponent;
        Marshal.StructureToPtr(value, (nint)dst, false);
    }

    public unsafe ref T Read<T>(void* pComponent) where T : unmanaged
    {
        return ref *(T*)((byte*)pComponent + OffsetInComponent);
    }

    public unsafe void Write<T>(void* pComponent, in T value) where T : unmanaged
    {
        if (IsReadOnly)
        {
            return;
        }

        *(T*)((byte*)pComponent + OffsetInComponent) = value;
    }
}
