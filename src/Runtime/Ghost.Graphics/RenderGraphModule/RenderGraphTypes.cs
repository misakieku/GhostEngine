using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Graphics.RenderGraphModule;

public enum RGResourceType : int
{
    Texture,
    Buffer,
    // AccelerationStructure,
    Count
}

/// <summary>
/// Specifies how texture dimensions are determined.
/// </summary>
public enum RGTextureSizeMode : byte
{
    /// <summary>
    /// Fixed pixel dimensions (width, height).
    /// </summary>
    Absolute,

    /// <summary>
    /// Scale relative to view state (scaleX * viewportWidth, scaleY * viewportHeight).
    /// </summary>
    Relative
}

/// <summary>
/// Render graph texture descriptor with support for relative sizing and clear operations.
/// </summary>
public record struct RGTextureDesc : IEquatable<RGTextureDesc>
{
    public RGTextureSizeMode sizeMode;

    // Size specification (union-like - only one set is used based on sizeMode)
    public uint width;          // For Absolute mode
    public uint height;         // For Absolute mode
    public float scaleX;        // For Relative mode
    public float scaleY;        // For Relative mode

    // Common texture properties
    public TextureFormat format;
    public TextureDimension dimension;
    public uint mipLevels;
    public uint slice;
    public TextureUsage usage;
    public TypelessFormatDesc typelessViewFormat;

    public bool clearAtFirstUse;
    public bool discardAtLastUse;

    public Color128 clearColor;
    public float clearDepth;
    public byte clearStencil;

    /// <summary>
    /// Creates a texture descriptor with absolute dimensions.
    /// </summary>
    public static RGTextureDesc Absolute(
        uint width,
        uint height,
        TextureFormat format,
        Color128 clearColor = default,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureDimension dimension = TextureDimension.Texture2D,
        uint mipLevels = 1,
        uint slice = 1,
        TextureUsage usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
        TypelessFormatDesc typelessViewFormat = default)
    {
        return new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Absolute,
            width = width,
            height = height,
            format = format,
            clearColor = clearColor,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            clearDepth = 0.0f,
            clearStencil = 0,
            dimension = dimension,
            mipLevels = mipLevels,
            slice = slice,
            usage = usage,
            typelessViewFormat = typelessViewFormat
        };
    }

    public static RGTextureDesc AbsoluteDepth(
        uint width,
        uint height,
        TextureFormat format,
        float clearDepth = 0.0f,
        byte clearStencil = 0,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureUsage usage = TextureUsage.DepthStencil,
        TypelessFormatDesc typelessViewFormat = default)
    {
        return new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Absolute,
            width = width,
            height = height,
            format = format,
            clearColor = default,
            clearDepth = clearDepth,
            clearStencil = clearStencil,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            dimension = TextureDimension.Texture2D,
            mipLevels = 1,
            slice = 1,
            usage = usage,
            typelessViewFormat = typelessViewFormat
        };
    }

    /// <summary>
    /// Creates a texture descriptor with relative dimensions (uniform scale).
    /// </summary>
    public static RGTextureDesc Relative(
        float scale,
        TextureFormat format,
        Color128 clearColor = default,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureDimension dimension = TextureDimension.Texture2D,
        uint mipLevels = 1,
        uint slice = 1,
        TextureUsage usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
        TypelessFormatDesc typelessViewFormat = default)
    {
        return new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Relative,
            scaleX = scale,
            scaleY = scale,
            format = format,
            clearColor = clearColor,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            clearDepth = 0.0f,
            clearStencil = 0,
            dimension = dimension,
            mipLevels = mipLevels,
            slice = slice,
            usage = usage,
            typelessViewFormat = typelessViewFormat
        };
    }

    /// <summary>
    /// Creates a texture descriptor with relative dimensions (non-uniform scale).
    /// </summary>
    public static RGTextureDesc Relative(
        float scaleX,
        float scaleY,
        TextureFormat format,
        Color128 clearColor = default,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureDimension dimension = TextureDimension.Texture2D,
        uint mipLevels = 1,
        uint slice = 1,
        TextureUsage usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
        TypelessFormatDesc typelessViewFormat = default)
    {
        return new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Relative,
            scaleX = scaleX,
            scaleY = scaleY,
            format = format,
            clearColor = clearColor,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            clearDepth = 0.0f,
            clearStencil = 0,
            dimension = dimension,
            mipLevels = mipLevels,
            slice = slice,
            usage = usage,
            typelessViewFormat = typelessViewFormat
        };
    }


    /// <summary>
    /// Creates a depth texture descriptor with relative dimensions.
    /// </summary>
    public static RGTextureDesc RelativeDepth(
        float scale,
        float clearDepth = 0.0f,
        byte clearStencil = 0,
        bool clearAtFirstUse = true,
        bool discardAtLastUse = true,
        TextureFormat format = TextureFormat.D32_Float,
        TextureUsage usage = TextureUsage.DepthStencil,
        TypelessFormatDesc typelessViewFormat = default)
    {
        return new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Relative,
            scaleX = scale,
            scaleY = scale,
            format = format,
            clearColor = default,
            clearDepth = clearDepth,
            clearStencil = clearStencil,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            dimension = TextureDimension.Texture2D,
            mipLevels = 1,
            slice = 1,
            usage = usage,
            typelessViewFormat = typelessViewFormat
        };
    }


    /// <summary>
    /// Converts to RHI TextureDesc using resolved dimensions.
    /// </summary>
    public readonly TextureDesc ToTextureDesc(uint resolvedWidth, uint resolvedHeight)
    {
        return new TextureDesc
        {
            Width = resolvedWidth,
            Height = resolvedHeight,
            Format = format,
            Dimension = dimension,
            MipLevels = mipLevels,
            Slice = slice,
            Usage = usage,
            TypelessViewFormat = typelessViewFormat
        };
    }
}

public struct RGResource;
public struct RGTexture;
public struct RGBuffer;

