namespace Ghost.RenderGraph.Concept;

public class RenderGraph
{
    private int _resourceIdCounter = 0;
    private int _passCounter = 0;
    
    private readonly List<RenderGraphResourceHandle> _resources = new();
    private readonly List<RenderGraphPass> _passes = new();
    
    // Use List instead of Dictionary since resource IDs are sequential (0, 1, 2, ...)
    private readonly List<ResourceLifetime> _resourceLifetimes = new();
    private readonly List<ResourceState> _currentResourceStates = new();
    private readonly List<int> _resourceToAllocationMap = new();
    
    private readonly Dictionary<int, RenderGraphResourceHandle?> _allocationActiveResource = new();
    private readonly RenderGraphBlackboard _blackboard = new();
    private readonly ResourceAllocator _allocator = new();

    public RenderGraphTextureHandle ImportTexture(string name, TextureDescriptor descriptor)
    {
        var handle = new RenderGraphTextureHandle(_resourceIdCounter++, name, descriptor, isImported: true);
        _resources.Add(handle);
        _resourceLifetimes.Add(new ResourceLifetime(handle));
        _currentResourceStates.Add(ResourceState.Undefined);
        _resourceToAllocationMap.Add(-1); // -1 means no allocation
        Console.WriteLine($"[RG] Import Texture: '{name}' ({descriptor.Width}x{descriptor.Height}, {descriptor.Format})");
        return handle;
    }

    public RenderGraphBufferHandle ImportBuffer(string name, BufferDescriptor descriptor)
    {
        var handle = new RenderGraphBufferHandle(_resourceIdCounter++, name, descriptor, isImported: true);
        _resources.Add(handle);
        _resourceLifetimes.Add(new ResourceLifetime(handle));
        _currentResourceStates.Add(ResourceState.Undefined);
        _resourceToAllocationMap.Add(-1);
        Console.WriteLine($"[RG] Import Buffer: '{name}' ({descriptor.SizeInBytes} bytes)");
        return handle;
    }

    internal RenderGraphTextureHandle CreateTransientTexture(TextureDescriptor descriptor)
    {
        var handle = new RenderGraphTextureHandle(_resourceIdCounter++, descriptor.DebugName, descriptor, isImported: false);
        _resources.Add(handle);
        _resourceLifetimes.Add(new ResourceLifetime(handle));
        _currentResourceStates.Add(ResourceState.Undefined);
        _resourceToAllocationMap.Add(-1);
        Console.WriteLine($"[RG] Create Transient Texture: '{descriptor.DebugName}' ({descriptor.Width}x{descriptor.Height}, {descriptor.Format})");
        return handle;
    }

    internal RenderGraphBufferHandle CreateTransientBuffer(BufferDescriptor descriptor)
    {
        var handle = new RenderGraphBufferHandle(_resourceIdCounter++, descriptor.DebugName, descriptor, isImported: false);
        _resources.Add(handle);
        _resourceLifetimes.Add(new ResourceLifetime(handle));
        _currentResourceStates.Add(ResourceState.Undefined);
        _resourceToAllocationMap.Add(-1);
        Console.WriteLine($"[RG] Create Transient Buffer: '{descriptor.DebugName}' ({descriptor.SizeInBytes} bytes)");
        return handle;
    }

    public RenderGraphBlackboard Blackboard => _blackboard;

    public RenderGraphTextureHandle CreateTexture(TextureDescriptor descriptor)
    {
        return CreateTransientTexture(descriptor);
    }

    public RenderGraphBufferHandle CreateBuffer(BufferDescriptor descriptor)
    {
        return CreateTransientBuffer(descriptor);
    }

    public RenderGraphPassBuilder<TPassData> AddRenderPass<TPassData>(string name, out TPassData passData)
        where TPassData : class, new()
    {
        var builder = new RenderGraphPassBuilder<TPassData>(this, name, _passCounter);
        passData = builder.PassData;
        return builder;
    }

    internal void CommitPass<TPassData>(RenderGraphPassBuilder<TPassData> builder, string name)
        where TPassData : class, new()
    {
        if (builder.RenderFunc == null)
        {
            throw new InvalidOperationException($"Pass '{name}' has no render function set. Call SetRenderFunc() on the builder.");
        }

        var pass = new RenderGraphPass<TPassData>(
            name,
            _passCounter++,
            builder.PassData,
            builder.RenderFunc,
            builder.ResourceAccesses.ToList(),
            builder.AllowCulling);
        
        _passes.Add(pass);

        foreach (var (handle, state) in pass.ResourceAccesses)
        {
            _resourceLifetimes[handle.Id].AddUsage(state, pass.Index);
        }

        Console.WriteLine($"[RG] Add Pass: '{name}' (Index: {pass.Index})");
        foreach (var (handle, state) in pass.ResourceAccesses)
        {
            Console.WriteLine($"     - {state}: '{handle.Name}'");
        }
    }

