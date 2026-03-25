using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderCompiler;
using Ghost.Graphics.Core;
using Ghost.Graphics.Core.Contracts;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.Image;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Utilities;
using System.Runtime.InteropServices;

namespace Ghost.Graphics;

internal class MeshRenderPassData
{
    public Handle<Mesh> mesh;
    public Handle<Material> material;
    public Identifier<RGTexture> renderTarget;
}

internal class BlitPassData
{
    public Identifier<RGTexture> source;
    public Identifier<RGTexture> destination;

    public Handle<Material> blitMaterial;
    public Identifier<Sampler> sampler;
}

/// <summary>
/// Simplified bindless mesh render pass using high-level bindless APIs with fully bindless vertex/index buffer access
/// </summary>
internal class MeshRenderPass : IRenderPass
{
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

    private Handle<Mesh> _mesh;
    private Identifier<Shader> _shader;
    private Handle<Material> _material;
    private Handle<Texture>[]? _textures;
    private Identifier<Sampler> _sampler;

    private Identifier<Shader> _blitShader;
    private Handle<Material> _blitMaterial;

    // Texture file paths for this demo
    private readonly string[] _textureFiles = [
        "C:/Users/Misaki/Downloads/Im/Icon.png",
        "C:/Users/Misaki/Downloads/Im/Backdrop.jpg",
        "C:/Users/Misaki/Downloads/Im/101167591_p0.png",
        "C:/Users/Misaki/Downloads/Im/yande.re 1134666 blue_archive nakamasa_ichika sugarhigh.jpg"
    ];

    private static IEnumerable<ReadOnlyMemory<string>> GetAllVariantCombination(KeywordsGroup[] keywordsGroups)
    {
        if (keywordsGroups.Length == 0)
        {
            yield return ReadOnlyMemory<string>.Empty;
            yield break;
        }

        var firstGroup = keywordsGroups[0];
        var remainingGroups = keywordsGroups[1..];

        foreach (var combination in GetAllVariantCombination(remainingGroups))
        {
            yield return combination;
        }

        foreach (var keyword in firstGroup.keywords)
        {
            foreach (var combination in GetAllVariantCombination(remainingGroups))
            {
                var array = new string[combination.Length + 1];
                array[0] = keyword;
                combination.Span.CopyTo(array.AsSpan(1));

                yield return array;
            }
        }
    }

    private void CompileBlitShader(ref readonly RenderingContext ctx)
    {
        var shaderDescriptor = DSLShaderCompiler.CompileShader("F:/csharp/GhostEngine/src/Runtime/Ghost.Graphics/Shaders/Blit.gshdr", "C:/Users/Misaki/Downloads/Archive").GetValueOrThrow();
        _blitShader = ctx.ResourceManager.CreateGraphicsShader(shaderDescriptor);
        _blitMaterial = ctx.ResourceManager.CreateMaterial(_blitShader);

        var config = new ShaderCompilationConfig
        {
            optimizeLevel = CompilerOptimizeLevel.O3,
            options = CompilerOption.KeepReflections,
            tier = CompilerTier.Tier2
        };

        var pass = shaderDescriptor.passes[0];
        var emptyKeywords = new LocalKeywordSet();
        var variantKey = RHIUtility.CreateShaderVariantKey(
            RHIUtility.CreateShaderPassKey(pass.identifier),
            in emptyKeywords);

        ctx.ShaderCompiler.CompilePass(in pass, in config, variantKey).GetValueOrThrow();
    }

