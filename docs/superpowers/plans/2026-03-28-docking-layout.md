# Docking Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a custom WinUI 3 docking layout system for GhostEngine's editor with Unity/Blender-style region highlighting and dynamic tab creation.

**Architecture:** A UI-driven approach where custom controls (`DockingLayout`, `DockPanel`, `DockGroup`, `DockDocument`) manage their own state and visual tree. Drag-and-drop manipulates the visual tree directly.

**Tech Stack:** C#, WinUI 3, Windows App SDK

---

### Task 1: Core Enums and Base Classes

**Files:**
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/Enums.cs`
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockModule.cs`
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockContainer.cs`

- [ ] **Step 1: Create Enums**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/Enums.cs`:
```csharp
namespace Ghost.Editor.View.Controls.Docking;

public enum DockTarget
{
    Center,
    Left,
    Right,
    Top,
    Bottom
}
```

- [ ] **Step 2: Create DockModule base class**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockModule.cs`:
```csharp
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

public abstract class DockModule : Control
{
    public DockContainer? Owner { get; internal set; }
    public DockingLayout? Root { get; internal set; }
    
    public void Detach()
    {
        Owner?.Children.Remove(this);
        Owner = null;
    }
}
```

- [ ] **Step 3: Create DockContainer base class**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockContainer.cs`:
```csharp
using System.Collections.ObjectModel;

namespace Ghost.Editor.View.Controls.Docking;

public abstract class DockContainer : DockModule
{
    public ObservableCollection<DockModule> Children { get; } = new();

    protected DockContainer()
    {
        Children.CollectionChanged += OnChildrenChanged;
    }

    private void OnChildrenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (DockModule module in e.OldItems)
            {
                module.Owner = null;
            }
        }

        if (e.NewItems != null)
        {
            foreach (DockModule module in e.NewItems)
            {
                module.Owner = this;
                module.Root = Root;
            }
        }
        
        OnChildrenUpdated();
    }
    
    protected virtual void OnChildrenUpdated() { }
}
```

- [ ] **Step 4: Commit**
```bash
git add src/Editor/Ghost.Editor/View/Controls/Docking/Enums.cs src/Editor/Ghost.Editor/View/Controls/Docking/DockModule.cs src/Editor/Ghost.Editor/View/Controls/Docking/DockContainer.cs
git commit -m "feat(docking): add core enums and base classes"
```

---

### Task 2: DockDocument and DockGroup

**Files:**
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockDocument.cs`
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockGroup.cs`
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockGroup.xaml`

- [ ] **Step 1: Create DockDocument**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockDocument.cs`:
```csharp
using Microsoft.UI.Xaml;

namespace Ghost.Editor.View.Controls.Docking;

public class DockDocument : DockModule
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(DockDocument), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(DockDocument), new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public DockDocument()
    {
        DefaultStyleKey = typeof(DockDocument);
    }
}
```

- [ ] **Step 2: Create DockGroup XAML**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockGroup.xaml`:
```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:Ghost.Editor.View.Controls.Docking">

    <Style TargetType="local:DockGroup">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="local:DockGroup">
                    <Grid>
                        <TabView x:Name="PART_TabView" 
                                 IsAddTabButtonVisible="False"
                                 CanDragTabs="True"
                                 CanReorderTabs="True"
                                 AllowDrop="True">
                        </TabView>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

- [ ] **Step 3: Create DockGroup Code-Behind**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockGroup.cs`:
```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

[TemplatePart(Name = "PART_TabView", Type = typeof(TabView))]
public class DockGroup : DockContainer
{
    private TabView? _tabView;

    public DockGroup()
    {
        DefaultStyleKey = typeof(DockGroup);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _tabView = GetTemplateChild("PART_TabView") as TabView;
        UpdateTabs();
    }

    protected override void OnChildrenUpdated()
    {
        UpdateTabs();
    }

