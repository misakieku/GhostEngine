namespace Ghost.SparseEntities.Systems;

/// <summary>
/// Attribute to declare that a system depends on one or more other systems.
/// </summary>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
public class DependsOnAttribute : Attribute
{
    public Type[] Prerequisites
    {
        get;
    }

    public DependsOnAttribute(params Type[] prerequisites)
    {
        Prerequisites = prerequisites;
    }
}

public interface ISystem
{
    public void OnCreate(in SystemState state);

    public void OnUpdate(in SystemState state);

    public void OnDestroy(in SystemState state);
}