using Ghost.Editor.Contracts;
using Ghost.Editor.Resources;
using Ghost.Entities;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Ghost.Editor.Core.SceneGraph;

public partial class EntityNode : SceneGraphNode
{
    private readonly Entity _entity;

    public Entity Entity => _entity;
    public override SceneGraphNodeType NodeType => SceneGraphNodeType.Entity;

    public EntityNode(Entity entity, string name)
    {
        _entity = entity;
        Name = name;
    }

    internal EntityNode()
    {
    }
}

public partial class EntityNode : IInspectable
{
    public IconSource? Icon => EditorIconSource.entity_24;

    public UIElement? HeaderContent()
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
            Text = $"ID: {_entity.ID}",
            Margin = new Thickness(0, 5, 0, 0),
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

    public UIElement? InspectorContent()
    {
        return null;
    }
}