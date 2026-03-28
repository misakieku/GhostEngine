using System.Collections.ObjectModel;

namespace Ghost.Editor.View.Controls.Docking;

public abstract class DockContainer : DockModule
{
    public ObservableCollection<DockModule> Children { get; } = new();

    protected DockContainer()
    {
        Children.CollectionChanged += OnChildrenChanged;
    }

    private void OnChildrenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (DockModule module in e.OldItems)
            {
                module.Owner = null;
            }
        }

        if (e.NewItems != null)
        {
            foreach (DockModule module in e.NewItems)
            {
                module.Owner = this;
                // module.Root = Root;
            }
        }
        
        OnChildrenUpdated();
    }
    
    protected virtual void OnChildrenUpdated() { }
}
