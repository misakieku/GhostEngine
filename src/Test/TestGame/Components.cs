using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace TestGame;

internal struct MoveDst : IComponentData
{
    public float3 position;
    public float3 lookAt;
    public float3 range;
    public bool updateRotation;
}

public struct FirstPersonCamera : IComponentData
{
    public float yaw;
    public float pitch;
    public float moveSpeed;
    public float sprintMultiplier;
    public float mouseSensitivity;
    public float gamepadSensitivity;
    public bool invertY;
    public bool requireRightClickToLook;

    public static FirstPersonCamera Default => new()
    {
        yaw = 0.0f,
        pitch = 0.0f,
        moveSpeed = 15.0f,
        sprintMultiplier = 2.5f,
        mouseSensitivity = 0.0008f,
        gamepadSensitivity = 2.0f,
        invertY = false,
        requireRightClickToLook = false,
    };
}
