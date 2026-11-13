using System.Runtime.InteropServices;

namespace Ghost.Core;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PrefabContentHeader
{
    public const uint MAGIC = 0x42465250; // 'PRFB'
    public const uint VERSION = 1;

    public uint magic;
    public uint version;
    public int entityCount;
    public int materialDependencyIndexCount;
    public int componentTypeCount;
    public int meshMaterialFixupCount;
    public long materialDependencyIndicesOffset;
    public long componentTypesOffset;
    public long entitiesOffset;
    public long meshMaterialFixupsOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PrefabEntityHeader
{
    public int parentIndex; // -1 for root
    public int firstChildIndex; // -1 if none
    public int nextSiblingIndex; // -1 if none
    public int componentCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PrefabComponentHeader
{
    public int componentTypeIndex;
    public int dataSize;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PrefabMeshMaterialFixup
{
    public int entityIndex;
    public int meshDependencyIndex;
    public int materialCount;
    public int materialDependencyStartIndex;
}
