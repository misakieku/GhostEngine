using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

public sealed unsafe class RayTracingManager : IDisposable
{
    public const float DEFAULT_RAY_TRACING_CULL_DISTANCE_METERS = 100.0f;
    public const float DEFAULT_SOLID_ANGLE_THRESHOLD = 0.0005f;
    public const float DEFAULT_NEAR_CULL_DISTANCE_METERS = 5.0f;
    public const float DEFAULT_MAX_CULL_DISTANCE_METERS = 300.0f;

    [StructLayout(LayoutKind.Sequential)]
    internal struct InstanceRecord
    {
        public float4x4 localToWorld;
        public Handle<Mesh> mesh;
    }

    private readonly IGraphicsEngine _graphicsEngine;
    private readonly ResourceManager _resourceManager;

    private UnsafeList<InstanceRecord> _instances;
    private UnsafeList<Handle<Mesh>> _pendingBlasBuilds;

    private Handle<GPUBuffer> _tlasBuffer;
    private Handle<GPUBuffer> _tlasScratchBuffer;
    private Handle<GPUBuffer> _instanceDescBuffer;
    private ulong _tlasBufferCapacity;
    private ulong _tlasScratchCapacity;
    private ulong _instanceDescCapacity;

    private Handle<GPUBuffer> _sharedBlasScratchBuffer;
    private ulong _sharedBlasScratchCapacity;

    private uint _tlasBindlessIndex;
    private bool _disposed;

    public float CullingDistance = DEFAULT_RAY_TRACING_CULL_DISTANCE_METERS;
    public float SolidAngleThreshold = DEFAULT_SOLID_ANGLE_THRESHOLD;
    public float NearCullDistance = DEFAULT_NEAR_CULL_DISTANCE_METERS;
    public float MaxCullDistance = DEFAULT_MAX_CULL_DISTANCE_METERS;
    public uint TLASBindlessIndex => _tlasBindlessIndex;
    public Handle<GPUBuffer> TLASBuffer => _tlasBuffer;

    public RayTracingManager(IGraphicsEngine graphicsEngine, ResourceManager resourceManager)
    {
        _graphicsEngine = graphicsEngine;
        _resourceManager = resourceManager;

        _instances = new UnsafeList<InstanceRecord>(1024, AllocationHandle.Persistent);
        _pendingBlasBuilds = new UnsafeList<Handle<Mesh>>(64, AllocationHandle.Persistent);
    }

    public void OnInstanceUpdated(uint instanceId, in float4x4 localToWorld, Handle<Mesh> mesh)
    {
        if ((int)instanceId >= _instances.Capacity)
        {
            var newCap = Math.Max((int)instanceId + 1, _instances.Capacity * 2);
            _instances.Resize(newCap);
        }

        if ((int)instanceId >= _instances.Count)
        {
            _instances.UnsafeSetCount((int)instanceId + 1);
        }

        _instances[(int)instanceId] = new InstanceRecord
        {
            localToWorld = localToWorld,
            mesh = mesh
        };

        if (mesh.IsValid)
        {
            var meshRes = _resourceManager.GetMeshReference(mesh);
            if (meshRes.IsSuccess && meshRes.Value.RayTracingProxyVertexCount > 0 && !meshRes.Value.RayTracingProxyBlas.IsValid)
            {
                var alreadyQueued = false;
                for (var i = 0; i < _pendingBlasBuilds.Count; i++)
                {
                    if (_pendingBlasBuilds[i] == mesh)
                    {
                        alreadyQueued = true;
                        break;
                    }
                }

                if (!alreadyQueued)
                {
                    _pendingBlasBuilds.Add(mesh);
                }
            }
        }
    }

    public void OnInstanceRemoved(uint instanceId, uint swapWithInstanceId)
    {
        if ((int)instanceId < _instances.Count && (int)swapWithInstanceId < _instances.Count)
        {
            _instances[(int)instanceId] = _instances[(int)swapWithInstanceId];
        }
    }

