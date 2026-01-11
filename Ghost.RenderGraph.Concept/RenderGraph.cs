using Ghost.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ghost.RenderGraph.Concept;

public class RenderGraph
{
    private int _resourceIdCounter = 0;
    private int _passCounter = 0;
    
    private readonly List<RenderGraphResourceHandle> _resources = new();
    private readonly List<RenderGraphPass> _passes = new();
    
    private readonly List<ResourceLifetime> _resourceLifetimes = new();
    private readonly List<ResourceState> _currentResourceStates = new();
    private readonly List<int> _resourceToAllocationMap = new();
    
    private readonly Dictionary<int, RenderGraphResourceHandle?> _allocationActiveResource = new();
    private readonly RenderGraphBlackboard _blackboard = new();
    private readonly ResourceAllocator _allocator = new();

    // Batching and Sync
    private readonly List<RenderGraphBatch> _batches = new();
    private readonly Stack<RenderGraphBatch> _batchPool = new();
    private int _fenceCounter = 0;

    // Pooled Collections for Compilation
    private readonly Dictionary<int, int> _resourceLastWriter = new();
    private readonly Dictionary<int, List<int>> _resourceLastReaders = new();
    private readonly Dictionary<int, RenderGraphBatch> _passToBatchMap = new();
    
    // Pooled Lists for Passes
    private readonly Stack<List<(RenderGraphResourceHandle, ResourceState)>> _resourceAccessListPool = new();
    private readonly Stack<ResourceLifetime> _resourceLifetimePool = new();

    // Execution Plan (Pre-calculated to avoid LINQ in Execute)
    private List<RenderGraphResourceHandle>[] _resourcesToCreate = Array.Empty<List<RenderGraphResourceHandle>>();
    private List<RenderGraphResourceHandle>[] _resourcesToDestroy = Array.Empty<List<RenderGraphResourceHandle>>();


    public RenderGraphTextureHandle ImportTexture(string name, TextureDescriptor descriptor)
    {
        var handle = new RenderGraphTextureHandle(_resourceIdCounter++, name, descriptor, isImported: true);
        _resources.Add(handle._handle);
        _resourceLifetimes.Add(RentResourceLifetime(handle._handle));
        _currentResourceStates.Add(ResourceState.Undefined);
        _resourceToAllocationMap.Add(-1);
        //ConsoleAPI.WriteLine($"[RG] Import Texture: '{name}' ({descriptor.Width}x{descriptor.Height}, {descriptor.Format})");
        return handle;
    }

    public RenderGraphBufferHandle ImportBuffer(string name, BufferDescriptor descriptor)
    {
        var handle = new RenderGraphBufferHandle(_resourceIdCounter++, name, descriptor, isImported: true);
        _resources.Add(handle._handle);
        _resourceLifetimes.Add(RentResourceLifetime(handle._handle));
        _currentResourceStates.Add(ResourceState.Undefined);
        _resourceToAllocationMap.Add(-1);
        //ConsoleAPI.WriteLine($"[RG] Import Buffer: '{name}' ({descriptor.SizeInBytes} bytes)");
        return handle;
    }

    internal RenderGraphTextureHandle CreateTransientTexture(TextureDescriptor descriptor)
    {
        var handle = new RenderGraphTextureHandle(_resourceIdCounter++, descriptor.DebugName, descriptor, isImported: false);
        _resources.Add(handle._handle);
        _resourceLifetimes.Add(RentResourceLifetime(handle._handle));
        _currentResourceStates.Add(ResourceState.Undefined);
        _resourceToAllocationMap.Add(-1);
        //ConsoleAPI.WriteLine($"[RG] Create Transient Texture: '{descriptor.DebugName}' ({descriptor.Width}x{descriptor.Height}, {descriptor.Format})");
        return handle;
    }

