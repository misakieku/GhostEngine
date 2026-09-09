using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics;

/// <summary>
/// Records allocation-free per-variant indirect compute dispatches from a semantic shader roster.
/// </summary>
public static class ShaderVariantRendering
{
    /// <summary>
    /// Binds each bytecode-ready variant implementing <paramref name="semantic"/> and executes its indirect range.
    /// </summary>
    /// <remarks>
    /// Variants without published bytecode, the requested pass, or an exact ready pipeline are skipped. The dense variant index selects both the argument range and count-buffer slot.
    /// </remarks>
    /// <returns>The number of indirect execute calls recorded.</returns>
    public static int ExecuteIndirectCompute(
        IShaderVariantSource variantSource,
        IComputeRenderContext context,
        PassSemantic semantic,
        ICommandSignature commandSignature,
        uint maxCommandCount,
        Handle<GPUBuffer> argumentBuffer,
        ulong argumentBaseOffset,
        ulong argumentRangeStride,
        Handle<GPUBuffer> countBuffer,
        ulong countBaseOffset,
        ulong countStride)
    {
        var variants = variantSource.GetDispatchVariants(semantic);
        var executeCount = 0;
        for (var i = 0; i < variants.Length; i++)
        {
            ref readonly var variant = ref variants[i];
            if (!variantSource.IsBytecodeReady(variant.DenseIndex) ||
                !context.TrySetActiveShaderPass(variant.Shader, semantic))
            {
                continue;
            }

            var variantIndex = (ulong)variant.DenseIndex;
            context.ExecuteIndirect(
                commandSignature,
                maxCommandCount,
                argumentBuffer,
                argumentBaseOffset + (variantIndex * argumentRangeStride),
                countBuffer,
                countBaseOffset + (variantIndex * countStride));
            executeCount++;
        }

        return executeCount;
    }
}
