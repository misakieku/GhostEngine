using Ghost.Entities;

namespace Ghost.Engine.Components;

/// <summary>
/// Component for a first-person camera controller, storing orientation angles and control parameters.
/// </summary>
public struct FirstPersonCamera : IComponentData
{
    /// <summary>
    /// Horizontal rotation angle in radians (around world Y axis).
    /// </summary>
    public float Yaw;

    /// <summary>
    /// Vertical rotation angle in radians (around camera X axis, clamped between -89° and +89°).
    /// </summary>
    public float Pitch;

    /// <summary>
    /// Base movement speed in world units per second.
    /// </summary>
    public float MoveSpeed;

    /// <summary>
    /// Speed multiplier applied when sprinting.
    /// </summary>
    public float SprintMultiplier;

    /// <summary>
    /// Mouse sensitivity in radians per pixel delta.
    /// </summary>
    public float MouseSensitivity;

    /// <summary>
    /// Gamepad analog stick sensitivity in radians per second.
    /// </summary>
    public float GamepadSensitivity;

    /// <summary>
    /// If true, inverts the vertical look axis (flight simulator style).
    /// </summary>
    public bool InvertY;

    /// <summary>
    /// If true, looking with the mouse requires holding the right mouse button (editor flycam style).
    /// </summary>
    public bool RequireRightClickToLook;

    public static FirstPersonCamera Default => new()
    {
        Yaw = 0.0f,
        Pitch = 0.0f,
        MoveSpeed = 15.0f,
        SprintMultiplier = 2.5f,
        MouseSensitivity = 0.0008f,
        GamepadSensitivity = 2.0f,
        InvertY = false,
        RequireRightClickToLook = false,
    };
}
