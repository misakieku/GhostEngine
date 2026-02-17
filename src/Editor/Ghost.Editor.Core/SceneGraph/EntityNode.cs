using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.SceneGraph;

public sealed partial class EntityNode : SceneGraphNode
{
    private readonly Entity _entity;

    public Entity Entity => _entity;

    public override IconSource? CreateIcon()
    {
        return new FontIconSource
        {
            Glyph = "\uF158"
        };
    }

    public override UIElement? CreateHeader()
    {
        return null;
    }

    public override UIElement? CreateInspector()
    {
        throw new NotImplementedException();
    }

    public override DataTemplate GetSceneHierarchyTemplate()
    {
        var template = @"
        <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:sg=""using:Ghost.Editor.Core.SceneGraph"" x:Key=""EntityTemplate"" x:DataType=""sg:SceneGraphNode"">
            <TreeViewItem AutomationProperties.Name=""{x:Bind Name, Mode=OneWay}"" ItemsSource=""{x:Bind Children, Mode=OneWay}"">
                <StackPanel Margin=""10,0"" Orientation=""Horizontal"">
                    <FontIcon FontSize=""14"" Glyph=""&#xF158;"" />
                    <TextBlock Margin=""5,0,0,0"" Text=""{x:Bind Name, Mode=OneWay}"" />
                </StackPanel>
            </TreeViewItem>
        </DataTemplate>";

        return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(template);
    }
}