    public void BuildPendingBlas(ICommandBuffer cmd)
    {
        if (_pendingBlasBuilds.Count == 0)
        {
            return;
        }

        for (var i = 0; i < _pendingBlasBuilds.Count; i++)
        {
            var meshHandle = _pendingBlasBuilds[i];
            var meshRes = _resourceManager.GetMeshReference(meshHandle);
            if (meshRes.IsFailure)
            {
                continue;
            }

            ref var mesh = ref meshRes.Value;
            if (mesh.RayTracingProxyVertexCount == 0 || mesh.RayTracingProxyBlas.IsValid)
            {
                continue;
            }

            var meshBufferGpuVA = _graphicsEngine.ResourceDatabase.GetGPUVirtualAddress(mesh.MeshBuffer.AsResource());
            if (meshBufferGpuVA == 0)
            {
                continue;
            }

            var vertexVA = meshBufferGpuVA + mesh.RayTracingProxyVertexBufferOffset;
            var indexVA = meshBufferGpuVA + mesh.RayTracingProxyIndexBufferOffset;

            var geomDesc = new RaytracingGeometryDesc
            {
                type = RaytracingGeometryType.Triangles,
                flags = RaytracingGeometryFlags.Opaque,
                triangles = new RaytracingGeometryTrianglesDesc
                {
                    transform3x4 = 0,
                    indexFormat = IndexType.UInt32,
                    vertexFormat = TextureFormat.R32G32B32_Float,
                    indexCount = (uint)mesh.RayTracingProxyIndexCount,
                    vertexCount = (uint)mesh.RayTracingProxyVertexCount,
                    indexBuffer = indexVA,
                    vertexBuffer = vertexVA,
                    vertexStride = (uint)sizeof(float3)
                }
            };

            var inputs = new AccelerationStructureBuildInputs
            {
                type = AccelerationStructureType.BottomLevel,
                flags = AccelerationStructureBuildFlags.PreferFastTrace,
                numDescs = 1,
                pGeometries = &geomDesc
            };

            var prebuildInfo = _graphicsEngine.Device.GetAccelerationStructurePrebuildInfo(in inputs);
            var alignedResultSize = Align256(prebuildInfo.resultDataMaxSizeInBytes);
            var alignedScratchSize = Align256(prebuildInfo.scratchDataSizeInBytes);

            var blasDesc = new BufferDesc
            {
                Size = alignedResultSize,
                Stride = 1,
                Usage = BufferUsage.AccelerationStructure,
                HeapType = HeapType.Default
            };

            var blasBuffer = _graphicsEngine.ResourceAllocator.CreateBuffer(in blasDesc, $"BLAS_Mesh_{meshHandle.ID}");
            var blasGpuVA = _graphicsEngine.ResourceDatabase.GetGPUVirtualAddress(blasBuffer.AsResource());

            EnsureBlasScratchBuffer(alignedScratchSize);
            var scratchGpuVA = _graphicsEngine.ResourceDatabase.GetGPUVirtualAddress(_sharedBlasScratchBuffer.AsResource());

            cmd.Barrier(
                BarrierDesc.Buffer(blasBuffer, BarrierSync.None, BarrierSync.BuildRaytracingAccelerationStructure, BarrierAccess.NoAccess, BarrierAccess.RaytracingAccelerationStructureWrite, force: true),
                BarrierDesc.Buffer(_sharedBlasScratchBuffer, BarrierSync.All, BarrierSync.BuildRaytracingAccelerationStructure, BarrierAccess.Common, BarrierAccess.UnorderedAccess, force: true)
            );

            var buildDesc = new BuildAccelerationStructureDesc
            {
                inputs = inputs,
                destinationAddress = blasGpuVA,
                scratchAddress = scratchGpuVA,
                sourceAddress = 0
            };

            cmd.BuildRaytracingAccelerationStructure(in buildDesc);

            cmd.Barrier(
                BarrierDesc.Buffer(blasBuffer, BarrierSync.BuildRaytracingAccelerationStructure, BarrierSync.BuildRaytracingAccelerationStructure | BarrierSync.AllShading, BarrierAccess.RaytracingAccelerationStructureWrite, BarrierAccess.RaytracingAccelerationStructureRead, force: true)
            );

            mesh.RayTracingProxyBlas = blasBuffer;
            mesh.ProxyBlasGpuAddress = blasGpuVA;
        }

        _pendingBlasBuilds.Clear();
        cmd.InsertFullPipelineBarrier();
    }

