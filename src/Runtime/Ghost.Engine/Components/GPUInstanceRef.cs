using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Services;

namespace Ghost.Engine.Components;

public struct GPUInstanceRef : IComponent
{
    public uint gpuInstanceIndex;
    public Identifier<MaterialPalette> materialPalette;
}
