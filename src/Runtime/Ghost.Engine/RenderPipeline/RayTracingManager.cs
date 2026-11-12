using Ghost.Core;
using Ghost.Engine.Utilities;
using Ghost.Engine.Volumes;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

public struct TLASBuildSettings
{
    public Frustum frustum;
    public float3 cameraPosition;
    public quaternion cameraRotation;
    public RayTracingCullMode cullMode;
    public float sphereCullDistance;
    public float solidAngleThreshold;
}

public struct BuildTLASPassData
{
    public RayTracingManager rayTracingManager;
    public TLASBuildSettings settings;
    public uint activeInstanceCount;
    public Identifier<RGBuffer> instanceDescBuffer;
    public Identifier<RGBuffer> scratchBuffer;
    public Identifier<RGBuffer> tlasBuffer;
}

public sealed unsafe class RayTracingManager : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct InstanceRecord
    {
        public float4x4 localToWorld;
        public Handle<Mesh> mesh;
    }

    private struct InstanceCullingJob : IJobParallelFor
    {
        public ReadOnlyView<InstanceRecord> instances;
        public ResourceManager resourceManager;
        public TLASBuildSettings settings;
        public OBB extendedFrustum;
        public float maxDistSq;
        public float solidAngle;

        public uint* pSurvivingCount;
        public RaytracingInstanceDesc* pMapped;

        public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
        {
            ref readonly var inst = ref instances[loopIndex];
            if (!inst.mesh.IsValid)
            {
                return;
            }

            var meshRes = resourceManager.GetMeshReference(inst.mesh);
            if (meshRes.IsFailure || meshRes.Value.ProxyBlasGpuAddress == 0)
            {
                return;
            }

            ref readonly var mesh = ref meshRes.Value;
            var center = (mesh.BoundingBox.Min + mesh.BoundingBox.Max) * 0.5f;
            var localRadius = math.length(mesh.BoundingBox.Max - center);
            if (localRadius < 0.001f)
            {
                localRadius = 1.0f;
            }

            var scale = inst.localToWorld.GetScale();
            var maxScale = math.max(scale.x, math.max(scale.y, scale.z));
            var worldRadius = localRadius * maxScale;
            var worldRadiusSq = worldRadius * worldRadius;

            var worldCenter = math.mul(inst.localToWorld, new float4(center, 1.0f)).xyz;
            var rwsCenter = worldCenter - settings.cameraPosition;
            var distSq = math.lengthsq(rwsCenter);

            switch (settings.cullMode)
            {
                case RayTracingCullMode.ExtendedFrustum:
                    var sphere = new SphereBounds(rwsCenter, worldRadius);
                    if (!extendedFrustum.Overlaps(sphere))
                    {
                        return;
                    }
                    break;
                case RayTracingCullMode.Sphere:
                    var maxAllowedDist = settings.sphereCullDistance + worldRadius;
                    if (distSq > maxAllowedDist * maxAllowedDist)
                    {
                        return;
                    }
                    break;
                case RayTracingCullMode.SolidAngle:
                    if (worldRadiusSq < distSq * solidAngle)
                    {
                        return;
                    }
                    break;
            }

            var id = Interlocked.Increment(ref *pSurvivingCount) - 1;

            ref var desc = ref pMapped[id];
            var localToRWS = inst.localToWorld;
            localToRWS.c3.xyz -= settings.cameraPosition;
            desc.SetTransform(in localToRWS);
            desc.InstanceID = (uint)loopIndex;
            desc.InstanceMask = 0xFF;
            desc.HitGroupIndex = 0;
            desc.Flags = RaytracingInstanceFlags.ForceOpaque;
            desc.accelerationStructure = meshRes.Value.ProxyBlasGpuAddress;
        }
    }

    private readonly IRenderDevice _renderDevice;
    private readonly IResourceAllocator _resourceAllocator;
    private readonly IResourceDatabase _resourceDatabase;
    private readonly ResourceManager _resourceManager;
    private readonly JobScheduler _jobScheduler;

    private UnsafeList<InstanceRecord> _instances;
    private UnsafeList<Handle<Mesh>> _pendingBlasBuilds;

    private Handle<GPUBuffer> _sharedBlasScratchBuffer;
    private ulong _sharedBlasScratchCapacity;

    private bool _disposed;

    public RayTracingManager(IRenderDevice device, IResourceAllocator resourceAllocator, IResourceDatabase resourceDatabase, ResourceManager resourceManager, JobScheduler jobScheduler)
    {
        _renderDevice = device;
        _resourceAllocator = resourceAllocator;
        _resourceDatabase = resourceDatabase;
        _resourceManager = resourceManager;
        _jobScheduler = jobScheduler;

        _instances = new UnsafeList<InstanceRecord>(1024, AllocationHandle.Persistent);
        _pendingBlasBuilds = new UnsafeList<Handle<Mesh>>(64, AllocationHandle.Persistent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Align256(ulong size) => (size + 255ul) & ~255ul;

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

            var meshBufferGpuVA = _resourceDatabase.GetGPUVirtualAddress(mesh.MeshBuffer.AsResource());
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

            var prebuildInfo = _renderDevice.GetAccelerationStructurePrebuildInfo(in inputs);
            var alignedResultSize = Align256(prebuildInfo.resultDataMaxSizeInBytes);
            var alignedScratchSize = Align256(prebuildInfo.scratchDataSizeInBytes);

            var blasDesc = new BufferDesc
            {
                Size = alignedResultSize,
                Stride = 1,
                Usage = BufferUsage.AccelerationStructure,
                HeapType = HeapType.Default
            };

            var blasBuffer = _resourceAllocator.CreateBuffer(in blasDesc, $"BLAS_Mesh_{meshHandle.ID}");
            var blasGpuVA = _resourceDatabase.GetGPUVirtualAddress(blasBuffer.AsResource());

            EnsureBlasScratchBuffer(alignedScratchSize);
            var scratchGpuVA = _resourceDatabase.GetGPUVirtualAddress(_sharedBlasScratchBuffer.AsResource());

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
            cmd.Barrier(BarrierDesc.Buffer(blasBuffer,
                BarrierSync.BuildRaytracingAccelerationStructure, BarrierSync.BuildRaytracingAccelerationStructure | BarrierSync.AllShading,
                BarrierAccess.RaytracingAccelerationStructureWrite, BarrierAccess.RaytracingAccelerationStructureRead, force: true));

            mesh.RayTracingProxyBlas = blasBuffer;
            mesh.ProxyBlasGpuAddress = blasGpuVA;
        }

        _pendingBlasBuilds.Clear();
    }

    public void BuildTLAS(
        in TLASBuildSettings settings,
        IComputeRenderContext computeCtx,
        Identifier<RGBuffer> instanceDescBuffer,
        Identifier<RGBuffer> scratchBuffer,
        Identifier<RGBuffer> tlasBuffer,
        uint activeInstanceCount)
    {
        var count = Math.Min((int)activeInstanceCount, _instances.Count);
        if (count == 0)
        {
            return;
        }

        var instanceDescResource = computeCtx.GetActualBuffer(instanceDescBuffer).AsResource();
        var pMapped = (RaytracingInstanceDesc*)computeCtx.ResourceDatabase.MapResource(instanceDescResource, 0, null);
        var survivingCount = 0u;

        var maxDistSq = settings.sphereCullDistance * settings.sphereCullDistance;
        var extendedFrustum = settings.frustum.GetOBB(settings.cameraRotation);
        extendedFrustum.Extents = new float3(extendedFrustum.Extents.xy, extendedFrustum.Extents.z * 2.0f);
        extendedFrustum.Center = float3.zero;
        var solidAngle = math.square(math.tan(settings.solidAngleThreshold / 2.0f));

        var job = new InstanceCullingJob
        {
            instances = _instances.AsReadOnly(),
            resourceManager = _resourceManager,
            settings = settings,
            extendedFrustum = extendedFrustum,
            maxDistSq = maxDistSq,
            solidAngle = solidAngle,
            pSurvivingCount = &survivingCount,
            pMapped = pMapped
        };

        var handle = _jobScheduler.ScheduleParallelFor(ref job, count, 128, JobPriority.High);
        _jobScheduler.Wait(handle);

        computeCtx.ResourceDatabase.UnmapResource(instanceDescResource, 0, null);

        if (survivingCount == 0)
        {
            return;
        }

        var instanceDescsVA = computeCtx.ResourceDatabase.GetGPUVirtualAddress(instanceDescResource);
        var tlasGpuVA = computeCtx.ResourceDatabase.GetGPUVirtualAddress(computeCtx.GetActualBuffer(tlasBuffer).AsResource());
        var scratchGpuVA = computeCtx.ResourceDatabase.GetGPUVirtualAddress(computeCtx.GetActualBuffer(scratchBuffer).AsResource());

        var inputs = new AccelerationStructureBuildInputs
        {
            type = AccelerationStructureType.TopLevel,
            flags = AccelerationStructureBuildFlags.PreferFastTrace,
            numDescs = survivingCount,
            instanceDescs = instanceDescsVA
        };

        var buildDesc = new BuildAccelerationStructureDesc
        {
            inputs = inputs,
            destinationAddress = tlasGpuVA,
            scratchAddress = scratchGpuVA,
            sourceAddress = 0
        };

        computeCtx.BuildRaytracingAccelerationStructure(in buildDesc);
    }

    private void EnsureBlasScratchBuffer(ulong requiredSize)
    {
        if (_sharedBlasScratchBuffer.IsValid && _sharedBlasScratchCapacity >= requiredSize)
        {
            return;
        }

        if (_sharedBlasScratchBuffer.IsValid)
        {
            _resourceDatabase.ReleaseResource(_sharedBlasScratchBuffer.AsResource());
        }

        _sharedBlasScratchCapacity = Math.Max(requiredSize, Math.Max(_sharedBlasScratchCapacity * 2, 65536ul));
        var desc = new BufferDesc
        {
            Size = _sharedBlasScratchCapacity,
            Stride = 1,
            Usage = BufferUsage.UnorderedAccess,
            HeapType = HeapType.Default
        };

        _sharedBlasScratchBuffer = _resourceAllocator.CreateBuffer(in desc, "BLAS_ScratchBuffer");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _instances.Dispose();
        _pendingBlasBuilds.Dispose();

        if (_sharedBlasScratchBuffer.IsValid)
        {
            _resourceDatabase.ReleaseResource(_sharedBlasScratchBuffer.AsResource());
        }

        _disposed = true;
    }
}
