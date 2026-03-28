# DockLayout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a fully-featured, dynamically splittable, multi-window docking layout system using WinUI 3.

**Architecture:** A C# data model (node tree) drives the recursive generation of XAML `Grid` and `NavigationTabView` controls. Drag and drop events mutate the node tree, and the UI automatically reflects the changes.

**Tech Stack:** C#, WinUI 3, CommunityToolkit.Mvvm (for ObservableObject).

---

### Task 1: Create Core Data Models

**Files:**
- Create: `src/Editor/Ghost.Editor.Core/Controls/Internal/Docking/DockNode.cs`
- Create: `src/Editor/Ghost.Editor.Core/Controls/Internal/Docking/DockGroupNode.cs`
- Create: `src/Editor/Ghost.Editor.Core/Controls/Internal/Docking/DockPanelNode.cs`

- [ ] **Step 1: Write `DockNode` base class**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

public abstract partial class DockNode : ObservableObject
{
    [ObservableProperty]
    private DockGroupNode? _parent;
}
```

- [ ] **Step 2: Write `DockGroupNode` class**

```csharp
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

public partial class DockGroupNode : DockNode
{
    [ObservableProperty]
    private Orientation _orientation = Orientation.Horizontal;

    public ObservableCollection<DockNode> Children { get; } = new();

    public void AddChild(DockNode node)
    {
        node.Parent = this;
        Children.Add(node);
    }

    public void RemoveChild(DockNode node)
    {
        if (Children.Remove(node))
        {
            node.Parent = null;
        }
    }
}
```

- [ ] **Step 3: Write `DockPanelNode` class**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

public partial class DockPanelNode : DockNode
{
    public ObservableCollection<object> Items { get; } = new();

    [ObservableProperty]
    private int _selectedIndex = -1;

    [ObservableProperty]
    private object? _selectedItem;
}
```

- [ ] **Step 4: Commit**

```bash
git add src/Editor/Ghost.Editor.Core/Controls/Internal/Docking/*
git commit -m "feat(dock): add core data models for docking system"
```

---

### Task 2: Implement Tree Renderer in XAML

**Files:**
- Modify: `src/Editor/Ghost.Editor/View/Controls/DockLayout.cs`
- Modify: `src/Editor/Ghost.Editor/View/Controls/DockLayout.xaml`

- [ ] **Step 1: Add DependencyProperty for Root in DockLayout.cs**

```csharp
using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls;

public sealed partial class DockLayout : Control
{
    public DockLayout()
    {
        DefaultStyleKey = typeof(DockLayout);
    }

    public DockGroupNode? Root
    {
        get => (DockGroupNode?)GetValue(RootProperty);
        set => SetValue(RootProperty, value);
    }

    public static readonly DependencyProperty RootProperty =
        DependencyProperty.Register("Root", typeof(DockGroupNode), typeof(DockLayout), new PropertyMetadata(null, OnRootChanged));

    private static void OnRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockLayout layout)
        {
            layout.RenderTree();
        }
    }

    private void RenderTree()
    {
        if (GetTemplateChild("PART_RootGrid") is Grid rootGrid)
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
            // Simple visualizer for now, full grid logic in next step
            var grid = new Grid();
            foreach (var child in groupNode.Children)
            {
                grid.Children.Add(CreateUIForNode(child));
            }
            return grid;
        }
        else if (node is DockPanelNode panelNode)
        {
            return new Ghost.Editor.Controls.NavigationTabView 
            { 
                ItemsSource = panelNode.Items,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }
        return new Grid(); // Fallback
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        RenderTree();
    }
}
```

- [ ] **Step 2: Define ControlTemplate in DockLayout.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:Ghost.Editor.View.Controls">
    <Style TargetType="local:DockLayout">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="local:DockLayout">
                    <Grid x:Name="PART_RootGrid" Background="{ThemeResource ApplicationPageBackgroundThemeBrush}" />
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

- [ ] **Step 3: Commit**

```bash
git add src/Editor/Ghost.Editor/View/Controls/DockLayout.*
git commit -m "feat(dock): implement basic recursive tree renderer"
```

---

### Task 3: Implement DockGroupNode Grid Builder

**Files:**
- Modify: `src/Editor/Ghost.Editor/View/Controls/DockLayout.cs`

- [ ] **Step 1: Replace `CreateUIForNode` Group logic to generate Columns/Rows and GridSplitters**

