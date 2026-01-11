namespace Ghost.RenderGraph.Concept;

public struct ResourceBarrierInfo
{
    public string ResourceName;
    public ResourceState BeforeState;
    public ResourceState AfterState;

    public ResourceBarrierInfo(string resourceName, ResourceState beforeState, ResourceState afterState)
    {
        ResourceName = resourceName;
        BeforeState = beforeState;
        AfterState = afterState;
    }
}

public struct AliasingBarrierInfo
{
    public string BeforeResourceName;
    public string AfterResourceName;
    public string PhysicalAllocationName;

    public AliasingBarrierInfo(string beforeResourceName, string afterResourceName, string physicalAllocationName)
    {
        BeforeResourceName = beforeResourceName;
        AfterResourceName = afterResourceName;
        PhysicalAllocationName = physicalAllocationName;
    }
}

public interface ICommandBuffer
{
    void ResourceBarrier(string resourceName, ResourceState beforeState, ResourceState afterState);
    void ResourceBarrier(Span<ResourceBarrierInfo> barriers);
    void AliasingBarrier(string beforeResourceName, string afterResourceName, string physicalAllocationName);
    void AliasingBarrier(Span<AliasingBarrierInfo> barriers);
    void BeginRenderPass(string passName);
    void EndRenderPass();
    void SetRenderTarget(string textureName);
    void SetDepthStencil(string textureName);
    void BindShaderResource(string resourceName, int slot);
    void BindUnorderedAccess(string resourceName, int slot);
    void Draw(int vertexCount);
    void Dispatch(int x, int y, int z);
    void ClearRenderTarget(string textureName, float r, float g, float b, float a);
    void ClearDepth(string textureName, float depth);
    void CopyTexture(string source, string destination);
}

public class SimulatedCommandBuffer : ICommandBuffer
{
    public void ResourceBarrier(string resourceName, ResourceState beforeState, ResourceState afterState)
    {
        //ConsoleAPI.WriteLine($"  [BARRIER] Transition '{resourceName}' from {beforeState} to {afterState}");
    }

    public void ResourceBarrier(Span<ResourceBarrierInfo> barriers)
    {
        if (barriers.Length == 0) return;
        //ConsoleAPI.WriteLine($"  [BARRIER_BATCH] Processing {barriers.Length} transitions:");
        foreach (var barrier in barriers)
        {
            //ConsoleAPI.WriteLine($"    - Transition '{barrier.ResourceName}' from {barrier.BeforeState} to {barrier.AfterState}");
        }
    }

    public void AliasingBarrier(string beforeResourceName, string afterResourceName, string physicalAllocationName)
    {
        //ConsoleAPI.WriteLine($"  [ALIAS_BARRIER] Alias '{physicalAllocationName}': '{beforeResourceName}' -> '{afterResourceName}'");
    }

    public void AliasingBarrier(Span<AliasingBarrierInfo> barriers)
    {
        if (barriers.Length == 0) return;
        //ConsoleAPI.WriteLine($"  [ALIAS_BARRIER_BATCH] Processing {barriers.Length} aliasing barriers:");
        foreach (var barrier in barriers)
        {
            //ConsoleAPI.WriteLine($"    - Alias '{barrier.PhysicalAllocationName}': '{barrier.BeforeResourceName}' -> '{barrier.AfterResourceName}'");
        }
    }


    public void BeginRenderPass(string passName)
    {
        //ConsoleAPI.WriteLine($"  [BEGIN] RenderPass '{passName}'");
    }

    public void EndRenderPass()
    {
        //ConsoleAPI.WriteLine($"  [END] RenderPass");
    }

    public void SetRenderTarget(string textureName)
    {
        //ConsoleAPI.WriteLine($"    [RT] Set RenderTarget: '{textureName}'");
    }

    public void SetDepthStencil(string textureName)
    {
        //ConsoleAPI.WriteLine($"    [DS] Set DepthStencil: '{textureName}'");
    }

    public void BindShaderResource(string resourceName, int slot)
    {
        //ConsoleAPI.WriteLine($"    [SRV] Bind ShaderResource: '{resourceName}' at slot {slot}");
    }

    public void BindUnorderedAccess(string resourceName, int slot)
    {
        //ConsoleAPI.WriteLine($"    [UAV] Bind UnorderedAccess: '{resourceName}' at slot {slot}");
    }

    public void Draw(int vertexCount)
    {
        //ConsoleAPI.WriteLine($"    [DRAW] Drawing {vertexCount} vertices");
    }

    public void Dispatch(int x, int y, int z)
    {
        //ConsoleAPI.WriteLine($"    [DISPATCH] Compute ({x}, {y}, {z})");
    }

    public void ClearRenderTarget(string textureName, float r, float g, float b, float a)
    {
        //ConsoleAPI.WriteLine($"    [CLEAR_RT] Clear '{textureName}' to ({r}, {g}, {b}, {a})");
    }

    public void ClearDepth(string textureName, float depth)
    {
        //ConsoleAPI.WriteLine($"    [CLEAR_DEPTH] Clear '{textureName}' to {depth}");
    }

    public void CopyTexture(string source, string destination)
    {
        //ConsoleAPI.WriteLine($"    [COPY] Copy from '{source}' to '{destination}'");
    }
}

public readonly struct RasterRenderContext
{
    private readonly ICommandBuffer _cmd;

    public RasterRenderContext(ICommandBuffer cmd)
    {
        _cmd = cmd;
    }

    public void SetRenderTarget(string textureName) => _cmd.SetRenderTarget(textureName);
    public void SetDepthStencil(string textureName) => _cmd.SetDepthStencil(textureName);
    public void BindShaderResource(string resourceName, int slot) => _cmd.BindShaderResource(resourceName, slot);
    public void Draw(int vertexCount) => _cmd.Draw(vertexCount);
    public void ClearRenderTarget(string textureName, float r, float g, float b, float a) => _cmd.ClearRenderTarget(textureName, r, g, b, a);
    public void ClearDepth(string textureName, float depth) => _cmd.ClearDepth(textureName, depth);
}

public readonly struct ComputeRenderContext
{
    private readonly ICommandBuffer _cmd;

    public ComputeRenderContext(ICommandBuffer cmd)
    {
        _cmd = cmd;
    }

    public void BindShaderResource(string resourceName, int slot) => _cmd.BindShaderResource(resourceName, slot);
    public void BindUnorderedAccess(string resourceName, int slot) => _cmd.BindUnorderedAccess(resourceName, slot);
    public void Dispatch(int x, int y, int z) => _cmd.Dispatch(x, y, z);
}
