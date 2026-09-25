using Ghost.Engine.Input;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Components;

/// <summary>
/// Universal action state component. Attach this to any controllable entity (Camera, Player, Vehicle, etc.).
/// </summary>
public unsafe struct ActionState : IComponentData
{
    private uint _buttonsDown;
    private uint _buttonsPressed;
    private uint _buttonsReleased;

    private fixed float _axes[8];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsDown(ActionButton button) =>
        (_buttonsDown & (1u << (int)button)) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool WasPressed(ActionButton button) =>
        (_buttonsPressed & (1u << (int)button)) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool WasReleased(ActionButton button) =>
        (_buttonsReleased & (1u << (int)button)) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float GetAxis(ActionAxis axis) =>
        _axes[(int)axis];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float2 GetVector2(ActionAxis axisX, ActionAxis axisY) =>
        new(_axes[(int)axisX], _axes[(int)axisY]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetButton(ActionButton button, bool isDown)
    {
        var mask = 1u << (int)button;
        var wasDown = (_buttonsDown & mask) != 0;

        if (isDown)
        {
            _buttonsDown |= mask;
        }
        else
        {
            _buttonsDown &= ~mask;
        }

        if (!wasDown && isDown)
        {
            _buttonsPressed |= mask;
        }
        else
        {
            _buttonsPressed &= ~mask;
        }

        if (wasDown && !isDown)
        {
            _buttonsReleased |= mask;
        }
        else
        {
            _buttonsReleased &= ~mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAxis(ActionAxis axis, float value)
    {
        _axes[(int)axis] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetFrameTransitions()
    {
        _buttonsPressed = 0;
        _buttonsReleased = 0;
    }
}
