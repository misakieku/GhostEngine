using System;
using System.Collections.ObjectModel;

namespace Ghost.Editor.View.Controls.Docking;

public abstract class DockContainer : DockModule
{
    private readonly ObservableCollection<DockModule> _children = new();
    public ReadOnlyObservableCollection<DockModule> Children { get; }

    protected DockContainer()
    {
        Children = new ReadOnlyObservableCollection<DockModule>(_children);
        _children.CollectionChanged += OnChildrenChanged;
    }

    private void OnChildrenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnChildrenUpdated();
    }

    public void AddChild(DockModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (module == this)
            throw new ArgumentException("Cannot add a container to itself.", nameof(module));

        if (module is DockContainer container)
        {
            var current = Owner;
            while (current != null)
            {
                if (current == container)
                    throw new ArgumentException("Cannot add a container that is an ancestor of this container.", nameof(module));
                current = current.Owner;
            }
        }

        if (_children.Contains(module))
            return;

        module.Owner?.RemoveChild(module);
        module.Owner = this;
        _children.Add(module);
    }

    public void RemoveChild(DockModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (_children.Remove(module))
        {
            module.Owner = null;
        }
    }

    public void Clear()
    {
        foreach (var child in _children)
        {
            child.Owner = null;
        }
        _children.Clear();
    }
    
    protected virtual void OnChildrenUpdated() { }
}