    private void UpdateTabs()
    {
        if (_tabView == null) return;

        _tabView.TabItems.Clear();
        foreach (var child in Children)
        {
            if (child is DockDocument doc)
            {
                var tabItem = new TabViewItem
                {
                    Header = doc.Title,
                    Content = doc.Content,
                    Tag = doc
                };
                _tabView.TabItems.Add(tabItem);
            }
        }
    }
}
```

- [ ] **Step 4: Commit**
```bash
git add src/Editor/Ghost.Editor/View/Controls/Docking/DockDocument.cs src/Editor/Ghost.Editor/View/Controls/Docking/DockGroup.cs src/Editor/Ghost.Editor/View/Controls/Docking/DockGroup.xaml
git commit -m "feat(docking): add DockDocument and DockGroup"
```

---

### Task 3: DockPanel

**Files:**
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockPanel.cs`
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockPanel.xaml`

- [ ] **Step 1: Create DockPanel XAML**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockPanel.xaml`:
```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:Ghost.Editor.View.Controls.Docking">

    <Style TargetType="local:DockPanel">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="local:DockPanel">
                    <Grid x:Name="PART_Grid" />
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

- [ ] **Step 2: Create DockPanel Code-Behind**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockPanel.cs`:
```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Controls;

namespace Ghost.Editor.View.Controls.Docking;

[TemplatePart(Name = "PART_Grid", Type = typeof(Grid))]
public class DockPanel : DockContainer
{
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(DockPanel), new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private Grid? _grid;

    public DockPanel()
    {
        DefaultStyleKey = typeof(DockPanel);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _grid = GetTemplateChild("PART_Grid") as Grid;
        UpdateLayoutStructure();
    }

    protected override void OnChildrenUpdated()
    {
        UpdateLayoutStructure();
    }

    private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((DockPanel)d).UpdateLayoutStructure();
    }

    private void UpdateLayoutStructure()
    {
        if (_grid == null) return;

        _grid.Children.Clear();
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();

        if (Children.Count == 0) return;

        if (Orientation == Orientation.Horizontal)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var child = Children[i];
                Grid.SetColumn(child, i * 2);
                _grid.Children.Add(child);

                if (i < Children.Count - 1)
                {
                    _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var splitter = new GridSplitter { ResizeDirection = GridSplitter.GridResizeDirection.Columns, Width = 4 };
                    Grid.SetColumn(splitter, i * 2 + 1);
                    _grid.Children.Add(splitter);
                }
            }
        }
        else
        {
            for (int i = 0; i < Children.Count; i++)
            {
                _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                var child = Children[i];
                Grid.SetRow(child, i * 2);
                _grid.Children.Add(child);

                if (i < Children.Count - 1)
                {
                    _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var splitter = new GridSplitter { ResizeDirection = GridSplitter.GridResizeDirection.Rows, Height = 4 };
                    Grid.SetRow(splitter, i * 2 + 1);
                    _grid.Children.Add(splitter);
                }
            }
        }
    }
}
```

- [ ] **Step 3: Commit**
```bash
git add src/Editor/Ghost.Editor/View/Controls/Docking/DockPanel.cs src/Editor/Ghost.Editor/View/Controls/Docking/DockPanel.xaml
git commit -m "feat(docking): add DockPanel"
```

---

### Task 4: DockRegionHighlight and DockingLayout

**Files:**
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockRegionHighlight.cs`
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockRegionHighlight.xaml`
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.cs`
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.xaml`

- [ ] **Step 1: Create DockRegionHighlight XAML**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockRegionHighlight.xaml`:
```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:Ghost.Editor.View.Controls.Docking">

    <Style TargetType="local:DockRegionHighlight">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="local:DockRegionHighlight">
                    <Border Background="#400078D7" BorderBrush="#800078D7" BorderThickness="2" CornerRadius="4" />
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

- [ ] **Step 2: Create DockRegionHighlight Code-Behind**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockRegionHighlight.cs`:
```csharp
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

