using System.Reflection;

namespace Ghost.Entities.Systems;

internal class SystemDependencyBuilder
{
    private readonly Dictionary<Type, List<Type>> _dependencies = new();
    private readonly List<Type> _systemTypes;

    public SystemDependencyBuilder(List<Type> allSystemTypes)
    {
        _systemTypes = allSystemTypes;
    }

    /// <summary>
    /// Builds a dependency graph for all system types that implement the <see cref="ISystem"/> interface.
    /// </summary>
    /// <remarks>This method analyzes all system types and their dependencies, as defined by the <see
    /// cref="DependsOnAttribute"/>. It validates that each system type is a concrete implementation of <see
    /// cref="ISystem"/> and constructs a mapping of each system type to its direct dependencies.</remarks>
    /// <exception cref="ArgumentException">Thrown if a type in <c>allSystemTypes</c> is not a concrete implementation of <see cref="ISystem"/>.</exception>
    public void BuildDependencyGraph()
    {
        foreach (var systemType in _systemTypes)
        {
            if (!typeof(ISystem).IsAssignableFrom(systemType) || systemType.IsAbstract || systemType.IsInterface)
            {
                throw new ArgumentException($"{systemType.Name} is not a concrete ISystem type.");
            }

            var directDependencies = new List<Type>();
            var dependsOnAttributes = systemType.GetCustomAttributes<DependsOnAttribute>(false);
            foreach (var attr in dependsOnAttributes)
            {
                directDependencies.AddRange(attr.Prerequisites);
            }

            _dependencies[systemType] = directDependencies;
        }
    }

    private void Visit(Type systemType, HashSet<Type> visited, HashSet<Type> permanentMark, List<Type> executionOrder)
    {
        if (permanentMark.Contains(systemType))
        {
            return;
        }

        if (visited.Contains(systemType))
        {
            throw new InvalidOperationException($"Circular dependency detected involving system: {systemType.Name}");
        }

        visited.Add(systemType); // Mark as currently visiting

        if (_dependencies.TryGetValue(systemType, out var directDependencies))
        {
            foreach (var dependencyType in directDependencies)
            {
                // Ensure the dependency is a registered system type
                if (!_systemTypes.Contains(dependencyType))
                {
                    throw new InvalidOperationException($"System {systemType.Name} depends on unregistered system {dependencyType.Name}.");
                }

                Visit(dependencyType, visited, permanentMark, executionOrder);
            }
        }

        visited.Remove(systemType); // Done visiting this node in the current path
        permanentMark.Add(systemType); // Mark as permanently processed
        executionOrder.Add(systemType); // Add to the sorted list (this will be reversed later for correct order)
    }

    /// <summary>
    /// Builds the topological order of systems.
    /// </summary>
    /// <returns>A list of system types in the order they should be executed.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a circular dependency is detected.</exception>"
    public List<Type> BuildExecutionOrder()
    {
        var executionOrder = new List<Type>(_systemTypes.Count);
        var visited = new HashSet<Type>(); // Tracks visited nodes in the current DFS path (for cycle detection)
        var permanentMark = new HashSet<Type>(); // Tracks nodes whose dependencies have been fully resolved

        // Initialize dependencies for all registered systems, even those without explicit attributes
        foreach (var sysType in _systemTypes)
        {
            if (!_dependencies.ContainsKey(sysType))
            {
                _dependencies[sysType] = new();
            }
        }

        foreach (var systemType in _systemTypes)
        {
            if (!permanentMark.Contains(systemType))
            {
                Visit(systemType, visited, permanentMark, executionOrder);
            }
        }

        return executionOrder;
    }
}