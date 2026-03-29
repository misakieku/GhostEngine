using System;
using System.Collections.ObjectModel;

namespace Ghost.Editor.View.Controls.Docking;

/// <summary>
/// Base class for containers that can hold other dock modules.
/// </summary>
public abstract class DockContainer : DockModule
{
    private readonly ObservableCollection<DockModule> _children = new();
    private bool _isCleaningUp;
    /// <summary>
    /// Gets the collection of child modules.
    /// </summary>
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

    /// <summary>
    /// Adds a child module to the end of the container.
    /// </summary>
    /// <param name="module">The module to add.</param>
    public virtual void AddChild(DockModule module)
    {
        InsertChild(_children.Count, module);
    }

    /// <summary>
    /// Inserts a child module at the specified index.
    /// </summary>
    /// <remarks>
    /// This method does not support reordering existing children within the same container.
    /// Cross-layout moves are intentionally allowed and supported (e.g., for dragging tabs between floating windows and the main window).
    /// </remarks>
    /// <param name="index">The zero-based index at which the module should be inserted.</param>
    /// <param name="module">The module to insert.</param>
    public virtual void InsertChild(int index, DockModule module)
    {
        ValidateChild(module);

        if (module.Owner == null && module.Root != null && module.Root != this.Root)
            throw new InvalidOperationException("Cannot insert a module that is the root of another layout. Detach it first.");

        if (index < 0 || index > _children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (_children.Contains(module))
            return;

        module.Owner?.RemoveChild(module);

        module.Owner = this;
        module.Root = Root;
        _children.Insert(index, module);
    }

    /// <summary>
    /// Removes a child module from the container.
    /// </summary>
    /// <param name="module">The module to remove.</param>
    public virtual void RemoveChild(DockModule module)
    {
        RemoveChildInternal(module, true);
    }

    internal void RemoveChildInternal(DockModule module, bool triggerCleanup)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (_children.Remove(module))
        {
            module.Owner = null;
            module.Root = null;
            if (!_isCleaningUp && triggerCleanup)
            {
                CheckCleanup();
            }
        }
    }

    /// <summary>
    /// Replaces an existing child module with a new one.
    /// </summary>
    /// <remarks>
    /// Cross-layout moves are intentionally allowed and supported (e.g., for dragging tabs between floating windows and the main window).
    /// </remarks>
    /// <param name="oldChild">The child module to be replaced.</param>
    /// <param name="newChild">The new child module to insert.</param>
    public virtual void ReplaceChild(DockModule oldChild, DockModule newChild)
    {
        ArgumentNullException.ThrowIfNull(oldChild);
        ValidateChild(newChild);

        if (newChild.Owner == null && newChild.Root != null && newChild.Root != this.Root)
            throw new InvalidOperationException("Cannot insert a module that is the root of another layout. Detach it first.");

        if (oldChild == newChild) return;

        int index = _children.IndexOf(oldChild);
        if (index < 0) throw new ArgumentException("oldChild not found in this container", nameof(oldChild));

        // Detach newChild from its current owner if any
        if (newChild.Owner == this)
        {
            throw new ArgumentException("newChild is already in this container", nameof(newChild));
        }

        var oldOwner = newChild.Owner;
        newChild.Owner?.RemoveChildInternal(newChild, false);

        // Remove oldChild without triggering cleanup
        _isCleaningUp = true;
        try
        {
            _children.RemoveAt(index);
            oldChild.Owner = null;
            oldChild.Root = null;

            newChild.Owner = this;
            newChild.Root = Root;
            _children.Insert(index, newChild);
        }
        finally
        {
            _isCleaningUp = false;
        }

        CheckCleanup();
        oldOwner?.CheckCleanup();
    }

    /// <summary>
    /// Checks if the container is empty and removes it from its owner if necessary.
    /// </summary>
    internal virtual void CheckCleanup()
    {
        if (Children.Count == 0)
        {
            if (Owner != null)
            {
                Owner.RemoveChildInternal(this, true);
            }
            else if (Root != null && Root.RootModule == this)
            {
                var root = Root;
                root.RootModule = null;
                root.NotifyLayoutEmpty();
            }
        }
    }

    /// <summary>
    /// Validates if a module can be added as a child to this container.
    /// </summary>
    /// <param name="module">The module to validate.</param>
    protected virtual void ValidateChild(DockModule module)
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
    }

    /// <summary>
    /// Removes all child modules from the container.
    /// </summary>
    public void Clear()
    {
        foreach (var child in _children)
        {
            child.Owner = null;
            child.Root = null;
        }
        _children.Clear();
        if (!_isCleaningUp)
        {
            CheckCleanup();
        }
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
