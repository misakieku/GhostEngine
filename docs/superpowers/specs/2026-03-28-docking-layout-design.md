# Docking Layout Design

## Overview
A custom WinUI 3 docking layout system for GhostEngine's editor, inspired by `WinUI.Dock` but tailored to support Unity/Blender-style region highlighting and dynamic tab creation.

## Architecture & Components

The system uses a UI-driven approach where custom controls manage their own state and visual tree.

- **`DockingLayout`**: The root control. Manages the overall state, drag-and-drop coordination, and floating windows.
- **`DockPanel`**: A container that splits its area horizontally or vertically (using a `Grid` with `GridSplitter`s).
- **`DockGroup`**: A container that holds multiple documents, rendered as a WinUI 3 `TabView`.
- **`DockDocument`**: The actual content item, representing a single tab and its payload.
- **`FloatingWindow`**: A separate WinUI `Window` that hosts a minimal `DockingLayout` internally for torn-off tabs.
- **`DockRegionHighlight`**: A visual overlay control (e.g., a semi-transparent blue rectangle) used during drag-and-drop to show exactly where the drop will occur (Left, Right, Top, Bottom, or Center).

## Drag and Drop Flow

1. **Start**: Dragging a `TabViewItem` initiates a drag operation. We store a reference to the dragged `DockDocument`.
2. **Over**: As the pointer moves over a `DockGroup` or `DockPanel`, we calculate the relative pointer position to determine the target region (Left 25%, Right 25%, Top 25%, Bottom 25%, or Center 50%). We display the `DockRegionHighlight` over that specific area.
3. **Drop**:
   - If dropped on an edge region (Left/Right/Top/Bottom), we split the target container: we create a new `DockPanel`, move the existing content to one side, and place the dragged document on the other.
   - If dropped in the Center of a `DockGroup`, we add the document as a new tab to that group.
   - If dropped outside the main window bounds, we create a new `FloatingWindow` and place the document inside it.

## Dynamic Creation API

The `DockingLayout` will expose methods to allow programmatically adding new panels or tabs at runtime (e.g., for a "plus icon" scenario).

```csharp
public void AddDocument(DockDocument document, DockTarget target, DockGroup? targetGroup = null);
```

## Implementation Details

- The controls will be created under `src\Editor\Ghost.Editor\View\Controls\Docking\`.
- We will use WinUI 3's built-in drag and drop APIs (`CanDrag`, `DragStarting`, `AllowDrop`, `DragOver`, `Drop`).
- The `DockRegionHighlight` will be a `Border` or `Rectangle` added to the `DockingLayout`'s visual tree (e.g., in a `Popup` or an overlay `Grid`) and positioned absolutely based on the current drag target.
