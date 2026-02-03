namespace Ghost.Editor.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class EditorInjectionAttribute : Attribute
{
    public enum ServiceLifetime
    {
        Singleton,
        Transient,
        Scoped
    }

    public ServiceLifetime Lifetime
    {
        get;
    }

    public Type? ImplementationType
    {
        get;
    }

    public EditorInjectionAttribute(ServiceLifetime lifetime, Type? implementationType = null)
    {
        Lifetime = lifetime;
        ImplementationType = implementationType;
    }
}