```csharp
    private UIElement CreateUIForNode(DockNode node)
    {
        if (node is DockGroupNode groupNode)
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
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    Grid.SetColumn((FrameworkElement)childUI, i * 2);
                }
                else
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    Grid.SetRow((FrameworkElement)childUI, i * 2);
                }
                
                grid.Children.Add(childUI);

                // Add GridSplitter between children
                if (i < childCount - 1)
                {
                    if (isHorizontal)
                    {
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        var splitter = new CommunityToolkit.WinUI.Controls.GridSplitter { Width = 4, HorizontalAlignment = HorizontalAlignment.Center, ResizeDirection = CommunityToolkit.WinUI.Controls.GridSplitter.GridResizeDirection.Columns };
                        Grid.SetColumn(splitter, (i * 2) + 1);
                        grid.Children.Add(splitter);
                    }
                    else
                    {
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        var splitter = new CommunityToolkit.WinUI.Controls.GridSplitter { Height = 4, VerticalAlignment = VerticalAlignment.Center, ResizeDirection = CommunityToolkit.WinUI.Controls.GridSplitter.GridResizeDirection.Rows };
                        Grid.SetRow(splitter, (i * 2) + 1);
                        grid.Children.Add(splitter);
                    }
                }
            }

            // Listen to CollectionChanged to trigger re-render
            groupNode.Children.CollectionChanged -= GroupNode_Children_CollectionChanged;
            groupNode.Children.CollectionChanged += GroupNode_Children_CollectionChanged;

            return grid;
        }
        else if (node is DockPanelNode panelNode)
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
            return tabView;
        }
        return new Grid();
    }

    private void GroupNode_Children_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // For MVP, just re-render the whole tree when a group changes structure
        RenderTree();
    }
```

- [ ] **Step 2: Commit**

```bash
git add src/Editor/Ghost.Editor/View/Controls/DockLayout.cs
git commit -m "feat(dock): implement grid and gridsplitter generation for groups"
```

---

### Task 4: Setup Visual Drop Target Overlay

**Files:**
- Modify: `src/Editor/Ghost.Editor/View/Controls/DockLayout.xaml`
- Modify: `src/Editor/Ghost.Editor/View/Controls/DockLayout.cs`

- [ ] **Step 1: Add Drop Overlay to ControlTemplate**

```xml
<ControlTemplate TargetType="local:DockLayout">
    <Grid>
        <Grid x:Name="PART_RootGrid" Background="{ThemeResource ApplicationPageBackgroundThemeBrush}" />
        <Border x:Name="PART_DropTargetOverlay" 
                Background="#660078D4" 
                BorderBrush="#FF0078D4" 
                BorderThickness="2" 
                Visibility="Collapsed" 
                IsHitTestVisible="False" />
    </Grid>
</ControlTemplate>
```

- [ ] **Step 2: Add Fields and ApplyTemplate logic in DockLayout.cs**

```csharp
    private Border? _dropTargetOverlay;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _dropTargetOverlay = GetTemplateChild("PART_DropTargetOverlay") as Border;
        RenderTree();
    }
    
    // Helper enum for later
    public enum DockPosition { Center, Top, Bottom, Left, Right, None }
```

- [ ] **Step 3: Commit**

```bash
git add src/Editor/Ghost.Editor/View/Controls/DockLayout.*
git commit -m "feat(dock): add visual drop target overlay"
```

---

### Task 5: Implement Drag and Drop Calculations (Highlighting)

**Files:**
- Modify: `src/Editor/Ghost.Editor/View/Controls/DockLayout.cs`

- [ ] **Step 1: Attach TabView Drag Events in `CreateUIForNode`**

```csharp
        else if (node is DockPanelNode panelNode)
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
            
            tabView.DragOver += TabView_DragOver;
            tabView.DragLeave += TabView_DragLeave;
            tabView.Drop += TabView_Drop;
            tabView.TabDragStarting += TabView_TabDragStarting;
            
            return tabView;
        }
```

- [ ] **Step 2: Implement Drag Handling Logic**

