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

    private static IEnumerable<List<string>> GetAllVariantCombination(List<KeywordsGroup> keywordsGroups)
    {
        if (keywordsGroups.Count == 0)
        {
            yield return [];
            yield break;
        }

        var firstGroup = keywordsGroups[0];
        var remainingGroups = keywordsGroups.Skip(1).ToList();
        foreach (var keyword in firstGroup.keywords)
        {
            foreach (var combination in GetAllVariantCombination(remainingGroups))
            {
                combination.Insert(0, keyword);
                yield return combination;
            }
        }
    }

    public void Initialize(ref readonly RenderingContext ctx)
    {
        var shaderDescriptor = DSLShaderCompiler.CompileShader("F:/csharp/GhostEngine/Ghost.Graphics/test.gsdef", "C:/Users/Misaki/Downloads/Archive").GetValueOrThrow();

        _shader = ctx.ResourceAllocator.CreateGraphicsShader(shaderDescriptor);
        _material = ctx.ResourceAllocator.CreateMaterial(_shader);

        for (var i = 0; i < shaderDescriptor.passes.Count; i++)
        {
            var pass = shaderDescriptor.passes[i];

            if (pass is not PassDescriptor fullPass)
            {
                continue;
            }

            var config = new ShaderCompilationConfig
            {
                optimizeLevel = CompilerOptimizeLevel.O3,
                options = CompilerOption.KeepReflections,
                tier = CompilerTier.Tier2
            };

            // TODO: Ideally, in editor mode, we compile a single variant when it's needed during rendering. Before the compilation is done, we fallback to a special "compilation in progress" shader.
            // During the build process, we can precompile all the variants and store them in the cache for fast loading in runtime.
            // After the compilation, we should store the compiled result in the disk cache even in editor mode. This allows us to avoid recompiling the same variant, same code hash and same version) multiple times.
            if (fullPass.keywords == null)
            {
                var emptyKeywords = new LocalKeywordSet();
                var variantKey = RHIUtility.CreateShaderVariantKey(
                    RHIUtility.CreateShaderPassKey(pass.Identifier),
                    in emptyKeywords);

                ctx.ShaderCompiler.CompilePass(pass, in config, variantKey).GetValueOrThrow();
            }
            else
            {
                ref var shaderRef = ref ctx.ResourceDatabase.GetShaderReference(_shader);

                foreach (var keyGroup in GetAllVariantCombination(fullPass.keywords))
                {
                    config.defines = keyGroup.AsSpan();
                    var keywordsSet = new LocalKeywordSet();

                    foreach (var key in keyGroup)
                    {
                        var localIndex = shaderRef.GetLocalKeywordIndex(Shader.GetKeywordID(key));
                        if (localIndex == -1)
                        {
                            continue;
                        }

                        keywordsSet.SetKeyword(localIndex, true);
                    }

                    var variantKey = RHIUtility.CreateShaderVariantKey(
                        RHIUtility.CreateShaderPassKey(pass.Identifier),
                        in keywordsSet);

                    ctx.ShaderCompiler.CompilePass(pass, in config, variantKey).GetValueOrThrow();
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

            _textures[i] = ctx.CreateTexture(in desc, imageData.AsSpan());
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
            texture1 = ctx.ResourceDatabase.GetBindlessIndex(_textures[0].AsResource()).GetValueOrThrow(),
            texture2 = ctx.ResourceDatabase.GetBindlessIndex(_textures[1].AsResource()).GetValueOrThrow(),
            texture3 = ctx.ResourceDatabase.GetBindlessIndex(_textures[2].AsResource()).GetValueOrThrow(),
            texture4 = ctx.ResourceDatabase.GetBindlessIndex(_textures[3].AsResource()).GetValueOrThrow(),
            tex_sampler = (uint)sampler.Value,
        };

        Debug.Assert(matRef.SetPropertyCache(in matProps) == ErrorStatus.None);
        matRef.UploadData(ctx.DirectCommandBuffer);

        var pso = matRef.GetPassPipelineOverride(0);
        pso.Cull = Cull.Back;
        matRef.SetPassPipelineOverride(0, in pso);

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
