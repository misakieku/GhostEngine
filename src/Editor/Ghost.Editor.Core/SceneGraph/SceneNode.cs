using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.SceneGraph;

public sealed partial class SceneNode : SceneGraphNode
{
    public override IconSource? CreateIcon()
    {
        return new FontIconSource
        {
            Glyph = "\uF156"
        };
    }

    // TODO: Implement custom header and inspector UI for the SceneNode
    public override UIElement? CreateHeader()
    {
        return null;
    }

    public override UIElement? CreateInspector()
    {
        return null;
    }

    public override DataTemplate GetSceneHierarchyTemplate()
    {
        var template = @"
        <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:sg=""using:Ghost.Editor.Core.SceneGraph"" x:DataType=""sg:SceneGraphNode"">
            <TreeViewItem
                AutomationProperties.Name=""{x:Bind Name, Mode=OneWay}""
                Background=""{ThemeResource ControlSolidFillColorDefaultBrush}""
                IsExpanded=""True""
                ItemsSource=""{ x:Bind Children, Mode=OneWay}"" >
                <StackPanel Orientation=""Horizontal"" >
                    <FontIcon FontSize=""14"" Glyph=""&#xF156;""/>
                    <TextBlock Margin=""10,0"" Text=""{ x:Bind Name, Mode=OneWay}""/>
                </StackPanel>
            </TreeViewItem>
        </DataTemplate>";

        return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(template);
    }
}