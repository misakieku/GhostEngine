using Ghost.Core;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Misaki.HighPerformance.Jobs;

namespace Ghost.Engine.RenderPipeline;

public enum RenderPipelineDebugMode : uint
{
    None = 0,
    Meshlet = 1 << 0,
    TileLightHeatmap = 1 << 1,
}

public enum DirectLightingMode : uint
{
    Standard,
    Stochastic,
}

public class GhostRenderPipelineSettings : IRenderPipelineSettings
{
    public RenderPipelineDebugMode DebugMode { get; set; } = RenderPipelineDebugMode.None;

    /// <summary>
    /// Maximum number of visible meshlets on screen. Default is 2,097,152 (2^21). Maximum is 16,777,216 (2^24).
    /// </summary>
    /// <remarks>
    /// This value controls the maximum number of meshlets that can be visible on screen at any given time. Adjust this value based on performance and visual quality requirements.
    /// If the number of visible meshlets exceeds this limit, some meshlets may be culled or not rendered and cause visual artifacts. It is recommended to set this value based on the expected scene complexity and the capabilities of the target hardware.
    /// </remarks>
    public uint MaxVisibleMeshletsOnScreen { get; init; } = 2_097_152;

    /// <summary>
    /// Resolution of the shadow atlas for punctual lights (point and spot lights). Default is 4096.
    /// </summary>
    public uint ShadowAtlasResolution { get; init; } = 4096;

    /// <summary>
    /// Size of the GPU Page Pool in Megabytes. Default is 0 (auto-calculate based on MaxVisibleMeshletsOnScreen).
    /// </summary>
    public uint MeshletStreamingPoolSizeMB { get; init; } = 0;

    /// <summary>
    /// Maximum number of page streaming requests emitted by the GPU per frame. Default is 16,384.
    /// </summary>
    public uint MaxStreamingRequestsPerFrame { get; init; } = 16_384;

    /// <summary>
    /// Maximum number of 128KB meshlet pages uploaded per frame. Default is 32 (4 MB/frame).
    /// </summary>
    public int MaxStreamingPagesPerFrame { get; init; } = 32;

    /// <summary>
    /// Enables or disables ray tracing support in the render pipeline. Default is false.
    /// </summary>
    public bool EnableRayTracing { get; init; } = false;

    /// <summary>
    /// Target screen-space pixel error threshold for meshlet DAG LOD refinement. Default is 2.0f.
    /// </summary>
    /// <remarks>
    /// This value controls the level of detail for meshlet rendering. A lower value results in higher detail (more meshlets), while a higher value results in lower detail (fewer meshlets). Adjust this value based on performance and visual quality requirements.
    /// </remarks>
    public float MeshletLodErrorThreshold { get; set; } = 2.0f;

    /// <summary>
    /// The screen percentage threshold for instance culling. Default is 2.0f.
    /// </summary>
    /// <remarks>
    /// This value controls the threshold for culling instance based on their size in screen-percentage. A lower value results in more aggressive culling (fewer instance rendered), while a higher value results in less aggressive culling (more instance rendered). Adjust this value based on performance and visual quality requirements.
    /// </remarks>
    public float InstanceCullingThreshold { get; set; } = 2.0f;

    /// <summary>
    /// The direct lighting mode to use for rendering. Default is FPTL.
    /// </summary>
    public DirectLightingMode DirectLightingMode
    {
        get;
        set
        {
            if (!EnableRayTracing && value == DirectLightingMode.Stochastic)
            {
                Logger.Warning("DirectLightingMode is set to Stochastic, but ray tracing is disabled. Skipping.");
                return;
            }

            field = value;
        }
    } = DirectLightingMode.Standard;

    /// <summary>
    /// Calculates the GPU Page Pool size in Megabytes based on MaxVisibleMeshletsOnScreen.
    /// </summary>
    public uint GetCalculatedPagePoolSizeMB()
    {
        if (MeshletStreamingPoolSizeMB > 0)
        {
            return MeshletStreamingPoolSizeMB;
        }

        // Formula: clamp(MaxVisibleMeshletsOnScreen / 36 * 128KB * 1.75, 256MB, 2048MB)
        // Average meshlets per 128KB page is ~36. Sized with 1.75x working headroom.
        var estimatedPages = MaxVisibleMeshletsOnScreen / 36.0;
        var bytes = estimatedPages * 131072.0 * 1.75;
        var mb = (uint)Math.Clamp(Math.Ceiling(bytes / (1024.0 * 1024.0)), 256.0, 2048.0);
        return mb;
    }

    /// <summary>
    /// Total number of 128KB physical page slots available in the GPU Page Pool.
    /// </summary>
    public uint GetPagePoolSlotCount()
    {
        var mb = GetCalculatedPagePoolSizeMB();
        return (mb * 1024u * 1024u) / 131072u;
    }

    public IRenderPipeline CreatePipeline(RenderEngine renderEngine, AssetManager assetManager, JobScheduler jobScheduler)
    {
        return new GhostRenderPipeline(renderEngine, assetManager, jobScheduler, this);
    }
}
