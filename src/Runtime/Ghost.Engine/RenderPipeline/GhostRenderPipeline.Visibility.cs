using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    private struct VisibilityPassData
    {
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> visibleMaskedMeshlets;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public ulong opaqueArgsOffset;
        public ulong maskedArgsOffset;
        public Handle<Shader> visibilityShader;
        public Handle<Shader> visibilityMaskedShader;
        public uint passIndex;
        public uint sceneBuffer;
    }

    private void AddVisibilityBufferPass(RenderGraph rg, in CameraCullingBuffers buffers, uint cullPassIndex, uint sceneBuffer, ref Identifier<RGTexture> existingVisBuffer, ref Identifier<RGTexture> existingDepth)
    {
        if (!_cullingResource.visibilityShader.IsValid || s_dispatchMeshCommandSignature == null)
        {
            return;
        }

        var isPass1 = cullPassIndex == 0;
        var passName = isPass1 ? "Visibility_Pass1_EarlyZ" : "Visibility_Pass2_LateZ";
        using var builder = rg.AddRasterRenderPass<VisibilityPassData>(passName);

        if (existingVisBuffer.IsInvalid)
        {
            var vbufferDesc = RGTextureDesc.Relative(
                1.0f,
                TextureFormat.R32G32_UInt,
                usage: TextureUsage.RenderTarget | TextureUsage.ShaderResource,
                clearAtFirstUse: true);
            existingVisBuffer = builder.CreateTexture(in vbufferDesc, "VisibilityBuffer");
        }

        if (existingDepth.IsInvalid)
        {
            var depthDesc = RGTextureDesc.Relative(
                1.0f,
                TextureFormat.D32_Float,
                usage: TextureUsage.DepthStencil | TextureUsage.ShaderResource,
                clearAtFirstUse: true);
            existingDepth = builder.CreateTexture(in depthDesc, "SceneDepthBuffer");
        }

        builder.SetColorAttachment(existingVisBuffer, 0);
        builder.SetDepthAttachment(existingDepth);

        var visibleBuffer = isPass1 ? buffers.visibleMeshletsPass1 : buffers.visibleMeshletsPass2;
        var visibleMaskedBuffer = isPass1 ? buffers.visibleMaskedMeshletsPass1 : buffers.visibleMaskedMeshletsPass2;
        builder.UseBuffer(visibleBuffer, AccessFlags.Read);
        builder.UseBuffer(visibleMaskedBuffer, AccessFlags.Read);
        builder.UseBuffer(buffers.indirectArgsBuffer, AccessFlags.Read);

        builder.SetPassData(new VisibilityPassData
        {
            visibleMeshlets = visibleBuffer,
            visibleMaskedMeshlets = visibleMaskedBuffer,
            indirectArgsBuffer = buffers.indirectArgsBuffer,
            opaqueArgsOffset = isPass1 ? 0UL : 32UL,
            maskedArgsOffset = isPass1 ? 16UL : 48UL,
            visibilityShader = _cullingResource.visibilityShader,
            visibilityMaskedShader = _cullingResource.visibilityMaskedShader,
            passIndex = cullPassIndex,
            sceneBuffer = sceneBuffer
        });

        builder.SetRenderFunc<VisibilityPassData>(static (ref readonly passData, renderCtx) =>
        {
            var actualIndirectBuf = renderCtx.GetActualBuffer(passData.indirectArgsBuffer);
            var passBit = passData.passIndex << 31;

            // 1. Draw Opaque Meshlets
            if (passData.visibilityShader.IsValid &&
                renderCtx.TrySetActiveShaderPass(passData.visibilityShader, PassSemantic.Visibility))
            {
                var visibleBufferIndex = renderCtx.GetActualBindlessIndex(passData.visibleMeshlets);
                renderCtx.SetUserData(visibleBufferIndex | passBit);
                renderCtx.ExecuteIndirect(s_dispatchMeshCommandSignature, 1, actualIndirectBuf, passData.opaqueArgsOffset);
            }

            // 2. Draw Masked (Alpha-Clipped) Meshlets
            if (passData.visibilityMaskedShader.IsValid &&
                renderCtx.TrySetActiveShaderPass(passData.visibilityMaskedShader, PassSemantic.Visibility))
            {
                var visibleMaskedIndex = renderCtx.GetActualBindlessIndex(passData.visibleMaskedMeshlets);
                renderCtx.SetUserData(visibleMaskedIndex | passBit);
                renderCtx.ExecuteIndirect(s_dispatchMeshCommandSignature, 1, actualIndirectBuf, passData.maskedArgsOffset);
            }
        });
    }
}
