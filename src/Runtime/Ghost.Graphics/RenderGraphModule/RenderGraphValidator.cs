#if GHOST_SAFETY_CHECKS
using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.RenderGraphModule;

internal static class RenderGraphValidator
{
    public static string? ValidateDeclaration(
        RenderGraphPass pass,
        Identifier<RGResource> resource,
        PassResourceUsageClass requestedUsage,
        IRenderGraphValidationResourceProvider resourceProvider)
    {
        var resourceType = resourceProvider.GetResourceType(resource);
        if (RequiresTexture(requestedUsage) && resourceType != RGResourceType.Texture)
        {
            return FormatInvalidType(pass, resource, resourceType, requestedUsage, resourceProvider);
        }

        var conflictingUsage = FindConflictingUsage(pass, resource, requestedUsage);
        return conflictingUsage == PassResourceUsageClass.None
            ? null
            : FormatConflict(pass, resource, resourceType, conflictingUsage, requestedUsage, resourceProvider);
    }

    public static string? ValidatePass(RenderGraphPass pass, IRenderGraphValidationResourceProvider resourceProvider)
    {
        if (!pass.HasRenderFunc())
        {
            return $"Render graph pass '{pass.name}' (#{pass.index}) does not have a render function.";
        }

        if (pass.type == RenderPassType.Raster && pass.colorAccess[0].id.IsInvalid && pass.depthAccess.id.IsInvalid)
        {
            return $"Raster render graph pass '{pass.name}' (#{pass.index}) must have at least one color or depth attachment.";
        }

        if (pass.type != RenderPassType.Raster && (pass.maxColorIndex >= 0 || pass.depthAccess.id.IsValid))
        {
            return $"Render graph pass '{pass.name}' (#{pass.index}) declares raster attachments but has pass type {pass.type}.";
        }

        if (pass.type != RenderPassType.Unsafe && pass.renderTargetWrites.Count != 0)
        {
            return $"Render graph pass '{pass.name}' (#{pass.index}) declares unsafe render-target usage but has pass type {pass.type}.";
        }

        for (var colorIndex = 0; colorIndex <= pass.maxColorIndex; colorIndex++)
        {
            var color = pass.colorAccess[colorIndex];
            if (color.id.IsInvalid)
            {
                continue;
            }

            var error = ValidateDeclaration(pass, color.id.AsResource(), PassResourceUsageClass.ColorAttachment, resourceProvider);
            if (error is not null)
            {
                return error;
            }
        }

        if (pass.depthAccess.id.IsValid)
        {
            var depthUsage = pass.depthAccess.usage.layout == BarrierLayout.DepthStencilWrite
                ? PassResourceUsageClass.DepthWrite
                : PassResourceUsageClass.DepthRead;
            var error = ValidateDeclaration(pass, pass.depthAccess.id.AsResource(), depthUsage, resourceProvider);
            if (error is not null)
            {
                return error;
            }
        }

        foreach (var resource in pass.randomAccess)
        {
            var error = ValidateDeclaration(pass, resource, PassResourceUsageClass.UnorderedAccess, resourceProvider);
            if (error is not null)
            {
                return error;
            }
        }

        foreach (var resource in pass.renderTargetWrites)
        {
            var error = ValidateDeclaration(pass, resource, PassResourceUsageClass.ColorAttachment, resourceProvider);
            if (error is not null)
            {
                return error;
            }
        }

        for (var resourceType = 0; resourceType < (int)RGResourceType.Count; resourceType++)
        {
            var expectedType = (RGResourceType)resourceType;
            var error = ValidateResourceSetTypes(pass, pass.resourceReads[resourceType], expectedType, "read", resourceProvider);
            if (error is not null)
            {
                return error;
            }

            error = ValidateResourceSetTypes(pass, pass.resourceWrites[resourceType], expectedType, "write", resourceProvider);
            if (error is not null)
            {
                return error;
            }

            error = ValidateResourceSetTypes(pass, pass.resourceCreates[resourceType], expectedType, "create", resourceProvider);
            if (error is not null)
            {
                return error;
            }

            if (pass.type == RenderPassType.Compute)
            {
                continue;
            }

            foreach (var resource in pass.resourceWrites[resourceType])
            {
                if (!HasExplicitWriteUsage(pass, resource))
                {
                    var actualType = resourceProvider.GetResourceType(resource);
                    var name = resourceProvider.GetResourceName(resource);
                    return $"Render graph pass '{pass.name}' (#{pass.index}), resource '{name}' [{actualType} #{resource.Value}]: " +
                           $"generic writes are ambiguous for {pass.type} passes; declare an attachment, random-access usage, or explicit unsafe usage.";
                }
            }
        }

        return null;
    }

    public static string? ValidateGraph(IReadOnlyList<RenderGraphPass> passes, IRenderGraphValidationResourceProvider resourceProvider)
    {
        for (var passIndex = 0; passIndex < passes.Count; passIndex++)
        {
            var error = ValidatePass(passes[passIndex], resourceProvider);
            if (error is not null)
            {
                return error;
            }
        }

        return null;
    }

