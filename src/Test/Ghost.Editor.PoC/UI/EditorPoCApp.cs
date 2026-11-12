using Ghost.Editor.PoC.Commands;
using Ghost.Editor.PoC.Drawers;
using Ghost.Editor.PoC.Ecs;
using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Editor.PoC.UI;

public class EditorPoCApp : Component
{
    private static readonly EngineHost s_host = new();
    private static readonly CommandHistory s_history = new();
    private static readonly PropertyDrawerRegistry s_propertyDrawers = new();
    private static readonly ComponentDrawerRegistry s_componentDrawers = new();

    public override Element Render()
    {
        var host = s_host;
        var (snapshotVer, setSnapshotVer) = UseState(0L, threadSafe: true);
        var (historyVer, setHistoryVer) = UseState(0);

        // Selected entity tracking
        var selectedEntity = host.SelectedEntity ?? host.Entities[0];
        var (selectedGuid, setSelectedGuid) = UseState(selectedEntity.EditorId);

        // Poll engine state at 20 Hz
        UseEffect(() =>
        {
            var timer = new System.Threading.Timer(_ =>
            {
                var curVer = host.Bridge.SnapshotVersion;
                setSnapshotVer(curVer);
            }, null, 0, 50);

            return () => timer.Dispose();
        }, Array.Empty<object>());

        // Subscribe to CommandHistory changes for Undo/Redo button reactivity
        UseEffect(() =>
        {
            Action onHistoryChanged = () => setHistoryVer(historyVer + 1);
            s_history.HistoryChanged += onHistoryChanged;
            return () => s_history.HistoryChanged -= onHistoryChanged;
        }, Array.Empty<object>());

        var drawerContext = new DrawerContext(s_history, s_propertyDrawers);

        var entityButtons = host.Entities.Select(e =>
        {
            var isSelected = e.EditorId == selectedEntity.EditorId;
            return (Element)Border(HStack(12,
                TextBlock(GetEntityIcon(e.Name)).FontSize(18),
                VStack(2,
                    TextBlock(e.Name).FontSize(14),
                    TextBlock($"{e.Components.Count} Components | ECS ID: {e.EcsEntity.ID}").FontSize(11)
                )
            ))
            .Padding(10)
            .BorderBrush(isSelected ? "#0078D4" : "#40808080")
            .BorderThickness(isSelected ? 2 : 1)
            .CornerRadius(6)
            .OnPointerPressed((_, _) =>
            {
                host.SelectEntity(e);
                setSelectedGuid(e.EditorId);
            });
        }).ToArray();

        // Render each component via its registered ComponentDrawer
        var componentCards = selectedEntity.Components.Select(comp =>
        {
            var drawer = s_componentDrawers.GetDrawer(comp.ComponentType);
            return drawer.Draw(comp, drawerContext);
        }).ToArray();

        var undoText = s_history.CanUndo ? $"Undo: {s_history.NextUndoName}" : "Undo (Ctrl+Z)";
        var redoText = s_history.CanRedo ? $"Redo: {s_history.NextRedoName}" : "Redo (Ctrl+Y)";

        return VStack(12,
            // ── TOP TOOLBAR ──────────────────────────────────────────────
            Border(HStack(16,
                TextBlock("GhostStudio — Hierarchy, Inspector & Undo/Redo").FontSize(18),
                TextBlock($"Sim: {(host.IsPaused ? "PAUSED" : "RUNNING @ 60 FPS")} | Frame: {host.FrameCount}"),
                Button(host.IsPaused ? "Resume Sim" : "Pause Sim", () => host.IsPaused = !host.IsPaused).AutomationName("Toggle Simulation"),
                Button("Step 1 Frame", () => host.StepSingleFrame()).AutomationName("Step Single Frame"),
                Button(undoText, () => s_history.Undo()).IsEnabled(s_history.CanUndo).AutomationName("Undo Command"),
                Button(redoText, () => s_history.Redo()).IsEnabled(s_history.CanRedo).AutomationName("Redo Command")
            )).Padding(12).BorderBrush("#40808080").BorderThickness(1).CornerRadius(8),

            // ── MAIN SPLIT PANELS ────────────────────────────────────────
            HStack(12,
                // ── LEFT: SCENE HIERARCHY ────────────────────────────────
                Border(VStack(8,
                    HStack(8,
                        TextBlock("Scene Hierarchy").FontSize(16),
                        TextBlock($"({host.Entities.Count} entities)")
                    ),
                    VStack(6, entityButtons)
                ))
                .Padding(12).BorderBrush("#40808080").BorderThickness(1).CornerRadius(8).Width(320),

                // ── RIGHT: DYNAMIC COMPONENT INSPECTOR ───────────────────
                Border(VStack(12,
                    // Entity Header
                    Border(HStack(12,
                        TextBlock(GetEntityIcon(selectedEntity.Name)).FontSize(24),
                        VStack(4,
                            HStack(8,
                                TextBlock("Entity Name:").FontSize(13),
                                TextBox(value: selectedEntity.Name, onChanged: newName => selectedEntity.Name = newName)
                                    .AutomationName("Entity Name")
                                    .OnLostFocus((_, _) => { /* auto-persisted on name edit */ })
                            ),
                            TextBlock($"GUID: {selectedEntity.EditorId} | Handle: Entity(ID={selectedEntity.EcsEntity.ID}, Gen={selectedEntity.EcsEntity.Generation})").FontSize(11)
                        )
                    )).Padding(10).BorderBrush("#40808080").BorderThickness(1).CornerRadius(6),

                    // Components list rendered via extensible ComponentDrawers
                    VStack(10, componentCards)
                ))
                .Padding(12).BorderBrush("#40808080").BorderThickness(1).CornerRadius(8)
            )
        )
        .Padding(12)
        .IsTabStop(true)
        .OnKeyDown((_, e) =>
        {
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            if (ctrl && e.Key == Windows.System.VirtualKey.Z)
            {
                s_history.Undo();
                e.Handled = true;
            }
            else if (ctrl && e.Key == Windows.System.VirtualKey.Y)
            {
                s_history.Redo();
                e.Handled = true;
            }
        });
    }

    private static string GetEntityIcon(string name) => name switch
    {
        "Player" => "\uE77B",             // Contact / Person
        "Main Camera" => "\uE714",        // Camera
        "Directional Light" => "\uE706",  // Brightness / Light
        "Counter Worker" => "\uE768",     // Play / Worker
        "Spinning Cube" => "\uE74C",      // Shape / Cube
        _ => "\uE790"                     // Folder / General
    };
}
