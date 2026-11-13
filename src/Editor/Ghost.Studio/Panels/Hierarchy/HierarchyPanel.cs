using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Ghost.Studio.Menus;
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
        var bridge = EditorSyncBridge.Instance;
        var (search, setSearch) = UseState(string.Empty);
        var (collapsedIds, setCollapsedIds) = UseState(UseMemo(() => new HashSet<int>(), Array.Empty<object>()));
        var (_, setRevision) = UseState(0);

        UseEffect(() =>
        {
            var rev = 0;
            Action onSelectionChanged = () => setRevision(++rev);
            Action<SceneSnapshot> onSnapshotPublished = _ => setRevision(++rev);
            selection.SelectionChanged += onSelectionChanged;
            bridge.SnapshotPublished += onSnapshotPublished;
            return () =>
            {
                selection.SelectionChanged -= onSelectionChanged;
                bridge.SnapshotPublished -= onSnapshotPublished;
            };
        }, Array.Empty<object>());

        var entities = bridge.LatestSnapshot?.Entities ?? Array.Empty<EntityNode>();
        var visibleRows = BuildVisibleRows(entities, search, collapsedIds);

        var entityElements = new Element[visibleRows.Count];
        for (var i = 0; i < visibleRows.Count; i++)
        {
            var row = visibleRows[i];
            var entity = row.Entity;
            var isSelected = selection.ActiveEntity == entity;
            var isCollapsed = collapsedIds.Contains(entity.Id);

            Element chevron = row.HasChildren
                ? TextBlock(isCollapsed ? "\uE76C" : "\uE70D")
                    .FontFamily("Segoe Fluent Icons")
                    .FontSize(10)
                    .Foreground(isSelected ? GhostColors.TextOnAccent : GhostColors.TextSecondary)
                    .VAlign(VerticalAlignment.Center)
                    .Width(14)
                    .OnPointerPressed((_, e) =>
                    {
                        var next = new HashSet<int>(collapsedIds);
                        if (!next.Add(entity.Id))
                        {
                            next.Remove(entity.Id);
                        }

                        setCollapsedIds(next);
                        e.Handled = true;
                    })
                : Border(Empty()).Width(14);

            entityElements[i] = Border(
                HStack(6,
                    chevron,
                    TextBlock("\uE7C3").FontFamily("Segoe Fluent Icons").FontSize(12).Foreground(isSelected ? GhostColors.TextOnAccent : GhostColors.TextSecondary).VAlign(VerticalAlignment.Center),
                    TextBlock(entity.Name).FontSize(12).Foreground(isSelected ? GhostColors.TextOnAccent : GhostColors.TextPrimary).VAlign(VerticalAlignment.Center)
                ).Padding(left: 8 + row.Depth * 16, top: 5, right: 8, bottom: 5)
            )
            .Background(isSelected ? GhostColors.CardSelected : GhostColors.TransparentHitTarget)
            .CornerRadius(4)
            .Margin(bottom: 2)
            .WithKey($"entity_{entity.Id}")
            .OnPointerPressed((_, _) => selection.Select(entity))
            .WithContextFlyout(EditorMenuBuilder.BuildFlyout("Hierarchy", new MenuContext(entity)));
        }

        return Border(
            Grid(
                columns: new GridSize[] { GridSize.Star() },
                rows: new GridSize[] { GridSize.Auto, GridSize.Star() },

                TextBox(search, setSearch, placeholderText: "Search entities...").AutomationName("Search entities").Margin(bottom: 6).Grid(row: 0, column: 0),

                Border(
                    ScrollView(
                        VStack(2, entityElements)
                    )
                )
                .Background(GhostColors.PanelBackground)
                .WithContextFlyout(EditorMenuBuilder.BuildFlyout("Hierarchy", new MenuContext(null)))
                .Grid(row: 1, column: 0)
            ).Padding(8)
        )
        .Background(GhostColors.PanelBackground)
        .WithContextFlyout(EditorMenuBuilder.BuildFlyout("Hierarchy", new MenuContext(null)));
    }

    private static List<(EntityNode Entity, int Depth, bool HasChildren)> BuildVisibleRows(IReadOnlyList<EntityNode> entities, string search, HashSet<int> collapsedIds)
    {
        var result = new List<(EntityNode Entity, int Depth, bool HasChildren)>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add((entities[i], 0, false));
                }
            }

            return result;
        }

        var byId = new Dictionary<int, EntityNode>(entities.Count);
        var childrenMap = new Dictionary<int, List<EntityNode>>();
        var roots = new List<EntityNode>();

        for (var i = 0; i < entities.Count; i++)
        {
            byId[entities[i].Id] = entities[i];
        }

        for (var i = 0; i < entities.Count; i++)
        {
            var e = entities[i];
            if (e.ParentId.HasValue && byId.ContainsKey(e.ParentId.Value))
            {
                if (!childrenMap.TryGetValue(e.ParentId.Value, out var list))
                {
                    list = new List<EntityNode>();
                    childrenMap[e.ParentId.Value] = list;
                }

                list.Add(e);
            }
            else
            {
                roots.Add(e);
            }
        }

        for (var i = 0; i < roots.Count; i++)
        {
            AppendRecursive(roots[i], 0, childrenMap, collapsedIds, result);
        }

        return result;
    }

    private static void AppendRecursive(EntityNode entity, int depth, Dictionary<int, List<EntityNode>> childrenMap, HashSet<int> collapsedIds, List<(EntityNode, int, bool)> output)
    {
        var hasChildren = childrenMap.TryGetValue(entity.Id, out var children) && children.Count > 0;
        output.Add((entity, depth, hasChildren));

        if (!hasChildren || collapsedIds.Contains(entity.Id))
        {
            return;
        }

        for (var i = 0; i < children!.Count; i++)
        {
            AppendRecursive(children[i], depth + 1, childrenMap, collapsedIds, output);
        }
    }
}