    public void BuildTLAS(ICommandBuffer cmd, in float3 cameraPos, uint activeInstanceCount)
    {
        var count = Math.Min((int)activeInstanceCount, _instances.Count);
        if (count == 0)
        {
            _tlasBindlessIndex = 0;
            return;
        }

        var maxPossible = Math.Max(count, 1);
        EnsureInstanceDescBuffer((ulong)maxPossible * (ulong)sizeof(RaytracingInstanceDesc));

        var pMapped = (RaytracingInstanceDesc*)_graphicsEngine.ResourceDatabase.MapResource(_instanceDescBuffer.AsResource(), 0, null);
        var survivingCount = 0u;
        var nearDistSq = NearCullDistance * NearCullDistance;
        var maxDistSq = MaxCullDistance * MaxCullDistance;

        for (var i = 0; i < count; i++)
        {
            ref readonly var inst = ref _instances[i];
            if (!inst.mesh.IsValid)
            {
                continue;
            }

            var meshRes = _resourceManager.GetMeshReference(inst.mesh);
            if (meshRes.IsFailure || meshRes.Value.ProxyBlasGpuAddress == 0)
            {
                continue;
            }

            ref readonly var mesh = ref meshRes.Value;
            var center = (mesh.BoundingBox.Min + mesh.BoundingBox.Max) * 0.5f;
            var localRadius = math.length(mesh.BoundingBox.Max - center);
            if (localRadius < 0.001f)
            {
                localRadius = 1.0f;
            }

            var scaleX = math.length(inst.localToWorld.c0.xyz);
            var scaleY = math.length(inst.localToWorld.c1.xyz);
            var scaleZ = math.length(inst.localToWorld.c2.xyz);
            var maxScale = math.max(scaleX, math.max(scaleY, scaleZ));
            var worldRadius = localRadius * maxScale;
            var worldRadiusSq = worldRadius * worldRadius;

            var worldCenter = math.mul(inst.localToWorld, new float4(center, 1.0f)).xyz;
            var distSq = math.distancesq(worldCenter, cameraPos);

            if (distSq > maxDistSq)
            {
                continue;
            }

            if (distSq > nearDistSq && worldRadiusSq < distSq * SolidAngleThreshold)
            {
                continue;
            }

            ref var desc = ref pMapped[survivingCount];
            desc.SetTransform(in inst.localToWorld);
            desc.InstanceID = (uint)i;
            desc.InstanceMask = 0xFF;
            desc.HitGroupIndex = 0;
            desc.Flags = RaytracingInstanceFlags.ForceOpaque;
            desc.accelerationStructure = meshRes.Value.ProxyBlasGpuAddress;

            survivingCount++;
        }

        _graphicsEngine.ResourceDatabase.UnmapResource(_instanceDescBuffer.AsResource(), 0, null);

        if (survivingCount == 0)
        {
            _tlasBindlessIndex = 0;
            return;
        }

        var instanceDescsVA = _graphicsEngine.ResourceDatabase.GetGPUVirtualAddress(_instanceDescBuffer.AsResource());

        var inputs = new AccelerationStructureBuildInputs
        {
            type = AccelerationStructureType.TopLevel,
            flags = AccelerationStructureBuildFlags.PreferFastTrace,
            numDescs = survivingCount,
            instanceDescs = instanceDescsVA
        };

        var prebuildInfo = _graphicsEngine.Device.GetAccelerationStructurePrebuildInfo(in inputs);
        var alignedResultSize = Align256(prebuildInfo.resultDataMaxSizeInBytes);
        var alignedScratchSize = Align256(prebuildInfo.scratchDataSizeInBytes);

        EnsureTLASBuffers(alignedResultSize, alignedScratchSize);

        var tlasGpuVA = _graphicsEngine.ResourceDatabase.GetGPUVirtualAddress(_tlasBuffer.AsResource());
        var scratchGpuVA = _graphicsEngine.ResourceDatabase.GetGPUVirtualAddress(_tlasScratchBuffer.AsResource());

        cmd.Barrier(
            BarrierDesc.Buffer(_tlasBuffer, BarrierSync.All, BarrierSync.BuildRaytracingAccelerationStructure, BarrierAccess.Common, BarrierAccess.RaytracingAccelerationStructureWrite, force: true),
            BarrierDesc.Buffer(_tlasScratchBuffer, BarrierSync.All, BarrierSync.BuildRaytracingAccelerationStructure, BarrierAccess.Common, BarrierAccess.UnorderedAccess, force: true)
        );

        var buildDesc = new BuildAccelerationStructureDesc
        {
            inputs = inputs,
            destinationAddress = tlasGpuVA,
            scratchAddress = scratchGpuVA,
            sourceAddress = 0
        };

        cmd.BuildRaytracingAccelerationStructure(in buildDesc);

        cmd.Barrier(
            BarrierDesc.Buffer(_tlasBuffer, BarrierSync.BuildRaytracingAccelerationStructure, BarrierSync.AllShading, BarrierAccess.RaytracingAccelerationStructureWrite, BarrierAccess.RaytracingAccelerationStructureRead, force: true)
        );
        cmd.InsertFullPipelineBarrier();

        _tlasBindlessIndex = _graphicsEngine.ResourceDatabase.GetBindlessIndex(_tlasBuffer.AsResource());
    }

