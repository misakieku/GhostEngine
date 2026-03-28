using System;
using System.Collections.ObjectModel;

namespace Ghost.Editor.View.Controls.Docking;

/// <summary>
/// Base class for containers that can hold other dock modules.
/// </summary>
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

    public virtual void AddChild(DockModule module)
    {
        InsertChild(_children.Count, module);
    }

    public virtual void InsertChild(int index, DockModule module)
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
        module.Root = Root;
        _children.Insert(index, module);
    }

    public virtual void RemoveChild(DockModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (_children.Remove(module))
        {
            module.Owner = null;
            module.Root = null;
            CheckCleanup();
        }
    }

    protected virtual void CheckCleanup()
    {
        if (_children.Count == 0)
        {
            Owner?.RemoveChild(this);
        }
    }

    public void Clear()
    {
        foreach (var child in _children)
        {
            child.Owner = null;
            child.Root = null;
        }
        _children.Clear();
    }

    protected override void OnRootChanged()
    {
        foreach (var child in _children)
        {
            child.Root = Root;
        }
    }
    
    protected virtual void OnChildrenUpdated() { }
}
