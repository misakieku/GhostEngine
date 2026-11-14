using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor.Core;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Studio.Drawers;

public sealed class QuaternionPropertyDrawer : PropertyDrawer
{
    private const float RAD_TO_DEG = 180.0f / MathF.PI;
    private const float DEG_TO_RAD = MathF.PI / 180.0f;

    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var current = property.Value is quaternion q ? q : quaternion.identity;
        var euler = ToEulerDegrees(current);

        return DrawAxisNumberRow(
            property,
            new AxisNumberSpec("X", GhostColors.StatusError, MathF.Round(euler.x, 2), v => context.ModifyProperty(property, current, FromEulerDegrees(new float3((float)v, euler.y, euler.z)))),
            new AxisNumberSpec("Y", GhostColors.StatusSuccess, MathF.Round(euler.y, 2), v => context.ModifyProperty(property, current, FromEulerDegrees(new float3(euler.x, (float)v, euler.z)))),
            new AxisNumberSpec("Z", GhostColors.AccentPrimary, MathF.Round(euler.z, 2), v => context.ModifyProperty(property, current, FromEulerDegrees(new float3(euler.x, euler.y, (float)v))))
        );
    }

    private static float3 ToEulerDegrees(quaternion q)
    {
        var v = q.value;
        var sinrCosp = 2.0f * (v.w * v.x + v.y * v.z);
        var cosrCosp = 1.0f - 2.0f * (v.x * v.x + v.y * v.y);
        var roll = MathF.Atan2(sinrCosp, cosrCosp);

        var sinp = 2.0f * (v.w * v.y - v.z * v.x);
        var pitch = MathF.Abs(sinp) >= 1.0f ? MathF.CopySign(MathF.PI / 2.0f, sinp) : MathF.Asin(sinp);

        var sinyCosp = 2.0f * (v.w * v.z + v.x * v.y);
        var cosyCosp = 1.0f - 2.0f * (v.y * v.y + v.z * v.z);
        var yaw = MathF.Atan2(sinyCosp, cosyCosp);

        return new float3(roll * RAD_TO_DEG, pitch * RAD_TO_DEG, yaw * RAD_TO_DEG);
    }

    private static quaternion FromEulerDegrees(float3 deg)
    {
        var half = deg * (0.5f * DEG_TO_RAD);
        var cx = MathF.Cos(half.x);
        var sx = MathF.Sin(half.x);
        var cy = MathF.Cos(half.y);
        var sy = MathF.Sin(half.y);
        var cz = MathF.Cos(half.z);
        var sz = MathF.Sin(half.z);

        return new quaternion(
            sx * cy * cz - cx * sy * sz,
            cx * sy * cz + sx * cy * sz,
            cx * cy * sz - sx * sy * cz,
            cx * cy * cz + sx * sy * sz);
    }
}
