namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Mock command buffer for recording GPU commands.
/// In a real implementation, this would wrap D3D12 command lists.
/// </summary>
public sealed class MockCommandBuffer
{
    public void SetRenderTarget(string name)
    {
#if DEBUG
        Console.WriteLine(nameof(SetRenderTarget) + ": " + name);
#endif
    }

    public void SetDepthStencil(string name)
    {
#if DEBUG
        Console.WriteLine(nameof(SetDepthStencil) + ": " + name);
#endif
    }

    public void ClearRenderTarget(string name, float r, float g, float b, float a)
    {
#if DEBUG
        Console.WriteLine(nameof(ClearRenderTarget) + ": " + name);
#endif
    }

    public void ClearDepth(string name, float depth)
    {
#if DEBUG
        Console.WriteLine(nameof(ClearDepth) + ": " + name);
#endif
    }

    public void Draw(int vertexCount)
    {
#if DEBUG
        Console.WriteLine(nameof(Draw) + ": " + vertexCount);
#endif
    }

    public void BindShaderResource(string name, int slot)
    {
#if DEBUG
        Console.WriteLine(nameof(BindShaderResource) + ": " + name + ", slot " + slot);
#endif
    }

    public void BindUnorderedAccess(string name, int slot)
    {
#if DEBUG
        Console.WriteLine(nameof(BindUnorderedAccess) + ": " + name + ", slot " + slot);
#endif
    }

    public void Dispatch(int x, int y, int z)
    {
#if DEBUG
        Console.WriteLine(nameof(Dispatch) + ": " + x + ", " + y + ", " + z);
#endif
    }

    public void ResourceBarrier(string resourceName, string stateBefore, string stateAfter)
    {
#if DEBUG
        Console.WriteLine(nameof(ResourceBarrier) + ": " + resourceName + " from " + stateBefore + " to " + stateAfter);
#endif
    }

    public void AliasBarrier(string resourceBefore, string resourceAfter)
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

    public RasterRenderContext(MockCommandBuffer cmd)
    {
        _cmd = cmd;
    }

    // Expose command buffer methods directly
    public void SetRenderTarget(string name) => _cmd.SetRenderTarget(name);
    public void SetDepthStencil(string name) => _cmd.SetDepthStencil(name);
    public void ClearRenderTarget(string name, float r, float g, float b, float a) => _cmd.ClearRenderTarget(name, r, g, b, a);
    public void ClearDepth(string name, float depth) => _cmd.ClearDepth(name, depth);
    public void Draw(int vertexCount) => _cmd.Draw(vertexCount);
    public void BindShaderResource(string name, int slot) => _cmd.BindShaderResource(name, slot);
}

/// <summary>
/// Context for compute rendering passes.
/// Directly exposes command buffer methods.
/// </summary>
public readonly struct ComputeRenderContext
{
    private readonly MockCommandBuffer _cmd;

    public ComputeRenderContext(MockCommandBuffer cmd)
    {
        _cmd = cmd;
    }

    // Expose command buffer methods directly
    public void BindShaderResource(string name, int slot) => _cmd.BindShaderResource(name, slot);
    public void BindUnorderedAccess(string name, int slot) => _cmd.BindUnorderedAccess(name, slot);
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
