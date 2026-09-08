using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using Ghost.Engine.Streaming;


using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

[GenerateShaderProperty("Internal/UpdateGPUScene")]
public partial struct UpdateGPUSceneShaderProperty
{
    public uint gpuSceneBuffer;
    public uint updateBuffer;
    public uint updateCount;
    public uint removeBuffer;
    public uint removeCount;
}

internal partial class GhostRenderPipeline
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Includes/Generated/GhostRenderPipeline.hlsl")]
    private struct UpdateInstanceData
    {
        public float4x4 localToWorld;
        public uint instanceID;
        public uint meshBuffer;
        public uint materialPaletteIndex;
        public uint renderingLayerMask;
        public uint shadowCastingMode;
        public uint pad0;
        public uint pad1;
        public uint pad2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Includes/Generated/GhostRenderPipeline.hlsl")]
    private struct RemoveInstanceData
    {
        public uint instanceID;
        public uint swapWithInstanceID;
    }

    private unsafe Handle<GPUBuffer> CreateUpdateInstanceBuffer(GhostRenderPayload ghostPayload, ResourceManager resourceManager, IResourceDatabase resourceDatabase, out int count)
    {
        // TODO: This should also include update requests like transform update, material update, etc.
        var totalUpdateCount = ghostPayload.UpdateRequest.Count;

        if (!ghostPayload.UpdateRequest.IsEmpty)
        {
            var addDesc = new BufferDesc
            {
                Size = (nuint)ghostPayload.UpdateRequest.Count * MemoryUtility.SizeOf<UpdateInstanceData>(),
                Stride = (uint)MemoryUtility.SizeOf<UpdateInstanceData>(),
                Usage = BufferUsage.Structured | BufferUsage.ShaderResource,
                HeapType = HeapType.Upload
            };

            var addBuffer = resourceManager.CreateTransientBuffer(in addDesc, "Add Instance Buffer");
            var pAddData = (UpdateInstanceData*)resourceDatabase.MapResource(addBuffer.AsResource(), 0, null);

            var i = 0;
            while (ghostPayload.UpdateRequest.TryDequeue(out var addRequest))
            {
                var (mesh, error) = resourceManager.GetMeshReference(addRequest.meshInstance.mesh);
                if (error.IsFailure)
                {
                    Logger.Error($"Failed to get mesh reference for mesh instance with ID {addRequest.instanceId}");
                    continue;
                }

                var meshBufferIndex = resourceDatabase.GetBindlessIndex(mesh.Get().MeshDataBuffer.AsResource());
                if (meshBufferIndex == uint.MaxValue)
                {
                    Logger.Warning($"MeshDataBuffer bindless index for instance {addRequest.instanceId} is invalid (0xFFFFFFFF).");
                }

                pAddData[i] = new UpdateInstanceData
                {
                    localToWorld = addRequest.localToWorld,
                    instanceID = addRequest.instanceId,
                    meshBuffer = meshBufferIndex,
                    materialPaletteIndex = (uint)addRequest.meshInstance.materialPalette.Value,
                    renderingLayerMask = addRequest.meshInstance.renderingLayerMask,
                    shadowCastingMode = (uint)addRequest.meshInstance.shadowCastingMode
                };

                SetCPUInstance(addRequest.instanceId, addRequest.meshInstance.mesh, resourceManager.GetMaterialPaletteMaterial(addRequest.meshInstance.materialPalette, 0));

                i++;
            }

            resourceDatabase.UnmapResource(addBuffer.AsResource(), 0, null);

            count = i;
            return addBuffer;
        }

        count = 0;
        return default;
    }

    private unsafe Handle<GPUBuffer> CreateRemoveInstanceBuffer(GhostRenderPayload ghostPayload, ResourceManager resourceManager, IResourceDatabase resourceDatabase, out int count)
    {
        if (!ghostPayload.RemoveRequest.IsEmpty)
        {
            var addDesc = new BufferDesc
            {
                Size = (nuint)ghostPayload.RemoveRequest.Count * MemoryUtility.SizeOf<RemoveInstanceData>(),
                Stride = (uint)MemoryUtility.SizeOf<RemoveInstanceData>(),
                Usage = BufferUsage.Structured | BufferUsage.ShaderResource,
                HeapType = HeapType.Upload
            };

            var removeBuffer = resourceManager.CreateTransientBuffer(in addDesc, "Remove Instance Buffer");
            var pRemoveData = (RemoveInstanceData*)resourceDatabase.MapResource(removeBuffer.AsResource(), 0, null);

            var i = 0;
            while (ghostPayload.RemoveRequest.TryDequeue(out var removeRequest))
            {
                pRemoveData[i] = new RemoveInstanceData
                {
                    instanceID = removeRequest.instanceId,
                    swapWithInstanceID = removeRequest.swapWithInstanceId
                };

                RemoveCPUInstance(removeRequest.instanceId, removeRequest.swapWithInstanceId);

                i++;
            }

            resourceDatabase.UnmapResource(removeBuffer.AsResource(), 0, null);

            count = i;
            return removeBuffer;
        }

        count = 0;
        return default;
    }

    private void SetCPUInstance(uint instanceId, Handle<Mesh> mesh, Handle<Material> material)
    {
        if (instanceId >= _instanceInfos.Length)
        {
            Array.Resize(ref _instanceInfos, Math.Max(_instanceInfos.Length * 2, (int)instanceId + 1));
        }

        _instanceInfos[instanceId] = new CPUInstanceInfo
        {
            mesh = mesh,
            material = material
        };
    }

    private void RemoveCPUInstance(uint instanceId, uint swapWithInstanceId)
    {
        if (instanceId < _instanceInfos.Length && swapWithInstanceId < _instanceInfos.Length)
        {
            _instanceInfos[instanceId] = _instanceInfos[swapWithInstanceId];
            _instanceInfos[swapWithInstanceId] = default;
        }
    }

    private Handle<ComputeShader> _updateGPUSceneShader = Handle<ComputeShader>.Invalid;
    private int _lastGpuUpdateProbe = -1;

    public void Initialize(AssetManager assetManager)
    {
        _assetManager = assetManager;
        var entry = assetManager.ResolveAsset("EngineResources/Shaders/UpdateGPUScene");
        entry.ReadAssetData(ref _updateGPUSceneShader);
    }

    private void UpdateGPUScene(RenderContext ctx, GhostRenderPayload payload)
    {
        void LogProbe(int state, string message)
        {
            if (_lastGpuUpdateProbe == state)
            {
                return;
            }

            _lastGpuUpdateProbe = state;
            Logger.Info($"GPU scene probe: {message}");
        }
        _gpuScene.ResizeIfNeeded(ctx.CommandBuffer);

        if (!_updateGPUSceneShader.IsValid)
        {
            LogProbe(0, "compute handle invalid.");
            Logger.Warning("UpdateGPUScene shader handle is invalid. Skipping GPU scene update.");
            return;
        }

        var shaderRef = ctx.ResourceManager.GetComputeShaderReference(_updateGPUSceneShader);
        if (shaderRef.IsFailure)
        {
            LogProbe(1, "compute handle lookup failed.");
            return;
        }

        var (compiledHash, error) = ctx.ShaderLibrary.GetCompiledHash(shaderRef.Value.UniqueID, 0);
        if (error.IsFailure)
        {
            LogProbe(2, "compute bytecode unavailable.");
            // Compute shader is not compiled/ready yet; keep update requests in queue.
            return;
        }

        var updateBuffer = CreateUpdateInstanceBuffer(payload, ctx.ResourceManager, ctx.ResourceDatabase, out var updateCount);
        var removeBuffer = CreateRemoveInstanceBuffer(payload, ctx.ResourceManager, ctx.ResourceDatabase, out var removeCount);

        if (updateCount <= 0 && removeCount <= 0)
        {
            LogProbe(3, "compute ready, no pending updates.");
            Logger.DebugAssert(updateBuffer.IsInvalid && removeBuffer.IsInvalid, "Buffers should be invalid when there are no updates.");
            return; // No updates needed
        }

        var property = new UpdateGPUSceneShaderProperty
        {
            gpuSceneBuffer = ctx.ResourceDatabase.GetBindlessIndex(_gpuScene.SceneBuffer.AsResource(), BindlessAccess.UnorderedAccess),
            updateBuffer = updateBuffer.IsValid ? ctx.ResourceDatabase.GetBindlessIndex(updateBuffer.AsResource()) : 0,
            updateCount = (uint)updateCount,
            removeBuffer = removeBuffer.IsValid ? ctx.ResourceDatabase.GetBindlessIndex(removeBuffer.AsResource()) : 0,
            removeCount = (uint)removeCount
        };

        var maxCount = Math.Max(updateCount, removeCount);
        var threadGroups = new uint3((uint)Math.Ceiling(maxCount / 64.0), 1, 1);

        LogProbe(4, $"dispatching updates={updateCount}, removes={removeCount}.");
        ctx.DispatchCompute(_updateGPUSceneShader, 0, in property, threadGroups);

        ctx.CommandBuffer.Barrier(BarrierDesc.Buffer(
            _gpuScene.SceneBuffer,
            BarrierSync.ComputeShading,
            BarrierSync.AllShading,
            BarrierAccess.UnorderedAccess,
            BarrierAccess.ShaderResource));
    }
}
