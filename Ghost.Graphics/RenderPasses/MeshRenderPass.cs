using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Ghost.SDL.Compiler;
using Misaki.HighPerformance.Image;

namespace Ghost.Graphics.RenderPasses;

/// <summary>
/// Simplified bindless mesh render pass using high-level bindless APIs with fully bindless vertex/index buffer access
/// </summary>
internal unsafe class MeshRenderPass : IRenderPass
{
    private Handle<Mesh> _mesh;
    private Identifier<Shader> _shader;
    private Handle<Material> _material;
    private Handle<Texture>[]? _textures;

    // Texture file paths for this demo
    private readonly string[] _textureFiles = [
        "C:/Users/Misaki/Downloads/Im/Icon.png",
        "C:/Users/Misaki/Downloads/Im/Backdrop.jpg",
        "C:/Users/Misaki/Downloads/Im/101167591_p0.png",
        "C:/Users/Misaki/Downloads/Im/yande.re 1134666 blue_archive nakamasa_ichika sugarhigh.jpg"
    ];

    public void Initialize(ref readonly RenderingContext ctx, IResourceAllocator resourceAllocator, IPipelineLibrary pipelineLibrary)
    {
        var shaderDescriptor = SDLCompiler.CompileShader("F:\\csharp\\GhostEngine\\Ghost.Graphics\\RenderPasses\\ShaderCode.hlsl").GetValueOrThrow();

        var key = pipelineLibrary.CompilePassPSO(shaderDescriptor.passes[0], [TextureFormat.B8G8R8A8_UNorm], TextureFormat.Unknown);

        MeshBuilder.CreateCube(0.75f, default, out var vertices, out var indices);

        _mesh = ctx.CreateMesh(vertices, indices);
        ctx.UploadMesh(_mesh, true);

        _shader = resourceAllocator.CreateShader(shaderDescriptor);
        _material = resourceAllocator.CreateMaterial(_shader);

        var imageResults = new ImageResult[_textureFiles.Length];

        _textures = new Handle<Texture>[_textureFiles.Length];
        for (var i = 0; i < _textureFiles.Length; i++)
        {
            using var stream = File.OpenRead(_textureFiles[i]);
            using var imageData = ImageResult.FromStream(stream);
            imageResults[i] = imageData;

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

            _textures[i] = ctx.CreateTexture(ref desc);
            ctx.UploadTexture(_textures[i], new Span<byte>(imageData.Data, (int)imageData.Size));
        }
    }

    public void Execute(ref readonly RenderingContext ctx)
    {
        ctx.DispatchMesh(_mesh, _material, "Forward", 8);
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