    internal RenderGraphBufferHandle CreateTransientBuffer(BufferDescriptor descriptor)
    {
        var handle = new RenderGraphBufferHandle(_resourceIdCounter++, descriptor.DebugName, descriptor, isImported: false);
        _resources.Add(handle._handle);
        _resourceLifetimes.Add(RentResourceLifetime(handle._handle));
        _currentResourceStates.Add(ResourceState.Undefined);
        _resourceToAllocationMap.Add(-1);
        //ConsoleAPI.WriteLine($"[RG] Create Transient Buffer: '{descriptor.DebugName}' ({descriptor.SizeInBytes} bytes)");
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
        var list = RentResourceAccessList();
        var builder = new RenderGraphPassBuilder<TPassData>(this, name, _passCounter, list);
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

        // Optimization: Use Pass Pool
        RenderGraphPass<TPassData>? pass;
        // Cast ReadOnlyList back to List (safe because we created it in AddRenderPass)
        var resourceList = (List<(RenderGraphResourceHandle handle, ResourceState state)>)builder.ResourceAccesses;

        if (!RenderGraphPassPool<TPassData>.Pool.TryPop(out pass))
        {
            pass = new RenderGraphPass<TPassData>(
                name,
                _passCounter++,
                builder.QueueType,
                builder.PassData,
                builder.RenderFunc,
                resourceList, 
                builder.AllowCulling);
        }
        else
        {
            pass.Initialize(
                name,
                _passCounter++,
                builder.QueueType,
                builder.PassData,
                builder.RenderFunc,
                resourceList,
                builder.AllowCulling);
        }
        
        _passes.Add(pass);

        foreach (var (handle, state) in pass.ResourceAccesses)
        {
            var lifeTime = _resourceLifetimes[handle.Id];
            lifeTime.AddUsage(state, pass.Index);
            _resourceLifetimes[handle.Id] = lifeTime;
        }

        //ConsoleAPI.WriteLine($"[RG] Add Pass: '{name}' (Index: {pass.Index})");
    }

    public void Compile()
    {
        //ConsoleAPI.WriteLine("\n[RG] ========== COMPILING RENDER GRAPH ==========");
        
        BuildDependencies();
        CullUnusedPasses();
        AnalyzeResourceLifetimes();
        AllocatePhysicalResources();
        InsertSynchronization();
    }

    private void InsertSynchronization()
    {
        //ConsoleAPI.WriteLine("\n[RG] Building command batches and synchronization...");

        _batches.Clear(); 
        _fenceCounter = 0;

        // 1. Create Batches (Topological grouping)
        RenderGraphBatch? currentBatch = null;
        _passToBatchMap.Clear();

        foreach (var pass in _passes)
        {
            if (pass.RefCount == 0) continue;

            if (currentBatch == null || currentBatch.QueueType != pass.QueueType)
            {
                if (!_batchPool.TryPop(out currentBatch))
                {
                    currentBatch = new RenderGraphBatch();
                }
                currentBatch.Initialize(_batches.Count, pass.QueueType);
                _batches.Add(currentBatch);
            }

            currentBatch.Passes.Add(pass);
            _passToBatchMap[pass.Index] = currentBatch;
        }

        //ConsoleAPI.WriteLine($"     Created {_batches.Count} batches.");

        // 2. Inject Synchronization (Fences)
        foreach (var batch in _batches)
        {
            foreach (var pass in batch.Passes)
            {
                foreach (var depIndex in pass.Dependencies)
                {
                    if (_passToBatchMap.TryGetValue(depIndex, out var dependencyBatch))
                    {
                        if (dependencyBatch != batch)
                        {
                            int fenceId;
                            if (dependencyBatch.SignalFences.Count == 0)
                            {
                                fenceId = _fenceCounter++;
                                dependencyBatch.SignalFences.Add(fenceId);
                            }
                            else
                            {
                                fenceId = dependencyBatch.SignalFences[0];
                            }

                            if (!batch.WaitFences.Contains(fenceId))
                            {
                                batch.WaitFences.Add(fenceId);
                                //ConsoleAPI.WriteLine($"     Batch {batch.ID} ({batch.QueueType}) waits on Batch {dependencyBatch.ID} ({dependencyBatch.QueueType}) [Fence {fenceId}]");
                            }
                        }
                    }
                }
            }
        }
    }