    private void EnsureBlasScratchBuffer(ulong requiredSize)
    {
        if (_sharedBlasScratchBuffer.IsValid && _sharedBlasScratchCapacity >= requiredSize)
        {
            return;
        }

        if (_sharedBlasScratchBuffer.IsValid)
        {
            _graphicsEngine.ResourceDatabase.ReleaseResource(_sharedBlasScratchBuffer.AsResource());
        }

        _sharedBlasScratchCapacity = Math.Max(requiredSize, Math.Max(_sharedBlasScratchCapacity * 2, 65536ul));
        var desc = new BufferDesc
        {
            Size = _sharedBlasScratchCapacity,
            Stride = 1,
            Usage = BufferUsage.UnorderedAccess,
            HeapType = HeapType.Default
        };

        _sharedBlasScratchBuffer = _graphicsEngine.ResourceAllocator.CreateBuffer(in desc, "BLAS_ScratchBuffer");
    }

    private void EnsureInstanceDescBuffer(ulong requiredSize)
    {
        if (_instanceDescBuffer.IsValid && _instanceDescCapacity >= requiredSize)
        {
            return;
        }

        if (_instanceDescBuffer.IsValid)
        {
            _graphicsEngine.ResourceDatabase.ReleaseResource(_instanceDescBuffer.AsResource());
        }

        _instanceDescCapacity = Math.Max(requiredSize, Math.Max(_instanceDescCapacity * 2, 65536ul));
        var desc = new BufferDesc
        {
            Size = _instanceDescCapacity,
            Stride = (uint)sizeof(RaytracingInstanceDesc),
            Usage = BufferUsage.ShaderResource,
            HeapType = HeapType.Upload
        };

        _instanceDescBuffer = _graphicsEngine.ResourceAllocator.CreateBuffer(in desc, "TLAS_InstanceDescBuffer");
    }

    private void EnsureTLASBuffers(ulong requiredResultSize, ulong requiredScratchSize)
    {
        if (!_tlasBuffer.IsValid || _tlasBufferCapacity < requiredResultSize)
        {
            if (_tlasBuffer.IsValid)
            {
                _graphicsEngine.ResourceDatabase.ReleaseResource(_tlasBuffer.AsResource());
            }

            _tlasBufferCapacity = Math.Max(requiredResultSize, Math.Max(_tlasBufferCapacity * 2, 65536ul));
            var desc = new BufferDesc
            {
                Size = _tlasBufferCapacity,
                Stride = 1,
                Usage = BufferUsage.AccelerationStructure | BufferUsage.ShaderResource,
                HeapType = HeapType.Default
            };

            _tlasBuffer = _graphicsEngine.ResourceAllocator.CreateBuffer(in desc, "Scene_TLAS");
        }

        if (!_tlasScratchBuffer.IsValid || _tlasScratchCapacity < requiredScratchSize)
        {
            if (_tlasScratchBuffer.IsValid)
            {
                _graphicsEngine.ResourceDatabase.ReleaseResource(_tlasScratchBuffer.AsResource());
            }

            _tlasScratchCapacity = Math.Max(requiredScratchSize, Math.Max(_tlasScratchCapacity * 2, 65536ul));
            var desc = new BufferDesc
            {
                Size = _tlasScratchCapacity,
                Stride = 1,
                Usage = BufferUsage.UnorderedAccess,
                HeapType = HeapType.Default
            };

            _tlasScratchBuffer = _graphicsEngine.ResourceAllocator.CreateBuffer(in desc, "TLAS_ScratchBuffer");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Align256(ulong size) => (size + 255ul) & ~255ul;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _instances.Dispose();
        _pendingBlasBuilds.Dispose();

        if (_tlasBuffer.IsValid)
        {
            _graphicsEngine.ResourceDatabase.ReleaseResource(_tlasBuffer.AsResource());
        }

        if (_tlasScratchBuffer.IsValid)
        {
            _graphicsEngine.ResourceDatabase.ReleaseResource(_tlasScratchBuffer.AsResource());
        }

        if (_instanceDescBuffer.IsValid)
        {
            _graphicsEngine.ResourceDatabase.ReleaseResource(_instanceDescBuffer.AsResource());
        }

        if (_sharedBlasScratchBuffer.IsValid)
        {
            _graphicsEngine.ResourceDatabase.ReleaseResource(_sharedBlasScratchBuffer.AsResource());
        }

        _disposed = true;
    }
}
