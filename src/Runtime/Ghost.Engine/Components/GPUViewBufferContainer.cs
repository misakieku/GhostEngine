using Ghost.Core;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.Components;

/// <summary>
/// Cleanup component that holds per-camera history buffers (depth target and HZB mip pyramid) across frames.
/// </summary>
public struct GPUViewBufferContainer : ICleanupComponent
{
    public Handle<GPUTexture> depthTarget;
    public HZBMipHandles hzbHistory;
    public uint hzbMipCount;
    public uint historyWidth;
    public uint historyHeight;
    public bool isHistoryValid;
}