    public void Initialize(ref readonly RenderingContext ctx)
    {
        CompileBlitShader(in ctx);

        var shaderDescriptor = DSLShaderCompiler.CompileShader("F:/csharp/GhostEngine/src/Runtime/Ghost.Graphics/test.gshdr", "C:/Users/Misaki/Downloads/Archive").GetValueOrThrow();

        _shader = ctx.ResourceManager.CreateGraphicsShader(shaderDescriptor);
        _material = ctx.ResourceManager.CreateMaterial(_shader);

        for (var i = 0; i < shaderDescriptor.passes.Length; i++)
        {
            ref var pass = ref shaderDescriptor.passes[i];
            var config = new ShaderCompilationConfig
            {
                optimizeLevel = CompilerOptimizeLevel.O3,
                options = CompilerOption.KeepReflections,
                tier = CompilerTier.Tier2
            };

            // TODO: Ideally, in editor mode, we compile a single variant when it's needed during rendering. Before the compilation is done, we fallback to a special "compilation in progress" shader.
            // During the build process, we can precompile all the variants and store them in the cache for fast loading in runtime.
            // After the compilation, we should store the compiled result in the disk cache even in editor mode. This allows us to avoid recompiling the same variant, same code hash and same version) multiple times.
            if (pass.keywords.Length == 0)
            {
                var emptyKeywords = new LocalKeywordSet();
                var variantKey = RHIUtility.CreateShaderVariantKey(
                    RHIUtility.CreateShaderPassKey(pass.identifier),
                    in emptyKeywords);

                ctx.ShaderCompiler.CompilePass(in pass, in config, variantKey).GetValueOrThrow();
            }
            else
            {
                var shaderResult = ctx.ResourceManager.GetShaderReference(_shader);
                if (shaderResult.IsFailure)
                {
                    throw new InvalidOperationException("Failed to get shader reference.");
                }

                ref readonly var shaderRef = ref shaderResult.Value;
                foreach (var keyGroup in GetAllVariantCombination(pass.keywords))
                {
                    config.defines = keyGroup.Span;
                    var keywordsSet = new LocalKeywordSet();

                    foreach (var key in keyGroup.Span)
                    {
                        var localIndex = shaderRef.GetLocalKeywordIndex(Shader.GetKeywordID(key));
                        if (localIndex == -1)
                        {
                            continue;
                        }

                        keywordsSet.SetKeyword(localIndex, true);
                    }

                    var variantKey = RHIUtility.CreateShaderVariantKey(
                        RHIUtility.CreateShaderPassKey(pass.identifier),
                        in keywordsSet);

                    ctx.ShaderCompiler.CompilePass(in pass, in config, variantKey).GetValueOrThrow();
                }
            }
        }

        MeshBuilder.CreateCube(0.75f, default, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent, out var vertices, out var indices);

        _mesh = ctx.CreateMesh(vertices, indices, true);

        // Cook meshlets for the mesh
        var meshRef = ctx.ResourceManager.GetMeshReference(_mesh);
        if (meshRef.IsSuccess)
        {
            meshRef.Value.CookMeshlets();
        }

        ctx.UploadMeshlets(_mesh);

        ctx.UpdateObjectData(_mesh);

        _textures = new Handle<Texture>[_textureFiles.Length];
        for (var i = 0; i < _textureFiles.Length; i++)
        {
            using var stream = File.OpenRead(_textureFiles[i]);
            using var imageData = ImageResult.FromStream(stream, ColorComponents.RGBA);
            var desc = new TextureDesc
            {
                Width = imageData.Width,
                Height = imageData.Height,
                Dimension = TextureDimension.Texture2D,
                Format = TextureFormat.R8G8B8A8_UNorm,
                MipLevels = 1,
                Slice = 1,
                Usage = TextureUsage.ShaderResource,
            };

            _textures[i] = ctx.CreateTexture<byte>(in desc, imageData.AsSpan(), $"Texture_{i}");
        }

        var samplerDesc = new SamplerDesc
        {
            AddressU = TextureAddressMode.Repeat,
            AddressV = TextureAddressMode.Repeat,
            AddressW = TextureAddressMode.Repeat,
            FilterMode = TextureFilterMode.Bilinear,
            MaxAnisotropy = 16,
        };

        _sampler = ctx.ResourceAllocator.CreateSampler(in samplerDesc);

        var meshResult = ctx.ResourceManager.GetMaterialReference(_material);
        if (meshResult.IsFailure)
        {
            throw new InvalidOperationException("Failed to get material reference.");
        }

        ref var matRef = ref meshResult.Value;
        var matProps = new ShaderProperties_MyShader_Standard
        {
            color = new float4(1.0f, 1.0f, 1.0f, 1.0f),
            texture1 = ctx.ResourceDatabase.GetBindlessIndex(_textures[0].AsResource()),
            texture2 = ctx.ResourceDatabase.GetBindlessIndex(_textures[1].AsResource()),
            texture3 = ctx.ResourceDatabase.GetBindlessIndex(_textures[2].AsResource()),
            texture4 = ctx.ResourceDatabase.GetBindlessIndex(_textures[3].AsResource()),
            tex_sampler = (uint)_sampler.Value,
        };

        matRef.SetPropertyCache(in matProps).ThrowIfFailed();
        matRef.UploadData(ctx.DirectCommandBuffer, ctx.ResourceDatabase);
    }

    public void Build(RenderGraph graph, Identifier<RGTexture> backbuffer)
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

    public void Cleanup(ResourceManager resourceManager, IResourceDatabase resourceDatabase)
    {
        resourceManager.ReleaseMaterial(_blitMaterial);

        resourceManager.ReleaseMaterial(_material);
        resourceManager.ReleaseShader(_shader);
        resourceManager.ReleaseMesh(_mesh);
        resourceDatabase.ReleaseSampler(_sampler);

        if (_textures != null)
        {
            foreach (var texture in _textures)
            {
                resourceDatabase.ReleaseResource(texture.AsResource());
            }
        }
    }
}
