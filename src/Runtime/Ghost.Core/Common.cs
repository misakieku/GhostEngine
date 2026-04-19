namespace Ghost.Core;

public enum AssetType : byte
{
    Texture = 0,
    Mesh = 1,
    Material = 2,
    Shaders = 3,
    Audio = 4,
    Scene = 5,
    Video = 6,
    Json = 7,

    Unknown = 64,
}
