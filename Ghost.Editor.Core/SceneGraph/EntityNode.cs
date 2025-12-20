using Ghost.Editor.Core.Controls.Internal;
using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Core.Resources;
using Ghost.Engine.Editor;
using Ghost.Entities;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Reflection;

namespace Ghost.Editor.Core.SceneGraph;

public partial class EntityNode : SceneGraphNode
{
    public WorldNode Owner
    {
        get;
        set;
    }

    public Entity Entity
    {
        get;
    }

    public override SceneGraphNodeType NodeType => SceneGraphNodeType.Entity;

    public EntityNode(WorldNode owner, Entity entity, string name)
    {
        Owner = owner;
        Entity = entity;
        Name = name;
    }
}

public partial class EntityNode : IInspectable
{
    public IconSource? Icon => EditorIconSource.entity_24;

    public UIElement? HeaderContent
    {
        get
        {
            var root = new StackPanel()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            var nameText = new TextBox
            {
                Text = Name,
                FontWeight = FontWeights.Bold,
            };
            var idText = new TextBlock
            {
                Text = $"ID: {Entity.ID}   Generation: {Entity.Generation}",
                Margin = new Thickness(5, 7, 0, 0),
                Opacity = 0.75,
                Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style
            };

            nameText.SetBinding(TextBox.TextProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(Name)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
            });

            root.Children.Add(nameText);
            root.Children.Add(idText);

            return root;
        }
    }

    public unsafe UIElement? InspectorContent
    {
        get
        {
            var r = Owner.World.EntityManager.GetEntityLocation(Entity);
            if (!r)
            {
                return null;
            }

            var root = new StackPanel()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };

            var location = r.Value;
            ref var archetype = ref Owner.World.GetArchetypeReference(location.archetypeID);

            var it = archetype._signature.GetIterator();
            while (it.Next(out var typeID))
            {
                var pComponent = archetype.GetComponentData(location.chunkIndex, location.rowIndex, typeID);
                if (pComponent == null)
                {
                    continue;
                }

                if (!ComponentRegistry.s_runtimeIDToType.TryGetValue(typeID, out var t))
                {
                    continue;
                }

                if (t.GetCustomAttribute<HideEditorAttribute>() != null)
                {
                    continue;
                }

                var componentView = new ComponentView(t.Name, Owner.World, Entity, t);
                root.Children.Add(componentView);
            }

            return root;
        }
    }
}