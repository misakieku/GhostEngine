using Ghost.Entities;
using Ghost.Graphics;

namespace Ghost.Engine.Systems;

public abstract class RenderPipelineSystemAttribute : Attribute
{
    public abstract Type SettingsType { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public class RenderPipelineSystemAttribute<T> : RenderPipelineSystemAttribute
    where T : class, IRenderPipelineSettings
{
    public override Type SettingsType => typeof(T);
}

public static class RenderPipelineSystemRegistry
{
    private static readonly Dictionary<nint, List<Func<ISystem>>> s_renderPipelineSystems = new();

    public static void RegisterRenderPipelineSystem(nint settingsType, Func<ISystem> systemFactory)
    {
        if (!s_renderPipelineSystems.TryGetValue(settingsType, out var systems))
        {
            systems = new List<Func<ISystem>>(4);
            s_renderPipelineSystems[settingsType] = systems;
        }

        systems.Add(systemFactory);
    }

    internal static IEnumerable<Func<ISystem>> GetRenderPipelineSystems(nint settingsType)
    {
        if (s_renderPipelineSystems.TryGetValue(settingsType, out var systems))
        {
            return systems;
        }

        return Enumerable.Empty<Func<ISystem>>();
    }
}