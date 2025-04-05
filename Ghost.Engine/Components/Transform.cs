using Ghost.Engine.Helpers;
using System.Numerics;

namespace Ghost.Engine.Components;

public class Transform : Component
{
    private Vector3 _position = Vector3.Zero;
    public Vector3 position
    {
        get => _position;
        set
        {
            _position = value;
            hasChanged = true;
            UpdateMatrices();
        }
    }

    private Quaternion _rotation = Quaternion.Identity;
    public Quaternion Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            hasChanged = true;
            UpdateMatrices();
        }
    }

    private Vector3 _scale = Vector3.One;
    public Vector3 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            hasChanged = true;
            UpdateMatrices();
        }
    }

    public bool hasChanged = true;

    private Matrix4x4 _localToWorldMatrix = Matrix4x4.Identity;
    private Matrix4x4 _worldToLocalMatrix = Matrix4x4.Identity;

    public Matrix4x4 LocalToWorldMatrix => _localToWorldMatrix;
    public Matrix4x4 WorldToLocalMatrix => _worldToLocalMatrix;

    private void UpdateMatrices()
    {
        _localToWorldMatrix = MatrixHelpers.CreateTRS(_position, _rotation, _scale);
        Matrix4x4.Invert(_localToWorldMatrix, out _worldToLocalMatrix);
    }

    public override void Start()
    {
        UpdateMatrices();
    }
}