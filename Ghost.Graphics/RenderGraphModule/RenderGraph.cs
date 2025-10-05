using Ghost.Core;
using Ghost.Graphics.Data;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Descriptor for render graph configuration
/// </summary>
public readonly struct RenderGraphDesc
{
    public readonly int InitialResourceCapacity;
    public readonly int InitialPassCapacity;

    public RenderGraphDesc(int initialResourceCapacity = 256, int initialPassCapacity = 64)
    {
        InitialResourceCapacity = initialResourceCapacity;
        InitialPassCapacity = initialPassCapacity;
    }
}

/// <summary>
/// Main render graph class for managing transient resources and render passes
/// </summary>
public sealed class RenderGraph : IDisposable
{
    private readonly string _name;
    private readonly List<RenderGraphResource> _resources;
    private readonly List<RenderPass> _passes;
    private readonly List<int> _compiledPassOrder;
    private readonly List<RenderGraphBarrier> _barriers;

    private bool _isRecording;
    private bool _isCompiled;
    private bool _isExecuted;
    private bool _disposed;

    public RenderGraph(string name, RenderGraphDesc desc = default)
    {
        _name = name;
        _resources = new(desc.InitialResourceCapacity > 0 ? desc.InitialResourceCapacity : 256);
        _passes = new(desc.InitialPassCapacity > 0 ? desc.InitialPassCapacity : 64);
        _compiledPassOrder = new();
        _barriers = new();
    }

    public string Name => _name;
    public bool IsRecording => _isRecording;
    public bool IsCompiled => _isCompiled;

    /// <summary>
    /// Begin recording render passes
    /// </summary>
    public void BeginRecord()
    {
        if (_isRecording)
            throw new InvalidOperationException("Render graph is already recording");
        if (_isCompiled)
            throw new InvalidOperationException("Cannot record on a compiled render graph");

        _isRecording = true;
        _isExecuted = false;

        // Clear previous frame data
        foreach (var resource in _resources)
        {
            if (resource.Lifetime == ResourceLifetime.Transient)
            {
                resource.ReleaseResource();
            }
        }

        _resources.RemoveAll(r => r.Lifetime == ResourceLifetime.Transient);
        _passes.Clear();
        _compiledPassOrder.Clear();
        _barriers.Clear();
    }

    /// <summary>
    /// End recording render passes
    /// </summary>
    public void EndRecord()
    {
        if (!_isRecording)
            throw new InvalidOperationException("Render graph is not recording");

        _isRecording = false;
    }

    /// <summary>
    /// Create a new render pass
    /// </summary>
    public RenderPassCreator<TPassData> CreatePass<TPassData>(string passName)
        where TPassData : struct
    {
        if (!_isRecording)
            throw new InvalidOperationException("Cannot create pass when not recording");

        return new RenderPassCreator<TPassData>(this, passName);
    }

    /// <summary>
    /// Create a transient texture resource
    /// </summary>
    public RGTextureHandle CreateTexture(string name, ResourceLifetime lifetime, TextureDescription description)
    {
        var texture = new RenderGraphTexture(_resources.Count, name, lifetime, description);

        _resources.Add(texture);
        return new RGTextureHandle(texture.Id, this);
    }

    /// <summary>
    /// Create a transient buffer resource
    /// </summary>
    public RGBufferHandle CreateBuffer(string name, ResourceLifetime lifetime, BufferDescription description)
    {
        var buffer = new RenderGraphBuffer(_resources.Count, name, lifetime, description);

        _resources.Add(buffer);
        return new RGBufferHandle(buffer.Id, this);
    }

    /// <summary>
    /// Import an external texture (e.g., from previous frame, swap chain)
    /// </summary>
    public RGTextureHandle ImportTexture(string name, Handle<Texture> externalHandle, TextureDescription description)
    {
        var texture = new RenderGraphTexture(_resources.Count, name, externalHandle, description);

        _resources.Add(texture);
        return new RGTextureHandle(texture.Id, this);
    }

