using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Ghost.Studio.Drawers;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Panels.Inspector;

public sealed class InspectorPanel : Component
{
    private readonly DrawerContext _context = new(CommandHistory.Instance, PropertyDrawerRegistry.Default);

    public override Element Render()
    {
        var selection = EditorSelection.Instance;
        var bridge = EditorSyncBridge.Instance;
        var activeEntity = selection.ActiveEntity;

        var (_, setRevision) = UseState(0);
        UseEffect(() =>
        {
            var rev = 0;
            Action onSelectionChanged = () => setRevision(++rev);
            Action onHistoryChanged = () => setRevision(++rev);
            Action<SceneSnapshot> onSnapshotPublished = _ => setRevision(++rev);
            selection.SelectionChanged += onSelectionChanged;
            CommandHistory.Instance.HistoryChanged += onHistoryChanged;
            bridge.SnapshotPublished += onSnapshotPublished;

            return () =>
            {
                selection.SelectionChanged -= onSelectionChanged;
                CommandHistory.Instance.HistoryChanged -= onHistoryChanged;
                bridge.SnapshotPublished -= onSnapshotPublished;
            };
        }, Array.Empty<object>());

        if (activeEntity == null)
        {
            return Border(
                TextBlock("Select an entity to view properties").Foreground(GhostColors.TextTertiary).FontSize(12).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center)
            ).Background(GhostColors.PanelBackground).Padding(16);
        }

        var componentCards = new List<Element>();
        for (var i = 0; i < activeEntity.Components.Count; i++)
        {
            var comp = activeEntity.Components[i];
            var drawer = ComponentDrawerRegistry.Default.GetDrawer(comp.ComponentType);
            componentCards.Add(drawer.Draw(comp, _context));
        }

        return Border(
            ScrollView(
                VStack(GhostLayout.CARD_GAP,
                    Border(
                        Grid(
                            columns: new GridSize[] { GridSize.Auto, GridSize.Star(), GridSize.Auto },
                            rows: new GridSize[] { GridSize.Auto },
                            TextBlock("\uE7C3").FontFamily("Segoe Fluent Icons").FontSize(14).Foreground(GhostColors.AccentPrimary).VAlign(VerticalAlignment.Center).Margin(right: 8).Grid(row: 0, column: 0),
                            TextBlock(activeEntity.Name).FontSize(13).SemiBold().Foreground(GhostColors.TextPrimary).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 1),
                            TextBlock($"ID: {activeEntity.Id}").FontSize(11).Foreground(GhostColors.TextTertiary).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 2)
                        ).Padding(horizontal: 10, vertical: 6)
                    )
                    .Background(GhostColors.CardBackground)
                    .CornerRadius(GhostLayout.CORNER_RADIUS)
                    .WithBorder(GhostColors.CardBorder, 1),

                    VStack(GhostLayout.CARD_GAP, componentCards.ToArray()),

                    Button("Add Component", () => {})
                        .HAlign(HorizontalAlignment.Stretch)
                        .WithFlyout(Component<AddComponentFlyout, EntityNode>(activeEntity))
                ).Padding(GhostLayout.PANEL_PADDING)
            )
        ).Background(GhostColors.PanelBackground);
    }

    private sealed class AddComponentFlyout : Component<EntityNode>
    {
        public override Element Render()
        {
            var entity = Props;
            var (filter, setFilter) = UseState(string.Empty);
            var existingTypes = new HashSet<Type>();
            for (var i = 0; i < entity.Components.Count; i++)
            {
                existingTypes.Add(entity.Components[i].ComponentType);
            }

            var allTypes = ComponentNodeFactory.DiscoverComponentTypes();
            var rows = new List<Element>();
            for (var i = 0; i < allTypes.Count; i++)
            {
                var t = allTypes[i];
                if (existingTypes.Contains(t))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(filter) && !t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var capturedType = t;
                rows.Add(
                    Button(capturedType.Name, () => CommandHistory.Instance.Execute(new AddComponentCommand(entity, capturedType)))
                        .SubtleButton()
                        .HAlign(HorizontalAlignment.Stretch)
                        .HorizontalContentAlignment(HorizontalAlignment.Left)
                        .WithKey(capturedType.FullName ?? capturedType.Name)
                );
            }

            return VStack(6,
                TextBox(filter, setFilter, placeholderText: "Filter components...").AutomationName("Filter components"),
                ScrollView(
                    rows.Count > 0
                        ? VStack(2, rows.ToArray())
                        : TextBlock("No matching components").FontSize(12).Foreground(GhostColors.TextTertiary).Padding(8)
                ).MaxHeight(260)
            ).Width(240).Padding(4);
        }
    }
}
