using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Studio.Backend;
using Ghost.Studio.Rendering;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Misaki.HighPerformance.Mathematics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Studio.UI;

/// <summary>
/// Displays the offscreen Scene View render target, Transform gizmos (<see cref="ImGuizmo"/>), and ViewCube.
/// </summary>
internal sealed unsafe class SceneViewportPanel
{
    private readonly SceneViewportTarget _viewportTarget;
    private readonly EditorViewportInputState _inputState;

    private bool _wasDraggingGizmo;
    private Entity _dragEntity = Entity.Invalid;
    private LocalTransform _dragStartLocalTransform;
    private LocalToWorld _dragStartLocalToWorld;
    private bool _dragHasLocalTransform;
    private PropertyNode? _dragPosProp;
    private PropertyNode? _dragRotProp;
    private PropertyNode? _dragScaleProp;
    private PropertyNode? _dragLtwMatrixProp;

    public SceneViewportPanel(SceneViewportTarget viewportTarget, EditorViewportInputState inputState)
    {
        _viewportTarget = viewportTarget;
        _inputState = inputState;
    }

    public bool Draw(ImGuiContextManager contextManager, EntityManager em, ref bool isOpen)
    {
        var usedViewportTexture = false;

        ImGui.SetNextWindowPos(new Vector2(300.0f, 32.0f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(960.0f, 600.0f), ImGuiCond.FirstUseEver);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        var visible = ImGui.Begin("Scene", ref isOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGui.PopStyleVar();

        if (!visible)
        {
            _inputState.IsHovered = false;
            _inputState.IsFocused = false;
            _inputState.IsGizmoBlocking = false;
            ImGui.End();
            return false;
        }

        _inputState.IsHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
        _inputState.IsFocused = ImGui.IsWindowFocused();

        var avail = ImGui.GetContentRegionAvail();
        var cursorScreen = ImGui.GetCursorScreenPos();
        _inputState.ViewportMin = cursorScreen;
        _inputState.ViewportSize = avail;

        var width = (uint)MathF.Max(0.0f, MathF.Floor(avail.X));
        var height = (uint)MathF.Max(0.0f, MathF.Floor(avail.Y));
        if (width >= 64 && height >= 64)
        {
            _viewportTarget.RequestResize(width, height);
        }

        if (_viewportTarget.IsReady && avail.X >= 1.0f && avail.Y >= 1.0f)
        {
            var texRef = new ImTextureRef(null, new ImTextureID((ulong)_viewportTarget.BindlessSrvIndex));
            ImGui.Image(texRef, avail);
            usedViewportTexture = true;
        }
        else
        {
            ref readonly var tokens = ref contextManager.Tokens;
            ImGui.SetCursorPos(new Vector2(16.0f, 16.0f));
            ImGui.TextColored(tokens.TextSecondary, "Initializing Scene Viewport...");
        }

        HandleToolShortcuts();

        var isToolbarHovered = DrawGizmoToolbar(contextManager, cursorScreen);
        var isViewCubeHovered = false;
        var isGizmoInteracting = false;

        if (avail.X >= 64.0f && avail.Y >= 64.0f)
        {
            ref var editorCam = ref em.GetOrCreateSingleton(EditorCamera.Default);
            BuildCameraMatrices(in editorCam, avail.X, avail.Y, out var viewMtx, out var projMtx);

            ImGuizmo.SetOrthographic(false);
            ImGuizmo.SetDrawlist();
            ImGuizmo.SetRect(cursorScreen.X, cursorScreen.Y, avail.X, avail.Y);

            isGizmoInteracting = DrawSelectedEntityGizmo(em, in viewMtx, in projMtx);
            isViewCubeHovered = DrawViewCube(contextManager, ref editorCam, cursorScreen, avail);
        }

        _inputState.IsGizmoBlocking = isToolbarHovered || isGizmoInteracting || isViewCubeHovered || ImGuizmo.IsUsing();

        DrawOverlayHud(contextManager, em, cursorScreen, avail);

        ImGui.End();
        return usedViewportTexture;
    }

    private void HandleToolShortcuts()
    {
        var io = ImGui.GetIO();
        if (!_inputState.IsHovered || _inputState.IsNavigating || io.WantTextInput || ImGui.IsMouseDown(ImGuiMouseButton.Right))
        {
            return;
        }

        if (ImGui.IsKeyPressed(ImGuiKey.W))
        {
            _inputState.GizmoOperation = ImGuizmoOperation.Translate;
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.E))
        {
            _inputState.GizmoOperation = ImGuizmoOperation.Rotate;
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.R))
        {
            _inputState.GizmoOperation = ImGuizmoOperation.Scale;
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.X))
        {
            _inputState.GizmoMode = _inputState.GizmoMode == ImGuizmoMode.World ? ImGuizmoMode.Local : ImGuizmoMode.World;
        }
    }

    private bool DrawGizmoToolbar(ImGuiContextManager contextManager, Vector2 viewportOrigin)
    {
        ref readonly var tokens = ref contextManager.Tokens;
        var dpi = contextManager.DpiScale;
        var pad = 8.0f * dpi;

        ImGui.SetCursorScreenPos(new Vector2(viewportOrigin.X + pad, viewportOrigin.Y + pad));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, EditorTheme.WithAlpha(tokens.HeaderBar, 0.88f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6.0f * dpi, 4.0f * dpi));
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4.0f * dpi);

        var flags = ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.Borders;
        ImGui.BeginChild("##SceneGizmoToolbar", Vector2.Zero, flags);

        if (ImGuiWidgets.ToggleButton("Move##gizmo", _inputState.GizmoOperation == ImGuizmoOperation.Translate, in tokens))
        {
            _inputState.GizmoOperation = ImGuizmoOperation.Translate;
        }

        ImGui.SameLine();
        if (ImGuiWidgets.ToggleButton("Rotate##gizmo", _inputState.GizmoOperation == ImGuizmoOperation.Rotate, in tokens))
        {
            _inputState.GizmoOperation = ImGuizmoOperation.Rotate;
        }

        ImGui.SameLine();
        if (ImGuiWidgets.ToggleButton("Scale##gizmo", _inputState.GizmoOperation == ImGuizmoOperation.Scale, in tokens))
        {
            _inputState.GizmoOperation = ImGuizmoOperation.Scale;
        }

        ImGui.SameLine();
        var isWorld = _inputState.GizmoMode == ImGuizmoMode.World;
        if (ImGuiWidgets.ToggleButton(isWorld ? "World##space" : "Local##space", !isWorld, in tokens))
        {
            _inputState.GizmoMode = isWorld ? ImGuizmoMode.Local : ImGuizmoMode.World;
        }

        ImGui.SameLine();
        if (ImGuiWidgets.ToggleButton("Snap##snap", _inputState.SnapEnabled, in tokens))
        {
            _inputState.SnapEnabled = !_inputState.SnapEnabled;
        }

        var hovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.ChildWindows);
        ImGui.EndChild();
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();

        return hovered;
    }

    private bool DrawSelectedEntityGizmo(EntityManager em, in float4x4 viewMtx, in float4x4 projMtx)
    {
        var activeNode = EditorSelection.Instance.ActiveEntity;
        if (activeNode == null || !activeNode.EcsEntity.IsValid || !em.Exists(activeNode.EcsEntity))
        {
            FinalizePendingDragIfNeeded(em);
            return false;
        }

        var entity = activeNode.EcsEntity;
        var hasLocal = em.HasComponent<LocalTransform>(entity);
        var hasLtw = em.HasComponent<LocalToWorld>(entity);
        if (!hasLocal && !hasLtw)
        {
            FinalizePendingDragIfNeeded(em);
            return false;
        }

        var parentWorldMtx = float4x4.identity;
        var hasParentWorld = false;
        if (em.HasComponent<Hierarchy>(entity))
        {
            var parentEntity = em.GetComponent<Hierarchy>(entity).parent;
            if (parentEntity.IsValid && em.Exists(parentEntity) && em.HasComponent<LocalToWorld>(parentEntity))
            {
                parentWorldMtx = em.GetComponent<LocalToWorld>(parentEntity).matrix;
                hasParentWorld = true;
            }
        }

        float4x4 worldMtx;
        if (hasLocal)
        {
            var localMtx = em.GetComponent<LocalTransform>(entity).ToMatrix();
            worldMtx = hasParentWorld ? math.mul(parentWorldMtx, localMtx) : localMtx;
        }
        else
        {
            worldMtx = em.GetComponent<LocalToWorld>(entity).matrix;
        }

        var viewCopy = Unsafe.BitCast<float4x4, Matrix4x4>(viewMtx);
        var projCopy = Unsafe.BitCast<float4x4, Matrix4x4>(projMtx);
        var modelCopy = Unsafe.BitCast<float4x4, Matrix4x4>(worldMtx);

        var mode = _inputState.GizmoOperation == ImGuizmoOperation.Scale ? ImGuizmoMode.Local : _inputState.GizmoMode;
        var useSnap = _inputState.SnapEnabled || ImGui.GetIO().KeyCtrl;
        var snapValues = _inputState.GizmoOperation switch
        {
            ImGuizmoOperation.Rotate => new Vector3(_inputState.RotateSnap),
            ImGuizmoOperation.Scale => new Vector3(_inputState.ScaleSnap),
            _ => new Vector3(_inputState.TranslateSnap),
        };

        var snapPtr = useSnap ? &snapValues.X : null;
        var manipulated = ImGuizmo.Manipulate(&viewCopy.M11, &projCopy.M11, _inputState.GizmoOperation, mode, &modelCopy.M11, null, snapPtr, null, null);
        var isUsing = ImGuizmo.IsUsing();
        var isOver = ImGuizmo.IsOver();

        if (isUsing && (!_wasDraggingGizmo || _dragEntity != entity))
        {
            BeginGizmoDrag(activeNode, entity, em, hasLocal);
        }

        if (manipulated)
        {
            var nextWorldMtx = Unsafe.BitCast<Matrix4x4, float4x4>(modelCopy);
            if (hasLocal)
            {
                var nextLocalMtx = hasParentWorld ? math.mul(math.inverse(parentWorldMtx), nextWorldMtx) : nextWorldMtx;
                nextLocalMtx.GetTRS(out var newPos, out var newRot, out var newScale);
                ref var lt = ref em.GetComponent<LocalTransform>(entity);
                lt.position = newPos;
                lt.rotation = newRot;
                lt.scale = newScale;
                if (hasLtw)
                {
                    em.GetComponent<LocalToWorld>(entity).matrix = nextWorldMtx;
                }

                EditorSyncBridge.Instance.DirtyTracker.MarkTransformDirty(entity);
            }
            else if (hasLtw)
            {
                em.GetComponent<LocalToWorld>(entity).matrix = nextWorldMtx;
                EditorSyncBridge.Instance.DirtyTracker.MarkTransformDirty(entity);
            }
        }

        if (!isUsing && _wasDraggingGizmo)
        {
            FinalizePendingDragIfNeeded(em);
        }

        return isUsing || isOver;
    }

    private void BeginGizmoDrag(EntityNode node, Entity entity, EntityManager em, bool hasLocal)
    {
        _wasDraggingGizmo = true;
        _dragEntity = entity;
        _dragHasLocalTransform = hasLocal;

        if (hasLocal)
        {
            _dragStartLocalTransform = em.GetComponent<LocalTransform>(entity);
            var ltComp = FindComponentNode(node, typeof(LocalTransform));
            _dragPosProp = FindPropertyNode(ltComp, nameof(LocalTransform.position));
            _dragRotProp = FindPropertyNode(ltComp, nameof(LocalTransform.rotation));
            _dragScaleProp = FindPropertyNode(ltComp, nameof(LocalTransform.scale));
            _dragLtwMatrixProp = null;
        }
        else
        {
            _dragStartLocalToWorld = em.GetComponent<LocalToWorld>(entity);
            var ltwComp = FindComponentNode(node, typeof(LocalToWorld));
            _dragLtwMatrixProp = FindPropertyNode(ltwComp, nameof(LocalToWorld.matrix));
            _dragPosProp = null;
            _dragRotProp = null;
            _dragScaleProp = null;
        }
    }

    private void FinalizePendingDragIfNeeded(EntityManager em)
    {
        if (!_wasDraggingGizmo)
        {
            return;
        }

        _wasDraggingGizmo = false;
        var entity = _dragEntity;
        _dragEntity = Entity.Invalid;

        if (!entity.IsValid || !em.Exists(entity))
        {
            return;
        }

        if (_dragHasLocalTransform && em.HasComponent<LocalTransform>(entity))
        {
            var current = em.GetComponent<LocalTransform>(entity);
            var cmds = new List<IEditorCommand>(3);

            if (_dragPosProp != null && !current.position.Equals(_dragStartLocalTransform.position))
            {
                cmds.Add(new ModifyPropertyCommand(_dragPosProp, _dragStartLocalTransform.position, current.position));
            }

            if (_dragRotProp != null && !current.rotation.Equals(_dragStartLocalTransform.rotation))
            {
                cmds.Add(new ModifyPropertyCommand(_dragRotProp, _dragStartLocalTransform.rotation, current.rotation));
            }

            if (_dragScaleProp != null && !current.scale.Equals(_dragStartLocalTransform.scale))
            {
                cmds.Add(new ModifyPropertyCommand(_dragScaleProp, _dragStartLocalTransform.scale, current.scale));
            }

            if (cmds.Count == 1)
            {
                CommandHistory.Instance.Execute(cmds[0]);
            }
            else if (cmds.Count > 1)
            {
                CommandHistory.Instance.Execute(new CompoundCommand("Transform Entity", cmds));
            }
        }
        else if (!_dragHasLocalTransform && _dragLtwMatrixProp != null && em.HasComponent<LocalToWorld>(entity))
        {
            var current = em.GetComponent<LocalToWorld>(entity);
            if (!current.matrix.Equals(_dragStartLocalToWorld.matrix))
            {
                CommandHistory.Instance.Execute(new ModifyPropertyCommand(_dragLtwMatrixProp, _dragStartLocalToWorld.matrix, current.matrix));
            }
        }
    }

    private static bool DrawViewCube(ImGuiContextManager contextManager, ref EditorCamera editorCam, Vector2 viewportOrigin, Vector2 avail)
    {
        var dpi = contextManager.DpiScale;
        var pad = 10.0f * dpi;
        var cubeExtent = 96.0f * dpi;
        if (avail.X < cubeExtent + pad * 2.0f || avail.Y < cubeExtent + pad * 2.0f)
        {
            return false;
        }

        var cubePos = new Vector2(viewportOrigin.X + avail.X - cubeExtent - pad, viewportOrigin.Y + pad);
        var cubeSize = new Vector2(cubeExtent, cubeExtent);

        var rotY = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), editorCam.yaw);
        var rotX = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), editorCam.pitch);
        var rotation = math.mul(rotY, rotX);
        var right = math.mul(rotation, new float3(1.0f, 0.0f, 0.0f));
        var up = math.mul(rotation, new float3(0.0f, 1.0f, 0.0f));
        var forward = math.mul(rotation, new float3(0.0f, 0.0f, 1.0f));

        // ImGuizmo ViewManipulate assumes a right-handed view (-Z forward), so flip column Z cleanly and invert back after edit.
        var rhWorld = new float4x4(
            right.x, up.x, -forward.x, editorCam.position.x,
            right.y, up.y, -forward.y, editorCam.position.y,
            right.z, up.z, -forward.z, editorCam.position.z,
            0.0f, 0.0f, 0.0f, 1.0f
        );
        var rhView = math.inverse(rhWorld);
        var viewCopy = Unsafe.BitCast<float4x4, Matrix4x4>(rhView);
        var beforeCopy = viewCopy;

        ref readonly var tokens = ref contextManager.Tokens;
        var bgCol = ImGui.ColorConvertFloat4ToU32(EditorTheme.WithAlpha(tokens.HeaderBar, 0.65f));
        ImGuizmo.ViewManipulate(&viewCopy.M11, editorCam.orbitDistance, cubePos, cubeSize, bgCol);

        if (!MatrixNearlyEqual(in beforeCopy, in viewCopy))
        {
            var editedRhWorld = math.inverse(Unsafe.BitCast<Matrix4x4, float4x4>(viewCopy));
            var newForward = math.normalizesafe(-editedRhWorld.c2.xyz, forward);
            var pitchLimit = math.radians(89.0f);

            editorCam.yaw = math.atan2(newForward.x, newForward.z);
            editorCam.pitch = math.clamp(-math.asin(math.clamp(newForward.y, -0.999f, 0.999f)), -pitchLimit, pitchLimit);
            editorCam.position = editedRhWorld.c3.xyz;
            editorCam.focusTarget = editorCam.position + newForward * editorCam.orbitDistance;

            var newRotY = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), editorCam.yaw);
            var newRotX = quaternion.AxisAngle(new float3(1.0f, 0.0f, 0.0f), editorCam.pitch);
            editorCam.localToWorld = MathUtility.TRS(editorCam.position, math.mul(newRotY, newRotX), new float3(1.0f));
        }

        var mouse = ImGui.GetMousePos();
        return mouse.X >= cubePos.X && mouse.X <= cubePos.X + cubeSize.X && mouse.Y >= cubePos.Y && mouse.Y <= cubePos.Y + cubeSize.Y;
    }

    private void DrawOverlayHud(ImGuiContextManager contextManager, EntityManager em, Vector2 viewportOrigin, Vector2 avail)
    {
        ref readonly var tokens = ref contextManager.Tokens;
        var dpi = contextManager.DpiScale;
        var pad = 8.0f * dpi;

        ref var cam = ref em.GetOrCreateSingleton(EditorCamera.Default);
        var entityCount = EditorSyncBridge.Instance.LatestSnapshot?.EntityCount ?? 0;

        ImGui.PushFont(contextManager.MonospaceFont, EditorTheme.FONT_SIZE_MONO);
        var label = $"{_viewportTarget.Width}x{_viewportTarget.Height}  |  Entities: {entityCount}  |  Speed: {cam.moveSpeed:0.0}  |  RMB+WASD Fly  Alt+LMB Orbit  MMB Pan  F Frame";
        var textSize = ImGui.CalcTextSize(label);
        var drawList = ImGui.GetWindowDrawList();

        var bgMin = new Vector2(viewportOrigin.X + pad, viewportOrigin.Y + avail.Y - textSize.Y - pad - 8.0f * dpi);
        var bgMax = new Vector2(bgMin.X + textSize.X + 14.0f * dpi, bgMin.Y + textSize.Y + 8.0f * dpi);

        var bgCol = ImGui.ColorConvertFloat4ToU32(EditorTheme.WithAlpha(tokens.HeaderBar, 0.78f));
        var borderCol = ImGui.ColorConvertFloat4ToU32(tokens.BorderSubtle);
        var textCol = ImGui.ColorConvertFloat4ToU32(tokens.TextSecondary);

        drawList.AddRectFilled(bgMin, bgMax, bgCol, 4.0f * dpi);
        drawList.AddRect(bgMin, bgMax, borderCol, 4.0f * dpi);
        drawList.AddText(new Vector2(bgMin.X + 7.0f * dpi, bgMin.Y + 4.0f * dpi), textCol, label);
        ImGui.PopFont();
    }

    private static void BuildCameraMatrices(in EditorCamera editorCam, float width, float height, out float4x4 view, out float4x4 projection)
    {
        view = math.inverse(editorCam.localToWorld);

        var aspectScreen = width / MathF.Max(1.0f, height);
        var vfov = 2.0f * math.atan(editorCam.camera.sensorSize.y / (2.0f * editorCam.camera.focalLength));
        var hfov = 2.0f * math.atan(editorCam.camera.sensorSize.x / (2.0f * editorCam.camera.focalLength));
        var aspectSensor = editorCam.camera.sensorSize.x / editorCam.camera.sensorSize.y;

        var vfovFinal = editorCam.camera.gateFit switch
        {
            GateFit.Horizontal => 2.0f * math.atan(math.tan(hfov * 0.5f) / aspectScreen),
            GateFit.Fill => aspectSensor > aspectScreen ? vfov : 2.0f * math.atan(math.tan(hfov * 0.5f) / aspectScreen),
            GateFit.Overscan => aspectSensor > aspectScreen ? 2.0f * math.atan(math.tan(hfov * 0.5f) / aspectScreen) : vfov,
            _ => vfov,
        };

        var m11 = 1.0f / math.tan(vfovFinal * 0.5f);
        var m00 = m11 / aspectScreen;
        var near = MathF.Max(0.05f, editorCam.camera.nearClipPlane);
        var far = MathF.Max(near + 1.0f, editorCam.camera.farClipPlane);
        var m22 = far / (far - near);
        var m23 = -(far * near) / (far - near);

        projection = new float4x4(
            m00, 0.0f, 0.0f, 0.0f,
            0.0f, m11, 0.0f, 0.0f,
            0.0f, 0.0f, m22, m23,
            0.0f, 0.0f, 1.0f, 0.0f
        );
    }

    private static ComponentNode? FindComponentNode(EntityNode entity, Type componentType)
    {
        for (var i = 0; i < entity.Components.Count; i++)
        {
            if (entity.Components[i].ComponentType == componentType)
            {
                return entity.Components[i];
            }
        }

        return null;
    }

    private static PropertyNode? FindPropertyNode(ComponentNode? comp, string propertyName)
    {
        if (comp == null)
        {
            return null;
        }

        for (var i = 0; i < comp.Properties.Count; i++)
        {
            if (comp.Properties[i].Name == propertyName)
            {
                return comp.Properties[i];
            }
        }

        return null;
    }

    private static bool MatrixNearlyEqual(in Matrix4x4 a, in Matrix4x4 b)
    {
        const float eps = 1e-5f;
        return MathF.Abs(a.M11 - b.M11) < eps && MathF.Abs(a.M12 - b.M12) < eps && MathF.Abs(a.M13 - b.M13) < eps && MathF.Abs(a.M14 - b.M14) < eps &&
               MathF.Abs(a.M21 - b.M21) < eps && MathF.Abs(a.M22 - b.M22) < eps && MathF.Abs(a.M23 - b.M23) < eps && MathF.Abs(a.M24 - b.M24) < eps &&
               MathF.Abs(a.M31 - b.M31) < eps && MathF.Abs(a.M32 - b.M32) < eps && MathF.Abs(a.M33 - b.M33) < eps && MathF.Abs(a.M34 - b.M34) < eps &&
               MathF.Abs(a.M41 - b.M41) < eps && MathF.Abs(a.M42 - b.M42) < eps && MathF.Abs(a.M43 - b.M43) < eps && MathF.Abs(a.M44 - b.M44) < eps;
    }
}
