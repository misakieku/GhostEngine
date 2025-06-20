using Ghost.Editor.Controls.Internal;
using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Resources;
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
    private WorldNode _owner;
    private readonly Entity _entity;

    public Entity Entity => _entity;
    public override SceneGraphNodeType NodeType => SceneGraphNodeType.Entity;

    public EntityNode(WorldNode owner, Entity entity, string name)
    {
        _owner = owner;
        _entity = entity;
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
                Text = $"ID: {_entity.ID}   Generation: {_entity.Generation}",
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

    public UIElement? InspectorContent
    {
        get
        {
            var root = new StackPanel()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };

            foreach (var (typeHandle, componentPtr) in _owner.World.EntityManager.GetComponentsUnsafe(_entity))
            {
                if (componentPtr == IntPtr.Zero)
                {
                    continue;
                }

                var type = Type.GetTypeFromHandle(RuntimeTypeHandle.FromIntPtr(typeHandle));
                if (type == null || type.GetCustomAttribute<HideEditorAttribute>() != null)
                {
                    continue;
                }

                var dataView = new ComponentDataView(type.Name, _owner.World, _entity, type);
                root.Children.Add(dataView);
            }

            return root;
        }
    }
}