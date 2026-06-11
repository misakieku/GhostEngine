using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Ghost.Editor.Core.Controls;

public sealed partial class ComponentExpander : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(ComponentExpander), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ExpandedContentProperty =
        DependencyProperty.Register("ExpandedContent", typeof(UIElement), typeof(ComponentExpander), new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public UIElement ExpandedContent
    {
        get => (UIElement)GetValue(ExpandedContentProperty);
        set => SetValue(ExpandedContentProperty, value);
    }

    public event RoutedEventHandler? RemoveRequested;

    public ComponentExpander()
    {
        this.InitializeComponent();
    }

    private void HeaderToggle_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (ExpandedContentPresenter == null || ChevronIcon == null) return;

        if (sender is ToggleButton toggle && toggle.IsChecked == true)
        {
            ExpandedContentPresenter.Visibility = Visibility.Visible;
            ChevronIcon.Glyph = "\uE70D"; // ChevronDown
        }
        else
        {
            ExpandedContentPresenter.Visibility = Visibility.Collapsed;
            ChevronIcon.Glyph = "\uE76C"; // ChevronRight
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        RemoveRequested?.Invoke(this, e);
    }
}
