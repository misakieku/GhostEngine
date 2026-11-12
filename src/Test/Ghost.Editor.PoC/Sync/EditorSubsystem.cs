using Ghost.Core;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Editor.PoC.Sync;

public sealed class EditorSubsystem
{
    private readonly EditorSyncBridge _bridge;
    private readonly List<PendingPropertyMutation> _drainedMutations = new();
    private readonly List<(Entity Entity, Type ComponentType)> _monitoredComponents = new();

    public EditorSubsystem(EditorSyncBridge bridge)
    {
        _bridge = bridge;
    }

    public void SetMonitored(Entity entity, IEnumerable<Type> componentTypes)
    {
        lock (_monitoredComponents)
        {
            _monitoredComponents.Clear();
            foreach (var t in componentTypes)
            {
                _monitoredComponents.Add((entity, t));
            }
        }
    }

    public void RegisterMonitored(Entity entity, Type componentType)
    {
        lock (_monitoredComponents)
        {
            if (!_monitoredComponents.Contains((entity, componentType)))
            {
                _monitoredComponents.Add((entity, componentType));
            }
        }
    }

    public unsafe void PreTick(World world)
    {
        _bridge.DrainMutations(_drainedMutations);
        if (_drainedMutations.Count == 0)
        {
            return;
        }

        for (var i = 0; i < _drainedMutations.Count; i++)
        {
            var mutation = _drainedMutations[i];
            if (!world.EntityManager.Exists(mutation.Entity))
            {
                continue;
            }

            var compId = GetOrRegisterComponentID(mutation.ComponentType);
            if (!compId.IsValid || !world.EntityManager.HasComponent(mutation.Entity, compId))
            {
                continue;
            }

            var ptr = (byte*)world.EntityManager.GetComponent(mutation.Entity, compId);
            if (ptr == null)
            {
                continue;
            }

            var field = mutation.ComponentType.GetField(mutation.FieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                WriteFieldValue(ptr, field, mutation.Value);
            }
        }
    }

    public unsafe void PostTick(World world)
    {
        var snapshot = new Dictionary<(Entity Entity, Type ComponentType, string FieldName), object?>();

        lock (_monitoredComponents)
        {
            for (var i = 0; i < _monitoredComponents.Count; i++)
            {
                var (entity, compType) = _monitoredComponents[i];
                if (!world.EntityManager.Exists(entity))
                {
                    continue;
                }

                var compId = GetOrRegisterComponentID(compType);
                if (!compId.IsValid || !world.EntityManager.HasComponent(entity, compId))
                {
                    continue;
                }

                var ptr = (byte*)world.EntityManager.GetComponent(entity, compId);
                if (ptr == null)
                {
                    continue;
                }

                var fields = compType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                for (var f = 0; f < fields.Length; f++)
                {
                    var field = fields[f];
                    var value = ReadFieldValue(ptr, field);
                    snapshot[(entity, compType, field.Name)] = value;
                }
            }
        }

        _bridge.PublishSnapshot(snapshot);
    }

    private static Identifier<IComponent> GetOrRegisterComponentID(Type type)
    {
        var id = ComponentRegistry.GetComponentID(type);
        if (!id.IsValid)
        {
            var openType = typeof(ComponentTypeID<>);
            var closedType = openType.MakeGenericType(type);
            RuntimeHelpers.RunClassConstructor(closedType.TypeHandle);
            id = ComponentRegistry.GetComponentID(type);
        }
        return id;
    }

    private static int GetFieldOffset(FieldInfo field)
    {
        return Marshal.OffsetOf(field.DeclaringType!, field.Name).ToInt32();
    }

    private static unsafe void WriteFieldValue(byte* basePtr, FieldInfo field, object? value)
    {
        if (value == null)
        {
            return;
        }

        var offset = GetFieldOffset(field);
        var dest = basePtr + offset;
        var type = field.FieldType;

        if (type == typeof(int))
        {
            *(int*)dest = Convert.ToInt32(value);
        }
        else if (type == typeof(float))
        {
            *(float*)dest = Convert.ToSingle(value);
        }
        else if (type == typeof(double))
        {
            *(double*)dest = Convert.ToDouble(value);
        }
        else if (type == typeof(bool))
        {
            *(bool*)dest = Convert.ToBoolean(value);
        }
        else if (type == typeof(float3))
        {
            *(float3*)dest = (float3)value;
        }
        else if (type == typeof(quaternion))
        {
            *(quaternion*)dest = (quaternion)value;
        }
        else if (type == typeof(uint))
        {
            *(uint*)dest = Convert.ToUInt32(value);
        }
        else if (type == typeof(long))
        {
            *(long*)dest = Convert.ToInt64(value);
        }
        else if (type == typeof(short))
        {
            *(short*)dest = Convert.ToInt16(value);
        }
        else if (type == typeof(byte))
        {
            *(byte*)dest = Convert.ToByte(value);
        }
        else
        {
            Marshal.StructureToPtr(value, new IntPtr(dest), false);
        }
    }

    private static unsafe object? ReadFieldValue(byte* basePtr, FieldInfo field)
    {
        var offset = GetFieldOffset(field);
        var src = basePtr + offset;
        var type = field.FieldType;

        if (type == typeof(int))
        {
            return *(int*)src;
        }
        if (type == typeof(float))
        {
            return *(float*)src;
        }
        if (type == typeof(double))
        {
            return *(double*)src;
        }
        if (type == typeof(bool))
        {
            return *(bool*)src;
        }
        if (type == typeof(float3))
        {
            return *(float3*)src;
        }
        if (type == typeof(quaternion))
        {
            return *(quaternion*)src;
        }
        if (type == typeof(uint))
        {
            return *(uint*)src;
        }
        if (type == typeof(long))
        {
            return *(long*)src;
        }
        if (type == typeof(short))
        {
            return *(short*)src;
        }
        if (type == typeof(byte))
        {
            return *(byte*)src;
        }

        return Marshal.PtrToStructure(new IntPtr(src), type);
    }
}
