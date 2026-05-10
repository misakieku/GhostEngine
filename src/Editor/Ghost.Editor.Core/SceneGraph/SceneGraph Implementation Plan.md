# SceneGraph Implementation Plan

## Summary of Design Decisions

| Decision | Choice |
|----------|--------|
| Hierarchy data model | Linked-list (`parent`/`firstChild`/`nextSibling`), efficient for ECS |
| Editor ↔ World relationship | Mirror — editor world holds ECS entities, SceneGraph reflects them as observable nodes |
| Transform representation | `LocalToWorld` matrix only (no separate PRS components) |
| Implementation order | Data structures → HierarchySystem → Mirror bridge → UI → Serialization |

---

## Phase 1: Fix SceneGraph Data Structures

**Goal:** Make `EntityNode`, `SceneNode`, and `SceneGraphNode` correct, complete, and constructible.

### 1.1 Enhance `SceneGraphNode`

**File:** `SceneGraphNode.cs`

- Add a `World` property so every node knows which world it belongs to.
- Make the constructor accept `World` and `string name`.
- Keep `ObservableObject` for MVVM binding.
- Keep `Children` as `ObservableCollection<SceneGraphNode>`.

```csharp
public abstract partial class SceneGraphNode : ObservableObject, IInspectable
{
    public World World { get; }
    
    [ObservableProperty]
    public partial string Name { get; set; }

    public ObservableCollection<SceneGraphNode> Children { get; } = new();

    protected SceneGraphNode(World world, string name)
    {
        World = world;
        Name = name;
    }

    // ... existing abstract members
}
```

### 1.2 Enhance `SceneNode`

**File:** `SceneNode.cs`

- Add a `Scene` field referencing the runtime `Scene` struct.
- Add a constructor: `SceneNode(World world, Scene scene, string name)`.
- Remove `XamlReader.Load()` template generation (Phase 4 will move to XAML resources).

```csharp
public sealed partial class SceneNode : SceneGraphNode
{
    public Scene Scene { get; }

    public SceneNode(World world, Scene scene, string name)
        : base(world, name)
    {
        Scene = scene;
    }

    // icon, header, inspector, template
}
```

### 1.3 Enhance `EntityNode`

**File:** `EntityNode.cs`

- Add a constructor: `EntityNode(World world, Entity entity, string name)`.
- Remove `XamlReader.Load()` template.
- Entity reference is read-only after construction (immutable node identity).

```csharp
public sealed partial class EntityNode : SceneGraphNode
{
    public Entity Entity { get; }

    public EntityNode(World world, Entity entity, string name)
        : base(world, name)
    {
        Entity = entity;
    }

    // icon, header, inspector, template
}
```

### 1.4 Add `SceneGraphBuilder`

**New file:** `SceneGraphBuilder.cs`

Used for the **initial full build** of the scene graph from an ECS `World`. After initial construction, incremental updates are handled by `SceneGraphSyncService` (Phase 3).

**Algorithm:**

1. Query all entities with `SceneID` component.
2. Group entities by `SceneID.scene.id`.
3. For each scene group:
   - Create a `SceneNode` (name comes from editor metadata, not from runtime).
   - Walk the `Hierarchy` component linked-list to build the tree:
     - Root entities are those with `Hierarchy.parent == Entity.Invalid`.
     - For each root, create an `EntityNode` and recursively visit `firstChild` → `nextSibling`.
   - Entity names come from a naming system (either a component like `EntityName`, or editor-side metadata stored on the node).
4. Return `ObservableCollection<SceneGraphNode>` (the list of root scenes).

**Name resolution strategy:**

Names are stored directly on `EntityNode.Name`. During initial build, all entities get default names. During incremental sync, existing nodes (and their user-assigned names) are preserved because nodes are matched by `Entity` identity rather than recreated. See Phase 1.5 and 3.2 for details.

### 1.5 Entity Names (Editor-Only)

**No runtime component.** Entity names have no purpose in the runtime ECS. Names live purely on the editor side, stored directly on `EntityNode.Name`.

**Strategy:** Entity names persist naturally across syncs because `EntityNode` instances survive — the sync is incremental, not a full rebuild (see Phase 3.2). Names are read directly from `EntityNode.Name` for inspector display and serialization.