    /// <summary>
    /// Import an external buffer (e.g., from previous frame)
    /// </summary>
    public RGBufferHandle ImportBuffer(string name, Handle<GraphicsBuffer> externalHandle, BufferDescription description)
    {
        var buffer = new RenderGraphBuffer(_resources.Count, name, externalHandle, description);

        _resources.Add(buffer);
        return new RGBufferHandle(buffer.Id, this);
    }

    /// <summary>
    /// Export a resource for use in the next frame (for history buffers)
    /// </summary>
    public Handle<Texture> ExportTexture(RGTextureHandle handle)
    {
        if (!handle.IsValid || handle._resourceId >= _resources.Count)
            throw new ArgumentException("Invalid texture handle", nameof(handle));

        var texture = (RenderGraphTexture)_resources[handle._resourceId];
        texture.Lifetime = ResourceLifetime.Persistent;
        return texture.Handle;
    }

    /// <summary>
    /// Export a buffer for use in the next frame
    /// </summary>
    public Handle<GraphicsBuffer> ExportBuffer(RGBufferHandle handle)
    {
        if (!handle.IsValid || handle._resourceId >= _resources.Count)
            throw new ArgumentException("Invalid buffer handle", nameof(handle));

        var buffer = (RenderGraphBuffer)_resources[handle._resourceId];
        buffer.Lifetime = ResourceLifetime.Persistent;
        return buffer.Handle;
    }

    public RenderGraphTexture GetTextureResource(RGTextureHandle handle)
    {
        if (!handle.IsValid || handle._resourceId >= _resources.Count)
            throw new ArgumentException("Invalid texture handle", nameof(handle));
        return (RenderGraphTexture)_resources[handle._resourceId];
    }

    public RenderGraphBuffer GetBufferResource(RGBufferHandle handle)
    {
        if (!handle.IsValid || handle._resourceId >= _resources.Count)
            throw new ArgumentException("Invalid buffer handle", nameof(handle));
        return (RenderGraphBuffer)_resources[handle._resourceId];
    }

    /// <summary>
    /// Internal method to add a pass to the render graph
    /// </summary>
    internal void AddPass(RenderPass pass)
    {
        pass.Index = _passes.Count;
        _passes.Add(pass);
    }

    /// <summary>
    /// Internal method to update resource lifetime during pass setup
    /// </summary>
    internal void UpdateResourceLifetime(int resourceId, int passIndex)
    {
        if (resourceId >= _resources.Count)
            return;

        var resource = _resources[resourceId];
        if (resource.FirstPassIndex == -1)
            resource.FirstPassIndex = passIndex;

        resource.LastPassIndex = Math.Max(resource.LastPassIndex, passIndex);
    }

    /// <summary>
    /// Compile the render graph - performs dependency analysis, topological sort, and resource lifetime analysis
    /// </summary>
    public void Compile()
    {
        if (_isRecording)
            throw new InvalidOperationException("Cannot compile while recording");
        if (_isCompiled)
            throw new InvalidOperationException("Render graph is already compiled");

        // Setup all passes to gather resource dependencies
        SetupPasses();

        // Build dependency graph and perform topological sort
        BuildDependencyGraph();
        TopologicalSort();

        // Analyze resource lifetimes and generate barriers
        AnalyzeResourceLifetimes();
        GenerateBarriers();

        _isCompiled = true;
    }

    /// <summary>
    /// Execute the compiled render graph
    /// </summary>
    public void Execute()
    {
        if (!_isCompiled)
            throw new InvalidOperationException("Render graph must be compiled before execution");
        if (_isExecuted)
            throw new InvalidOperationException("Render graph has already been executed");

        // Execute passes in topological order
        foreach (var passIndex in _compiledPassOrder)
        {
            var pass = _passes[passIndex];
            var context = new RenderPassContext(passIndex);

            CreateTransientResources(passIndex);
            ApplyBarriersForPass(passIndex);

            // Execute the pass
            pass.Execute(context);
        }

        _isExecuted = true;
    }

    private void SetupPasses()
    {
        for (var i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            var builder = new RenderPassBuilder(this, i);
            pass.Setup(builder);
        }
    }

