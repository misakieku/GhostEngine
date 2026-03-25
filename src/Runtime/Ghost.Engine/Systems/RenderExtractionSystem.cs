using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;

namespace Ghost.Engine.Systems;

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

        _meshQueryID = builder
            .WithAll<MeshInstance, LocalToWorld>()
            .Build(systemAPI.World, true);
    }

    private static float3 IntersectFrustumPlanes(float4 p0, float4 p1, float4 p2)
    {
        float3 n0 = p0.xyz;
        float3 n1 = p1.xyz;
        float3 n2 = p2.xyz;

        float det = math.dot(math.cross(n0, n1), n2);
        return (math.cross(n2, n1) * p0.w + math.cross(n0, n2) * p1.w - math.cross(n0, n1) * p2.w) * (1.0f / det);
    }

    private static Frustum CreateFrustum(Camera camRef, float4x4 vp, float3 viewDir, float3 viewPos)
    {
        var frustum = new Frustum();
        Frustum.CalculateFrustumPlanes(vp, ref frustum.planes);

        // We need to recalculate the near and far planes otherwise it does not work for oblique projection matrices used for reflection.
        var nearPlane = Plane.CreateFromUnitNormalAndPointInPlane(viewDir, viewPos);
        nearPlane.Distance -= camRef.nearClipPlane;

        var farPlane = Plane.CreateFromUnitNormalAndPointInPlane(-viewDir, viewPos);
        farPlane.Distance += camRef.farClipPlane;

        frustum.planes[4] = nearPlane;
        frustum.planes[5] = farPlane;

        frustum.corners[0] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[3], frustum.planes[4]);
        frustum.corners[1] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[3], frustum.planes[4]);
        frustum.corners[2] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[2], frustum.planes[4]);
        frustum.corners[3] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[2], frustum.planes[4]);
        frustum.corners[4] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[3], frustum.planes[5]);
        frustum.corners[5] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[3], frustum.planes[5]);
        frustum.corners[6] = IntersectFrustumPlanes(frustum.planes[0], frustum.planes[2], frustum.planes[5]);
        frustum.corners[7] = IntersectFrustumPlanes(frustum.planes[1], frustum.planes[2], frustum.planes[5]);
        return frustum;
    }

    public unsafe void Update(ref readonly SystemAPI systemAPI)
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

            var rtResult = _renderSystem.GraphicsEngine.ResourceDatabase.GetResourceDescription(camRef.colorTarget.AsResource());
            if (rtResult.IsFailure)
            {
                continue;
            }

            var rtSize = new uint2(rtResult.Value.TextureDescription.Width, rtResult.Value.TextureDescription.Height);
            var aspectScreen = (float)rtSize.x / rtSize.y;

            // TODO: Classify transparent objects into a separate render list and render via oit.
            var renderList = new RenderList(1, 64, Allocator.FreeList);
            var transparentRenderList = new RenderList(1, 64, Allocator.FreeList);
            var shadowCasterRenderList = new RenderList(1, 64, Allocator.FreeList);

            // TODO: This chould be done in parallel jobs.
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

            // NOTE: We assume camera's scale is always (1, 1, 1). Otherwise fastinverse will fail and we need to use regular inverse which is more expensive.
            var viewMatrix = math.fastinverse(camLtwRef.matrix);

            var vfov = 2.0f * math.atan(camRef.sensorSize.y / 2.0f * camRef.focalLength);
            var hfov = 2.0f * math.atan(camRef.sensorSize.x / 2.0f * camRef.focalLength);
            var aspectSensor = camRef.sensorSize.x / camRef.sensorSize.y;

            float vfovF;
            switch (camRef.gateFit)
            {
                case GateFit.Vertical:
                    vfovF = vfov;
                    break;

                case GateFit.Horizontal:
                    // Adjust VFOV so that the sensor width fits the screen width
                    var horizontalAspectBuffer = math.tan(hfov * 0.5f);
                    vfovF = 2.0f * math.atan(horizontalAspectBuffer / aspectScreen);
                    break;

                case GateFit.Fill:
                    if (aspectSensor > aspectScreen)
                    {
                        goto case GateFit.Vertical;
                    }
                    else
                    {
                        goto case GateFit.Horizontal;
                    }

                case GateFit.Overscan:
                    if (aspectSensor > aspectScreen)
                    {
                        goto case GateFit.Horizontal;
                    }
                    else
                    {
                        goto case GateFit.Vertical;
                    }
                default:
                    vfovF = vfov;
                    break;
            }

            var m_00 = 1.0f / aspectScreen * math.tan(vfovF * 0.5f);
            var m_11 = 1.0f / math.tan(vfovF * 0.5f);
            var m_22 = -(camRef.farClipPlane + camRef.nearClipPlane) / (camRef.farClipPlane - camRef.nearClipPlane);
            var m_23 = -(2.0f * camRef.farClipPlane * camRef.nearClipPlane) / (camRef.farClipPlane - camRef.nearClipPlane);

            var projectionMatrix = new float4x4
            (
                m_00, 0, 0, 0,
                0, m_11, 0, 0,
                0, 0, m_22, m_23,
                0, 0, -1, 0
            );

            var vp = math.mul(projectionMatrix, viewMatrix);
            var viewDir = math.normalize(camLtwRef.matrix.c2.xyz);
            var viewPos = camLtwRef.matrix.c3.xyz;
            var frustum = CreateFrustum(camRef, vp, viewDir, viewPos);

            var request = new RenderRequest
            {
                colorTarget = camRef.colorTarget,
                depthTarget = camRef.depthTarget,
                opaqueRenderList = renderList,
                shadowCasterRenderList = shadowCasterRenderList,
                transparentRenderList = transparentRenderList,
                renderFunc = camRef.renderFunc,
                view = new RenderView
                {
                    viewMatrix = viewMatrix,
                    projectionMatrix = projectionMatrix,
                    position = camLtwRef.matrix.c3.xyz,

                    frustum = frustum,
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

            _renderSystem.AddRenderRequest(request);
        }
    }

    public void Cleanup(ref readonly SystemAPI systemAPI)
    {
    }
}
