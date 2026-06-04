using Ghost.Entities;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.Services;

/// <summary>
/// Caches the memory offsets of all fields of type `Entity` within unmanaged ECS components.
/// Used by the Undo system to patch references during serialization/deserialization.
/// </summary>
internal static class EntityFieldTracker
{
    private static readonly Dictionary<int, int[]> s_entityOffsets = new();
    private static readonly Lock s_lock = new();

    public static int[] GetEntityOffsets(int componentId)
    {
        lock (s_lock)
        {
            if (s_entityOffsets.TryGetValue(componentId, out var offsets))
            {
                return offsets;
            }

            if (!ComponentRegistry.s_runtimeIDToType.TryGetValue(componentId, out var type))
            {
                s_entityOffsets[componentId] = Array.Empty<int>();
                return Array.Empty<int>();
            }

            var offsetList = new List<int>();
            FindEntityFieldsRecursive(type, 0, offsetList);
            
            offsets = offsetList.ToArray();
            s_entityOffsets[componentId] = offsets;
            return offsets;
        }
    }

    private static void FindEntityFieldsRecursive(Type type, int currentOffset, List<int> offsetList)
    {
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            var fieldOffset = currentOffset + (int)Marshal.OffsetOf(type, field.Name);

            if (field.FieldType == typeof(Entity))
            {
                offsetList.Add(fieldOffset);
            }
            else if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && !field.FieldType.IsEnum)
            {
                // Recursively check nested structs
                FindEntityFieldsRecursive(field.FieldType, fieldOffset, offsetList);
            }
        }
    }
}