- New entities get a default name (e.g., `"Entity"`).
- User-renamed entities keep their name because the node instance is preserved.
- Destroyed entities have their node removed (names disappear with them).
- No dictionary, no cross-world identity concerns, no runtime footprint.

---

## Phase 2: HierarchySystem (Runtime)

**Goal:** Maintain the `Hierarchy` component's linked-list invariants.

**New file:** `Runtime/Ghost.Engine/Systems/HierarchySystem.cs`

### 2.1 Operations

The system exposes static helper methods (or instance methods on `EntityManager`) for hierarchy mutation. All mutations use `EntityCommandBuffer` internally to avoid structural changes mid-iteration.

| Operation | Description |
|-----------|-------------|
| `SetParent(child, parent)` | Links `child` as a child of `parent`. Updates `parent`, `firstChild`, `nextSibling` on both sides. |
| `RemoveParent(child)` | Unlinks `child` from its current parent. It becomes a root entity. |
| `DestroyEntity(entity)` | Also unlinks from parent, reparents children to grandparent (or makes them roots). |

### 2.2 `SetParent(Entity child, Entity parent)`

**Preconditions:**
- `child.IsValid` and `parent.IsValid`.
- Both entities exist in the world.
- Both have the `Hierarchy` component.
- `child != parent` (no self-parenting).
- `parent` is not a descendant of `child` (no cycles).

**Algorithm:**

1. If `child` already has a parent, call `RemoveParent(child)` first.
2. Get `ref childHierarchy`, `ref parentHierarchy`.
3. Set `childHierarchy.parent = parent`.
4. Set `childHierarchy.nextSibling = parentHierarchy.firstChild`.
5. Set `parentHierarchy.firstChild = child`.

### 2.3 `RemoveParent(Entity child)`

**Preconditions:** `child` has the `Hierarchy` component and has a parent.

**Algorithm:**

1. Get `ref childHierarchy`. Let `parent_entity = childHierarchy.parent`.
2. Get `ref parentHierarchy`.
3. Walk the sibling chain of `parentHierarchy.firstChild` to find and unlink `child`:
   - If `firstChild == child`: set `firstChild = childHierarchy.nextSibling`.
   - Else: walk `prev → current → nextSibling`, set `prev.nextSibling = childHierarchy.nextSibling`.
4. Set `childHierarchy.parent = Entity.Invalid`.
5. Set `childHierarchy.nextSibling = Entity.Invalid`.

### 2.4 Entity Destruction Cleanup

When an entity is destroyed via `EntityManager.DestroyEntity()`:

1. Get its `Hierarchy` component.
2. Call `RemoveParent(entity)`.
3. **Cascade destroy all children:** Walk `firstChild` → `nextSibling` and recursively destroy every descendant entity. Destroy children before destroying the parent to ensure correct cleanup order.
4. When saving/loading, this means saving an entity implies saving its entire subtree. Deleting an entity implies deleting its entire subtree.

### 2.5 Validation

Each method validates invariants in `DEBUG`/`GHOST_EDITOR` builds:
- No entity appears as its own ancestor (cycle detection via ancestor walk).
- `firstChild`/`parent`/`nextSibling` references are all valid entities or `Entity.Invalid`.
- No dangling references.

### 2.6 `HierarchySystem` as an `ISystem`

The system does NOT run every frame automatically. Parent/child mutations are explicit (called from editor commands or loading code). The system's `Update()` is a no-op. It exists as an `ISystem` so it can:
- Register its existence in the system graph.
- Be queried via `World.SystemManager`.
- Hold references to necessary queries.

Alternatively: make it a static utility class that takes `World` as a parameter. **Recommendation:** static utility class (`HierarchyUtility`) to avoid entity archetype cost for a no-op system. The `Hierarchy` component itself drives the tree shape.

---

## Phase 3: Mirror Bridge (Editor ↔ ECS sync)

**Goal:** Keep the `SceneGraphNode` tree in the editor synchronized with the ECS `World`.

### 3.1 `EditorWorldService`

**New file:** `Editor/Ghost.Editor.Core/Services/EditorWorldService.cs`

- Registers as a singleton via `[EditorInjection(ServiceLifetime.Singleton)]`.
- Creates the editor `World` on startup.
- Holds the `World` reference.
- Disposes the world on editor shutdown.

