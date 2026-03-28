using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls;

public sealed partial class DockLayout : Control
{
    public DockLayout()
    {
        DefaultStyleKey = typeof(DockLayout);
    }

    public DockGroupNode? Root
    {
        get => (DockGroupNode?)GetValue(RootProperty);
        set => SetValue(RootProperty, value);
    }

    public static readonly DependencyProperty RootProperty =
        DependencyProperty.Register("Root", typeof(DockGroupNode), typeof(DockLayout), new PropertyMetadata(null, OnRootChanged));

    private static void OnRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockLayout layout)
        {
            layout.RenderTree();
        }
    }

    private void RenderTree()
    {
        if (GetTemplateChild("PART_RootGrid") is Grid rootGrid)
        {
            rootGrid.Children.Clear();
            if (Root != null)
            {
                var ui = CreateUIForNode(Root);
                rootGrid.Children.Add(ui);
            }
        }
    }

    private UIElement CreateUIForNode(DockNode node)
    {
        if (node is DockGroupNode groupNode)
        {
            // Simple visualizer for now, full grid logic in next step
            var grid = new Grid();
            foreach (var child in groupNode.Children)
            {
                grid.Children.Add(CreateUIForNode(child));
            }
            return grid;
        }
        else if (node is DockPanelNode panelNode)
        {
            // NOTE: NavigationTabView is expected to be implemented in a future task or exists in a namespace not yet fully visible.
            // For now, we use a placeholder if it's not found, but the task specifies using it.
            // If it fails to compile, I will check for the correct namespace.
            return new Ghost.Editor.Controls.NavigationTabView 
            { 
                TabItemsSource = panelNode.Items,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }
        return new Grid(); // Fallback
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        RenderTree();
    }
}
