using Ghost.Engine.Components;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Studio.Rendering;

/// <summary>
/// Editor-only singleton component holding the Scene View camera state and GPU view handle.
/// Stored separately from scene entities so user Hierarchy queries never display the editor camera.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct EditorCamera : IComponentData
{
    public Camera camera;
    public float4x4 localToWorld;
    public float3 position;
    public float3 focusTarget;
    public float orbitDistance;
    public float yaw;
    public float pitch;
    public float moveSpeed;
    public uint viewId;

    public static EditorCamera Default
    {
        get
        {
            var cam = Camera.Default;
            cam.swapChainIndex = -1;
            cam.focalLength = 24.0f;

            var focus = float3.zero;
            var pos = new float3(2.8f, 2.0f, -4.5f);
            var toTarget = math.normalize(focus - pos);
            var yaw = math.atan2(toTarget.x, toTarget.z);
            var pitch = -math.asin(math.clamp(toTarget.y, -0.999f, 0.999f));

            var rotY = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), yaw);
            var rotX = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), pitch);
            var rot = math.mul(rotY, rotX);

            return new EditorCamera
            {
                camera = cam,
                localToWorld = MathUtility.TRS(pos, rot, new float3(1.0f)),
                position = pos,
                focusTarget = focus,
                orbitDistance = math.length(focus - pos),
                yaw = yaw,
                pitch = pitch,
                moveSpeed = 5.0f,
                viewId = uint.MaxValue,
            };
        }
    }
}