```csharp
    private object? _draggedItem;
    private DockPanelNode? _sourceNode;
    private DockPosition _currentDropPosition = DockPosition.None;

    private void TabView_TabDragStarting(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabDragStartingEventArgs args)
    {
        _draggedItem = args.Item;
        _sourceNode = sender.Tag as DockPanelNode;
        args.Data.Properties.Add("DockTab", _draggedItem); // Identify our drag
    }

    private void TabView_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey("DockTab") && sender is FrameworkElement targetElement)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            
            var position = e.GetPosition(targetElement);
            double width = targetElement.ActualWidth;
            double height = targetElement.ActualHeight;
            
            double edgeThreshold = 0.25; // 25% of edge triggers split
            
            if (position.X < width * edgeThreshold) _currentDropPosition = DockPosition.Left;
            else if (position.X > width * (1 - edgeThreshold)) _currentDropPosition = DockPosition.Right;
            else if (position.Y < height * edgeThreshold) _currentDropPosition = DockPosition.Top;
            else if (position.Y > height * (1 - edgeThreshold)) _currentDropPosition = DockPosition.Bottom;
            else _currentDropPosition = DockPosition.Center;

            UpdateDropOverlay(targetElement, _currentDropPosition);
        }
    }

    private void TabView_DragLeave(object sender, DragEventArgs e)
    {
        if (_dropTargetOverlay != null)
        {
            _dropTargetOverlay.Visibility = Visibility.Collapsed;
            _currentDropPosition = DockPosition.None;
        }
    }

    private void UpdateDropOverlay(FrameworkElement targetElement, DockPosition position)
    {
        if (_dropTargetOverlay == null) return;
        if (position == DockPosition.None)
        {
            _dropTargetOverlay.Visibility = Visibility.Collapsed;
            return;
        }

        var transform = targetElement.TransformToVisual(this);
        var bounds = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, targetElement.ActualWidth, targetElement.ActualHeight));

        _dropTargetOverlay.Visibility = Visibility.Visible;
        _dropTargetOverlay.Width = double.NaN;
        _dropTargetOverlay.Height = double.NaN;

        switch (position)
        {
            case DockPosition.Center:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top, ActualWidth - bounds.Right, ActualHeight - bounds.Bottom);
                break;
            case DockPosition.Left:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top, ActualWidth - (bounds.Left + bounds.Width / 2), ActualHeight - bounds.Bottom);
                break;
            case DockPosition.Right:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left + bounds.Width / 2, bounds.Top, ActualWidth - bounds.Right, ActualHeight - bounds.Bottom);
                break;
            case DockPosition.Top:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top, ActualWidth - bounds.Right, ActualHeight - (bounds.Top + bounds.Height / 2));
                break;
            case DockPosition.Bottom:
                _dropTargetOverlay.Margin = new Thickness(bounds.Left, bounds.Top + bounds.Height / 2, ActualWidth - bounds.Right, ActualHeight - bounds.Bottom);
                break;
        }
    }
```

- [ ] **Step 3: Commit**

```bash
git add src/Editor/Ghost.Editor/View/Controls/DockLayout.cs
git commit -m "feat(dock): implement drop highlight calculations"
```

---

### Task 6: Implement Dropping (Data Tree Mutation)

**Files:**
- Modify: `src/Editor/Ghost.Editor/View/Controls/DockLayout.cs`

- [ ] **Step 1: Implement `TabView_Drop` logic**

```csharp
    private void TabView_Drop(object sender, DragEventArgs e)
    {
        if (_dropTargetOverlay != null) _dropTargetOverlay.Visibility = Visibility.Collapsed;

        if (_draggedItem == null || _sourceNode == null || !(sender is FrameworkElement targetElement) || !(targetElement.Tag is DockPanelNode targetNode))
            return;

        if (_sourceNode == targetNode && _currentDropPosition == DockPosition.Center)
            return; // Reordering within same tab is handled natively by TabView

        // 1. Remove from source
        _sourceNode.Items.Remove(_draggedItem);
        CleanupEmptyNodes(_sourceNode);

        // 2. Add to target
        if (_currentDropPosition == DockPosition.Center)
        {
            targetNode.Items.Add(_draggedItem);
        }
        else
        {
            // Split scenario
            var parentGroup = targetNode.Parent;
            if (parentGroup != null)
            {
                int index = parentGroup.Children.IndexOf(targetNode);
                parentGroup.Children.RemoveAt(index);

                var newGroup = new DockGroupNode
                {
                    Orientation = (_currentDropPosition == DockPosition.Left || _currentDropPosition == DockPosition.Right) ? Orientation.Horizontal : Orientation.Vertical
                };

                var newPanel = new DockPanelNode();
                newPanel.Items.Add(_draggedItem);

                if (_currentDropPosition == DockPosition.Left || _currentDropPosition == DockPosition.Top)
                {
                    newGroup.AddChild(newPanel);
                    newGroup.AddChild(targetNode);
                }
                else
                {
                    newGroup.AddChild(targetNode);
                    newGroup.AddChild(newPanel);
                }

                parentGroup.Children.Insert(index, newGroup);
            }
        }

        _draggedItem = null;
        _sourceNode = null;
        _currentDropPosition = DockPosition.None;
    }

    private void CleanupEmptyNodes(DockPanelNode panelNode)
    {
        if (panelNode.Items.Count > 0) return;

        var parentGroup = panelNode.Parent;
        if (parentGroup != null)
        {
            parentGroup.RemoveChild(panelNode);
            
            // If group only has 1 child left, collapse it
            if (parentGroup.Children.Count == 1)
            {
                var onlyChild = parentGroup.Children[0];
                var grandParent = parentGroup.Parent;
                if (grandParent != null)
                {
                    int index = grandParent.Children.IndexOf(parentGroup);
                    parentGroup.RemoveChild(onlyChild);
                    grandParent.Children.RemoveAt(index);
                    grandParent.Children.Insert(index, onlyChild);
                }
                else if (parentGroup == Root)
                {
                    // If root is collapsing, the only child becomes the new root
                    parentGroup.RemoveChild(onlyChild);
                    if (onlyChild is DockGroupNode newRootGroup)
                    {
                        Root = newRootGroup;
                    }
                    else
                    {
                        // Wrap panel in a new group to keep Root as a GroupNode
                        var wrapperGroup = new DockGroupNode();
                        wrapperGroup.AddChild(onlyChild);
                        Root = wrapperGroup;
                    }
                }
            }
        }
    }
```

