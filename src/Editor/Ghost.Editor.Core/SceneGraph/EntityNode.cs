using Ghost.Editor.Core.Contracts;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.SceneGraph;

public sealed partial class EntityNode : SceneGraphNode
{
    public Entity Entity
    {
        get;
    }
    public List<ComponentNode> Components { get; } = new();

    public EntityNode(World world, Entity entity, string name)
        : base(world, name)
    {
        Entity = entity;
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
                Components.Add(new ComponentNode(World, Entity, type, compDescriptor));
            }
        }
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