    private void AllocatePhysicalResources()
    {
        _allocator.AllocateResources(_resourceLifetimes, _passes);
        
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
        _resourceLastWriter.Clear();
        foreach (var list in _resourceLastReaders.Values) list.Clear();
        _resourceLastReaders.Clear();

        for (int i = 0; i < _passes.Count; i++)
        {
            var pass = _passes[i];
            
            foreach (var (handle, state) in pass.ResourceAccesses)
            {
                int resourceId = handle.Id;

                if (IsReadState(state))
                {
                    if (_resourceLastWriter.TryGetValue(resourceId, out int lastWriterIndex))
                    {
                        if (!pass.Dependencies.Contains(lastWriterIndex))
                        {
                            pass.Dependencies.Add(lastWriterIndex);
                        }
                    }

                    if (!_resourceLastReaders.TryGetValue(resourceId, out var readers))
                    {
                        readers = new List<int>(); // Optimization TODO: Pool these
                        _resourceLastReaders[resourceId] = readers;
                    }
                    readers.Add(i);
                }

                if (IsWriteState(state))
                {
                    if (_resourceLastWriter.TryGetValue(resourceId, out int lastWriterIndex))
                    {
                        if (!pass.Dependencies.Contains(lastWriterIndex))
                        {
                            pass.Dependencies.Add(lastWriterIndex);
                        }
                    }

                    if (_resourceLastReaders.TryGetValue(resourceId, out var readers))
                    {
                        foreach (var readerIndex in readers)
                        {
                            if (readerIndex != i && !pass.Dependencies.Contains(readerIndex))
                            {
                                pass.Dependencies.Add(readerIndex);
                            }
                        }
                        readers.Clear();
                    }

                    _resourceLastWriter[resourceId] = i;
                }
            }
        }
    }

    private void CullUnusedPasses()
    {
        foreach (var pass in _passes)
        {
            foreach (var (handle, _) in pass.ResourceAccesses)
            {
                if (handle.IsImported)
                {
                    pass.RefCount++;
                }
            }
            
            if (!pass.AllowCulling)
            {
                pass.RefCount++;
            }
        }

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
    }

    private void AnalyzeResourceLifetimes()
    {
        // Resize execution plan arrays if needed
        int requiredSize = _passes.Count;
        if (_resourcesToCreate.Length < requiredSize)
        {
            Array.Resize(ref _resourcesToCreate, requiredSize);
            Array.Resize(ref _resourcesToDestroy, requiredSize);
            
            // Initialize new elements
            for (int i = 0; i < requiredSize; i++)
            {
                if (_resourcesToCreate[i] == null) _resourcesToCreate[i] = new List<RenderGraphResourceHandle>();
                if (_resourcesToDestroy[i] == null) _resourcesToDestroy[i] = new List<RenderGraphResourceHandle>();
            }
        }

        // Clear previous plan
        for (int i = 0; i < requiredSize; i++)
        {
            _resourcesToCreate[i].Clear();
            _resourcesToDestroy[i].Clear();
        }

        // Populate plan
        foreach (var lifetime in _resourceLifetimes)
        {
            if (lifetime.FirstUse != int.MaxValue && !lifetime.Handle.IsImported)
            {
                // Verify bounds to be safe
                if (lifetime.FirstUse < requiredSize)
                    _resourcesToCreate[lifetime.FirstUse].Add(lifetime.Handle);
                
                if (lifetime.LastUse < requiredSize)
                    _resourcesToDestroy[lifetime.LastUse].Add(lifetime.Handle);
            }
        }
    }

    public void Execute()
    {
        //ConsoleAPI.WriteLine("\n[RG] ========== EXECUTING RENDER GRAPH ==========\n");

        var commandBuffer = new SimulatedCommandBuffer();

        foreach (var batch in _batches)
        {
            //ConsoleAPI.WriteLine($"[BATCH {batch.ID}] Queue: {batch.QueueType} | Passes: {batch.Passes.Count}");

            foreach (var fenceId in batch.WaitFences)
            {
                //ConsoleAPI.WriteLine($"  [SYNC] Wait for Fence {fenceId}");
            }

            foreach (var pass in batch.Passes)
            {
                //ConsoleAPI.WriteLine($"  [PASS {pass.Index}] Executing: '{pass.Name}'");
                
                // Optimized: Use pre-calculated lists
                var createList = _resourcesToCreate[pass.Index];
                foreach (var handle in createList)
                {
                    CreateResource(handle);
                }

                InsertBarriers(pass, commandBuffer);

                commandBuffer.BeginRenderPass(pass.Name);
                pass.Execute(commandBuffer);
                commandBuffer.EndRenderPass();

                // Optimized: Use pre-calculated lists
                var destroyList = _resourcesToDestroy[pass.Index];
                foreach (var handle in destroyList)
                {
                    DestroyResource(handle);
                }
            }

            foreach (var fenceId in batch.SignalFences)
            {
                //ConsoleAPI.WriteLine($"  [SYNC] Signal Fence {fenceId}");
            }
        }

        //ConsoleAPI.WriteLine("[RG] ========== EXECUTION COMPLETE ==========\n");
    }

