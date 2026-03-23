using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RenderPipeline;

public partial class GhostRenderPipeline
{
    private class MeshRenderPassData
    {
        public RenderList renderList;
        public Identifier<RGTexture> renderTarget;
    }

    private class BlitPassData
    {
        public Identifier<RGTexture> source;
        public Identifier<RGTexture> destination;

        public Handle<Material> blitMaterial;
        public Identifier<Sampler> sampler;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShaderProperties_MyShader_Standard
    {
        public float4 color;
        public uint texture1;
        public uint texture2;
        public uint texture3;
        public uint texture4;
        public uint tex_sampler;

        private readonly uint _padding1;
        private readonly uint _padding2;
        private readonly uint _padding3;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShaderProperties_Hidden_Blit
    {
        public uint mainTex;
        public uint sampler_mainTex;
        private readonly uint _padding1;
        private readonly uint _padding2;
    }
#if flase
    private void RenderTest(RenderGraph graph, Identifier<RGTexture> backbuffer)
    {
        Identifier<RGTexture> renderTarget;
        using (var builder = graph.AddRasterRenderPass<MeshRenderPassData>("Mesh Render Pass", out var passData))
        {
            passData.mesh = _mesh;
            passData.material = _material;

            passData.renderTarget = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), "Render Target");
            builder.SetColorAttachment(passData.renderTarget, 0);

            renderTarget = passData.renderTarget;

            builder.SetRenderFunc<MeshRenderPassData>(static (data, ctx) =>
            {
                ctx.SetActiveMaterial(data.material);
                ctx.SetActiveMesh(data.mesh);

                var threadGroupCountX = ((uint)ctx.ActiveMeshIndexCount + 2u) / 3u;
                ctx.DispatchMesh(new uint3(threadGroupCountX, 1u, 1u));
            });
        }

        using (var builder = graph.AddUnsafeRenderPass<BlitPassData>("Blit Pass", out var passData))
        {
            passData.source = renderTarget;
            passData.destination = backbuffer;
            passData.blitMaterial = _blitMaterial;
            passData.sampler = _sampler;

            builder.UseTexture(passData.source, AccessFlags.Read);
            builder.UseTexture(passData.destination, AccessFlags.WriteAll);

            builder.SetRenderFunc<BlitPassData>(static (data, ctx) =>
            {
                var r = ctx.ResourceManager.GetMaterialReference(data.blitMaterial);
                if (r.IsFailure)
                {
                    return;
                }

                ref var matRef = ref r.Value;
                var blitProps = new ShaderProperties_Hidden_Blit
                {
                    mainTex = ctx.ResourceDatabase.GetBindlessIndex(ctx.GetActualResource(data.source.AsResource())),
                    sampler_mainTex = (uint)data.sampler.Value,
                };

                matRef.SetPropertyCache(in blitProps).ThrowIfFailed();
                matRef.UploadData(ctx.CommandBuffer, ctx.ResourceDatabase);

                ctx.CommandBuffer.SetRenderTargets([ctx.GetActualTexture(data.destination)], Handle<Texture>.Invalid);

                ctx.SetActiveMaterial(data.blitMaterial);
                ctx.SetActiveMesh(Handle<Mesh>.Invalid); // Generate a full-screen triangle dynamically in mesh shader.
                ctx.DispatchMesh(new uint3(1, 1, 1));
            });
        }
    }
# endif
}
