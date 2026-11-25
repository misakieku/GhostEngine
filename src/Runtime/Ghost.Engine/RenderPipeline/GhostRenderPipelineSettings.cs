using Ghost.Core;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Misaki.HighPerformance.Jobs;
using System.Numerics;

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
    /// Maximum number of visible meshlets on screen. Default is 2,097,152 (2^21). Maximum is 16,777,216 (2^24). Acceptable range is 1,024 to 8,388,608.
    /// </summary>
    /// <remarks>
    /// This value controls the maximum number of meshlets that can be visible on screen at any given time. Adjust this value based on performance and visual quality requirements.
    /// If the number of visible meshlets exceeds this limit, some meshlets may be culled or not rendered and cause visual artifacts. It is recommended to set this value based on the expected scene complexity and the capabilities of the target hardware.
    /// </remarks>
    public uint MaxVisibleMeshletsOnScreen
    {
        get;
        init => field = Math.Clamp(value, 1024u, 8_388_608u);
    } = 2_097_152;

    /// <summary>
    /// Resolution of the shadow atlas for punctual lights (point and spot lights). Default is 4096. Acceptable range is 512 to 8192.
    /// </summary>
    public uint ShadowAtlasResolution
    {
        get;
        init => field = Math.Clamp(BitOperations.RoundUpToPowerOf2(value), 512u, 8192u); // Clamp between 512 and 8192
    } = 4096;

    /// <summary>
    /// Size of the GPU Page Pool in Megabytes. Default is 512 MB. Acceptable range is 128 MB to 2048 MB.
    /// </summary>
    public uint MeshletStreamingPoolSizeMB
    {
        get;
        init => field = Math.Clamp(value, 128u, 2048u);
    } = 512;

    /// <summary>
    /// Maximum number of page streaming requests emitted by the GPU per frame. Default is 16,384. Acceptable range is 1,024 to 65,536.
    /// </summary>
    public uint MaxStreamingRequestsPerFrame
    {
        get;
        init => field = Math.Clamp(value, 1024u, 65_536u);
    } = 16_384;

    /// <summary>
    /// Maximum number of 128KB meshlet pages uploaded per frame. Default is 32 (4 MB/frame). Acceptable range is 8 to 128 (1 MB/frame to 16 MB/frame).
    /// </summary>
    public int MaxStreamingPagesPerFrame
    {
        get;
        init => field = Math.Clamp(value, 8, 128);
    } = 32;

    /// <summary>
    /// Enables or disables ray tracing support in the render pipeline. Default is false.
    /// </summary>
    public bool EnableRayTracing { get; init; } = false;

    /// <summary>
    /// Target screen-space pixel error threshold for meshlet DAG LOD refinement. Default is 2.0f. Acceptable range is 0.5f to 10.0f.
    /// </summary>
    /// <remarks>
    /// This value controls the level of detail for meshlet rendering. A lower value results in higher detail (more meshlets), while a higher value results in lower detail (fewer meshlets). Adjust this value based on performance and visual quality requirements.
    /// </remarks>
    public float MeshletLodErrorThreshold
    {
        get;
        set => field = Math.Clamp(value, 0.5f, 10.0f);
    } = 2.0f;

    /// <summary>
    /// The screen percentage threshold for instance culling. Default is 2.0f. Acceptable range is 0.1f to 10.0f.
    /// </summary>
    /// <remarks>
    /// This value controls the threshold for culling instance based on their size in screen-percentage. A lower value results in more aggressive culling (fewer instance rendered), while a higher value results in less aggressive culling (more instance rendered). Adjust this value based on performance and visual quality requirements.
    /// </remarks>
    public float InstanceCullingThreshold
    {
        get;
        set => field = Math.Clamp(value, 0.1f, 10.0f);
    } = 2.0f;

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
    /// Total number of 128KB physical page slots available in the GPU Page Pool.
    /// </summary>
    public uint GetPagePoolSlotCount()
    {
        return MeshletStreamingPoolSizeMB * 1024u * 1024u / 131072u;
    }

    public IRenderPipeline CreatePipeline(RenderEngine renderEngine, AssetManager assetManager, JobScheduler jobScheduler)
    {
        return new GhostRenderPipeline(renderEngine, assetManager, jobScheduler, this);
    }
}
