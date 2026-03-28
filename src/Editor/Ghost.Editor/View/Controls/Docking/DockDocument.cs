using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

/// <summary>
/// Represents a document module in the docking system.
/// </summary>
public partial class DockDocument : DockModule
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(DockDocument), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(DockDocument), new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the title of the document.
    /// </summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the content of the document.
    /// </summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockDocument"/> class.
    /// </summary>
    public DockDocument()
    {
        DefaultStyleKey = typeof(DockDocument);
    }
}