    public void Compile()
    {
        Console.WriteLine("\n[RG] ========== COMPILING RENDER GRAPH ==========");
        
        BuildDependencies();
        CullUnusedPasses();
        AnalyzeResourceLifetimes();
        AllocatePhysicalResources();
    }

    private void AllocatePhysicalResources()
    {
        // Pass as IReadOnlyList since it's now a List
        _allocator.AllocateResources(_resourceLifetimes, _passes);
        
        // Build mapping from virtual resource to physical allocation
        foreach (var allocation in _allocator.Allocations)
        {
            foreach (var resource in allocation.AliasedResources)
            {
                _resourceToAllocationMap[resource.Id] = allocation.AllocationId;
            }
        }
    }

    private void BuildDependencies()
    {
        Console.WriteLine("\n[RG] Building pass dependencies...");

        for (int i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            
            var writtenResources = pass.ResourceAccesses
                .Where(access => IsWriteState(access.state))
                .Select(access => access.handle.Id)
                .ToHashSet();

            for (int j = 0; j < i; j++)
            {
                var previousPass = _passes[j];
                
                var hasReadAfterWrite = previousPass.ResourceAccesses
                    .Where(access => IsWriteState(access.state))
                    .Any(access => pass.ResourceAccesses.Any(
                        current => current.handle.Id == access.handle.Id && IsReadState(current.state)));

                var hasWriteAfterRead = pass.ResourceAccesses
                    .Where(access => IsWriteState(access.state))
                    .Any(access => previousPass.ResourceAccesses.Any(
                        prev => prev.handle.Id == access.handle.Id && IsReadState(prev.state)));

                var hasWriteAfterWrite = previousPass.ResourceAccesses
                    .Where(access => IsWriteState(access.state))
                    .Any(access => writtenResources.Contains(access.handle.Id));

                if (hasReadAfterWrite || hasWriteAfterRead || hasWriteAfterWrite)
                {
                    if (!pass.Dependencies.Contains(j))
                    {
                        pass.Dependencies.Add(j);
                        Console.WriteLine($"     Pass '{pass.Name}' depends on '{previousPass.Name}'");
                    }
                }
            }
        }
    }