public class DockRegionHighlight : Control
{
    public DockRegionHighlight()
    {
        DefaultStyleKey = typeof(DockRegionHighlight);
        IsHitTestVisible = false;
    }
}
```

- [ ] **Step 3: Create DockingLayout XAML**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.xaml`:
```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:Ghost.Editor.View.Controls.Docking">

    <Style TargetType="local:DockingLayout">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="local:DockingLayout">
                    <Grid>
                        <ContentPresenter x:Name="PART_Content" Content="{TemplateBinding RootPanel}" />
                        <Canvas x:Name="PART_OverlayCanvas" IsHitTestVisible="False">
                            <local:DockRegionHighlight x:Name="PART_Highlight" Visibility="Collapsed" />
                        </Canvas>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

- [ ] **Step 4: Create DockingLayout Code-Behind**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.cs`:
```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

[TemplatePart(Name = "PART_Content", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_OverlayCanvas", Type = typeof(Canvas))]
[TemplatePart(Name = "PART_Highlight", Type = typeof(DockRegionHighlight))]
public class DockingLayout : Control
{
    public static readonly DependencyProperty RootPanelProperty = DependencyProperty.Register(
        nameof(RootPanel), typeof(DockPanel), typeof(DockingLayout), new PropertyMetadata(null, OnRootPanelChanged));

    public DockPanel? RootPanel
    {
        get => (DockPanel?)GetValue(RootPanelProperty);
        set => SetValue(RootPanelProperty, value);
    }

    private Canvas? _overlayCanvas;
    private DockRegionHighlight? _highlight;

    public DockingLayout()
    {
        DefaultStyleKey = typeof(DockingLayout);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _overlayCanvas = GetTemplateChild("PART_OverlayCanvas") as Canvas;
        _highlight = GetTemplateChild("PART_Highlight") as DockRegionHighlight;
    }

    private static void OnRootPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingLayout layout && e.NewValue is DockPanel panel)
        {
            panel.Root = layout;
        }
    }

    public void AddDocument(DockDocument document, DockTarget target, DockGroup? targetGroup = null)
    {
        if (RootPanel == null)
        {
            RootPanel = new DockPanel();
        }

        if (targetGroup == null)
        {
            if (RootPanel.Children.Count == 0)
            {
                var group = new DockGroup();
                group.Children.Add(document);
                RootPanel.Children.Add(group);
                return;
            }
            targetGroup = RootPanel.Children[0] as DockGroup;
        }

        if (targetGroup != null)
        {
            targetGroup.Children.Add(document);
        }
    }
}
```

- [ ] **Step 5: Commit**
```bash
git add src/Editor/Ghost.Editor/View/Controls/Docking/DockRegionHighlight.cs src/Editor/Ghost.Editor/View/Controls/Docking/DockRegionHighlight.xaml src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.cs src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.xaml
git commit -m "feat(docking): add DockRegionHighlight and DockingLayout"
```

---

### Task 5: Drag and Drop Logic

**Files:**
- Modify: `src/Editor/Ghost.Editor/View/Controls/Docking/DockGroup.cs`
- Modify: `src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.cs`

- [ ] **Step 1: Implement Drag and Drop in DockGroup**
Modify `src/Editor/Ghost.Editor/View/Controls/Docking/DockGroup.cs` to handle drag events on the TabView:
```csharp
// Add to OnApplyTemplate:
if (_tabView != null)
{
    _tabView.TabDragStarting += OnTabDragStarting;
    _tabView.TabDroppedOutside += OnTabDroppedOutside;
    _tabView.DragOver += OnDragOver;
    _tabView.Drop += OnDrop;
    _tabView.DragLeave += OnDragLeave;
}

// Add methods:
private void OnTabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
{
    if (args.Tab.Tag is DockDocument doc)
    {
        args.Data.Properties.Add("DockDocument", doc);
        doc.Detach();
    }
}

private void OnTabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
{
    if (args.Tab.Tag is DockDocument doc)
    {
        Root?.CreateFloatingWindow(doc);
    }
}

private void OnDragOver(object sender, DragEventArgs e)
{
    if (e.DataView.Properties.ContainsKey("DockDocument"))
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        Root?.ShowHighlight(this, e.GetPosition(this));
    }
}

private void OnDrop(object sender, DragEventArgs e)
{
    if (e.DataView.Properties.TryGetValue("DockDocument", out var obj) && obj is DockDocument doc)
    {
        Root?.HandleDrop(doc, this, e.GetPosition(this));
    }
}

private void OnDragLeave(object sender, DragEventArgs e)
{
    Root?.HideHighlight();
}
```

