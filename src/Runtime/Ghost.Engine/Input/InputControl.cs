using SDL;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Input;

public enum DeviceType : byte
{
    Keyboard = 0,
    Mouse = 1,
    Gamepad = 2
}

public enum MouseControl : ushort
{
    LeftButton = 1,
    MiddleButton = 2,
    RightButton = 3,
    X1Button = 4,
    X2Button = 5,
    WheelX = 10,
    WheelY = 11,
    DeltaX = 20,
    DeltaY = 21
}

/// <summary>
/// A unified representation of any physical input control across devices (4 bytes).
/// Analogous to Unreal's FKey.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly struct InputControl : IEquatable<InputControl>
{
    [FieldOffset(0)] public readonly DeviceType Device;
    [FieldOffset(1)] public readonly byte DeviceIndex; // Gamepad index: 0, 1, 2, 3
    [FieldOffset(2)] public readonly ushort ControlId; // Keycode, MouseControl, or SDL_GamepadButton / SDL_GamepadAxis

    public InputControl(DeviceType device, byte deviceIndex, ushort controlId)
    {
        Device = device;
        DeviceIndex = deviceIndex;
        ControlId = controlId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputControl Key(SDL_Keycode key) =>
        new(DeviceType.Keyboard, 0, (ushort)key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputControl Mouse(MouseControl button) =>
        new(DeviceType.Mouse, 0, (ushort)button);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputControl GamepadBtn(SDL_GamepadButton button, byte playerIndex = 0) =>
        new(DeviceType.Gamepad, playerIndex, (ushort)button);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputControl GamepadAxis(SDL_GamepadAxis axis, byte playerIndex = 0) =>
        new(DeviceType.Gamepad, playerIndex, (ushort)axis);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(InputControl other) =>
        Device == other.Device && DeviceIndex == other.DeviceIndex && ControlId == other.ControlId;

    public override bool Equals(object? obj) =>
        obj is InputControl other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine((byte)Device, DeviceIndex, ControlId);

    public static bool operator ==(InputControl left, InputControl right) =>
        left.Equals(right);

    public static bool operator !=(InputControl left, InputControl right) =>
        !left.Equals(right);
}