    private void CullUnusedPasses()
    {
        Console.WriteLine("\n[RG] Culling unused passes...");
        
        // Mark passes that contribute to imported resources or don't allow culling
        foreach (var pass in _passes)
        {
            foreach (var (handle, _) in pass.ResourceAccesses)
            {
                if (handle.IsImported)
                {
                    pass.RefCount++;
                }
            }
            
            // Mark passes that don't allow culling (synchronization, debug, etc.)
            if (!pass.AllowCulling)
            {
                pass.RefCount++;
                Console.WriteLine($"     Pass '{pass.Name}' marked as non-cullable");
            }
        }

        // Propagate reference counts through dependencies
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var pass in _passes)
            {
                if (pass.RefCount > 0)
                {
                    foreach (var depIndex in pass.Dependencies)
                    {
                        var depPass = _passes[depIndex];
                        if (depPass.RefCount == 0)
                        {
                            depPass.RefCount++;
                            changed = true;
                        }
                    }
                }
            }
        }

        var culledPasses = _passes.Where(p => p.RefCount == 0 && p.AllowCulling).ToList();
        if (culledPasses.Count != 0)
        {
            foreach (var pass in culledPasses)
            {
                Console.WriteLine($"     Culled unused pass: '{pass.Name}'");
            }
        }
        else
        {
            Console.WriteLine("     No passes culled.");
        }
    }

    private void AnalyzeResourceLifetimes()
    {
        Console.WriteLine("\n[RG] Resource lifetimes:");

        foreach (var lifetime in _resourceLifetimes)
        {
            if (lifetime.FirstUse == int.MaxValue)
            {
                Console.WriteLine($"     '{lifetime.Handle.Name}': UNUSED");
            }
            else
            {
                var passNames = _passes
                    .Where(p => p.Index >= lifetime.FirstUse && p.Index <= lifetime.LastUse && p.RefCount > 0)
                    .Select(p => p.Name);
                Console.WriteLine($"     '{lifetime.Handle.Name}': [{lifetime.FirstUse}..{lifetime.LastUse}] ({string.Join(", ", passNames)})");
            }
        }
    }

    public void Execute()
    {
        Console.WriteLine("\n[RG] ========== EXECUTING RENDER GRAPH ==========\n");

        var commandBuffer = new SimulatedCommandBuffer();

        foreach (var pass in _passes.Where(p => p.RefCount > 0).OrderBy(p => p.Index))
        {
            Console.WriteLine($"[PASS {pass.Index}] Executing: '{pass.Name}'");
            
            var lifetime = _resourceLifetimes
                .Where(lt => lt.FirstUse == pass.Index)
                .ToList();

            foreach (var lt in lifetime)
            {
                if (!lt.Handle.IsImported)
                {
                    CreateResource(lt.Handle);
                }
            }

            InsertBarriers(pass, commandBuffer);

            commandBuffer.BeginRenderPass(pass.Name);
            pass.Execute(commandBuffer);
            commandBuffer.EndRenderPass();

            var endLifetime = _resourceLifetimes
                .Where(lt => lt.LastUse == pass.Index && !lt.Handle.IsImported)
                .ToList();

            foreach (var lt in endLifetime)
            {
                DestroyResource(lt.Handle);
            }

            Console.WriteLine();
        }

        Console.WriteLine("[RG] ========== EXECUTION COMPLETE ==========\n");
    }

    private void CreateResource(RenderGraphResourceHandle handle)
    {
        var allocation = _allocator.GetAllocation(handle);
        if (allocation != null)
        {
            if (handle is RenderGraphTextureHandle textureHandle)
            {
                var desc = textureHandle.Descriptor;
                Console.WriteLine($"  [CREATE] Texture '{handle.Name}' using '{allocation.DebugName}' " +
                                $"({desc.Width}x{desc.Height}, {desc.Format}, offset: {allocation.OffsetInBytes})");
            }
            else if (handle is RenderGraphBufferHandle bufferHandle)
            {
                var desc = bufferHandle.Descriptor;
                Console.WriteLine($"  [CREATE] Buffer '{handle.Name}' using '{allocation.DebugName}' " +
                                $"({desc.SizeInBytes} bytes, offset: {allocation.OffsetInBytes})");
            }
            
            // Note: We do NOT set _allocationActiveResource here
            // That happens in InsertBarriers when the resource is first accessed
        }
        else
        {
            if (handle is RenderGraphTextureHandle textureHandle)
            {
                var desc = textureHandle.Descriptor;
                Console.WriteLine($"  [CREATE] Texture '{handle.Name}' ({desc.Width}x{desc.Height}, {desc.Format})");
            }
            else if (handle is RenderGraphBufferHandle bufferHandle)
            {
                var desc = bufferHandle.Descriptor;
                Console.WriteLine($"  [CREATE] Buffer '{handle.Name}' ({desc.SizeInBytes} bytes)");
            }
        }
        
        _currentResourceStates[handle.Id] = ResourceState.Undefined;
    }

    private void DestroyResource(RenderGraphResourceHandle handle)
    {
        Console.WriteLine($"  [DESTROY] Resource '{handle.Name}'");
        _currentResourceStates[handle.Id] = ResourceState.Undefined;
        
        // Note: We intentionally DO NOT clear _allocationActiveResource here
        // The allocation remains "owned" by this resource until another resource aliases it
        // This allows us to track aliasing barriers correctly
    }

    private void InsertBarriers(RenderGraphPass pass, ICommandBuffer commandBuffer)
    {
        foreach (var (handle, targetState) in pass.ResourceAccesses)
        {
            // Check if this resource shares a physical allocation
            var allocation = _allocator.GetAllocation(handle);
            if (allocation != null)
            {
                // Check what resource is currently active on this allocation
                if (_allocationActiveResource.TryGetValue(allocation.AllocationId, out var activeResource))
                {
                    // If a different resource is currently active on this allocation, insert aliasing barrier
                    if (activeResource != null && activeResource.Id != handle.Id)
                    {
                        commandBuffer.AliasingBarrier(activeResource.Name, handle.Name, allocation.DebugName);
                        
                    // Clear state for the old resource since it's being aliased away
                    _currentResourceStates[activeResource.Id] = ResourceState.Undefined;
                }
                }
                
                // Update the active resource for this allocation
                _allocationActiveResource[allocation.AllocationId] = handle;
            }

            var currentState = _currentResourceStates[handle.Id];

            if (currentState != targetState)
            {
                commandBuffer.ResourceBarrier(handle.Name, currentState, targetState);
                _currentResourceStates[handle.Id] = targetState;
            }
        }
    }

    private static bool IsWriteState(ResourceState state)
    {
        return state.HasFlag(ResourceState.RenderTarget) ||
               state.HasFlag(ResourceState.DepthWrite) ||
               state.HasFlag(ResourceState.UnorderedAccess) ||
               state.HasFlag(ResourceState.CopyDest);
    }

    private static bool IsReadState(ResourceState state)
    {
        return state.HasFlag(ResourceState.ShaderResource) ||
               state.HasFlag(ResourceState.DepthRead) ||
               state.HasFlag(ResourceState.CopySource);
    }

    public void Reset()
    {
        _passes.Clear();
        _resources.Clear();
        _resourceLifetimes.Clear();
        _currentResourceStates.Clear();
        _resourceToAllocationMap.Clear();
        _allocationActiveResource.Clear();
        _blackboard.Clear();
        _passCounter = 0;
        _resourceIdCounter = 0;
        Console.WriteLine("[RG] Render graph reset.");
    }
}
