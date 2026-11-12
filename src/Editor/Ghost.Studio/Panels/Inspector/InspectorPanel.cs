using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Selection;
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
        var activeEntity = selection.ActiveEntity;

        var (revision, setRevision) = UseState(0);
        UseEffect(() =>
        {
            Action onSelectionChanged = () => setRevision(revision + 1);
            Action onHistoryChanged = () => setRevision(revision + 1);
            selection.SelectionChanged += onSelectionChanged;
            CommandHistory.Instance.HistoryChanged += onHistoryChanged;

            return () =>
            {
                selection.SelectionChanged -= onSelectionChanged;
                CommandHistory.Instance.HistoryChanged -= onHistoryChanged;
            };
        }, Array.Empty<object>());

        if (activeEntity == null)
        {
            return Border(
                TextBlock("Select an entity to view properties").Foreground(GhostColors.TextTertiary).FontSize(11).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center)
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
                VStack(8,
                    Border(
                        HStack(8,
                            TextBlock("\uE7C3").FontSize(14).Foreground(GhostColors.AccentPrimary).VAlign(VerticalAlignment.Center),
                            TextBlock(activeEntity.Name).FontSize(13).SemiBold().Foreground(GhostColors.TextPrimary).VAlign(VerticalAlignment.Center),
                            TextBlock($"ID: {activeEntity.Id}").FontSize(10).Foreground(GhostColors.TextTertiary).VAlign(VerticalAlignment.Center).HAlign(HorizontalAlignment.Right)
                        ).Padding(horizontal: 10, vertical: 6)
                    )
                    .Background(GhostColors.CardBackground)
                    .CornerRadius(4)
                    .WithBorder(GhostColors.SubtleBorder, 1),

                    VStack(6, componentCards.ToArray())
                ).Padding(8)
            )
        ).Background(GhostColors.PanelBackground);
    }
}