public static class RGResourceExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<RGResource> AsResource(this Identifier<RGTexture> texture)
    {
        return Unsafe.BitCast<Identifier<RGTexture>, Identifier<RGResource>>(texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Identifier<RGResource> AsResource(this Identifier<RGBuffer> buffer)
    {
        return Unsafe.BitCast<Identifier<RGBuffer>, Identifier<RGResource>>(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Identifier<RGTexture> AsTexture(this Identifier<RGResource> resource)
    {
        return Unsafe.BitCast<Identifier<RGResource>, Identifier<RGTexture>>(resource);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Identifier<RGBuffer> AsBuffer(this Identifier<RGResource> resource)
    {
        return Unsafe.BitCast<Identifier<RGResource>, Identifier<RGBuffer>>(resource);
    }
}

internal readonly struct TextureAccess
{
    public readonly Identifier<RGTexture> id;
    public readonly AccessFlags accessFlags;
    public readonly ResourceBarrierData usage;

    public TextureAccess(Identifier<RGTexture> id, AccessFlags accessFlags, ResourceBarrierData usage)
    {
        this.id = id;
        this.accessFlags = accessFlags;
        this.usage = usage;
    }
}

[InlineArray(RHIUtility.MAX_RENDER_TARGETS)]
internal struct TextureAccessArray
{
    public TextureAccess access;
}

internal readonly struct BufferAccess
{
    public readonly Identifier<RGBuffer> id;
    public readonly AccessFlags accessFlags;
    public readonly ResourceBarrierData usage;

    public BufferAccess(Identifier<RGBuffer> id, AccessFlags accessFlags, ResourceBarrierData usage)
    {
        this.id = id;
        this.accessFlags = accessFlags;
        this.usage = usage;
    }
}

internal struct RenderTargetInfo
{
    public Identifier<RGTexture> texture;
    public AccessFlags access;
    public AttachmentLoadOp loadOp;
    public AttachmentStoreOp storeOp;
    public Color128 clearColor;
}

[InlineArray(RHIUtility.MAX_RENDER_TARGETS)]
internal struct RenderTargetInfoArray
{
    public RenderTargetInfo info;
}

internal struct DepthStencilInfo
{
    public Identifier<RGTexture> texture;
    public AccessFlags access;
    public AttachmentLoadOp loadOp;
    public AttachmentStoreOp storeOp;
    public AttachmentLoadOp stencilLoadOp;
    public AttachmentStoreOp stencilStoreOp;
    public float clearDepth;
    public byte clearStencil;
}

internal enum RGExecutionOpType : byte
{
    IssueBarriers = 0,
    BeginNativePass = 1,
    ExecutePass = 2,
    EndNativePass = 3,
    CommandBufferSyncPoint = 4,
}

[Flags]
public enum RGFlags
{
    /// <summary>
    /// Default execution behavior without any special flags.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Generate a detailed dump of the render graph execution for debugging and analysis.
    /// </summary>
    GenerateDump = 1 << 0,
    /// <summary>
    /// Record every structural segment on the Graphics queue and suppress internal cross-queue dependencies.
    /// </summary>
    ForceGraphics = 1 << 1,
    /// <summary>
    /// Prevent memory heap aliasing for resources, ensuring each resource has a unique memory allocation.
    /// </summary>
    NoAliasing = 1 << 2,
}

public sealed class RenderGraphDump
{
    public ulong GraphHash
    {
        get; init;
    }

    public bool IsCacheHit
    {
        get; init;
    }

    public ViewportState Viewport
    {
        get; init;
    }

    // Memory Heap Aliasing Map
    public ulong TotalHeapSize
    {
        get; init;
    }

    public ulong SizeWithoutAliasing
    {
        get; init;
    }

    public List<HeapBlockDumpInfo> MemoryBlocks
    {
        get; init;
    } = new();

    // Complete Pass List (with culling & merge info)
    public List<PassDumpInfo> Passes
    {
        get; init;
    } = new();

    // Disassembled Binary Command Stream
    public List<string> CommandStream
    {
        get; init;
    } = new();

    // Complete Resource List (with offsets & lifetimes)
    public List<ResourceDumpInfo> Resources
    {
        get; init;
    } = new();

    public string ExportHtml()
    {
        var passesJson = JsonSerializer.Serialize(Passes, RGDumpJsonContext.Default.ListPassDumpInfo);
        var resourcesJson = JsonSerializer.Serialize(Resources, RGDumpJsonContext.Default.ListResourceDumpInfo);
        var memoryJson = JsonSerializer.Serialize(MemoryBlocks, RGDumpJsonContext.Default.ListHeapBlockDumpInfo);

        var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <title>GhostEngine - Render Graph Inspector</title>
            <style>
                :root {
                    --bg-dark: #121418;
                    --bg-panel: #181b20;
                    --bg-card: #20242c;
                    --border-color: #2e3440;
                    --border-light: #3d4454;
                    --text-primary: #f0f6fc;
                    --text-secondary: #8b949e;
                    --text-muted: #656d76;

                    --color-read: #7ee787;
                    --color-write: #f85149;
                    --color-retained: #383e4a;
                    --color-empty: transparent;

                    --pass-raster: #388bfd;
                    --pass-compute: #a371f7;
                    --pass-async: #e3b341;
                    --pass-unsafe: #8b949e;
                    --pass-culled: #21262d;

                    --badge-tex: #388bfd;
                    --badge-buf: #f0883e;

                    --col-w: 36px;
                    --res-w: 380px;
                    --header-h: 175px;
                    --row-h: 24px;
                }

                * { box-sizing: border-box; }
                body {
                    margin: 0;
                    padding: 12px 16px;
                    background-color: var(--bg-dark);
                    color: var(--text-primary);
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, monospace;
                    font-size: 13px;
                    overflow: hidden;
                    height: 100vh;
                    display: flex;
                    flex-direction: column;
                }

                /* Top Header & Stats */
                header {
                    display: flex;
                    flex-wrap: wrap;
                    align-items: center;
                    justify-content: space-between;
                    gap: 16px;
                    background: var(--bg-panel);
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    padding: 10px 16px;
                    margin-bottom: 8px;
                    flex-shrink: 0;
                }
                .title-group {
                    display: flex;
                    align-items: center;
                    gap: 12px;
                }
                .title-group h1 {
                    margin: 0;
                    font-size: 17px;
                    font-weight: 700;
                    letter-spacing: 0.5px;
                    color: #58a6ff;
                }
                .stats-group {
                    display: flex;
                    gap: 20px;
                    align-items: center;
                }
                .stat-pill {
                    display: flex;
                    flex-direction: column;
                }
                .stat-pill .label {
                    font-size: 10px;
                    color: var(--text-secondary);
                    font-weight: 600;
                    text-transform: uppercase;
                }
                .stat-pill .val {
                    font-size: 15px;
                    font-weight: 700;
                    color: var(--text-primary);
                }
                .val.saved { color: #3fb950; }

                /* Legend Bar */
                .legend-bar {
                    display: flex;
                    flex-wrap: wrap;
                    gap: 18px;
                    align-items: center;
                    background: var(--bg-card);
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    padding: 6px 14px;
                    margin-bottom: 8px;
                    font-size: 11.5px;
                    flex-shrink: 0;
                }
                .legend-section {
                    display: flex;
                    align-items: center;
                    gap: 10px;
                }
                .legend-title {
                    font-weight: bold;
                    color: var(--text-secondary);
                    margin-right: 4px;
                }
                .legend-item {
                    display: flex;
                    align-items: center;
                    gap: 5px;
                }
                .legend-box {
                    width: 13px;
                    height: 13px;
                    border-radius: 2px;
                    border: 1px solid rgba(255,255,255,0.1);
                }
                .box-write { background: var(--color-write); }
                .box-read { background: var(--color-read); }
                .box-readwrite { background: linear-gradient(135deg, var(--color-write) 50%, var(--color-read) 50%); }
                .box-retained { background: var(--color-retained); border-color: #555; }

                .legend-dot {
                    width: 9px;
                    height: 9px;
                    border-radius: 50%;
                }
                .dot-raster { background: var(--pass-raster); }
                .dot-compute { background: var(--pass-compute); }
                .dot-async { background: var(--pass-async); box-shadow: 0 0 6px rgba(227, 179, 65, 0.6); }
                .dot-unsafe { background: var(--pass-unsafe); }
                .dot-culled { background: var(--pass-culled); border: 1px solid #555; }

                /* Controls Toolbar */
                .controls-bar {
                    display: flex;
                    align-items: center;
                    gap: 16px;
                    margin-bottom: 8px;
                    font-size: 12px;
                    flex-shrink: 0;
                }
                .search-box {
                    background: var(--bg-panel);
                    border: 1px solid var(--border-color);
                    border-radius: 4px;
                    padding: 4px 10px;
                    color: var(--text-primary);
                    font-size: 12px;
                    width: 240px;
                }
                .search-box:focus {
                    outline: none;
                    border-color: #58a6ff;
                }
                .control-item {
                    display: flex;
                    align-items: center;
                    gap: 6px;
                    cursor: pointer;
                    user-select: none;
                }
                .btn-zoom {
                    background: var(--bg-card);
                    border: 1px solid var(--border-color);
                    color: var(--text-primary);
                    border-radius: 4px;
                    padding: 3px 8px;
                    cursor: pointer;
                    font-size: 12px;
                    font-weight: bold;
                }
                .btn-zoom:hover {
                    background: #2b313d;
                    border-color: #58a6ff;
                }

                /* Layout Container */
                .inspector-container {
                    flex: 1;
                    display: flex;
                    flex-direction: column;
                    background: var(--bg-panel);
                    border: 1px solid var(--border-color);
                    border-radius: 6px;
                    overflow: hidden;
                    position: relative;
                }

                /* Fixed Header Pane (No Vertical Scroll) */
                .header-pane {
                    display: flex;
                    height: var(--header-h);
                    background: var(--bg-panel);
                    border-bottom: 2px solid var(--border-color);
                    flex-shrink: 0;
                    z-index: 10;
                }
                .corner-header {
                    width: var(--res-w);
                    min-width: var(--res-w);
                    max-width: var(--res-w);
                    height: var(--header-h);
                    background: var(--bg-panel);
                    border-right: 2px solid var(--border-color);
                    padding: 12px 16px;
                    display: flex;
                    flex-direction: column;
                    justify-content: flex-end;
                    box-shadow: 2px 0 6px rgba(0,0,0,0.25);
                    z-index: 15;
                    flex-shrink: 0;
                }
                .corner-title {
                    font-size: 13px;
                    font-weight: 700;
                    color: var(--text-primary);
                    margin-bottom: 3px;
                }
                .corner-sub {
                    font-size: 11px;
                    color: var(--text-secondary);
                }

                .pass-headers-viewport {
                    flex: 1;
                    overflow-x: hidden; /* Programmatically synced with bodyViewport.scrollLeft */
                    overflow-y: hidden;
                    position: relative;
                }
                .pass-headers-track {
                    display: flex;
                    height: var(--header-h);
                    width: max-content;
                }

                .pass-header-col {
                    width: var(--col-w);
                    min-width: var(--col-w);
                    max-width: var(--col-w);
                    height: var(--header-h);
                    position: relative;
                    flex-shrink: 0;
                    border-right: 1px solid #232730;
                }
                .pass-header-col:hover {
                    background: rgba(255,255,255,0.03);
                }
                .pass-name-anchor {
                    position: absolute;
                    bottom: 26px; /* Right above the badge */
                    left: calc(var(--col-w) / 2); /* Exact horizontal center of the column */
                    width: 0;
                    height: 0;
                }
                .pass-name {
                    position: absolute;
                    bottom: 0;
                    left: 0;
                    transform: rotate(-45deg);
                    transform-origin: bottom left;
                    white-space: nowrap;
                    font-size: 11px;
                    font-weight: 600;
                    color: #e6edf3;
                    letter-spacing: 0.3px;
                    padding-left: 6px;
                    transition: color 0.15s ease;
                    pointer-events: auto;
                    cursor: default;
                }
                .pass-header-col:hover .pass-name {
                    color: #58a6ff;
                    text-shadow: 0 0 8px rgba(88, 166, 255, 0.6);
                }
                .pass-name.is-culled {
                    color: var(--text-muted);
                    text-decoration: line-through;
                }

                .pass-badge {
                    position: absolute;
                    bottom: 0;
                    left: 0;
                    right: 0;
                    height: 24px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-size: 9.5px;
                    font-weight: 700;
                    border-top: 1px solid #2a2e38;
                }
                .pass-badge.badge-raster { background: rgba(56, 139, 253, 0.2); color: #58a6ff; border-bottom: 2px solid var(--pass-raster); }
                .pass-badge.badge-compute { background: rgba(163, 113, 247, 0.2); color: #bc8cff; border-bottom: 2px solid var(--pass-compute); }
                .pass-badge.badge-async { background: rgba(227, 179, 65, 0.25); color: #f1e05a; border-bottom: 2px solid var(--pass-async); }
                .pass-badge.badge-unsafe { background: rgba(139, 148, 158, 0.15); color: #c9d1d9; border-bottom: 2px solid var(--pass-unsafe); }
                .pass-badge.badge-culled { background: #1b1e24; color: var(--text-muted); border-bottom: 2px solid #333; }

                /* Body Pane (Scrollable Vertically & Horizontally) */
                .body-viewport {
                    flex: 1;
                    overflow: auto;
                    position: relative;
                }
                table.body-table {
                    border-collapse: separate;
                    border-spacing: 0;
                    table-layout: fixed;
                    width: max-content;
                }

                /* Resource Rows */
                tbody tr:hover { background: #222733; }
                tbody tr:hover td.res-cell { background: #222733; }
                td.res-cell {
                    position: sticky;
                    left: 0;
                    z-index: 5;
                    width: var(--res-w);
                    min-width: var(--res-w);
                    max-width: var(--res-w);
                    background: var(--bg-panel);
                    border-bottom: 1px solid #232730;
                    border-right: 2px solid var(--border-color);
                    padding: 3px 10px;
                    height: var(--row-h);
                    display: flex;
                    align-items: center;
                    gap: 6px;
                    white-space: nowrap;
                    overflow: hidden;
                    box-shadow: 2px 0 6px rgba(0,0,0,0.25);
                }
                .type-tag {
                    font-size: 9px;
                    font-weight: 800;
                    padding: 1px 4px;
                    border-radius: 3px;
                    letter-spacing: 0.5px;
                }
                .tag-tex { background: #1f6feb; color: #fff; }
                .tag-buf { background: #d97706; color: #fff; }
                .tag-imp { background: #238636; color: #fff; font-size: 8px; padding: 1px 3px; }
                .tag-ext { background: #8957e5; color: #fff; font-size: 8px; padding: 1px 3px; }
                .tag-alias { background: #9e6a03; color: #fff; font-size: 8px; padding: 1px 3px; }

                .res-name {
                    font-weight: 500;
                    color: var(--text-primary);
                    overflow: hidden;
                    text-overflow: ellipsis;
                    flex: 1;
                    font-size: 12px;
                }
                .res-size {
                    font-size: 10.5px;
                    color: var(--text-secondary);
                    font-family: 'Consolas', monospace;
                    margin-left: auto;
                }

                /* Grid Cells */
                td.grid-cell {
                    width: var(--col-w);
                    min-width: var(--col-w);
                    max-width: var(--col-w);
                    height: var(--row-h);
                    padding: 0;
                    border-bottom: 1px solid #232730;
                    border-right: 1px solid #232730;
                    text-align: center;
                    vertical-align: middle;
                }
                .cell-block {
                    width: 100%;
                    height: 18px;
                    transition: transform 0.1s ease;
                }
                .cell-block:hover {
                    transform: scale(1.18);
                    z-index: 2;
                    position: relative;
                    box-shadow: 0 0 6px rgba(255,255,255,0.5);
                }

                .cell-empty { background: transparent; }
                .cell-retained { background: var(--color-retained); opacity: 0.85; }
                .cell-write { background: var(--color-write); border-radius: 2px; }
                .cell-read { background: var(--color-read); border-radius: 2px; }
                .cell-readwrite {
                    background: linear-gradient(135deg, var(--color-write) 50%, var(--color-read) 50%);
                    border-radius: 2px;
                }

                /* Custom Tooltip */
                #tooltip {
                    position: fixed;
                    display: none;
                    background: #1c2128;
                    border: 1px solid #444c56;
                    border-radius: 5px;
                    padding: 8px 12px;
                    color: var(--text-primary);
                    font-size: 11.5px;
                    line-height: 1.4;
                    box-shadow: 0 6px 16px rgba(0,0,0,0.6);
                    pointer-events: none;
                    z-index: 1000;
                }
                #tooltip .tt-title { font-weight: bold; color: #58a6ff; margin-bottom: 4px; font-size: 12px; }
                #tooltip .tt-row { display: flex; justify-content: space-between; gap: 14px; color: #adbac7; }
                #tooltip .tt-val { font-weight: 600; color: #cdd9e5; }
            </style>
        </head>
        <body>
            <header>
                <div class="title-group">
                    <h1>GhostEngine Render Graph Inspector</h1>
                    <span style="font-size:11.5px; color:var(--text-secondary);">Hash: <code>{{GraphHash:X16}}</code></span>
                </div>
                <div class="stats-group">
                    <div class="stat-pill">
                        <span class="label">Total Heap Committed</span>
                        <span class="val">{{TotalHeapSize / (1024.0 * 1024.0):F2}} MB</span>
                    </div>
                    <div class="stat-pill">
                        <span class="label">Without Aliasing</span>
                        <span class="val">{{SizeWithoutAliasing / (1024.0 * 1024.0):F2}} MB</span>
                    </div>
                    <div class="stat-pill">
                        <span class="label">VRAM Saved</span>
                        <span class="val saved">{{(1.0 - (double)TotalHeapSize / (SizeWithoutAliasing > 0 ? SizeWithoutAliasing : 1)) * 100:F1}}%</span>
                    </div>
                    <div class="stat-pill">
                        <span class="label">Passes (Total / Culled)</span>
                        <span class="val">{{Passes.Count}} / {{Passes.Count(p => p.IsCulled)}}</span>
                    </div>
                    <div class="stat-pill">
                        <span class="label">Cache Status</span>
                        <span class="val" style="color:{{(IsCacheHit ? "#3fb950" : "#d29922")}};">{{(IsCacheHit ? "CACHE HIT" : "COMPILED")}}</span>
                    </div>
                </div>
            </header>

            <div class="legend-bar">
                <div class="legend-section">
                    <span class="legend-title">Access:</span>
                    <div class="legend-item"><div class="legend-box box-write"></div><span>Write</span></div>
                    <div class="legend-item"><div class="legend-box box-read"></div><span>Read</span></div>
                    <div class="legend-item"><div class="legend-box box-readwrite"></div><span>Read & Write</span></div>
                    <div class="legend-item"><div class="legend-box box-retained"></div><span>Retained in Heap</span></div>
                </div>
                <div class="legend-section">
                    <span class="legend-title">Pass Types:</span>
                    <div class="legend-item"><div class="legend-dot dot-raster"></div><span>Raster / Native</span></div>
                    <div class="legend-item"><div class="legend-dot dot-compute"></div><span>Direct Compute</span></div>
                    <div class="legend-item"><div class="legend-dot dot-async"></div><span>Async Compute</span></div>
                    <div class="legend-item"><div class="legend-dot dot-unsafe"></div><span>Unsafe</span></div>
                    <div class="legend-item"><div class="legend-dot dot-culled"></div><span>Culled</span></div>
                </div>
                <div class="legend-section">
                    <span class="legend-title">Resources:</span>
                    <span class="type-tag tag-tex">TEX</span><span>Texture</span>
                    <span class="type-tag tag-buf">BUF</span><span>Buffer</span>
                    <span class="type-tag tag-imp">IMP</span><span>Imported</span>
                    <span class="type-tag tag-ext">EXT</span><span>Extracted</span>
                    <span class="type-tag tag-alias">ALIAS</span><span>Aliased</span>
                </div>
            </div>

            <div class="controls-bar">
                <input type="text" id="searchInput" class="search-box" placeholder="Filter resources by name...">
                <label class="control-item">
                    <input type="checkbox" id="hideCulledCheck" checked>
                    <span>Hide Culled Passes</span>
                </label>
                <label class="control-item">
                    <input type="checkbox" id="onlyAliasedCheck">
                    <span>Only Aliased Resources</span>
                </label>
                <label class="control-item">
                    <span>Filter:</span>
                    <select id="typeFilter" class="search-box" style="width:110px;">
                        <option value="all">All Types</option>
                        <option value="tex">Textures Only</option>
                        <option value="buf">Buffers Only</option>
                    </select>
                </label>
                <label class="control-item">
                    <span>Pass Order:</span>
                    <select id="orderSelect" class="search-box" style="width:130px;">
                        <option value="declaration">Declaration Order</option>
                        <option value="schedule">Schedule Order</option>
                    </select>
                </label>
                <div class="control-item" style="margin-left: auto;">
                    <span>Zoom:</span>
                    <button id="zoomOutBtn" class="btn-zoom">−</button>
                    <span id="zoomLevel" style="min-width:40px; text-align:center; font-weight:bold;">100%</span>
                    <button id="zoomInBtn" class="btn-zoom">＋</button>
                    <button id="zoomResetBtn" class="btn-zoom" style="font-size:11px;">Reset</button>
                </div>
            </div>

            <div class="inspector-container" id="inspectorContainer">
                <!-- Top Header Pane: Fixed, No Vertical Scroll -->
                <div class="header-pane">
                    <div class="corner-header">
                        <div class="corner-title" id="cornerTitle">Resources</div>
                        <div class="corner-sub" id="cornerSub">Loading...</div>
                    </div>
                    <div class="pass-headers-viewport" id="headerViewport">
                        <div class="pass-headers-track" id="headerTrack">
                            <!-- Populated with .pass-header-col via JS -->
                        </div>
                    </div>
                </div>

                <!-- Main Body Pane: Vertical & Horizontal Scroll -->
                <div class="body-viewport" id="bodyViewport">
                    <table class="body-table" id="bodyTable">
                        <!-- Populated with resource rows via JS -->
                    </table>
                </div>
            </div>

            <div id="tooltip"></div>

            <script>
                const rawPasses = {{passesJson}};
                const rawResources = {{resourcesJson}};
                const rawMemory = {{memoryJson}};

                // Horizontal scroll synchronization: body drives header
                const bodyViewport = document.getElementById('bodyViewport');
                const headerViewport = document.getElementById('headerViewport');
                bodyViewport.addEventListener('scroll', () => {
                    headerViewport.scrollLeft = bodyViewport.scrollLeft;
                }, { passive: true });

                let currentZoom = 1.0;
                function setZoom(val) {
                    currentZoom = Math.max(0.6, Math.min(2.0, Math.round(val * 10) / 10));
                    document.getElementById('inspectorContainer').style.zoom = currentZoom;
                    document.getElementById('zoomLevel').innerText = Math.round(currentZoom * 100) + '%';
                }
                document.getElementById('zoomInBtn').onclick = () => setZoom(currentZoom + 0.1);
                document.getElementById('zoomOutBtn').onclick = () => setZoom(currentZoom - 0.1);
                document.getElementById('zoomResetBtn').onclick = () => setZoom(1.0);

                function formatBytes(bytes) {
                    if (!bytes || bytes === 0) return '0 B';
                    if (bytes >= 1024 * 1024 * 1024) return (bytes / (1024 * 1024 * 1024)).toFixed(2) + ' GB';
                    if (bytes >= 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
                    if (bytes >= 1024) return (bytes / 1024).toFixed(0) + ' KB';
                    return bytes + ' B';
                }

                function getPassBadgeInfo(pass) {
                    if (pass.IsCulled) return { label: 'CUL', css: 'badge-culled', desc: 'Culled' };
                    if (pass.AsyncCompute) return { label: 'ASYNC', css: 'badge-async', desc: 'Async Compute' };
                    if (pass.Type === 1) return { label: 'COMP', css: 'badge-compute', desc: 'Direct Compute' };
                    if (pass.Type === 0) {
                        const isNative = pass.NativePassIndex !== undefined && pass.NativePassIndex >= 0;
                        return { label: isNative ? ('N' + pass.NativePassIndex) : 'RAST', css: 'badge-raster', desc: isNative ? ('Native RenderPass #' + pass.NativePassIndex) : 'Raster Pass' };
                    }
                    return { label: 'UNSAFE', css: 'badge-unsafe', desc: 'Unsafe Pass' };
                }

                function renderTable() {
                    const hideCulled = document.getElementById('hideCulledCheck').checked;
                    const onlyAliased = document.getElementById('onlyAliasedCheck').checked;
                    const typeFilter = document.getElementById('typeFilter').value;
                    const passOrder = document.getElementById('orderSelect').value;
                    const searchText = document.getElementById('searchInput').value.trim().toLowerCase();

                    // Filter and sort passes
                    let displayPasses = [...rawPasses];
                    if (hideCulled) {
                        displayPasses = displayPasses.filter(p => !p.IsCulled);
                    }
                    if (passOrder === 'schedule') {
                        displayPasses.sort((a, b) => {
                            if (a.IsCulled !== b.IsCulled) return a.IsCulled ? 1 : -1;
                            return a.Index - b.Index;
                        });
                    } else {
                        displayPasses.sort((a, b) => a.Index - b.Index);
                    }

                    // Filter resources
                    let displayResources = rawResources.filter(r => {
                        if (onlyAliased && (!r.AliasedWithResources || r.AliasedWithResources.length === 0)) return false;
                        if (typeFilter === 'tex' && r.Type !== 0) return false;
                        if (typeFilter === 'buf' && r.Type !== 1) return false;
                        if (searchText && !r.Name.toLowerCase().includes(searchText)) return false;
                        return true;
                    });

                    // Update Corner Info
                    document.getElementById('cornerTitle').innerText = `Resources (${displayResources.length})`;
                    document.getElementById('cornerSub').innerText = `Showing ${displayPasses.length} passes`;

                    // Render Pass Headers
                    let headerHtml = '';
                    displayPasses.forEach((pass, pIdx) => {
                        const badge = getPassBadgeInfo(pass);
                        const culledClass = pass.IsCulled ? 'is-culled' : '';
                        headerHtml += `
                        <div class="pass-header-col" data-pass-idx="${pass.Index}">
                            <div class="pass-name-anchor">
                                <span class="pass-name ${culledClass}" title="P${pass.Index}: ${pass.Name} (${badge.desc})">P${pass.Index} ${pass.Name}</span>
                            </div>
                            <div class="pass-badge ${badge.css}" title="${badge.desc}">${badge.label}</div>
                        </div>`;
                    });
                    document.getElementById('headerTrack').innerHTML = headerHtml;

                    // Render Body Rows
                    let bodyHtml = '<tbody>';
                    displayResources.forEach(res => {
                        const isTex = res.Type === 0;
                        const isAliased = res.AliasedWithResources && res.AliasedWithResources.length > 0;

                        bodyHtml += '<tr>';
                        bodyHtml += `<td class="res-cell" title="${res.Name}&#10;Logical ID: ${res.LogicalResourceId}&#10;Offset: 0x${(res.HeapOffset || 0).toString(16)}&#10;Size: ${formatBytes(res.SizeInBytes)}">
                            <span class="type-tag ${isTex ? 'tag-tex' : 'tag-buf'}">${isTex ? 'TEX' : 'BUF'}</span>
                            ${res.IsImported ? '<span class="type-tag tag-imp">IMP</span>' : ''}
                            ${res.IsExtracted ? '<span class="type-tag tag-ext">EXT</span>' : ''}
                            ${isAliased ? '<span class="type-tag tag-alias">ALIAS</span>' : ''}
                            <span class="res-name">${res.Name}</span>
                            <span class="res-size">${formatBytes(res.SizeInBytes)}</span>
                        </td>`;

                        displayPasses.forEach(pass => {
                            const isWrite = (pass.ResourceWrites && pass.ResourceWrites.includes(res.LogicalResourceId))
                                         || (pass.ResourceCreates && pass.ResourceCreates.includes(res.LogicalResourceId));
                            const isRead = pass.ResourceReads && pass.ResourceReads.includes(res.LogicalResourceId);

                            // Lifetime calculation (declaration order or schedule order)
                            let isAlive = false;
                            if (passOrder === 'schedule' && res.ScheduledFirstUseIndex >= 0) {
                                isAlive = pass.Index >= res.ScheduledFirstUseIndex && pass.Index <= res.ScheduledLastUseIndex;
                            } else if (res.FirstUsePass >= 0) {
                                isAlive = pass.Index >= res.FirstUsePass && pass.Index <= res.LastUsePass;
                            }

                            let blockClass = 'cell-empty';
                            let actionText = 'No Access';

                            if (isWrite && isRead) {
                                blockClass = 'cell-readwrite';
                                actionText = 'Read & Write';
                            } else if (isWrite) {
                                blockClass = 'cell-write';
                                actionText = 'Write';
                            } else if (isRead) {
                                blockClass = 'cell-read';
                                actionText = 'Read';
                            } else if (isAlive && !pass.IsCulled) {
                                blockClass = 'cell-retained';
                                actionText = 'Retained in Memory (Alive)';
                            }

                            bodyHtml += `<td class="grid-cell" 
                                data-pass-name="P${pass.Index}: ${pass.Name}" 
                                data-res-name="${res.Name}" 
                                data-res-type="${isTex ? 'Texture' : 'Buffer'}"
                                data-res-size="${formatBytes(res.SizeInBytes)}"
                                data-action="${actionText}">
                                <div class="cell-block ${blockClass}"></div>
                            </td>`;
                        });

                        bodyHtml += '</tr>';
                    });

                    bodyHtml += '</tbody>';
                    document.getElementById('bodyTable').innerHTML = bodyHtml;
                    attachTooltips();
                }

                function attachTooltips() {
                    const tooltip = document.getElementById('tooltip');
                    const cells = document.querySelectorAll('td.grid-cell');

                    cells.forEach(cell => {
                        cell.addEventListener('mouseenter', e => {
                            const action = cell.getAttribute('data-action');
                            if (action === 'No Access') return;

                            const pName = cell.getAttribute('data-pass-name');
                            const rName = cell.getAttribute('data-res-name');
                            const rType = cell.getAttribute('data-res-type');
                            const rSize = cell.getAttribute('data-res-size');

                            tooltip.innerHTML = `
                                <div class="tt-title">${rName}</div>
                                <div class="tt-row"><span>Type:</span><span class="tt-val">${rType} (${rSize})</span></div>
                                <div class="tt-row"><span>Pass:</span><span class="tt-val">${pName}</span></div>
                                <div class="tt-row"><span>Access:</span><span class="tt-val" style="color:${action.includes('Write') ? '#f85149' : (action.includes('Read') ? '#7ee787' : '#8b949e')}">${action}</span></div>
                            `;
                            tooltip.style.display = 'block';
                            tooltip.style.left = (e.clientX + 14) + 'px';
                            tooltip.style.top = (e.clientY + 14) + 'px';
                        });

                        cell.addEventListener('mousemove', e => {
                            tooltip.style.left = (e.clientX + 14) + 'px';
                            tooltip.style.top = (e.clientY + 14) + 'px';
                        });

                        cell.addEventListener('mouseleave', () => {
                            tooltip.style.display = 'none';
                        });
                    });
                }

                document.getElementById('searchInput').addEventListener('input', renderTable);
                document.getElementById('hideCulledCheck').addEventListener('change', renderTable);
                document.getElementById('onlyAliasedCheck').addEventListener('change', renderTable);
                document.getElementById('typeFilter').addEventListener('change', renderTable);
                document.getElementById('orderSelect').addEventListener('change', renderTable);

                renderTable();
            </script>
        </body>
        </html>
        """;

        return html;
    }
}

[JsonSerializable(typeof(List<PassDumpInfo>))]
[JsonSerializable(typeof(List<ResourceDumpInfo>))]
[JsonSerializable(typeof(List<HeapBlockDumpInfo>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class RGDumpJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Describes why a render-graph pass was assigned to its effective command-buffer type.
/// </summary>
public enum RGQueueDecision : byte
{
    /// <summary>The pass does not request asynchronous Compute execution.</summary>
    AsyncNotRequested,

    /// <summary>The pass type cannot execute on an asynchronous Compute command buffer.</summary>
    IneligiblePassType,

    /// <summary>The pass was culled and has no effective command-buffer assignment.</summary>
    Culled,

    /// <summary>The dependency-window planner selected the pass for a Compute command buffer.</summary>
    AsyncComputeSelected,

    /// <summary>The async request was demoted because no legal overlap window was found.</summary>
    NoLegalOverlapWindow,
}

/// <summary>
/// Diagnostic description of one structural command-buffer boundary adjacent to a pass.
/// </summary>
public readonly struct PassSyncBoundaryDumpInfo
{
    /// <summary>Type of the command buffer that ends at the boundary.</summary>
    public CommandQueueType SourceType
    {
        get; init;
    }

    /// <summary>Type of the command buffer that begins at the boundary.</summary>
    public CommandQueueType DestinationType
    {
        get; init;
    }

    /// <summary>Relative producer command-buffer IDs required by the destination.</summary>
    public int[] ProducerCommandBufferIds
    {
        get; init;
    }

    /// <summary>Command-buffer types corresponding to <see cref="ProducerCommandBufferIds"/>.</summary>
    public CommandQueueType[] ProducerTypes
    {
        get; init;
    }
}

public readonly struct HeapBlockDumpInfo
{
    public ulong Offset
    {
        get; init;
    }

    public ulong Size
    {
        get; init;
    }

    public bool IsFree
    {
        get; init;
    }

    // Resources occupying this memory block
    public List<int> AliasedLogicalResources
    {
        get; init;
    }
}

public readonly struct PassDumpInfo
{
    public int Index
    {
        get; init;
    }

    public string Name
    {
        get; init;
    }

    public RenderPassType Type
    {
        get; init;
    }

    public bool IsCulled
    {
        get; init;
    }

    public bool AsyncCompute
    {
        get; init;
    }

    public bool AsyncRequested
    {
        get; init;
    }

    public bool AllowAsyncComputeOverlap
    {
        get; init;
    }

    public CommandQueueType? EffectiveQueue
    {
        get; init;
    }

    /// <summary>Explains the effective command-buffer assignment.</summary>
    public RGQueueDecision QueueDecision
    {
        get; init;
    }

    /// <summary>Structural command-buffer boundary immediately before this pass, when present.</summary>
    public PassSyncBoundaryDumpInfo? SyncBoundaryBefore
    {
        get; init;
    }

    /// <summary>Structural command-buffer boundary immediately after this pass, when present.</summary>
    public PassSyncBoundaryDumpInfo? SyncBoundaryAfter
    {
        get; init;
    }

    public int NativePassIndex
    {
        get; init;
    }

    public List<int> ResourceReads
    {
        get; init;
    }

    public List<int> ResourceWrites
    {
        get; init;
    }

    public List<int> ResourceCreates
    {
        get; init;
    }
}

public readonly struct ResourceDumpInfo
{
    public int LogicalResourceId
    {
        get; init;
    }

    public Handle<GPUResource> BackingResource
    {
        get; init;
    }

    public string Name
    {
        get; init;
    }

    public RGResourceType Type
    {
        get; init;
    }

    public ulong SizeInBytes
    {
        get; init;
    }

    public bool IsImported
    {
        get; init;
    }

    public bool IsExtracted
    {
        get; init;
    }

    public ulong HeapOffset
    {
        get; init;
    }

    public int FirstUsePass
    {
        get; init;
    }

    public int LastUsePass
    {
        get; init;
    }

    public int ScheduledFirstUseIndex
    {
        get; init;
    }

    public int ScheduledLastUseIndex
    {
        get; init;
    }

    public int[] ProducerPass
    {
        get; init;
    }

    public int[] ConsumerPasses
    {
        get; init;
    }

    // Other resources sharing the same heap offset range
    public List<int> AliasedWithResources
    {
        get; init;
    }
}

/// <summary>
/// Decoded payload of a <see cref="RGExecutionOpType.CommandBufferSyncPoint"/> command-stream entry.
/// </summary>
/// <remarks>
/// <para>The span points directly into the originating command bytes and is only valid for the lifetime of that buffer.</para>
/// <para>
/// Relative command-buffer IDs are assigned in stream order: the first command buffer has ID 0 and each
/// <see cref="RGExecutionOpType.CommandBufferSyncPoint"/> ends command buffer N and starts command buffer N + 1.
/// Every element of <see cref="ProducerCommandBufferIds"/> must therefore be strictly less than N + 1.
/// </para>
/// </remarks>
internal readonly ref struct RGSyncMarker
{
    /// <summary>Queue type of the command buffer that begins after this boundary.</summary>
    public readonly CommandQueueType NextCommandBufferType;

    /// <summary>
    /// Relative IDs of command buffers that must complete before the next command buffer may execute.
    /// Each ID is strictly less than the implicit ID assigned to the next command buffer.
    /// </summary>
    public readonly ReadOnlySpan<int> ProducerCommandBufferIds;

    public RGSyncMarker(CommandQueueType nextCommandBufferType, ReadOnlySpan<int> producerCommandBufferIds)
    {
        NextCommandBufferType = nextCommandBufferType;
        ProducerCommandBufferIds = producerCommandBufferIds;
    }
}

/// <summary>
/// Helpers for writing and reading <see cref="RGExecutionOpType.CommandBufferSyncPoint"/> entries in the
/// binary command stream used by <see cref="RenderGraphCompiler"/> and <see cref="RenderGraphExecutor"/>.
/// </summary>
internal static class RGCommandStream
{
    /// <summary>
    /// Writes a <see cref="RGExecutionOpType.CommandBufferSyncPoint"/> entry into <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Destination buffer writer.</param>
    /// <param name="nextCommandBufferType">Queue type for the command buffer that starts after this boundary.</param>
    /// <param name="producerIds">Relative IDs of producer command buffers that the next command buffer depends on.</param>
    /// <param name="nextCommandBufferId">
    /// The implicit relative ID that will be assigned to the command buffer that starts after this marker.
    /// Used to validate that every element of <paramref name="producerIds"/> is a strictly earlier command buffer.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when any producer ID is greater than or equal to <paramref name="nextCommandBufferId"/>, or when
    /// producer IDs contain duplicates.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSyncMarker(
        ref BufferWriter writer,
        CommandQueueType nextCommandBufferType,
        ReadOnlySpan<int> producerIds,
        int nextCommandBufferId)
    {
        ValidateProducerIds(producerIds, nextCommandBufferId);

        writer.Write(RGExecutionOpType.CommandBufferSyncPoint);
        writer.Write(nextCommandBufferType);
        writer.Write(producerIds.Length);
        writer.WriteSpan(producerIds);
    }

    /// <summary>
    /// Reads a <see cref="RGExecutionOpType.CommandBufferSyncPoint"/> payload from <paramref name="reader"/>.
    /// The opcode byte itself must already have been consumed by the caller.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RGSyncMarker ReadSyncMarker(ref SpanReader reader)
    {
        var nextType = reader.Read<CommandQueueType>();
        var count = reader.Read<int>();
        var ids = reader.ReadSpan<int>(count);
        return new RGSyncMarker(nextType, ids);
    }

    /// <summary>
    /// Validates that producer IDs form a legal dependency set for a sync marker whose next command buffer will
    /// receive the relative ID <paramref name="nextCommandBufferId"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any ID is out of range or when the set contains duplicates.
    /// </exception>
    public static void ValidateProducerIds(ReadOnlySpan<int> producerIds, int nextCommandBufferId)
    {
        for (var i = 0; i < producerIds.Length; i++)
        {
            var id = producerIds[i];
            if (id < 0 || id >= nextCommandBufferId)
            {
                throw new ArgumentException(
                    $"Producer command-buffer ID {id} is out of range. " +
                    $"All IDs must be in [0, {nextCommandBufferId - 1}].");
            }

            for (var j = i + 1; j < producerIds.Length; j++)
            {
                if (producerIds[j] == id)
                {
                    throw new ArgumentException(
                        $"Duplicate producer command-buffer ID {id} in sync marker.");
                }
            }
        }
    }
}
