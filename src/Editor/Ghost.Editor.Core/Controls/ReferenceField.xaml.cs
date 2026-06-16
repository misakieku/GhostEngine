using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace Ghost.Editor.Core.Controls;

public sealed partial class ReferenceField : UserControl
{
    public static readonly DependencyProperty DisplayTextProperty =
        DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(ReferenceField), new PropertyMetadata(string.Empty, OnStateChanged));

    public static readonly DependencyProperty TypeLabelProperty =
        DependencyProperty.Register(nameof(TypeLabel), typeof(string), typeof(ReferenceField), new PropertyMetadata("Object", OnStateChanged));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(ReferenceField), new PropertyMetadata("\uEA86"));

    public static readonly DependencyProperty HasValueProperty =
        DependencyProperty.Register(nameof(HasValue), typeof(bool), typeof(ReferenceField), new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(ReferenceField), new PropertyMetadata(false, OnStateChanged));

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    public string TypeLabel
    {
        get => (string)GetValue(TypeLabelProperty);
        set => SetValue(TypeLabelProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public bool HasValue
    {
        get => (bool)GetValue(HasValueProperty);
        set => SetValue(HasValueProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public event Func<DragEventArgs, bool>? ValidateDrop;
    public event Action<DragEventArgs>? OnDropAccepted;
    public event Action? OnClearClicked;
    public event Action? OnGotoClicked;

    private readonly SolidColorBrush _accentBrush;
    private readonly SolidColorBrush _errorBrush;
    private readonly SolidColorBrush _defaultBorderBrush;

    public ReferenceField()
    {
        InitializeComponent();

        _accentBrush = (SolidColorBrush)Application.Current.Resources["SystemControlHighlightAccentBrush"];
        _errorBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
        _defaultBorderBrush = (SolidColorBrush)Application.Current.Resources["CardStrokeColorDefaultBrush"];

        UpdateState();
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ReferenceField)d).UpdateState();
    }

    private void UpdateState()
    {
        if (HasValue)
        {
            ClearButton.Visibility = IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
            GotoButton.Visibility = Visibility.Visible;
        }
        else
        {
            ClearButton.Visibility = Visibility.Collapsed;
            GotoButton.Visibility = Visibility.Collapsed;
            if (string.IsNullOrEmpty(DisplayText))
            {
                // We shouldn't change DependencyProperty value here to avoid loops, 
                // but we can bind a different text if needed. For now, rely on caller to set DisplayText to "None (Type)".
            }
        }

        AllowDrop = !IsReadOnly;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (IsReadOnly)
        {
            e.AcceptedOperation = DataPackageOperation.None;
            return;
        }

        var isValid = ValidateDrop?.Invoke(e) ?? false;

        if (isValid)
        {
            e.AcceptedOperation = DataPackageOperation.Link;
            RootBorder.BorderBrush = _accentBrush;
            RootBorder.BorderThickness = new Thickness(1);
        }
        else
        {
            e.AcceptedOperation = DataPackageOperation.None;
            // Optionally set error brush
            RootBorder.BorderBrush = _errorBrush;
            RootBorder.BorderThickness = new Thickness(1);
        }

        e.Handled = true;
    }

    protected override void OnDragLeave(DragEventArgs e)
    {
        base.OnDragLeave(e);
        RootBorder.BorderBrush = _defaultBorderBrush;
        RootBorder.BorderThickness = new Thickness(1);
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        RootBorder.BorderBrush = _defaultBorderBrush;
        RootBorder.BorderThickness = new Thickness(1);

        if (IsReadOnly) return;

        var isValid = ValidateDrop?.Invoke(e) ?? false;
        if (isValid)
        {
            OnDropAccepted?.Invoke(e);
        }
    }

    private void OnClearButtonClicked(object sender, RoutedEventArgs e)
    {
        OnClearClicked?.Invoke();
    }

    private void OnGotoButtonClicked(object sender, RoutedEventArgs e)
    {
        OnGotoClicked?.Invoke();
    }
}
