using System;
using System.Collections.Generic;
using System.Linq;
using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Services;

public readonly record struct GraphicsDeviceCapabilities(
    bool SupportsRayTracing,
    bool SupportsMeshShaders,
    bool SupportsVariableRateShading,
    bool SupportsSamplerFeedback);

public class RenderPipelineFeatureContext
{
    public required IResourceDatabase ResourceDatabase { get; init; }
}

public interface IRenderPipelineFeatureProvider
{
    ShaderImplementationId ImplementationId { get; }
    bool IsSupported(in GraphicsDeviceCapabilities capabilities);
    Result Prepare(RenderPipelineFeatureContext context);
}

public sealed class RenderPipelineConfiguration
{
    private readonly Dictionary<ShaderInterfaceId, ShaderImplementationId> _bindings = new();

    public IReadOnlyDictionary<ShaderInterfaceId, ShaderImplementationId> Bindings => _bindings;

    public void Bind<TInterface, TImplementation>()
        where TInterface : struct, IShaderInterfaceTag
        where TImplementation : struct, IShaderImplementationTag<TInterface>
    {
        _bindings[TInterface.Id] = TImplementation.Id;
    }

    public void Bind(ShaderInterfaceId interfaceId, ShaderImplementationId implementationId)
    {
        _bindings[interfaceId] = implementationId;
    }

    public bool TryGetBinding(ShaderInterfaceId interfaceId, out ShaderImplementationId implementationId)
    {
        return _bindings.TryGetValue(interfaceId, out implementationId);
    }
}

public sealed class PreparedRenderPipelineConfiguration : IDisposable
{
    public IReadOnlyDictionary<ShaderInterfaceId, ShaderImplementationId> Bindings { get; }
    public IReadOnlyList<IRenderPipelineFeatureProvider> Providers { get; }

    public PreparedRenderPipelineConfiguration(
        IReadOnlyDictionary<ShaderInterfaceId, ShaderImplementationId> bindings,
        IReadOnlyList<IRenderPipelineFeatureProvider> providers)
    {
        Bindings = new Dictionary<ShaderInterfaceId, ShaderImplementationId>(bindings);
        Providers = providers;
    }

    public static Result<PreparedRenderPipelineConfiguration> Prepare(
        RenderPipelineConfiguration requested,
        IReadOnlyList<IRenderPipelineFeatureProvider> availableProviders,
        in GraphicsDeviceCapabilities capabilities,
        RenderPipelineFeatureContext featureContext)
    {
        var activeProviders = new List<IRenderPipelineFeatureProvider>();

        foreach (var kvp in requested.Bindings)
        {
            var ifaceId = kvp.Key;
            var implId = kvp.Value;
            var provider = availableProviders.FirstOrDefault(p => p.ImplementationId == implId);
            if (provider != null)
            {
                if (!provider.IsSupported(in capabilities))
                {
                    return Result.Failure($"Implementation 0x{implId.Value:X16} is not supported by current graphics hardware.");
                }

                var prepareResult = provider.Prepare(featureContext);
                if (prepareResult.IsFailure)
                {
                    return Result.Failure($"Failed to prepare feature provider for 0x{implId.Value:X16}: {prepareResult.Message}");
                }

                activeProviders.Add(provider);
            }
        }

        return Result.Success(new PreparedRenderPipelineConfiguration(requested.Bindings, activeProviders));
    }

    public void Dispose()
    {
    }
}
