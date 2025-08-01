using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Handle for render graph texture resource
/// </summary>
public readonly struct RGTextureHandle : IEquatable<RGTextureHandle>
{
    internal readonly RenderGraph? _renderGraph;
    internal readonly int _resourceId;

    internal RGTextureHandle(int resourceId, RenderGraph renderGraph)
    {
        _resourceId = resourceId;
        _renderGraph = renderGraph;
    }

    public bool IsValid => _resourceId >= 0 && _renderGraph != null;

    public bool Equals(RGTextureHandle other)
    {
        return _resourceId == other._resourceId && ReferenceEquals(_renderGraph, other._renderGraph);
    }

    public override bool Equals(object? obj)
    {
        return obj is RGTextureHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_resourceId, _renderGraph);
    }

    public static bool operator ==(RGTextureHandle left, RGTextureHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RGTextureHandle left, RGTextureHandle right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Handle for render graph buffer resource
/// </summary>
public readonly struct RGBufferHandle : IEquatable<RGBufferHandle>
{
    internal readonly RenderGraph? _renderGraph;
    internal readonly int _resourceId;

    internal RGBufferHandle(int resourceId, RenderGraph renderGraph)
    {
        _resourceId = resourceId;
        _renderGraph = renderGraph;
    }

    public bool IsValid => _resourceId >= 0 && _renderGraph != null;

    public bool Equals(RGBufferHandle other)
    {
        return _resourceId == other._resourceId && ReferenceEquals(_renderGraph, other._renderGraph);
    }

    public override bool Equals(object? obj)
    {
        return obj is RGBufferHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_resourceId, _renderGraph);
    }

    public static bool operator ==(RGBufferHandle left, RGBufferHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RGBufferHandle left, RGBufferHandle right)
    {
        return !(left == right);
    }
}

/// <summary>
/// Resource access information for dependency tracking
/// </summary>
internal readonly struct ResourceAccess
{
    public readonly int resourceId;
    public readonly int passIndex;
    public readonly ResourceAccessType accessType;

    public ResourceAccess(int resourceId, ResourceAccessType accessType, int passIndex)
    {
        this.resourceId = resourceId;
        this.accessType = accessType;
        this.passIndex = passIndex;
    }
}

/// <summary>
/// Represents a barrier to be executed for resource state transitions
/// </summary>
internal readonly struct RenderGraphBarrier
{
    public readonly int resourceId;
    public readonly ResourceStates stateBefore;
    public readonly ResourceStates stateAfter;

    public RenderGraphBarrier(int resourceId, ResourceStates stateBefore, ResourceStates stateAfter)
    {
        this.resourceId = resourceId;
        this.stateBefore = stateBefore;
        this.stateAfter = stateAfter;
    }
}
