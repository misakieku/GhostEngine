using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Controls;
using Windows.Foundation;

namespace Ghost.Editor.Views.Controls.Docking;

/// <summary>
/// A container that can host multiple dock modules with splitters.
/// </summary>
[TemplatePart(Name = PART_GRID, Type = typeof(Grid))]
public partial class DockPanel : DockContainer
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

    internal void SyncLengths()
    {
        if (_grid == null) return;
        if (Orientation == Orientation.Horizontal)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                int col = i * 2;
                if (col < _grid.ColumnDefinitions.Count)
                {
                    Children[i].DockLength = _grid.ColumnDefinitions[col].Width.Value;
                }
            }
        }
        else
        {
            for (int i = 0; i < Children.Count; i++)
            {
                int row = i * 2;
                if (row < _grid.RowDefinitions.Count)
                {
                    Children[i].DockLength = _grid.RowDefinitions[row].Height.Value;
                }
            }
        }
    }

    internal override void CheckCleanup()
    {
        base.CheckCleanup();

        if (Children.Count == 1)
        {
            var child = Children[0];
            var owner = Owner;

            if (owner != null)
            {
                owner.ReplaceChild(this, child);
            }
            else if (Root != null && Root.RootModule == this)
            {
                RemoveChildInternal(child, false);
                Root.RootModule = child;
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

        // Remove splitters and children that are no longer in the collection
        for (int i = _grid.Children.Count - 1; i >= 0; i--)
        {
            var child = _grid.Children[i];
            if (child is GridSplitter)
            {
                _grid.Children.RemoveAt(i);
            }
            else if (child is DockModule module && !Children.Contains(module))
            {
                _grid.Children.RemoveAt(i);
            }
        }

        if (Children.Count == 0)
        {
            _grid.RowDefinitions.Clear();
            _grid.ColumnDefinitions.Clear();
            return;
        }

        if (Orientation == Orientation.Horizontal)
        {
            _grid.RowDefinitions.Clear();

            int requiredColumns = (Children.Count * 2) - 1;
            while (_grid.ColumnDefinitions.Count > requiredColumns)
            {
                _grid.ColumnDefinitions.RemoveAt(_grid.ColumnDefinitions.Count - 1);
            }

            for (var i = 0; i < Children.Count; i++)
            {
                int columnIndex = i * 2;
                if (columnIndex >= _grid.ColumnDefinitions.Count)
                {
                    _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Children[i].DockLength, GridUnitType.Star), MinWidth = 250 });
                }
                else
                {
                    _grid.ColumnDefinitions[columnIndex].Width = new GridLength(Children[i].DockLength, GridUnitType.Star);
                    _grid.ColumnDefinitions[columnIndex].MinWidth = 250;
                }

                var child = Children[i];
                if (!_grid.Children.Contains(child))
                {
                    _grid.Children.Add(child);
                }

                Grid.SetColumn(child, columnIndex);
                Grid.SetRow(child, 0);

                if (i < Children.Count - 1)
                {
                    int splitterIndex = columnIndex + 1;
                    if (splitterIndex >= _grid.ColumnDefinitions.Count)
                    {
                        _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    }
                    else
                    {
                        _grid.ColumnDefinitions[splitterIndex].Width = GridLength.Auto;
                    }

                    var splitter = new GridSplitter { ResizeDirection = GridSplitter.GridResizeDirection.Columns, Width = SPLITTER_THICKNESS };
                    Grid.SetColumn(splitter, splitterIndex);
                    Grid.SetRow(splitter, 0);
                    _grid.Children.Add(splitter);
                }
            }
        }
        else
        {
            _grid.ColumnDefinitions.Clear();

            int requiredRows = (Children.Count * 2) - 1;
            while (_grid.RowDefinitions.Count > requiredRows)
            {
                _grid.RowDefinitions.RemoveAt(_grid.RowDefinitions.Count - 1);
            }

            for (var i = 0; i < Children.Count; i++)
            {
                int rowIndex = i * 2;
                if (rowIndex >= _grid.RowDefinitions.Count)
                {
                    _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Children[i].DockLength, GridUnitType.Star), MinHeight = 250 });
                }
                else
                {
                    _grid.RowDefinitions[rowIndex].Height = new GridLength(Children[i].DockLength, GridUnitType.Star);
                    _grid.RowDefinitions[rowIndex].MinHeight = 250;
                }

                var child = Children[i];
                if (!_grid.Children.Contains(child))
                {
                    _grid.Children.Add(child);
                }

                Grid.SetRow(child, rowIndex);
                Grid.SetColumn(child, 0);

                if (i < Children.Count - 1)
                {
                    int splitterIndex = rowIndex + 1;
                    if (splitterIndex >= _grid.RowDefinitions.Count)
                    {
                        _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }
                    else
                    {
                        _grid.RowDefinitions[splitterIndex].Height = GridLength.Auto;
                    }

                    var splitter = new GridSplitter { ResizeDirection = GridSplitter.GridResizeDirection.Rows, Height = SPLITTER_THICKNESS };
                    Grid.SetRow(splitter, splitterIndex);
                    Grid.SetColumn(splitter, 0);
                    _grid.Children.Add(splitter);
                }
            }
        }

        UpdateLayout();
    }
}
