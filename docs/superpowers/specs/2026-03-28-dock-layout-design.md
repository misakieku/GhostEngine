# DockLayout System Design

## Purpose
To create a fully-featured docking layout system for the Ghost Engine Editor using WinUI 3, supporting tab tearing, window popping, and dynamic splitting of regions in a style similar to Unity or Blender.

## Architecture

The DockLayout will be entirely driven by a C# data model that represents a tree of nodes. The UI (`DockLayout` control) will observe this tree and recursively generate the corresponding XAML `Grid` and `TabView` elements.

### Core Data Model (The Node Tree)

```csharp
public abstract class DockNode : INotifyPropertyChanged { }

// Represents a split region (Grid)
public class DockGroupNode : DockNode 
{
    public Orientation Orientation { get; set; } // Horizontal or Vertical
    public ObservableCollection<DockNode> Children { get; }
    public ObservableCollection<GridLength> Sizes { get; } // Replaces Ratios for better WinUI 3 Grid binding
}

// Represents a leaf node containing a TabView
public class DockPanelNode : DockNode
{
    // The items shown in the TabView
    public ObservableCollection<object> Items { get; } 
    public int SelectedIndex { get; set; }
}
```

### Visual Components

1.  **`DockLayout` (Control)**
    *   The root control.
    *   Takes a `DockNode` (usually a `DockGroupNode`) as its `Root`.
    *   Listens to Drag/Drop events to render the transparent drop target overlay over the layout.

2.  **Node Renderers**
    *   A recursive template selector or code-behind builder that converts `DockGroupNode` into a `Grid` with `GridSplitter`s.
    *   Converts `DockPanelNode` into a `NavigationTabView` (or standard `TabView` with customized drag behaviors).

3.  **`DockDropTarget` (Visual Overlay)**
    *   A simple XAML structure (e.g., a colored `Border` with opacity) that highlights a portion of a `DockPanelNode` based on mouse position during a drag operation (Left/Right/Top/Bottom 25% for splitting, Center 50% for merging).

## Interactions & Data Flow

### 1. Internal Dragging (Within the same window)
*   User starts dragging a tab.
*   The `DockLayout` tracks the mouse pointer `DragOver` events.
*   It determines which `DockPanelNode` the mouse is currently hovering over.
*   It calculates relative coordinates to show the Unity-style drop highlight.
*   On **Drop**:
    *   If dropped in the **center**: The tab object is moved from its source `DockPanelNode.Items` to the target `DockPanelNode.Items`.
    *   If dropped on an **edge** (e.g., Right): The target `DockPanelNode` is removed from its parent `DockGroupNode`. A new `DockGroupNode` (Horizontal) is created to replace it. The target node and a *new* `DockPanelNode` (containing the dragged tab) are added as children to this new group.

### 2. Window Tear-Off (Full Docking)
*   User drags a tab completely outside the main window.
*   `TabView.TabDroppedOutside` is triggered.
*   The system creates a new WinUI 3 `Window`.
*   A new `DockLayout` instance is placed in this window.
*   The dragged tab object is removed from its original tree and added to a new `DockPanelNode` inside the new window's tree.
*   *Note: Because WinUI 3 supports multiple windows on the same UI thread, we don't have to worry about cross-thread marshaling of UI elements, making this much simpler than UWP.*

### 3. Empty Node Cleanup
*   When a `DockPanelNode`'s `Items` collection reaches 0 (the last tab is dragged away), it is removed from the tree.
*   If its parent `DockGroupNode` now only has 1 child remaining, that `DockGroupNode` is removed and replaced by its single child, collapsing the tree.

## Implementation Phases
1.  Define the Data Model (`DockNode` structure).
2.  Implement the recursive UI generation (binding the tree to nested Grids and TabViews).
3.  Implement basic tab moving (Merge) between existing `DockPanelNode`s.
4.  Implement edge dropping (Split) and the drop target highlight overlay.
5.  Implement empty node cleanup logic.
6.  Implement multi-window tear-off via `TabDroppedOutside`.
