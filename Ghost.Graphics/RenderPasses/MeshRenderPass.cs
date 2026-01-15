using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.DSL.ShaderCompiler;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.Image;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Utilities;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RenderPasses;

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

    private Handle<Mesh> _mesh;
    private Identifier<Shader> _shader;
    private Handle<Material> _material;
    private Handle<Texture>[]? _textures;

    private Identifier<ShaderPass> _forwardPassID;

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

    public void Initialize(ref readonly RenderingContext ctx)
    {
        var shaderDescriptor = DSLShaderCompiler.CompileShader("F:/csharp/GhostEngine/Ghost.Graphics/test.gsdef", "C:/Users/Misaki/Downloads/Archive").GetValueOrThrow();

        _shader = ctx.ResourceAllocator.CreateGraphicsShader(shaderDescriptor);
        _material = ctx.ResourceAllocator.CreateMaterial(_shader);

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
                ref var shaderRef = ref ctx.ResourceDatabase.GetShaderReference(_shader);

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
        ctx.UpdateObjectData(_mesh, float4x4.identity);

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

            _textures[i] = ctx.CreateTexture(in desc, imageData.AsSpan(), $"Texture_{i}");
        }

        var samplerDesc = new SamplerDesc
        {
            AddressU = TextureAddressMode.Repeat,
            AddressV = TextureAddressMode.Repeat,
            AddressW = TextureAddressMode.Repeat,
            FilterMode = TextureFilterMode.Bilinear,
            MaxAnisotropy = 16,
        };

        var sampler = ctx.ResourceAllocator.CreateSampler(in samplerDesc);

        ref var matRef = ref ctx.ResourceDatabase.GetMaterialReference(_material);
        var matProps = new ShaderProperties_MyShader_Standard
        {
            color = new float4(1.0f, 1.0f, 1.0f, 1.0f),
            texture1 = ctx.ResourceDatabase.GetBindlessIndex(_textures[0].AsResource()),
            texture2 = ctx.ResourceDatabase.GetBindlessIndex(_textures[1].AsResource()),
            texture3 = ctx.ResourceDatabase.GetBindlessIndex(_textures[2].AsResource()),
            texture4 = ctx.ResourceDatabase.GetBindlessIndex(_textures[3].AsResource()),
            tex_sampler = (uint)sampler.Value,
        };

        matRef.SetPropertyCache(in matProps).ThrowIfFailed();
        matRef.UploadData(ctx.DirectCommandBuffer);

        _forwardPassID = Shader.GetPassID("Forward");
    }

    public void Execute(ref readonly RenderingContext ctx)
    {
        ctx.DispatchMesh(_mesh, _material, _forwardPassID, 3);
    }

    public void Cleanup(IResourceDatabase resourceDatabase)
    {
        resourceDatabase.ReleaseMaterial(_material);
        resourceDatabase.ReleaseShader(_shader);
        resourceDatabase.ReleaseMesh(_mesh);

        if (_textures != null)
        {
            foreach (var texture in _textures)
            {
                resourceDatabase.ReleaseResource(texture.AsResource());
            }
        }
    }
}