    private static string? ValidateResourceSetTypes(
        RenderGraphPass pass,
        RenderGraphResourceSet resources,
        RGResourceType expectedType,
        string declaration,
        IRenderGraphValidationResourceProvider resourceProvider)
    {
        foreach (var resource in resources)
        {
            var actualType = resourceProvider.GetResourceType(resource);
            if (actualType != expectedType)
            {
                var name = resourceProvider.GetResourceName(resource);
                return $"Render graph pass '{pass.name}' (#{pass.index}), resource '{name}' [{actualType} #{resource.Value}]: " +
                       $"the {declaration} declaration records it as {expectedType}.";
            }
        }

        return null;
    }

    private static PassResourceUsageClass FindConflictingUsage(
        RenderGraphPass pass,
        Identifier<RGResource> resource,
        PassResourceUsageClass requestedUsage)
    {
        for (var colorIndex = 0; colorIndex <= pass.maxColorIndex; colorIndex++)
        {
            if (pass.colorAccess[colorIndex].id.AsResource() == resource &&
                AreConflicting(PassResourceUsageClass.ColorAttachment, requestedUsage))
            {
                return PassResourceUsageClass.ColorAttachment;
            }
        }

        if (pass.depthAccess.id.IsValid && pass.depthAccess.id.AsResource() == resource)
        {
            var depthUsage = pass.depthAccess.usage.layout == BarrierLayout.DepthStencilWrite
                ? PassResourceUsageClass.DepthWrite
                : PassResourceUsageClass.DepthRead;
            if (AreConflicting(depthUsage, requestedUsage))
            {
                return depthUsage;
            }
        }

        if (pass.randomAccess.Contains(resource) && AreConflicting(PassResourceUsageClass.UnorderedAccess, requestedUsage))
        {
            return PassResourceUsageClass.UnorderedAccess;
        }

        if (pass.renderTargetWrites.Contains(resource) && AreConflicting(PassResourceUsageClass.ColorAttachment, requestedUsage))
        {
            return PassResourceUsageClass.ColorAttachment;
        }

        return PassResourceUsageClass.None;
    }

    private static bool HasExplicitWriteUsage(RenderGraphPass pass, Identifier<RGResource> resource)
    {
        if (pass.randomAccess.Contains(resource) || pass.renderTargetWrites.Contains(resource))
        {
            return true;
        }

        for (var colorIndex = 0; colorIndex <= pass.maxColorIndex; colorIndex++)
        {
            if (pass.colorAccess[colorIndex].id.AsResource() == resource)
            {
                return true;
            }
        }

        return pass.depthAccess.id.IsValid && pass.depthAccess.id.AsResource() == resource;
    }

    private static bool AreConflicting(PassResourceUsageClass existingUsage, PassResourceUsageClass requestedUsage)
    {
        var existingGroup = GetConcreteUsageGroup(existingUsage);
        var requestedGroup = GetConcreteUsageGroup(requestedUsage);
        return existingGroup != 0 && requestedGroup != 0 && existingGroup != requestedGroup;
    }

    private static int GetConcreteUsageGroup(PassResourceUsageClass usage)
    {
        return usage switch
        {
            PassResourceUsageClass.ColorAttachment => 1,
            PassResourceUsageClass.DepthRead or PassResourceUsageClass.DepthWrite => 2,
            PassResourceUsageClass.UnorderedAccess => 3,
            _ => 0
        };
    }

    private static bool RequiresTexture(PassResourceUsageClass usage)
    {
        return usage is PassResourceUsageClass.ColorAttachment or PassResourceUsageClass.DepthRead or PassResourceUsageClass.DepthWrite;
    }

    private static string FormatConflict(
        RenderGraphPass pass,
        Identifier<RGResource> resource,
        RGResourceType resourceType,
        PassResourceUsageClass existingUsage,
        PassResourceUsageClass requestedUsage,
        IRenderGraphValidationResourceProvider resourceProvider)
    {
        var name = resourceProvider.GetResourceName(resource);
        return $"Render graph pass '{pass.name}' (#{pass.index}), resource '{name}' [{resourceType} #{resource.Value}]: " +
               $"{requestedUsage} conflicts with {existingUsage} for the whole-resource range.";
    }

    private static string FormatInvalidType(
        RenderGraphPass pass,
        Identifier<RGResource> resource,
        RGResourceType resourceType,
        PassResourceUsageClass requestedUsage,
        IRenderGraphValidationResourceProvider resourceProvider)
    {
        var name = resourceProvider.GetResourceName(resource);
        return $"Render graph pass '{pass.name}' (#{pass.index}), resource '{name}' [{resourceType} #{resource.Value}]: " +
               $"{requestedUsage} requires a texture resource.";
    }
}
#endif
