namespace Ghost.RenderGraph.Concept;

public interface ICommandBuffer
{
    void ResourceBarrier(string resourceName, ResourceState beforeState, ResourceState afterState);
    void AliasingBarrier(string beforeResourceName, string afterResourceName, string physicalAllocationName);
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
        Console.WriteLine($"  [BARRIER] Transition '{resourceName}' from {beforeState} to {afterState}");
    }

    public void AliasingBarrier(string beforeResourceName, string afterResourceName, string physicalAllocationName)
    {
        Console.WriteLine($"  [ALIAS_BARRIER] Alias '{physicalAllocationName}': '{beforeResourceName}' -> '{afterResourceName}'");
    }

    public void BeginRenderPass(string passName)
    {
        Console.WriteLine($"  [BEGIN] RenderPass '{passName}'");
    }

    public void EndRenderPass()
    {
        Console.WriteLine($"  [END] RenderPass");
    }

    public void SetRenderTarget(string textureName)
    {
        Console.WriteLine($"    [RT] Set RenderTarget: '{textureName}'");
    }

    public void SetDepthStencil(string textureName)
    {
        Console.WriteLine($"    [DS] Set DepthStencil: '{textureName}'");
    }

    public void BindShaderResource(string resourceName, int slot)
    {
        Console.WriteLine($"    [SRV] Bind ShaderResource: '{resourceName}' at slot {slot}");
    }

    public void BindUnorderedAccess(string resourceName, int slot)
    {
        Console.WriteLine($"    [UAV] Bind UnorderedAccess: '{resourceName}' at slot {slot}");
    }

    public void Draw(int vertexCount)
    {
        Console.WriteLine($"    [DRAW] Drawing {vertexCount} vertices");
    }

    public void Dispatch(int x, int y, int z)
    {
        Console.WriteLine($"    [DISPATCH] Compute ({x}, {y}, {z})");
    }

    public void ClearRenderTarget(string textureName, float r, float g, float b, float a)
    {
        Console.WriteLine($"    [CLEAR_RT] Clear '{textureName}' to ({r}, {g}, {b}, {a})");
    }

    public void ClearDepth(string textureName, float depth)
    {
        Console.WriteLine($"    [CLEAR_DEPTH] Clear '{textureName}' to {depth}");
    }

    public void CopyTexture(string source, string destination)
    {
        Console.WriteLine($"    [COPY] Copy from '{source}' to '{destination}'");
    }
}
