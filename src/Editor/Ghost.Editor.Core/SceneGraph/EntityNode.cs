using Ghost.Editor.Core.Contracts;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace Ghost.Editor.Core.SceneGraph;

public sealed partial class EntityNode : SceneGraphNode
{
    public Entity Entity
    {
        get;
    }
    public List<ComponentNode> Components { get; } = new();

    public SceneNode? SceneNode { get; }

    internal EntityNode(World world, Entity entity, string name, SceneNode? sceneNode)
        : base(world, name)
    {
        Entity = entity;
        SceneNode = sceneNode;
    }

    public override SceneNode? GetOwningSceneNode() => SceneNode;

    public unsafe override void SerializeState(BinaryWriter writer)
    {
        base.SerializeState(writer);

        var isAlive = World.EntityManager.Exists(Entity);
        writer.Write(isAlive);

        if (!isAlive)
        {
            return;
        }

        var locRes = World.EntityManager.GetEntityLocation(Entity);
        if (!locRes.IsSuccess)
        {
            writer.Write(0);
            return;
        }

        var archetypeId = locRes.Value.archetypeID;
        ref var archetype = ref World.ComponentManager.GetArchetypeReference(archetypeId);
        ref var chunk = ref archetype.GetChunkReference(locRes.Value.chunkIndex);

        EditorApplication.TryGetService<Services.SceneGraphSyncService>(out var syncService);

        writer.Write(archetype._layouts.Count);

        for (var i = 0; i < archetype._layouts.Count; i++)
        {
            var layout = archetype._layouts[i];
            var typeId = new Ghost.Core.Identifier<IComponent>(layout.componentID);

            writer.Write(typeId.Value);
            writer.Write(layout.size);

            var pSrc = chunk.GetUnsafePtr() + layout.offset + (layout.size * locRes.Value.rowIndex);

            // Copy to temp buffer
            var buffer = new byte[layout.size];
            fixed (byte* pDst = buffer)
            {
                Buffer.MemoryCopy(pSrc, pDst, layout.size, layout.size);
            }

            // Reference Translation
            var entityOffsets = Services.EntityFieldTracker.GetEntityOffsets(typeId.Value);
            foreach (var offset in entityOffsets)
            {
                Entity oldEntity;
                fixed (byte* pBuf = buffer)
                {
                    oldEntity = *(Entity*)(pBuf + offset);
                }

                Guid targetGuid = Guid.Empty;
                if (syncService != null && syncService.TryGetNode(oldEntity, out var targetNode))
                {
                    targetGuid = targetNode.InstanceID;
                }

                writer.Write(true);
                writer.Write(offset);
                writer.Write(targetGuid.ToByteArray());
            }
            writer.Write(false); // End of patch records

            // Write patched bytes
            writer.Write(buffer);
        }

        // Shared Data
        if (chunk._groupIndex >= 0 && chunk._groupIndex < archetype._chunkGroups.Count)
        {
            var group = archetype._chunkGroups[chunk._groupIndex];
            writer.Write(true);
            writer.Write(group.sharedDataHash);
            writer.Write(group.sharedData.Length);
            writer.Write(group.sharedData.AsSpan().ToArray());
        }
        else
        {
            writer.Write(false);
        }
    }

