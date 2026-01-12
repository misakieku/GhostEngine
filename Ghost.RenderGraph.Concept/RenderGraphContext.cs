using Ghost.Core;

namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Mock command buffer for recording GPU commands.
/// In a real implementation, this would wrap D3D12 command lists.
/// </summary>
internal sealed class MockCommandBuffer
{
    public void SetRenderTarget(Identifier<RGTexture> texture)
    {
#if DEBUG
        Console.WriteLine(nameof(SetRenderTarget) + ": " + texture);
#endif
    }

    public void SetDepthStencil(Identifier<RGTexture> texture)
    {
#if DEBUG
        Console.WriteLine(nameof(SetDepthStencil) + ": " + texture);
#endif
    }

    public void ClearRenderTarget(Identifier<RGTexture> texture, float r, float g, float b, float a)
    {
#if DEBUG
        Console.WriteLine(nameof(ClearRenderTarget) + ": " + texture);
#endif
    }

    public void ClearDepth(Identifier<RGTexture> texture, float depth)
    {
#if DEBUG
        Console.WriteLine(nameof(ClearDepth) + ": " + texture);
#endif
    }

    public void Draw(int vertexCount)
    {
#if DEBUG
        Console.WriteLine(nameof(Draw) + ": " + vertexCount);
#endif
    }

    public void BindShaderResource(Identifier<RGResource> resource, int slot)
    {
#if DEBUG
        Console.WriteLine(nameof(BindShaderResource) + ": " + resource + ", slot " + slot);
#endif
    }

    public void BindUnorderedAccess(Identifier<RGResource> resource, int slot)
    {
#if DEBUG
        Console.WriteLine(nameof(BindUnorderedAccess) + ": " + resource + ", slot " + slot);
#endif
    }

    public void Dispatch(int x, int y, int z)
    {
#if DEBUG
        Console.WriteLine(nameof(Dispatch) + ": " + x + ", " + y + ", " + z);
#endif
    }

    public void ResourceBarrier(Identifier<RGResource> resource, ResourceState stateBefore, ResourceState stateAfter)
    {
#if DEBUG
        Console.WriteLine(nameof(ResourceBarrier) + ": " + resource + " from " + stateBefore + " to " + stateAfter);
#endif
    }

    public void AliasBarrier(Identifier<RGResource> resourceBefore, Identifier<RGResource> resourceAfter)
    {
#if DEBUG
        Console.WriteLine(nameof(AliasBarrier) + ": " + resourceBefore + " to " + resourceAfter);
#endif
    }
}

/// <summary>
/// Context for raster rendering passes.
/// Directly exposes command buffer methods.
/// </summary>
public readonly struct RasterRenderContext
{
    private readonly MockCommandBuffer _cmd;

    internal RasterRenderContext(MockCommandBuffer cmd)
    {
        _cmd = cmd;
    }

    // Expose command buffer methods directly
    public void SetRenderTarget(Identifier<RGTexture> texture) => _cmd.SetRenderTarget(texture);
    public void SetDepthStencil(Identifier<RGTexture> texture) => _cmd.SetDepthStencil(texture);
    public void ClearRenderTarget(Identifier<RGTexture> texture, float r, float g, float b, float a) => _cmd.ClearRenderTarget(texture, r, g, b, a);
    public void ClearDepth(Identifier<RGTexture> texture, float depth) => _cmd.ClearDepth(texture, depth);
    public void Draw(int vertexCount) => _cmd.Draw(vertexCount);
    public void BindShaderResource(Identifier<RGResource> resource, int slot) => _cmd.BindShaderResource(resource, slot);
}

/// <summary>
/// Context for compute rendering passes.
/// Directly exposes command buffer methods.
/// </summary>
public readonly struct ComputeRenderContext
{
    private readonly MockCommandBuffer _cmd;

    internal ComputeRenderContext(MockCommandBuffer cmd)
    {
        _cmd = cmd;
    }

    // Expose command buffer methods directly
    public void BindShaderResource(Identifier<RGResource> resource, int slot) => _cmd.BindShaderResource(resource, slot);
    public void BindUnorderedAccess(Identifier<RGResource> resource, int slot) => _cmd.BindUnorderedAccess(resource, slot);
    public void Dispatch(int x, int y, int z) => _cmd.Dispatch(x, y, z);
}

/// <summary>
/// Unified render context containing both raster and compute contexts.
/// </summary>
internal readonly struct RenderContext
{
    public readonly RasterRenderContext RasterContext;
    public readonly ComputeRenderContext ComputeContext;

    public RenderContext(MockCommandBuffer cmd)
    {
        RasterContext = new RasterRenderContext(cmd);
        ComputeContext = new ComputeRenderContext(cmd);
    }
}
