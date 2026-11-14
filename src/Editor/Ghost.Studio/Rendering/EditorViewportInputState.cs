using System.Numerics;

namespace Ghost.Studio.Rendering;

/// <summary>
/// Shared per-frame Scene Viewport UI interaction state used to arbitrate camera navigation and gizmo input.
/// </summary>
internal sealed class EditorViewportInputState
{
    public bool IsHovered { get; set; }
    public bool IsFocused { get; set; }
    public bool IsGizmoBlocking { get; set; }
    public bool IsNavigating { get; set; }
    public Vector2 ViewportMin { get; set; }
    public Vector2 ViewportSize { get; set; }
    public float LatestOrbitYawDelta { get; set; }
    public float LatestOrbitPitchDelta { get; set; }

    public bool CanControlCamera => !IsGizmoBlocking && (IsHovered || IsNavigating);
}