    public unsafe override void DeserializeState(BinaryReader reader)
    {
        base.DeserializeState(reader);

        var isAlive = reader.ReadBoolean();
        var currentlyAlive = World.EntityManager.Exists(Entity);

        if (isAlive && !currentlyAlive)
        {
            // Resurrect
            var newEntity = World.EntityManager.CreateEntity();
            
            // Update the Entity property via reflection
            var entityField = typeof(EntityNode).GetField("<Entity>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            entityField?.SetValue(this, newEntity);
        }
        else if (!isAlive && currentlyAlive)
        {
            // Destroy
            World.EntityManager.DestroyEntity(Entity);
            return;
        }
        else if (!isAlive && !currentlyAlive)
        {
            return;
        }

        var componentCount = reader.ReadInt32();
        var componentsToRestore = new List<Ghost.Core.Identifier<IComponent>>();
        var componentDataMap = new Dictionary<int, byte[]>();

        EditorApplication.TryGetService<Services.SceneGraphSyncService>(out var syncService);

        for (var i = 0; i < componentCount; i++)
        {
            var typeIdVal = reader.ReadInt32();
            var size = reader.ReadInt32();
            var typeId = new Ghost.Core.Identifier<IComponent>(typeIdVal);
            componentsToRestore.Add(typeId);

            var patchRecords = new List<(int offset, Guid guid)>();
            while (reader.ReadBoolean())
            {
                var offset = reader.ReadInt32();
                var guidBytes = reader.ReadBytes(16);
                patchRecords.Add((offset, new Guid(guidBytes)));
            }

            var buffer = reader.ReadBytes(size);

            // Apply patch records
            foreach (var record in patchRecords)
            {
                Entity newEntity = Entity.Invalid;
                if (record.guid != Guid.Empty)
                {
                    if (Find(record.guid) is EntityNode targetNode)
                    {
                        newEntity = targetNode.Entity;
                    }
                }

                fixed (byte* pBuf = buffer)
                {
                    *(Entity*)(pBuf + record.offset) = newEntity;
                }
            }

            componentDataMap[typeIdVal] = buffer;
        }

        var hasSharedData = reader.ReadBoolean();
        int sharedDataHash = 0;
        byte[] sharedData = Array.Empty<byte>();

        if (hasSharedData)
        {
            sharedDataHash = reader.ReadInt32();
            var sharedSize = reader.ReadInt32();
            sharedData = reader.ReadBytes(sharedSize);
        }

        // Migrate entity to match snapshot archetype
        var view = new ComponentSetView(componentsToRestore.ToArray(), sharedData);
        World.EntityManager.MigrateEntity(Entity, view);

        // Restore unmanaged data
        var locRes = World.EntityManager.GetEntityLocation(Entity);
        if (locRes.IsSuccess)
        {
            ref var archetype = ref World.ComponentManager.GetArchetypeReference(locRes.Value.archetypeID);
            ref var chunk = ref archetype.GetChunkReference(locRes.Value.chunkIndex);

            for (var i = 0; i < archetype._layouts.Count; i++)
            {
                var layout = archetype._layouts[i];
                if (componentDataMap.TryGetValue(layout.componentID, out var buffer))
                {
                    var pDst = chunk.GetUnsafePtr() + layout.offset + (layout.size * locRes.Value.rowIndex);
                    fixed (byte* pSrc = buffer)
                    {
                        Buffer.MemoryCopy(pSrc, pDst, layout.size, layout.size);
                    }
                }
            }
        }
    }

    public void BuildComponents()
    {
        Components.Clear();
        var locationResult = World.EntityManager.GetEntityLocation(Entity);
        if (!locationResult.IsSuccess)
        {
            return;
        }

        var location = locationResult.Value;
        ref var archetype = ref World.ComponentManager.GetArchetypeReference(location.archetypeID);

        var it = archetype._signature.GetIterator();
        Debug.WriteLine(archetype._signature.ToString());
        while (it.Next(out var componentID))
        {
            if (ComponentRegistry.s_runtimeIDToType.TryGetValue(componentID, out var type))
            {
                var compInfo = ComponentRegistry.GetComponentInfo(new Ghost.Core.Identifier<IComponent>(componentID));
                if (compInfo.isCleanup)
                {
                    continue;
                }

                var compDescriptor = Inspector.ComponentDescriptor.Create(type);
                Components.Add(new ComponentNode(World, this, type, compDescriptor));
            }
        }
    }

    public void AddComponent(Type componentType)
    {
        Modify($"Add component {componentType.Name}");
        var componentId = ComponentRegistry.GetComponentID(componentType);
        if (componentId.IsInvalid) return;

        var compInfo = ComponentRegistry.GetComponentInfo(componentId);

        var worldService = EditorApplication.GetService<IEditorWorldService>();
        worldService.Defer(() =>
        {
            unsafe
            {
                var pData = System.Runtime.InteropServices.Marshal.AllocHGlobal(compInfo.size);
                try
                {
                    System.Runtime.InteropServices.Marshal.StructureToPtr(Activator.CreateInstance(componentType)!, pData, false);
                    World.EntityManager.AddComponent(Entity, componentId, (void*)pData);
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(pData);
                }
            }
        });
    }

    public void RemoveComponent(Type componentType)
    {
        Modify($"Remove component {componentType.Name}");
        var componentId = ComponentRegistry.GetComponentID(componentType);
        if (componentId.IsInvalid) return;

        var worldService = EditorApplication.GetService<IEditorWorldService>();
        worldService.Defer(() =>
        {
            World.EntityManager.RemoveComponent(Entity, componentId);
        });
    }

    public override IconSource? CreateIcon()
    {
        return new FontIconSource
        {
            Glyph = "\uF158"
        };
    }

    public override UIElement? CreateHeader()
    {
        var root = new Grid
        {
            ColumnSpacing = 8,
        };

        root.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star)
        });
        root.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = GridLength.Auto,
            MinWidth = 20
        });

        var nameBox = new TextBox
        {
            Text = Name,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        };

        nameBox.SetBinding(TextBox.TextProperty, new Microsoft.UI.Xaml.Data.Binding
        {
            Source = this,
            Path = new PropertyPath(nameof(Name)),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
            UpdateSourceTrigger = Microsoft.UI.Xaml.Data.UpdateSourceTrigger.PropertyChanged
        });

        var entityBlock = new TextBlock
        {
            Text = $"{Entity.ID}:{Entity.Generation}",
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        Grid.SetColumn(nameBox, 0);
        Grid.SetColumn(entityBlock, 1);

        root.Children.Add(nameBox);
        root.Children.Add(entityBlock);

        return root;
    }

    public override IInspectorModel CreateInspectorModel()
    {
        return new Inspector.EntityInspectorModel(World, Entity);
    }
}