```csharp
[EditorInjection(ServiceLifetime.Singleton)]
public class EditorWorldService : IDisposable
{
    public World EditorWorld { get; }

    public EditorWorldService()
    {
        EditorWorld = World.Create(entityCapacity: 1024);
    }

    public void Dispose()
    {
        World.Destroy(EditorWorld.ID);
    }
}
```

### 3.2 Scene Graph Sync Strategy

**Incremental sync via polling.** Full rebuild is avoided — existing nodes are matched by `Entity` identity and preserved across syncs. This keeps names, selection state, and expanded/collapsed state intact.

**Algorithm (runs on timer, e.g., every 100ms):**

1. Check `EditorWorld.Version` — if unchanged, skip.
2. Query all entities with `SceneID` component from the editor world.
3. Group by `SceneID.scene.id`.
4. **For each scene group:**
   - Find or create the `SceneNode` in `RootNodes` (match by `Scene.ID`).
   - Walk the `Hierarchy` linked-list of roots.
   - **For each root entity:** find existing `EntityNode` in the tree by `Entity` identity. If found, keep the node (name, expansion, selection intact). If not found, create a new `EntityNode` with default name.
   - **Recurse** into `firstChild` → `nextSibling`, matching existing nodes at each level.
5. **Remove stale nodes:** Any `EntityNode` not matched in step 4 is destroyed (entity no longer exists in world). Remove these nodes (and their subtrees) from the tree.
6. **Update hierarchy links:** If a matched entity's parent changed, move the `EntityNode` to its new parent's `Children` collection.

**Key benefits of incremental sync:**
- `EntityNode.Name` persists naturally — node instances survive.
- No dictionary, no name map.
- Selection and tree expansion state survive across syncs.
- Only affected subtrees change — minimal UI churn.

### 3.3 Incremental Sync Implementation

**New file:** `Editor/Ghost.Editor.Core/Services/SceneGraphSyncService.cs`

```csharp
[EditorInjection(ServiceLifetime.Singleton)]
public class SceneGraphSyncService
{
    private readonly EditorWorldService _worldService;
    private uint _lastSyncedVersion;

    public ObservableCollection<SceneGraphNode> RootNodes { get; } = new();

    public void Tick()
    {
        var currentVersion = _worldService.EditorWorld.Version;
        if (currentVersion == _lastSyncedVersion)
            return;

        _lastSyncedVersion = currentVersion;
        SyncScenesAndEntities(_worldService.EditorWorld);
    }

    private void SyncScenesAndEntities(World world)
    {
        // 1. Query all entities with SceneID
        // 2. Group by scene ID
        // 3. For each scene: match/create SceneNode, walk Hierarchy linked-list,
        //    match/create EntityNode, remove stale nodes, update Children links
    }
}
```

### 3.4 Selection → Inspector Wiring

Already partially wired:
- `InspectorService.SetSelected(IInspectable, source)` exists.
- `SceneGraphNode : IInspectable` exists.
- Clicking a `TreeViewItem` sets selection → fires event → calls `SetSelected`.

**To complete:**
- In `Hierarchy.xaml.cs`, handle `TreeView.ItemInvoked` or selection changed.
- Call `InspectorService.SetSelected(node, this)`.

---

## Phase 4: TreeView UI

**Goal:** Replace `ListView` placeholder with a functional `TreeView` bound to the scene graph.

### 4.1 Replace `ListView` with `TreeView`

**File:** `Hierarchy.xaml`

Changes:
- Replace `<ListView>` with `<TreeView>`.
- Bind `ItemsSource` to `RootNodes` from `SceneGraphSyncService`.
- Use a `DataTemplateSelector` that picks `SceneNode.GetSceneHierarchyTemplate()` or `EntityNode.GetSceneHierarchyTemplate()` based on node type.
- Move templates to XAML resources (static `DataTemplate` in `Page.Resources` or a `ResourceDictionary`) instead of `XamlReader.Load()`.

**Template selection:**

```xml
<TreeView ItemsSource="{x:Bind ViewModel.RootNodes, Mode=OneWay}">
    <TreeView.ItemTemplateSelector>
        <local:SceneGraphTemplateSelector />
    </TreeView.ItemTemplateSelector>
</TreeView>
```

### 4.2 Move Templates to XAML Resources

Create a `ResourceDictionary` (or put in `App.xaml` / `Hierarchy.xaml.Resources`):

