using Ghost.Engine.Models;
using System.Numerics;

namespace Ghost.Engine.Components;

public class Transform : Component
{
    public Vector3 position = Vector3.Zero;
    public Quaternion rotation = Quaternion.Identity;
    public Vector3 scale = Vector3.One;
}