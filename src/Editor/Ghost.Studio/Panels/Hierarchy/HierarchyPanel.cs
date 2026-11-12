using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Panels.Hierarchy;

public sealed class HierarchyPanel : Component
{
    public override Element Render()
    {
        var selection = EditorSelection.Instance;
        var (search, setSearch) = UseState(string.Empty);
        var snapshot = EditorSyncBridge.Instance.LatestSnapshot;
        var entities = snapshot?.Entities ?? Array.Empty<EntityNode>();

        var (revision, setRevision) = UseState(0);
        UseEffect(() =>
        {
            Action onSelectionChanged = () => setRevision(revision + 1);
            selection.SelectionChanged += onSelectionChanged;
            return () => selection.SelectionChanged -= onSelectionChanged;
        }, Array.Empty<object>());

        var filteredEntities = string.IsNullOrWhiteSpace(search)
            ? entities
            : entities.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        var entityRows = filteredEntities.Select(e =>
        {
            var isSelected = selection.ActiveEntity == e;
            return Border(
                HStack(8,
                    TextBlock("\uE7C3").FontSize(11).Foreground(isSelected ? GhostColors.AccentPrimary : GhostColors.TextSecondary).VAlign(VerticalAlignment.Center),
                    TextBlock(e.Name).FontSize(11).Foreground(isSelected ? "#FFFFFF" : GhostColors.TextPrimary).VAlign(VerticalAlignment.Center)
                ).Padding(horizontal: 8, vertical: 4)
            )
            .Background(isSelected ? GhostColors.CardSelected : "Transparent")
            .CornerRadius(3)
            .Margin(bottom: 2)
            .WithKey($"entity_{e.Id}")
            .OnPointerPressed((_, _) => selection.Select(e));
        }).ToArray();

        return Border(
            VStack(6,
                HStack(6,
                    TextBox(search, setSearch, placeholderText: "Search entities...").Height(22).FontSize(11).HorizontalAlignment(HorizontalAlignment.Stretch)
                ),
                ScrollView(
                    VStack(2, entityRows)
                )
            ).Padding(8)
        ).Background(GhostColors.PanelBackground);
    }
}
