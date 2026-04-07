using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.Components;
using Ghost.Engine.Systems;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.Test.RenderPipeline;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.Test.Systems;

[RenderPipelineSystem<TestRenderPipelineSettings>]
[UpdateAfter<CameraMovingSystem>]
public class RenderExtractionSystem : ISystem
{
    private RenderSystem _renderSystem = null!;

    private Identifier<EntityQuery> _cameraQueryID;
    private Identifier<EntityQuery> _meshQueryID;

    public void Initialize(ref readonly SystemAPI systemAPI)
    {
        _renderSystem = systemAPI.World.GetService<RenderSystem>();

        var builder = new QueryBuilder();

        _cameraQueryID = builder
            .WithAll<Camera, LocalToWorld>()
            .Build(systemAPI.World, false);

        builder.Clear();

        _meshQueryID = builder
            .WithAll<MeshInstance, LocalToWorld>()
            .Build(systemAPI.World, true);
    }

    public void Update(ref readonly SystemAPI systemAPI)
    {
        if (_meshQueryID.IsInvalid)
        {
            return;
        }

        ref var cameraQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_cameraQueryID);
        ref var meshQuery = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_meshQueryID);

        foreach (var (cam, camLtw) in cameraQuery.GetComponentIterator<Camera, LocalToWorld>())
        {
            ref readonly var camRef = ref cam.Get();
            ref readonly var camLtwRef = ref camLtw.Get();

            // TODO: Classify transparent objects into a separate render list and render via oit.
            var renderList = new RenderList(1, 64, Allocator.FreeList);
            var transparentRenderList = new RenderList(1, 64, Allocator.FreeList);
            var shadowCasterRenderList = new RenderList(1, 64, Allocator.FreeList);

            // TODO: This chould be done in earallel jobs.
            foreach (var chunk in meshQuery.GetChunkIterator())
            {
                var meshInstances = chunk.GetComponentData<MeshInstance>();
                var localToWorlds = chunk.GetComponentData<LocalToWorld>();

                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref readonly var meshInstance = ref meshInstances[i];
                    if ((meshInstance.renderingLayerMask & camRef.renderingLayerMask) == 0u)
                    {
                        // Not in the same rendering layer, skip.
                        continue;
                    }

                    ref readonly var meshLtw = ref localToWorlds[i];

                    var meshPosition = meshLtw.matrix.c3.xyz;
                    var camPosition = camLtwRef.matrix.c3.xyz;
                    var distance = math.distance(meshPosition, camPosition);

                    // TODO: Use bounding sphere or AABB for better culling. Currently it just uses the pivot point which can cause popping when the pivot is far from the actual geometry.
                    if (distance < camRef.nearClipPlane || distance > camRef.farClipPlane)
                    {
                        continue;
                    }

                    if (meshInstance.shadowCastingMode != ShadowCastingMode.ShadowsOnly)
                    {
                        renderList.Add(new RenderRecord
                        {
                            localToWorld = meshLtw.matrix,
                            mesh = meshInstance.mesh,
                            materialPalette = meshInstance.materialPalette,
                            renderingLayerMask = meshInstance.renderingLayerMask,
                        }, 0);
                    }

                    if (meshInstance.shadowCastingMode != ShadowCastingMode.Off)
                    {
                        shadowCasterRenderList.Add(new RenderRecord
                        {
                            localToWorld = meshLtw.matrix,
                            mesh = meshInstance.mesh,
                            materialPalette = meshInstance.materialPalette,
                            renderingLayerMask = meshInstance.renderingLayerMask,
                        }, 0);
                    }
                }
            }

            var request = new RenderRequest
            {
                swapChainIndex = camRef.swapChainIndex,
                colorTarget = camRef.colorTarget,
                depthTarget = camRef.depthTarget,
                opaqueRenderList = renderList,
                shadowCasterRenderList = shadowCasterRenderList,
                transparentRenderList = transparentRenderList,
                view = new RenderView
                {
                    localToWorld = camLtwRef.matrix,
                    //viewMatrix = viewMatrix,
                    //projectionMatrix = projectionMatrix,
                    //position = camLtwRef.matrix.c3.xyz,

                    //frustum = frustum,
                    nearClipPlane = camRef.nearClipPlane,
                    farClipPlane = camRef.farClipPlane,

                    sensorSize = camRef.sensorSize,
                    gateFit = camRef.gateFit,
                    iso = camRef.iso,
                    shutterSpeed = camRef.shutterSpeed,
                    aperture = camRef.aperture,
                    focalLength = camRef.focalLength,
                    focusDistance = camRef.focusDistance,

                    renderingLayerMask = camRef.renderingLayerMask,
                },
            };

            ((TestRenderPayload)_renderSystem.GetCurrentFramePayload()).renderRequests.Add(request);
        }
    }

    public void Cleanup(ref readonly SystemAPI systemAPI)
    {
    }
}
