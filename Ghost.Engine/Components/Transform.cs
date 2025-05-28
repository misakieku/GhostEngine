using Ghost.Engine.Helpers;
using Ghost.Entities;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Components;

public struct Transform : IComponentData
{
    private Vector3 _position = Vector3.Zero;
    public Vector3 Position
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

    public bool hasChanged;

    private Matrix4x4 _localToWorldMatrix;
    private Matrix4x4 _worldToLocalMatrix;

    public readonly Matrix4x4 LocalToWorldMatrix => _localToWorldMatrix;
    public readonly Matrix4x4 WorldToLocalMatrix => _worldToLocalMatrix;

    public static Transform Default
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Vector3.Zero, Quaternion.Identity, Vector3.One);
    }

    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        _position = position;
        _rotation = rotation;
        _scale = scale;
        hasChanged = false;
        _localToWorldMatrix = Matrix4x4.Identity;
        _worldToLocalMatrix = Matrix4x4.Identity;

        UpdateMatrices();
    }

    private void UpdateMatrices()
    {
        _localToWorldMatrix = MatrixHelpers.CreateTRS(_position, _rotation, _scale);
        Matrix4x4.Invert(_localToWorldMatrix, out _worldToLocalMatrix);
    }
}