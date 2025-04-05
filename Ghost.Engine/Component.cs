namespace Ghost.Engine;

public abstract class Component
{
    public virtual void Start()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void LateUpdate()
    {
    }

    public virtual void FixedUpdate()
    {
    }

    public virtual void OnDestroy()
    {
    }
}