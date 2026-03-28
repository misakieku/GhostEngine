using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Controls;

namespace Ghost.Editor.View.Controls.Docking;

/// <summary>
/// A container that can host multiple dock modules with splitters.
/// </summary>
[TemplatePart(Name = PART_GRID, Type = typeof(Grid))]
public class DockPanel : DockContainer
{
    private const string PART_GRID = "PART_Grid";
    private const double SPLITTER_THICKNESS = 4;

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(DockPanel), new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

    /// <summary>
    /// Gets or sets the orientation of the panel.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private Grid? _grid;

    public DockPanel()
    {
        DefaultStyleKey = typeof(DockPanel);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _grid = GetTemplateChild(PART_GRID) as Grid;
        UpdateLayoutStructure();
    }

    protected override void OnChildrenUpdated()
    {
        UpdateLayoutStructure();
    }

    protected override void CheckCleanup()
    {
        if (Children.Count == 0)
        {
            base.CheckCleanup();
        }
        else if (Children.Count == 1)
        {
            var child = Children[0];
            var owner = Owner;

            if (owner != null)
            {
                owner.ReplaceChild(this, child);
            }
            else if (Root != null && Root.RootPanel == this)
            {
                // If this is the root panel, we can't easily replace it if the child is a DockGroup, 
                // because RootPanel must be a DockPanel. So we just leave it.
            }
        }
    }

    private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((DockPanel)d).UpdateLayoutStructure();
    }

    private void UpdateLayoutStructure()
    {
        if (_grid == null) return;

        _grid.Children.Clear();
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();

        if (Children.Count == 0) return;

        if (Orientation == Orientation.Horizontal)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var child = Children[i];
                Grid.SetColumn(child, i * 2);
                _grid.Children.Add(child);

                if (i < Children.Count - 1)
                {
                    _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var splitter = new GridSplitter { ResizeDirection = GridSplitter.GridResizeDirection.Columns, Width = SPLITTER_THICKNESS };
                    Grid.SetColumn(splitter, i * 2 + 1);
                    _grid.Children.Add(splitter);
                }
            }
        }
        else
        {
            for (int i = 0; i < Children.Count; i++)
            {
                _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                var child = Children[i];
                Grid.SetRow(child, i * 2);
                _grid.Children.Add(child);

                if (i < Children.Count - 1)
                {
                    _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var splitter = new GridSplitter { ResizeDirection = GridSplitter.GridResizeDirection.Rows, Height = SPLITTER_THICKNESS };
                    Grid.SetRow(splitter, i * 2 + 1);
                    _grid.Children.Add(splitter);
                }
            }
        }
    }
}
