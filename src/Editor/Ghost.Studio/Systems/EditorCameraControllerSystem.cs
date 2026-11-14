using Ghost.Editor.Core.Selection;
using Ghost.Engine.Components;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Studio.Rendering;
using Hexa.NET.ImGui;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio.Systems;

/// <summary>
/// Controls the <see cref="EditorCamera"/> singleton using Unity-style Scene View fly, orbit, pan, dolly, and frame navigation.
/// </summary>
[UpdateAfter<EditorImGuiSystem>]
[UpdateBefore<RenderSystemGroup>]
internal sealed unsafe class EditorCameraControllerSystem : SystemBase
{
    private const float LOOK_SENSITIVITY = 0.0032f;
    private const float ORBIT_SENSITIVITY = 0.0045f;
    private const float PAN_SENSITIVITY = 0.0022f;
    private static readonly float PITCH_LIMIT = math.radians(89.0f);

    private readonly EditorViewportInputState _inputState;

    public EditorCameraControllerSystem(EditorViewportInputState inputState)
    {
        _inputState = inputState;
    }

    protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        ref var editorCam = ref systemAPI.World.EntityManager.GetOrCreateSingleton(EditorCamera.Default);

        var dt = systemAPI.Time.DeltaTime;
        var io = ImGui.GetIO();

        var isRightDown = ImGui.IsMouseDown(ImGuiMouseButton.Right);
        var isMiddleDown = ImGui.IsMouseDown(ImGuiMouseButton.Middle);
        var isLeftDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
        var isAltDown = io.KeyAlt;
        var anyNavButton = isRightDown || isMiddleDown || (isLeftDown && isAltDown);

        if (!_inputState.CanControlCamera)
        {
            _inputState.IsNavigating = false;
            return;
        }

        if (_inputState.IsHovered && anyNavButton)
        {
            _inputState.IsNavigating = true;
        }
        else if (!anyNavButton)
        {
            _inputState.IsNavigating = false;
        }

        var rotY = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), editorCam.yaw);
        var rotX = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), editorCam.pitch);
        var rotation = math.mul(rotY, rotX);
        var right = math.mul(rotation, new float3(1.0f, 0.0f, 0.0f));
        var up = math.mul(rotation, new float3(0.0f, 1.0f, 0.0f));
        var forward = math.mul(rotation, new float3(0.0f, 0.0f, 1.0f));

        var delta = io.MouseDelta;

        if (_inputState.IsNavigating && isRightDown)
        {
            editorCam.yaw += delta.X * LOOK_SENSITIVITY;
            editorCam.pitch = math.clamp(editorCam.pitch + delta.Y * LOOK_SENSITIVITY, -PITCH_LIMIT, PITCH_LIMIT);

            rotY = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), editorCam.yaw);
            rotX = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), editorCam.pitch);
            rotation = math.mul(rotY, rotX);
            right = math.mul(rotation, new float3(1.0f, 0.0f, 0.0f));
            up = math.mul(rotation, new float3(0.0f, 1.0f, 0.0f));
            forward = math.mul(rotation, new float3(0.0f, 0.0f, 1.0f));

            var wheel = io.MouseWheel;
            if (MathF.Abs(wheel) > 0.001f)
            {
                editorCam.moveSpeed = math.clamp(editorCam.moveSpeed * (1.0f + wheel * 0.15f), 0.25f, 200.0f);
            }

            if (dt > 0.0f)
            {
                var moveDir = float3.zero;
                if (ImGui.IsKeyDown(ImGuiKey.W)) { moveDir += forward; }
                if (ImGui.IsKeyDown(ImGuiKey.S)) { moveDir -= forward; }
                if (ImGui.IsKeyDown(ImGuiKey.D)) { moveDir += right; }
                if (ImGui.IsKeyDown(ImGuiKey.A)) { moveDir -= right; }
                if (ImGui.IsKeyDown(ImGuiKey.E)) { moveDir += new float3(0.0f, 1.0f, 0.0f); }
                if (ImGui.IsKeyDown(ImGuiKey.Q)) { moveDir -= new float3(0.0f, 1.0f, 0.0f); }

                if (math.lengthsq(moveDir) > 1e-6f)
                {
                    var boost = io.KeyShift ? 3.0f : 1.0f;
                    var offset = math.normalize(moveDir) * (editorCam.moveSpeed * boost * dt);
                    editorCam.position += offset;
                    editorCam.focusTarget = editorCam.position + forward * editorCam.orbitDistance;
                }
            }
        }
        else if (_inputState.IsNavigating && isAltDown && isLeftDown)
        {
            editorCam.yaw += delta.X * ORBIT_SENSITIVITY;
            editorCam.pitch = math.clamp(editorCam.pitch + delta.Y * ORBIT_SENSITIVITY, -PITCH_LIMIT, PITCH_LIMIT);

            rotY = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), editorCam.yaw);
            rotX = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), editorCam.pitch);
            rotation = math.mul(rotY, rotX);
            forward = math.mul(rotation, new float3(0.0f, 0.0f, 1.0f));
            editorCam.position = editorCam.focusTarget - forward * editorCam.orbitDistance;
        }
        else if (_inputState.IsNavigating && isMiddleDown)
        {
            var panScale = editorCam.orbitDistance * PAN_SENSITIVITY;
            var panOffset = (-right * delta.X + up * delta.Y) * panScale;
            editorCam.position += panOffset;
            editorCam.focusTarget += panOffset;
        }
        else if (_inputState.IsHovered && !io.WantTextInput)
        {
            var wheel = io.MouseWheel;
            if (MathF.Abs(wheel) > 0.001f)
            {
                var step = math.max(0.25f, editorCam.orbitDistance * 0.15f) * wheel;
                editorCam.position += forward * step;
                editorCam.orbitDistance = math.max(0.5f, math.length(editorCam.focusTarget - editorCam.position));
            }

            if (ImGui.IsKeyPressed(ImGuiKey.F))
            {
                TryFrameSelectedEntity(systemAPI.World.EntityManager, ref editorCam, in forward);
            }
        }

        editorCam.localToWorld = MathUtility.TRS(editorCam.position, rotation, new float3(1.0f));
    }

    private static void TryFrameSelectedEntity(EntityManager em, ref EditorCamera editorCam, in float3 forward)
    {
        var activeNode = EditorSelection.Instance.ActiveEntity;
        if (activeNode == null || !activeNode.EcsEntity.IsValid || !em.Exists(activeNode.EcsEntity))
        {
            return;
        }

        float3 targetPos;
        if (em.HasComponent<LocalToWorld>(activeNode.EcsEntity))
        {
            targetPos = em.GetComponent<LocalToWorld>(activeNode.EcsEntity).matrix.c3.xyz;
        }
        else if (em.HasComponent<LocalTransform>(activeNode.EcsEntity))
        {
            targetPos = em.GetComponent<LocalTransform>(activeNode.EcsEntity).position;
        }
        else
        {
            return;
        }

        editorCam.focusTarget = targetPos;
        editorCam.orbitDistance = math.clamp(editorCam.orbitDistance, 2.5f, 15.0f);
        editorCam.position = targetPos - forward * editorCam.orbitDistance;
    }
}