- [ ] **Step 2: Commit**

```bash
git add src/Editor/Ghost.Editor/View/Controls/DockLayout.cs
git commit -m "feat(dock): implement tree mutation on drop and empty node cleanup"
```

---

### Task 7: Implement Window Tear-Off (TabDroppedOutside)

**Files:**
- Create: `src/Editor/Ghost.Editor/View/Windows/DockWindow.xaml`
- Create: `src/Editor/Ghost.Editor/View/Windows/DockWindow.xaml.cs`
- Modify: `src/Editor/Ghost.Editor/View/Controls/DockLayout.cs`

- [ ] **Step 1: Create `DockWindow` wrapper**

`DockWindow.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<winex:WindowEx
    x:Class="Ghost.Editor.View.Windows.DockWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:Ghost.Editor.View.Controls"
    xmlns:winex="using:WinUIEx">
    <Grid>
        <controls:DockLayout x:Name="PART_DockLayout" />
    </Grid>
</winex:WindowEx>
```

`DockWindow.xaml.cs`:
```csharp
using Ghost.Editor.Core.Controls.Internal.Docking;
using WinUIEx;

namespace Ghost.Editor.View.Windows;

public sealed partial class DockWindow : WindowEx
{
    public DockWindow(object initialTabContent)
    {
        InitializeComponent();
        
        // Setup initial single panel layout
        var rootGroup = new DockGroupNode();
        var panel = new DockPanelNode();
        panel.Items.Add(initialTabContent);
        rootGroup.AddChild(panel);
        
        PART_DockLayout.Root = rootGroup;
        
        // Optional: Titlebar setup etc.
    }
}
```

- [ ] **Step 2: Handle `TabDroppedOutside` in `DockLayout.cs`**

Modify `CreateUIForNode` in `DockLayout.cs`:
```csharp
        // ... inside CreateUIForNode for DockPanelNode ...
            tabView.TabDragStarting += TabView_TabDragStarting;
            tabView.TabDroppedOutside += TabView_TabDroppedOutside; // NEW
            
            return tabView;
```

Add event handler:
```csharp
    private void TabView_TabDroppedOutside(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabDroppedOutsideEventArgs args)
    {
        if (_sourceNode != null && _draggedItem != null)
        {
            // Remove from current tree
            _sourceNode.Items.Remove(_draggedItem);
            CleanupEmptyNodes(_sourceNode);

            // Create new window
            var newWindow = new Ghost.Editor.View.Windows.DockWindow(_draggedItem);
            newWindow.Activate();

            _draggedItem = null;
            _sourceNode = null;
            _currentDropPosition = DockPosition.None;
            if (_dropTargetOverlay != null) _dropTargetOverlay.Visibility = Visibility.Collapsed;
        }
    }
```

- [ ] **Step 3: Commit**

```bash
git add src/Editor/Ghost.Editor/View/Windows/DockWindow.* src/Editor/Ghost.Editor/View/Controls/DockLayout.cs
git commit -m "feat(dock): implement tab tear-off to new window"
```