```xml
<DataTemplate x:Key="SceneNodeTemplate" x:DataType="sg:SceneNode">
    <TreeViewItem AutomationProperties.Name="{x:Bind Name, Mode=OneWay}"
                  IsExpanded="True"
                  ItemsSource="{x:Bind Children, Mode=OneWay}">
        <StackPanel Orientation="Horizontal">
            <FontIcon FontSize="14" Glyph="&#xF156;"/>
            <TextBlock Margin="10,0,0,0" Text="{x:Bind Name, Mode=OneWay}"/>
        </StackPanel>
    </TreeViewItem>
</DataTemplate>

<DataTemplate x:Key="EntityNodeTemplate" x:DataType="sg:EntityNode">
    <TreeViewItem AutomationProperties.Name="{x:Bind Name, Mode=OneWay}"
                  ItemsSource="{x:Bind Children, Mode=OneWay}">
        <StackPanel Orientation="Horizontal">
            <FontIcon FontSize="14" Glyph="&#xF158;"/>
            <TextBlock Margin="5,0,0,0" Text="{x:Bind Name, Mode=OneWay}"/>
        </StackPanel>
    </TreeViewItem>
</DataTemplate>
```

### 4.3 DataTemplateSelector

**New file:** `Editor/Ghost.Editor/Views/Controls/SceneGraphTemplateSelector.cs`

```csharp
public class SceneGraphTemplateSelector : DataTemplateSelector
{
    public DataTemplate SceneNodeTemplate { get; set; }
    public DataTemplate EntityNodeTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item switch
        {
            SceneNode => SceneNodeTemplate,
            EntityNode => EntityNodeTemplate,
            _ => base.SelectTemplateCore(item)
        };
    }
}
```

### 4.4 Remove `GetSceneHierarchyTemplate()` from Node Classes

Once templates are in XAML resources (Phase 4.2), the `GetSceneHierarchyTemplate()` method on `SceneGraphNode` is no longer needed. Remove it from the abstract class and both subclasses. Do NOT remove it in Phase 1 — the method is still referenced until the XAML templates are ready.

### 4.5 Context Menu Support

Add `TreeViewItem.ContextFlyout` or right-click handling for:
- **Scene-level:** Create Entity, Rename Scene, Unload Scene, Save Scene.
- **Entity-level:** Create Child Entity, Delete Entity, Duplicate Entity, Rename Entity.

Create entity commands go through `EntityCommandBuffer` on the editor world.

### 4.6 Search/Filter

Wire the search `TextBox` to filter the TreeView:
- On text change, iterate `RootNodes` recursively.
- If a node or any descendant matches, show it. Otherwise collapse/hide.
- WinUI `TreeView` doesn't have built-in filtering, so implement a filtered copy of the tree or use `Visibility` toggling.

### 4.7 Drag & Drop Reparenting (Stretch Goal)

- Allow dragging an `EntityNode` onto another `EntityNode` to reparent.
- Uses WinUI drag-and-drop APIs.
- Calls `HierarchyUtility.SetParent()` on the editor world.
- Scene graph refreshes via sync.

---

## Phase 5: Serialization (JSON Editor / Binary Runtime)

### 5.1 File Local ID Scheme

When serializing a scene, entities are ordered (index in the list = file-local ID). All `Entity` references within components are serialized as file-local IDs, not global entity IDs.

**Rationale:** Global entity IDs are unpredictable across loads. File-local IDs are deterministic (they are just the list index) and remapped on load.

### 5.2 Serialization Format

**JSON (Editor) — in `Ghost.Editor.Core`:**

```json
{
  "name": "MainScene",
  "entities": [
    {
      "components": {
        "Ghost.Engine.Components.Hierarchy": {
          "parent": -1,
          "firstChild": 1,
          "nextSibling": -1
        },
        "Ghost.Engine.Components.LocalToWorld": {
          "matrix": { "m00": 1.0, ... }
        }
      }
    },
    {
      "components": { ... }
    }
  ]
}
```

- Entity index in the array = file-local ID.
- `Entity` references (`parent`, `firstChild`, `nextSibling`) stored as `int` file-local IDs. `-1` = `Entity.Invalid`.
- Component types stored by their stable `FullName` string.
- Uses `System.Text.Json` with reflection (allowed in editor).

**Binary (Runtime) — in `Ghost.Engine`:**

- MemoryPack serialization of the same structure.
- Must be AOT-compatible (use source-generated formatters).
- Components are blittable, so MemoryPack handles them efficiently.