    private void CreateResource(RenderGraphResourceHandle handle)
    {
        var allocation = _allocator.GetAllocation(handle);
        if (allocation != null)
        {
            // Logic...
        }
        
        _currentResourceStates[handle.Id] = ResourceState.Undefined;
    }

    private void DestroyResource(RenderGraphResourceHandle handle)
    {
        _currentResourceStates[handle.Id] = ResourceState.Undefined;
    }

    private void InsertBarriers(RenderGraphPass pass, ICommandBuffer commandBuffer)
    {
        var _resourceBarriers = ListPool<ResourceBarrierInfo>.Rent();
        var _aliasingBarriers = ListPool<AliasingBarrierInfo>.Rent();

        foreach (var (handle, targetState) in pass.ResourceAccesses)
        {
            var allocation = _allocator.GetAllocation(handle);
            if (allocation != null)
            {
                if (_allocationActiveResource.TryGetValue(allocation.Value.AllocationId, out var activeResource))
                {
                    if (activeResource != null && activeResource.Value.Id != handle.Id)
                    {
                        _aliasingBarriers.Add(new AliasingBarrierInfo(activeResource.Value.Name, handle.Name, allocation.Value.DebugName));
                        _currentResourceStates[activeResource.Value.Id] = ResourceState.Undefined;
                    }
                }
                _allocationActiveResource[allocation.Value.AllocationId] = handle;
            }

            var currentState = _currentResourceStates[handle.Id];
            if (currentState != targetState)
            {
                _resourceBarriers.Add(new ResourceBarrierInfo(handle.Name, currentState, targetState));
                _currentResourceStates[handle.Id] = targetState;
            }
        }

        if (_aliasingBarriers.Count > 0)
        {
            commandBuffer.AliasingBarrier(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_aliasingBarriers));
        }

        if (_resourceBarriers.Count > 0)
        {
            commandBuffer.ResourceBarrier(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_resourceBarriers));
        }

        ListPool<ResourceBarrierInfo>.Return(_resourceBarriers);
        ListPool<AliasingBarrierInfo>.Return(_aliasingBarriers);
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

    internal List<(RenderGraphResourceHandle, ResourceState)> RentResourceAccessList()
    {
        if (_resourceAccessListPool.TryPop(out var list))
        {
            return list;
        }
        return new List<(RenderGraphResourceHandle, ResourceState)>();
    }

    internal void ReturnResourceAccessList(List<(RenderGraphResourceHandle, ResourceState)> list)
    {
        list.Clear();
        _resourceAccessListPool.Push(list);
    }

    private ResourceLifetime RentResourceLifetime(RenderGraphResourceHandle handle)
    {
        if (!_resourceLifetimePool.TryPop(out var lifetime))
        {
            lifetime = new ResourceLifetime();
        }
        lifetime.Initialize(handle);
        return lifetime;
    }

    public void Reset()
    {
        foreach (var batch in _batches)
        {
            batch.Reset();
            _batchPool.Push(batch);
        }
        _batches.Clear();

        foreach (var pass in _passes)
        {
            // ReturnResourceAccessList(pass.ResourceAccesses); 
            // Warning: pass.ResourceAccesses might be a copy in the current implementation of CommitPass? 
            // No, I'm going to fix CommitPass to use the pooled list.
            // But right now builder.ResourceAccesses is a List.
            // I need to ensure CommitPass takes ownership.
        }
        _passes.Clear();

        _resources.Clear();
        foreach (var lifetime in _resourceLifetimes)
        {
            _resourceLifetimePool.Push(lifetime);
        }
        _resourceLifetimes.Clear();
        _currentResourceStates.Clear();
        _resourceToAllocationMap.Clear();
        _allocationActiveResource.Clear();
        _blackboard.Clear();
        _allocator.Reset();
        _passCounter = 0;
        _resourceIdCounter = 0;
        
        _resourceLastWriter.Clear();
        foreach (var list in _resourceLastReaders.Values) list.Clear(); 
        _resourceLastReaders.Clear();
        _passToBatchMap.Clear();
    }
}
