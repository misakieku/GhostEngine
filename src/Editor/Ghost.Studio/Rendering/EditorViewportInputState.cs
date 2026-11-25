using Hexa.NET.ImGuizmo;
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
    public bool FocusRequested { get; set; }
    public Vector2 ViewportMin { get; set; }
    public Vector2 ViewportSize { get; set; }

    public ImGuizmoOperation GizmoOperation { get; set; } = ImGuizmoOperation.Translate;
    public ImGuizmoMode GizmoMode { get; set; } = ImGuizmoMode.World;
    public bool SnapEnabled { get; set; }
    public float TranslateSnap { get; set; } = 0.5f;
    public float RotateSnap { get; set; } = 15.0f;
    public float ScaleSnap { get; set; } = 0.25f;

    public bool CanControlCamera => !IsGizmoBlocking && (IsHovered || IsNavigating);
}
