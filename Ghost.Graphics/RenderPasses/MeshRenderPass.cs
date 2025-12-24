using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Ghost.SDL.Compiler;
using Misaki.HighPerformance.Image;
using Misaki.HighPerformance.Mathematics;
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

    private GraphicsCompiledResult[]? _compileResults;

    private Identifier<ShaderPass> _forwardPassID;

    // Texture file paths for this demo
    private readonly string[] _textureFiles = [
        "C:/Users/Misaki/Downloads/Im/Icon.png",
        "C:/Users/Misaki/Downloads/Im/Backdrop.jpg",
        "C:/Users/Misaki/Downloads/Im/101167591_p0.png",
        "C:/Users/Misaki/Downloads/Im/yande.re 1134666 blue_archive nakamasa_ichika sugarhigh.jpg"
    ];

    public void Initialize(ref readonly RenderingContext ctx)
    {
        var shaderDescriptor = SDLCompiler.CompileShader("F:/csharp/GhostEngine/Ghost.Graphics/test.gshader", "C:/Users/Misaki/Downloads/Archive").GetValueOrThrow();

        _compileResults = new GraphicsCompiledResult[shaderDescriptor.passes.Count];
        for (var i = 0; i < shaderDescriptor.passes.Count; i++)
        {
            var pass = shaderDescriptor.passes[i];
            var compiled = ctx.ShaderCompiler.CompilePass(pass, shaderDescriptor.generatedCodePath).GetValueOrThrow();
            if (pass is not FullPassDescriptor fullPass)
            {
                continue;
            }

            var psoDes = new GraphicsPSODescriptor
            {
                PassId = new ShaderPassKey(fullPass.Identifier),
                PipelineOption = fullPass.localPipeline,

                RtvFormats = [TextureFormat.B8G8R8A8_UNorm],
                DsvFormat = TextureFormat.Unknown,
            };

            _compileResults[i] = compiled;
            ctx.PipelineLibrary.CompilePSO(in psoDes, in _compileResults[i]).GetValueOrThrow();
        }

        MeshBuilder.CreateCube(0.75f, default, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent, out var vertices, out var indices);

        _mesh = ctx.CreateMesh(vertices, indices, true);
        ctx.UpdateObjectData(_mesh, float4x4.identity);

        _shader = ctx.ResourceAllocator.CreateGraphicsShader(shaderDescriptor);
        _material = ctx.ResourceAllocator.CreateMaterial(_shader);

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

        if (_compileResults != null)
        {
            for (var i = 0; i < _compileResults.Length; i++)
            {
                _compileResults[i].Dispose();
            }
        }
    }
}
