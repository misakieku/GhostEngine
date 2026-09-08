using Ghost.Core;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.Services;

namespace Ghost.Engine.RenderPipeline;

/// <summary>
/// Manages the active render pipeline, payload ring buffer swapping, and asset initialization
/// for the render engine.
/// </summary>
public sealed class RenderPipelineManager : IDisposable
{
    private readonly RenderEngine _renderEngine;
    private readonly ResourceManager _resourceManager;
    private readonly AssetManager _assetManager;

    private IRenderPipelineSettings _settings;
    private IRenderPipeline _renderPipeline;
    private IRenderPayload[] _payloads;
    private bool _disposed;

    /// <summary>
    /// Gets the current active render pipeline.
    /// </summary>
    public IRenderPipeline RenderPipeline => _renderPipeline;

    /// <summary>
    /// Gets the current render pipeline settings.
    /// </summary>
    public IRenderPipelineSettings Settings => _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderPipelineManager"/> class.
    /// </summary>
    /// <param name="settings">The initial render pipeline settings.</param>
    /// <param name="renderEngine">The render engine instance.</param>
    /// <param name="resourceManager">The resource manager instance.</param>
    /// <param name="assetManager">The asset manager instance.</param>
    public RenderPipelineManager(
        IRenderPipelineSettings settings,
        RenderEngine renderEngine,
        ResourceManager resourceManager,
        AssetManager assetManager)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(renderEngine);
        ArgumentNullException.ThrowIfNull(resourceManager);
        ArgumentNullException.ThrowIfNull(assetManager);

        _settings = settings;
        _renderEngine = renderEngine;
        _resourceManager = resourceManager;
        _assetManager = assetManager;

        _renderPipeline = null!;
        _payloads = null!;

        RecreatePipeline(settings);
    }

    /// <summary>
    /// Recreates the render pipeline and its frame payloads using the specified settings.
    /// </summary>
    /// <param name="settings">The new render pipeline settings to apply.</param>
    public void RecreatePipeline(IRenderPipelineSettings settings)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;

        if (_renderPipeline != null)
        {
            _renderPipeline.Dispose();
        }

        if (_payloads != null)
        {
            for (var i = 0; i < _payloads.Length; i++)
            {
                _payloads[i]?.Dispose();
            }
        }

        var pipeline = settings.CreatePipeline(_renderEngine);

        if (pipeline is GhostRenderPipeline ghostPipeline)
        {
            ghostPipeline.Initialize(_assetManager);
        }

        var payloadCount = _renderEngine.MaxFrameLatency;
        var payloads = new IRenderPayload[payloadCount];
        for (var i = 0; i < payloadCount; i++)
        {
            payloads[i] = settings.CreatePayload(_renderEngine, pipeline);
        }

        _renderPipeline = pipeline;
        _payloads = payloads;

        _renderEngine.SetRenderPipeline(_renderPipeline, _payloads);
    }

    /// <summary>
    /// Gets the render payload corresponding to the given frame index.
    /// </summary>
    /// <param name="frameIndex">The current frame index.</param>
    /// <returns>The frame's render payload.</returns>
    public IRenderPayload GetCurrentPayload(int frameIndex)
    {
        ThrowIfDisposed();
        var index = frameIndex % _payloads.Length;
        return _payloads[index];
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Disposes the render pipeline manager, its active render pipeline, and all frame payloads.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_payloads != null)
        {
            for (var i = 0; i < _payloads.Length; i++)
            {
                _payloads[i]?.Dispose();
            }
        }

        _renderPipeline?.Dispose();
    }
}
