namespace Ghost.Engine.Models;

public abstract class Component
{
    public required GameEntity Owner
    {
        get;
        set;
    }
}