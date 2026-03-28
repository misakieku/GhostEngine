using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.View.Controls;

public sealed partial class DockLayout
{
    private void RenderTree()
    {
        if (GetTemplateChild(PART_ROOT_GRID) is Grid rootGrid)
        {
            rootGrid.Children.Clear();
            if (Root != null)
            {
                var ui = CreateUIForNode(Root);
                rootGrid.Children.Add(ui);
            }
        }
    }

    private UIElement CreateUIForNode(DockNode node)
    {
        if (node is DockGroupNode groupNode)
        {
            return CreateGroupUI(groupNode);
        }
        else if (node is DockPanelNode panelNode)
        {
            return CreatePanelUI(panelNode);
        }

        throw new InvalidOperationException($"Unsupported node type: {node.GetType().Name}");
    }

    private UIElement CreateGroupUI(DockGroupNode groupNode)
    {
        var grid = new Grid();
        bool isHorizontal = groupNode.Orientation == Orientation.Horizontal;
        int childCount = groupNode.Children.Count;

        for (int i = 0; i < childCount; i++)
        {
            var childNode = groupNode.Children[i];
            var childUI = CreateUIForNode(childNode);

            if (isHorizontal)
            {
                var width = (i < groupNode.Sizes.Count) ? groupNode.Sizes[i] : new GridLength(1, GridUnitType.Star);
                var colDef = new ColumnDefinition
                {
                    Width = width,
                    MinWidth = MIN_PANE_SIZE
                };
                grid.ColumnDefinitions.Add(colDef);
                Grid.SetColumn((FrameworkElement)childUI, i * 2);
            }
            else
            {
                var height = (i < groupNode.Sizes.Count) ? groupNode.Sizes[i] : new GridLength(1, GridUnitType.Star);
                var rowDef = new RowDefinition
                {
                    Height = height,
                    MinHeight = MIN_PANE_SIZE
                };
                grid.RowDefinitions.Add(rowDef);
                Grid.SetRow((FrameworkElement)childUI, i * 2);
            }

            grid.Children.Add(childUI);

            // Add GridSplitter between children
            if (i < childCount - 1)
            {
                if (isHorizontal)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var splitter = new CommunityToolkit.WinUI.Controls.GridSplitter
                    {
                        Width = SPLITTER_THICKNESS,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        ResizeDirection = CommunityToolkit.WinUI.Controls.GridSplitter.GridResizeDirection.Columns
                    };
                    Grid.SetColumn(splitter, (i * 2) + 1);
                    grid.Children.Add(splitter);
                }
                else
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var splitter = new CommunityToolkit.WinUI.Controls.GridSplitter
                    {
                        Height = SPLITTER_THICKNESS,
                        VerticalAlignment = VerticalAlignment.Center,
                        ResizeDirection = CommunityToolkit.WinUI.Controls.GridSplitter.GridResizeDirection.Rows
                    };
                    Grid.SetRow(splitter, (i * 2) + 1);
                    grid.Children.Add(splitter);
                }
            }
        }

        // Capture size changes to persist back to model
        grid.LayoutUpdated += (s, e) => SyncSizesToModel(groupNode, grid);

        return grid;
    }

    private void SyncSizesToModel(DockGroupNode groupNode, Grid grid)
    {
        if (_isSyncingSizes) return;

        try
        {
            bool isHorizontal = groupNode.Orientation == Orientation.Horizontal;
            bool changed = false;

            if (isHorizontal)
            {
                for (int i = 0; i < groupNode.Children.Count; i++)
                {
                    if (i < groupNode.Sizes.Count && i * 2 < grid.ColumnDefinitions.Count)
                    {
                        var newWidth = grid.ColumnDefinitions[i * 2].Width;
                        if (!groupNode.Sizes[i].Equals(newWidth))
                        {
                            // Only sync if it's a star or pixel value (not Auto)
                            if (newWidth.IsStar || newWidth.IsAbsolute)
                            {
                                _isSyncingSizes = true;
                                groupNode.Sizes[i] = newWidth;
                                changed = true;
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < groupNode.Children.Count; i++)
                {
                    if (i < groupNode.Sizes.Count && i * 2 < grid.RowDefinitions.Count)
                    {
                        var newHeight = grid.RowDefinitions[i * 2].Height;
                        if (!groupNode.Sizes[i].Equals(newHeight))
                        {
                            if (newHeight.IsStar || newHeight.IsAbsolute)
                            {
                                _isSyncingSizes = true;
                                groupNode.Sizes[i] = newHeight;
                                changed = true;
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            _isSyncingSizes = false;
        }
    }

    private UIElement CreatePanelUI(DockPanelNode panelNode)
    {
        var tabView = new Ghost.Editor.Controls.NavigationTabView
        {
            TabItemsSource = panelNode.Items,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            CanDragTabs = true,
            AllowDrop = true,
            Tag = panelNode // Store reference to data node
        };

        // Bind selection state using TabView DPs
        tabView.SetBinding(TabView.SelectedIndexProperty, new Binding
        {
            Source = panelNode,
            Path = new PropertyPath(nameof(DockPanelNode.SelectedIndex)),
            Mode = BindingMode.TwoWay
        });

        tabView.SetBinding(TabView.SelectedItemProperty, new Binding
        {
            Source = panelNode,
            Path = new PropertyPath(nameof(DockPanelNode.SelectedItem)),
            Mode = BindingMode.TwoWay
        });

        tabView.DragOver += TabView_DragOver;
        tabView.DragLeave += TabView_DragLeave;
        tabView.Drop += TabView_Drop;
        tabView.TabDragStarting += TabView_TabDragStarting;
        tabView.TabDroppedOutside += TabView_TabDroppedOutside;

        return tabView;
    }
}
