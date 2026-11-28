namespace Ghost.Core.Graphics;

/// <summary>
/// Rendering surface classification for a material.
/// </summary>
public enum MaterialSurfaceType : byte
{
    Opaque = 0,
    AlphaClip = 1,
    Transparent = 2
}