    private void BuildDependencyGraph()
    {
        // Build dependencies based on resource usage
        foreach (var pass in _passes)
        {
            var writeResources = new HashSet<int>();
            var readResources = new HashSet<int>();

            // Categorize resource accesses
            foreach (var access in pass.ResourceAccesses)
            {
                switch (access.accessType)
                {
                    case ResourceAccessType.Write:
                        writeResources.Add(access.resourceId);
                        break;
                    case ResourceAccessType.Read:
                        readResources.Add(access.resourceId);
                        break;
                    case ResourceAccessType.ReadWrite:
                        writeResources.Add(access.resourceId);
                        readResources.Add(access.resourceId);
                        break;
                }
            }

            // Add dependencies based on Write-After-Read, Read-After-Write, Write-After-Write
            for (var otherPassIndex = 0; otherPassIndex < pass.Index; otherPassIndex++)
            {
                var otherPass = _passes[otherPassIndex];
                var hasDependency = false;

                foreach (var otherAccess in otherPass.ResourceAccesses)
                {
                    // WAR, RAW, WAW dependencies
                    if ((writeResources.Contains(otherAccess.resourceId) && otherAccess.accessType == ResourceAccessType.Read) ||
                        (readResources.Contains(otherAccess.resourceId) && otherAccess.accessType != ResourceAccessType.Read) ||
                        (writeResources.Contains(otherAccess.resourceId) && otherAccess.accessType != ResourceAccessType.Read))
                    {
                        hasDependency = true;
                        break;
                    }
                }

                if (hasDependency && !pass.Dependencies.Contains(otherPassIndex))
                {
                    pass.Dependencies.Add(otherPassIndex);
                }
            }
        }
    }

    private void TopologicalSort()
    {
        _compiledPassOrder.Clear();
        var visited = new bool[_passes.Count];
        var inDegree = new int[_passes.Count];

        // Calculate in-degrees
        foreach (var pass in _passes)
        {
            foreach (var dependency in pass.Dependencies)
            {
                inDegree[pass.Index]++;
            }
        }

        // Kahn's algorithm for topological sorting
        var queue = new Queue<int>();
        for (var i = 0; i < _passes.Count; i++)
        {
            if (inDegree[i] == 0)
            {
                queue.Enqueue(i);
            }
        }

        while (queue.Count > 0)
        {
            var passIndex = queue.Dequeue();
            _compiledPassOrder.Add(passIndex);

            var currentPass = _passes[passIndex];
            foreach (var dependentPassIndex in _passes.Where(p => p.Dependencies.Contains(passIndex)).Select(p => p.Index))
            {
                inDegree[dependentPassIndex]--;
                if (inDegree[dependentPassIndex] == 0)
                {
                    queue.Enqueue(dependentPassIndex);
                }
            }
        }

        if (_compiledPassOrder.Count != _passes.Count)
        {
            throw new InvalidOperationException("Circular dependency detected in render graph");
        }
    }

    private void AnalyzeResourceLifetimes()
    {
        // Resource lifetimes are already tracked during pass setup
        // Additional analysis can be added here if needed
    }

    private void GenerateBarriers()
    {
        _barriers.Clear();

        // TODO: Implement barrier generation based on resource state transitions
        // This would analyze the resource usage patterns and generate appropriate D3D12 barriers
    }

    private void CreateTransientResources(int passIndex)
    {
        var pass = _passes[passIndex];
        foreach (var access in pass.ResourceAccesses)
        {
            var resource = _resources[access.resourceId];
            if (!resource.IsCreated)
            {
                resource.CreateResource();
            }
        }
    }

    private void ApplyBarriersForPass(int passIndex)
    {
        // TODO: Apply the generated barriers for the given pass
        // This would involve creating D3D12 resource barriers and executing them on the command list
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var resource in _resources)
        {
            resource.ReleaseResource();
        }

        _resources.Clear();
        _passes.Clear();
        _compiledPassOrder.Clear();
        _barriers.Clear();

        _disposed = true;
    }
}