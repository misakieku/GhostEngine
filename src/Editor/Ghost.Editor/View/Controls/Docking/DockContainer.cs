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
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
        {
            // Reset is tricky because e.OldItems is null.
            // We should have handled this by clearing owners before Clear() was called,
            // or by using a custom collection that notifies before clearing.
            // For now, we'll just log or handle it if we can.
        }

        if (e.OldItems != null)
        {
            foreach (DockModule module in e.OldItems)
            {
                if (module.Owner == this)
                {
                    module.Owner = null;
                }
            }
        }

        if (e.NewItems != null)
        {
            foreach (DockModule module in e.NewItems)
            {
                // Re-parenting: if it has an owner, detach it
                if (module.Owner != null && module.Owner != this)
                {
                    module.Owner.Children.Remove(module);
                }
                
                module.Owner = this;
                // module.Root = Root;
            }
        }
        
        OnChildrenUpdated();
    }

    public void AddChild(DockModule module)
    {
        if (Children.Contains(module))
            return;

        Children.Add(module);
    }

    public void Clear()
    {
        foreach (var child in Children)
        {
            child.Owner = null;
        }
        Children.Clear();
    }
    
    protected virtual void OnChildrenUpdated() { }
}
