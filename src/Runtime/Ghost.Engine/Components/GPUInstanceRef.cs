using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Services;

namespace Ghost.Engine.Components;

[TransientComponent]
public struct GPUInstanceRef : ICleanupComponent
{
    public uint gpuInstanceIndex;
    public Identifier<MaterialPalette> materialPalette;
}
