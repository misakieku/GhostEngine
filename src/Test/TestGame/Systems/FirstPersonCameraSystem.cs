using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Input;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace TestGame.Systems;

[UpdateAfter<InputEvaluationSystem>]
[UpdateBefore<CameraRenderSystem>]
public class FirstPersonCameraSystem : SystemBase
{
    private Identifier<EntityQuery> _queryID;
    private RawInputManager _rawInput = null!;

    private static readonly float PITCH_LIMIT = math.radians(89.0f);

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _rawInput = systemAPI.World.GetService<RawInputManager>();

        _queryID = QueryBuilder.New()
            .WithAll<FirstPersonCamera, ActionState, LocalToWorld>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_queryID);
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        var dt = systemAPI.Time.DeltaTime;
        if (dt <= 0.0f)
        {
            return;
        }

        ref var query = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_queryID);

        foreach (var chunk in query.GetChunkIterator())
        {
            var cameras = chunk.GetComponentDataRW<FirstPersonCamera>();
            var actionStates = chunk.GetComponentData<ActionState>();
            var transforms = chunk.GetComponentDataRW<LocalToWorld>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref var cam = ref cameras[i];
                ref readonly var input = ref actionStates[i];
                ref var transform = ref transforms[i];

                // 1. Mouse Lock Toggle (e.g. Escape or Start button)
                if (input.WasPressed(ActionButton.ToggleMouseLock))
                {
                    _rawInput.RelativeMouseMode = !_rawInput.RelativeMouseMode;
                }

                // Clicking into window captures mouse
                if (!_rawInput.RelativeMouseMode && _rawInput.ReadButton(InputControl.Mouse(MouseControl.LeftButton)))
                {
                    _rawInput.RelativeMouseMode = true;
                }

                // 2. Orientation (Look / Rotation)
                var lookInput = input.GetVector2(ActionAxis.LookX, ActionAxis.LookY);
                var isGamepad = _rawInput.ActiveDevice == DeviceType.Gamepad;

                var canLookWithMouse = !cam.requireRightClickToLook || _rawInput.RelativeMouseMode;
                if (cam.requireRightClickToLook)
                {
                    var isRightClickDown = input.IsDown(ActionButton.Secondary) ||
                        _rawInput.ReadButton(InputControl.Mouse(MouseControl.RightButton));
                    canLookWithMouse = isRightClickDown || _rawInput.RelativeMouseMode;
                }

                if (math.lengthsq(lookInput) > 1e-5f && (isGamepad || canLookWithMouse))
                {
                    var sensitivity = isGamepad
                        ? cam.gamepadSensitivity * dt
                        : cam.mouseSensitivity;

                    cam.yaw += lookInput.x * sensitivity;

                    var pitchDelta = lookInput.y * sensitivity;
                    if (cam.invertY)
                    {
                        pitchDelta = -pitchDelta;
                    }
                    cam.pitch += pitchDelta;

                    cam.pitch = math.clamp(cam.pitch, -PITCH_LIMIT, PITCH_LIMIT);
                }

                // Construct rotation quaternion: Yaw around world Up, Pitch around camera Right
                var rotY = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), cam.yaw);
                var rotX = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), cam.pitch);
                var rotation = math.mul(rotY, rotX);

                // 3. Movement (Displacement along view forward / right / world up)
                var moveInput = input.GetVector2(ActionAxis.MoveX, ActionAxis.MoveY);
                var elevation = input.GetAxis(ActionAxis.Elevation);
                var isSprinting = input.IsDown(ActionButton.Sprint);

                var forward = math.mul(rotation, new float3(0.0f, 0.0f, 1.0f));
                var right = math.mul(rotation, new float3(1.0f, 0.0f, 0.0f));
                var up = new float3(0.0f, 1.0f, 0.0f);

                var speed = cam.moveSpeed * (isSprinting ? cam.sprintMultiplier : 1.0f);
                var moveDir = (forward * moveInput.y) + (right * moveInput.x) + (up * elevation);

                var position = transform.matrix.c3.xyz;
                position += moveDir * (speed * dt);

                // Preserve scale
                var scale = new float3(
                    math.length(transform.matrix.c0.xyz),
                    math.length(transform.matrix.c1.xyz),
                    math.length(transform.matrix.c2.xyz)
                );

                if (math.lengthsq(scale) < 1e-5f)
                {
                    scale = new float3(1.0f, 1.0f, 1.0f);
                }

                transform.matrix = MathUtility.TRS(position, rotation, scale);
            }
        }
    }
}