### 5.3 Save Algorithm

1. Get all entities with `SceneID == targetScene` via `SceneManager.GetSceneEntities()`.
2. Sort entities in a deterministic order (e.g., by hierarchy depth-first traversal for deterministic output).
3. Create a `fileLocalID → Entity` map (list index → entity).
4. Create a reverse `Entity → fileLocalID` map.
5. For each entity, serialize all components.
6. For any component field of type `Entity`, replace with the file-local ID (using the reverse map).
7. For `ManagedEntityRef` / script data, serialize via MemoryPack.
8. Write to file.

### 5.4 Load Algorithm

1. Deserialize JSON/binary → list of entity component data.
2. Allocate all entities in the target `World` (no components yet, or minimal archetype).
3. Build `fileLocalID → Entity` map (list index → new global entity).
4. For each entity:
   - Add its components to the entity.
   - For any `Entity`-typed field, look up the file-local ID in the map and replace with the new global entity.
5. Call `HierarchyUtility` to validate/repair hierarchy invariants if needed.
6. Return the count of loaded entities.

### 5.5 References to Other Scenes

Per the architecture plan, cross-scene references are not supported. If a component references an entity from another scene:
- On save: log a warning and serialize as `-1` (invalid).
- On load: the reference will be `Entity.Invalid`.

### 5.6 File Naming

Scene files use the `.gscene` extension (`g` = GhostEngine):
- `Assets/Scenes/{SceneName}.gscene.json` (editor JSON)
- `Assets/Scenes/{SceneName}.gscene` (runtime binary)

**Scene name resolution:**
- The scene's name derives from the file name (minus extension). E.g., `MyScene.gscene` → name is `"MyScene"`.
- For unsaved/new scenes: default name is `"NewScene"`.
- `SceneNode.Name` is set from the file name on load, and used as the save target on save.

---

## Component Checklist

| Phase | Task | File(s) |
|-------|------|---------|
| **1.1** | Enhance `SceneGraphNode` — add `World`, constructor | `SceneGraphNode.cs` |
| **1.2** | Enhance `SceneNode` — add `Scene` field, constructor | `SceneNode.cs` |
| **1.3** | Enhance `EntityNode` — add constructor, make usable | `EntityNode.cs` |
| **1.4** | Add `SceneGraphBuilder` | New: `SceneGraphBuilder.cs` |
| **1.5** | Entity name strategy (editor-only, no component) | See Section 1.4 |
| **2** | Add `HierarchyUtility` static class | New: `Runtime/.../HierarchyUtility.cs` |
| **3.1** | Add `EditorWorldService` | New: `Editor/.../EditorWorldService.cs` |
| **3.2-3** | Add `SceneGraphSyncService` | New: `Editor/.../SceneGraphSyncService.cs` |
| **4.1** | Replace `ListView` with `TreeView` in XAML | `Hierarchy.xaml` |
| **4.2** | Move templates to XAML resources | `Hierarchy.xaml` |
| **4.3** | Add `SceneGraphTemplateSelector` | New: `Views/Controls/SceneGraphTemplateSelector.cs` |
| **4.4** | Remove `GetSceneHierarchyTemplate()` methods | `SceneGraphNode.cs`, `.cs`, `.cs` |
| **4.5** | Context menu (create/delete entity) | `Hierarchy.xaml.cs` |
| **4.6** | Search/filter | `Hierarchy.xaml.cs` |
| **4.7** | Drag-drop reparenting (stretch) | `Hierarchy.xaml.cs` |
| **5.1-5** | Scene save/load with ID remapping | New files in `Serialization/` |
| **5.5** | MemoryPack source-gen formatters | `Ghost.Engine` |

---

## Resolved Questions

| # | Question | Decision |
|---|----------|----------|
| 1 | Entity name storage | **Editor-only** — names live on `EntityNode.Name` directly. Persist across syncs via incremental matching by `Entity` identity (no dictionary needed). No runtime component. |
| 2 | Orphan behavior on parent destroy | **Cascade destroy** all children recursively. |
| 3 | Remove `GetSceneHierarchyTemplate()` timing | **Phase 4** — keep until XAML templates are in place. |
| 4 | Scene name persistence | **File name** — name = file name minus `.gscene` extension. Unsaved scenes default to `"NewScene"`. |
