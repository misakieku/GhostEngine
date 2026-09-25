using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Input;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Systems;

public class InputEvaluationSystem : SystemBase
{
    private RawInputManager _rawInput = null!;
    private InputProfileDatabase _profiles = null!;
    private Identifier<EntityQuery> _queryID;

    protected override void OnInitialize(scoped in SystemAPI systemAPI)
    {
        _rawInput = systemAPI.World.GetService<RawInputManager>();
        _profiles = systemAPI.World.GetService<InputProfileDatabase>();

        _queryID = QueryBuilder.New()
            .WithAll<InputReceiver, ActionState>()
            .Build(systemAPI.World, true);

        RequireQueryForUpdate(_queryID);
    }

    protected override void OnUpdate(scoped in SystemAPI systemAPI)
    {
        ref var query = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_queryID);

        foreach (var chunk in query.GetChunkIterator())
        {
            var receivers = chunk.GetComponentData<InputReceiver>();
            var actionStates = chunk.GetComponentDataRW<ActionState>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ref readonly var receiver = ref receivers[i];
                if (!receiver.Enabled)
                {
                    continue;
                }

                ref var state = ref actionStates[i];
                state.ResetFrameTransitions();

                var profile = _profiles.GetProfile(receiver.ProfileId);
                if (profile == null)
                {
                    continue;
                }

                // 1. Digital Buttons
                for (var b = 0; b < profile.Buttons.Count; b++)
                {
                    var binding = profile.Buttons[b];
                    var isDown = _rawInput.ReadButton(binding.Primary) || _rawInput.ReadButton(binding.Secondary);
                    state.SetButton(binding.Action, isDown);
                }

                // 2. 1D Axes
                for (var a = 0; a < profile.Axes1D.Count; a++)
                {
                    var binding = profile.Axes1D[a];
                    var val = 0.0f;
                    if (_rawInput.ReadButton(binding.Positive))
                    {
                        val += 1.0f;
                    }
                    if (_rawInput.ReadButton(binding.Negative))
                    {
                        val -= 1.0f;
                    }
                    state.SetAxis(binding.Axis, val);
                }

                // 3. 2D Composites (e.g. WASD)
                for (var c = 0; c < profile.Composites2D.Count; c++)
                {
                    var comp = profile.Composites2D[c];
                    var vec = float2.zero;
                    if (_rawInput.ReadButton(comp.Up))
                    {
                        vec.y += 1.0f;
                    }
                    if (_rawInput.ReadButton(comp.Down))
                    {
                        vec.y -= 1.0f;
                    }
                    if (_rawInput.ReadButton(comp.Right))
                    {
                        vec.x += 1.0f;
                    }
                    if (_rawInput.ReadButton(comp.Left))
                    {
                        vec.x -= 1.0f;
                    }

                    if (math.lengthsq(vec) > 1e-4f)
                    {
                        vec = math.normalize(vec);
                    }

                    state.SetAxis(comp.AxisX, vec.x);
                    state.SetAxis(comp.AxisY, vec.y);
                }

                // 4. 2D Analog Sticks (Override or combine with digital composite if active)
                var hasStickLook = false;
                for (var s = 0; s < profile.Sticks2D.Count; s++)
                {
                    var stick = profile.Sticks2D[s];
                    var stickVec = new float2(
                        _rawInput.ReadAxis(stick.StickX, stick.Deadzone) * (stick.InvertX ? -1.0f : 1.0f),
                        _rawInput.ReadAxis(stick.StickY, stick.Deadzone) * (stick.InvertY ? -1.0f : 1.0f)
                    );

                    if (math.lengthsq(stickVec) > 1e-4f)
                    {
                        state.SetAxis(stick.AxisX, stickVec.x);
                        state.SetAxis(stick.AxisY, stickVec.y);
                        if (stick.AxisX == ActionAxis.LookX || stick.AxisY == ActionAxis.LookY)
                        {
                            hasStickLook = true;
                        }
                    }
                }

                // 5. Mouse Look Delta
                if (profile.MouseLook.HasValue)
                {
                    var mouse = profile.MouseLook.Value;
                    var mouseDelta = _rawInput.MouseDelta;
                    if (math.lengthsq(mouseDelta) > 1e-5f)
                    {
                        state.SetAxis(mouse.AxisX, mouseDelta.x * mouse.Sensitivity);
                        state.SetAxis(mouse.AxisY, mouseDelta.y * mouse.Sensitivity);
                    }
                    else if (!hasStickLook)
                    {
                        state.SetAxis(mouse.AxisX, 0.0f);
                        state.SetAxis(mouse.AxisY, 0.0f);
                    }
                }
            }
        }
    }
}
