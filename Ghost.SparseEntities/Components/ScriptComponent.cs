namespace Ghost.SparseEntities.Components;

public abstract class ScriptComponent : IComponentData
{
    private bool _enable;

    internal World _world = null!;

    /// <summary>
    /// Gets or sets a Value indicating whether this script component is enabled.
    /// </summary>
    public bool Enable
    {
        get => _enable;
        set
        {
            if (_enable == value)
            {
                return;
            }

            _enable = value;
            if (_enable)
            {
                OnEnable();
            }
            else
            {
                OnDisable();
            }
        }
    }

    /// <summary>
    /// Gets the entity that owns this script component.
    /// </summary>
    public Entity Owner
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the EntityManager instance associated with the current world.
    /// </summary>
    protected EntityManager EntityManager => _world.EntityManager;

    /// <summary>
    /// Gets or sets the priority of the script component.
    /// Change this during runtime does not affect the execution order.
    /// </summary>
    public virtual int ExecutionOrder => 0;

    /// <summary>
    /// Called when the script component is enabled.
    /// </summary>
    public virtual void OnEnable()
    {
    }

    /// <summary>
    /// Called when the script component is disabled.
    /// </summary>
    public virtual void OnDisable()
    {
    }

    /// <summary>
    /// Called when the script component is initialized.
    /// </summary>
    public virtual void Initialize()
    {
    }

    /// <summary>
    /// Called when the script component is started.
    /// </summary>
    public virtual void Start()
    {
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    /// Called every frame after all Update methods have been called.
    /// </summary>
    public virtual void LateUpdate()
    {
    }

    /// <summary>
    /// Called at a fixed interval.
    /// This method is called at a fixed time step, independent of the frame rate.
    /// </summary>
    public virtual void FixedUpdate()
    {
    }

    /// <summary>
    /// Called when the script component is destroyed.
    /// </summary>
    public virtual void OnDestroy()
    {
    }
}