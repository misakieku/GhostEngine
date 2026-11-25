using System.Runtime.InteropServices;

namespace Ghost.Core;

public enum SceneTargetKind : int
{
    None = -1,
    Standalone = 0,
    PrefabNode = 1,
}

public enum SceneFieldValueKind : byte
{
    None = 0,
    Bool = 1,
    Int64 = 2,
    UInt64 = 3,
    Double = 4,
    FloatArray = 5,
    IntArray = 6,
    String = 7,
    AssetHandle = 8,
    EntityRef = 9,
    MaterialPalette = 10,
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SceneEntityTarget : IEquatable<SceneEntityTarget>
{
    public static readonly SceneEntityTarget Invalid = new() { kind = SceneTargetKind.None, index = -1, subIndex = -1 };

    public SceneTargetKind kind;
    public int index;
    public int subIndex;

    public readonly bool IsValid => kind != SceneTargetKind.None && index >= 0;

    public readonly bool Equals(SceneEntityTarget other) => kind == other.kind && index == other.index && subIndex == other.subIndex;
    public override readonly bool Equals(object? obj) => obj is SceneEntityTarget other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine((int)kind, index, subIndex);
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SceneContentHeader
{
    public const uint MAGIC = 0x4E435347; // 'GSCN'
    public const uint VERSION = 2;

    public uint magic;
    public uint version;

    public int stringCount;
    public int entityCount;
    public int prefabInstanceCount;
    public int componentCount;
    public int fieldCount;
    public int volumeOverrideCount;
    public int materialDependencyIndexCount;
    public int nameOverrideCount;
    public int addedComponentCount;
    public int removedComponentCount;
    public int propertyOverrideCount;
    public int prefabVolumeOverrideCount;
    public int dataBlobSize;

    public long stringsOffset;
    public long entitiesOffset;
    public long prefabInstancesOffset;
    public long componentsOffset;
    public long fieldsOffset;
    public long volumeOverridesOffset;
    public long materialDependencyIndicesOffset;
    public long nameOverridesOffset;
    public long addedComponentsOffset;
    public long removedComponentsOffset;
    public long propertyOverridesOffset;
    public long prefabVolumeOverridesOffset;
    public long dataBlobOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SceneEntityHeader
{
    public SceneEntityTarget parentTarget;
    public int nameOffset;
    public int nameByteLength;
    public int componentStartIndex;
    public int componentCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ScenePrefabSpawnHeader
{
    public int prefabDependencyIndex;
    public SceneEntityTarget parentTarget;
    public int templateEntityCount;
    public int nameOverrideStartIndex;
    public int nameOverrideCount;
    public int addedComponentStartIndex;
    public int addedComponentCount;
    public int removedComponentStartIndex;
    public int removedComponentCount;
    public int propertyOverrideStartIndex;
    public int propertyOverrideCount;
    public int volumeOverrideStartIndex;
    public int volumeOverrideCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SceneComponentHeader
{
    public int typeNameIndex;
    public int fieldStartIndex;
    public int fieldCount;
    public int volumeOverrideStartIndex;
    public int volumeOverrideCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SceneVolumeOverrideHeader
{
    public int typeNameIndex;
    public int fieldStartIndex;
    public int fieldCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ScenePrefabNameOverrideHeader
{
    public int templateEntityIndex;
    public int nameOffset;
    public int nameByteLength;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SceneAddedComponentHeader
{
    public int templateEntityIndex;
    public SceneComponentHeader component;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SceneRemovedComponentHeader
{
    public int templateEntityIndex;
    public int typeNameIndex;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SceneFieldRecord
{
    public uint fieldNameHash;
    public SceneFieldValueKind kind;
    public byte reserved0;
    public ushort reserved1;
    public long scalarValue;
    public int payloadOffset;
    public int payloadCount;
    public SceneEntityTarget entityTarget;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ScenePropertyOverrideHeader
{
    public int templateEntityIndex;
    public int componentTypeNameIndex;
    public SceneFieldRecord field;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ScenePrefabVolumeOverrideHeader
{
    public int templateEntityIndex;
    public int volumeTypeNameIndex;
    public byte removed;
    public byte reserved0;
    public ushort reserved1;
    public int fieldStartIndex;
    public int fieldCount;
}