- [ ] **Step 2: Implement Highlight and Drop in DockingLayout**
Modify `src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.cs` to add `ShowHighlight`, `HideHighlight`, `HandleDrop`, and `CreateFloatingWindow`:
```csharp
// Add methods:
internal void ShowHighlight(DockGroup targetGroup, Windows.Foundation.Point position)
{
    if (_highlight == null || _overlayCanvas == null) return;

    _highlight.Visibility = Visibility.Visible;
    var target = CalculateDockTarget(targetGroup, position);
    
    // Calculate rect based on target (simplified for brevity, needs actual math based on targetGroup's ActualWidth/Height)
    double width = targetGroup.ActualWidth;
    double height = targetGroup.ActualHeight;
    double x = 0, y = 0;

    switch (target)
    {
        case DockTarget.Left: width /= 2; break;
        case DockTarget.Right: width /= 2; x = width; break;
        case DockTarget.Top: height /= 2; break;
        case DockTarget.Bottom: height /= 2; y = height; break;
        case DockTarget.Center: break;
    }

    var transform = targetGroup.TransformToVisual(_overlayCanvas);
    var point = transform.TransformPoint(new Windows.Foundation.Point(x, y));

    Canvas.SetLeft(_highlight, point.X);
    Canvas.SetTop(_highlight, point.Y);
    _highlight.Width = width;
    _highlight.Height = height;
}

internal void HideHighlight()
{
    if (_highlight != null) _highlight.Visibility = Visibility.Collapsed;
}

internal void HandleDrop(DockDocument doc, DockGroup targetGroup, Windows.Foundation.Point position)
{
    HideHighlight();
    var target = CalculateDockTarget(targetGroup, position);

    if (target == DockTarget.Center)
    {
        targetGroup.Children.Add(doc);
    }
    else
    {
        // Split logic: create new DockPanel, move targetGroup and doc into it
        var parentPanel = targetGroup.Owner as DockPanel;
        if (parentPanel != null)
        {
            int index = parentPanel.Children.IndexOf(targetGroup);
            targetGroup.Detach();

            var newPanel = new DockPanel { Orientation = (target == DockTarget.Left || target == DockTarget.Right) ? Orientation.Horizontal : Orientation.Vertical };
            var newGroup = new DockGroup();
            newGroup.Children.Add(doc);

            if (target == DockTarget.Left || target == DockTarget.Top)
            {
                newPanel.Children.Add(newGroup);
                newPanel.Children.Add(targetGroup);
            }
            else
            {
                newPanel.Children.Add(targetGroup);
                newPanel.Children.Add(newGroup);
            }

            parentPanel.Children.Insert(index, newPanel);
        }
    }
}

private DockTarget CalculateDockTarget(DockGroup group, Windows.Foundation.Point position)
{
    double w = group.ActualWidth;
    double h = group.ActualHeight;
    double x = position.X;
    double y = position.Y;

    if (x < w * 0.25) return DockTarget.Left;
    if (x > w * 0.75) return DockTarget.Right;
    if (y < h * 0.25) return DockTarget.Top;
    if (y > h * 0.75) return DockTarget.Bottom;
    return DockTarget.Center;
}

internal void CreateFloatingWindow(DockDocument doc)
{
    // To be implemented in Task 6
}
```

- [ ] **Step 3: Commit**
```bash
git add src/Editor/Ghost.Editor/View/Controls/Docking/DockGroup.cs src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.cs
git commit -m "feat(docking): implement drag and drop logic"
```

---

### Task 6: Floating Window

**Files:**
- Create: `src/Editor/Ghost.Editor/View/Controls/Docking/FloatingWindow.cs`
- Modify: `src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.cs`

- [ ] **Step 1: Create FloatingWindow**
Create `src/Editor/Ghost.Editor/View/Controls/Docking/FloatingWindow.cs`:
```csharp
using Microsoft.UI.Xaml;

namespace Ghost.Editor.View.Controls.Docking;

public class FloatingWindow : Window
{
    public FloatingWindow(DockDocument document)
    {
        var layout = new DockingLayout();
        var group = new DockGroup();
        group.Children.Add(document);
        
        var panel = new DockPanel();
        panel.Children.Add(group);
        layout.RootPanel = panel;

        Content = layout;
        
        // Basic window setup
        AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
    }
}
```

- [ ] **Step 2: Update DockingLayout**
Modify `src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.cs` to implement `CreateFloatingWindow`:
```csharp
internal void CreateFloatingWindow(DockDocument doc)
{
    var window = new FloatingWindow(doc);
    window.Activate();
}
```

- [ ] **Step 3: Commit**
```bash
git add src/Editor/Ghost.Editor/View/Controls/Docking/FloatingWindow.cs src/Editor/Ghost.Editor/View/Controls/Docking/DockingLayout.cs
git commit -m "feat(docking): add floating window support"
```
