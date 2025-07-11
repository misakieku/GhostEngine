using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.Data;
using Ghost.Graphics.Shading;
using Ghost.Graphics.Utilities;
using StbImageSharp;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.RenderPasses;

internal unsafe class MeshRenderPass : IRenderPass
{
    private const string _HLSL_SOURCE = @"
cbuffer ConstantBuffer : register(b0)
{
    float4 _Color;
};

Texture2D _MainTex : register(t0);
SamplerState _MainSampler : register(s0);

struct VertexInput
{
    float4 position : POSITION;
    float4 color    : COLOR;
    float4 uv        : TEXCOORD0;
};

struct PixelInput
{
    float4 position : SV_POSITION;
    float4 color    : COLOR;
    float4 uv       : TEXCOORD0;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    output.position = input.position;
    output.color = input.color;
    output.uv = input.uv;
    return output;
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    return _MainTex.Sample(_MainSampler, input.uv.xy);
}
";

    private Mesh? _mesh;
    private Shader? _shader;
    private Material? _material;

    // Direct D3D12 resources for texture
    private ComPtr<ID3D12Resource> _textureResource;
    private ComPtr<ID3D12DescriptorHeap> _srvHeap;
    private CpuDescriptorHandle _srvHandle;
    private GpuDescriptorHandle _srvGpuHandle;
    private uint _srvDescriptorSize;

    // Additional fields for deferred texture upload
    private ComPtr<ID3D12Resource> _uploadBuffer;
    private uint _textureWidth;
    private uint _textureHeight;
    private uint _texturePitch;
    private bool _textureUploaded = false;

    public void Initialize(CommandList cmd)
    {
        _mesh = MeshBuilder.CreateCube(0.25f);
        _mesh.UploadMeshData();

        _shader = new(_HLSL_SOURCE);
        _material = new(_shader);

        // Create direct D3D12 texture resources
        CreateTextureDirectly();

        _material.UploadMaterialData();

        // Copy from upload buffer to texture
        var srcLocation = new TextureCopyLocation(_uploadBuffer.Get(), new PlacedSubresourceFootprint
        {
            Offset = 0,
            Footprint = new SubresourceFootprint
            {
                Format = Format.R8G8B8A8Unorm,
                Width = _textureWidth,
                Height = _textureHeight,
                Depth = 1,
                RowPitch = _texturePitch
            }
        });

        var dstLocation = new TextureCopyLocation(_textureResource.Get(), 0);

        cmd.NativeCommandList.Ptr->CopyTextureRegion(&dstLocation, 0, 0, 0, &srcLocation, null);

        // Transition texture to shader resource
        var barrier = new ResourceBarrier
        {
            Type = ResourceBarrierType.Transition,
            Flags = ResourceBarrierFlags.None,
            Transition = new ResourceTransitionBarrier
            {
                pResource = _textureResource.Get(),
                StateBefore = ResourceStates.CopyDest,
                StateAfter = ResourceStates.PixelShaderResource,
                Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES
            }
        };
        cmd.NativeCommandList.Ptr->ResourceBarrier(1, &barrier);
    }

    private void CreateTextureDirectly()
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

        // Load image data
        using var stream = new FileStream("C:/Users/Misaki/Downloads/Im/Icon.png", FileMode.Open, FileAccess.Read);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        var width = (uint)image.Width;
        var height = (uint)image.Height;
        uint bytesPerPixel = 4; // RGBA
        var pitch = width * bytesPerPixel;
        var textureSize = pitch * height;

        // Create the texture resource
        var textureDesc = new ResourceDescription
        {
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0,
            Width = width,
            Height = height,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = Format.R8G8B8A8Unorm,
            SampleDesc = new SampleDescription(1, 0),
            Layout = TextureLayout.Unknown,
            Flags = ResourceFlags.None
        };

        var heapProps = new HeapProperties(HeapType.Default);
        device->CreateCommittedResource(
            &heapProps,
            HeapFlags.None,
            &textureDesc,
            ResourceStates.CopyDest,
            null,
            __uuidof<ID3D12Resource>(),
            _textureResource.GetVoidAddressOf()
        );

        // Create upload buffer
        var uploadBufferSize = GetRequiredIntermediateSize(_textureResource.Get(), 0, 1);
        var uploadHeapProps = new HeapProperties(HeapType.Upload);
        var uploadBufferDesc = ResourceDescription.Buffer(uploadBufferSize);

        ComPtr<ID3D12Resource> uploadBuffer = default;
        device->CreateCommittedResource(
            &uploadHeapProps,
            HeapFlags.None,
            &uploadBufferDesc,
            ResourceStates.GenericRead,
            null,
            __uuidof<ID3D12Resource>(),
            uploadBuffer.GetVoidAddressOf()
        );

        // Map and copy texture data
        void* mappedData = null;
        uploadBuffer.Get()->Map(0, null, &mappedData);

        // Copy image data to upload buffer
        var srcData = image.Data.AsSpan();
        var dstSpan = new Span<byte>(mappedData, (int)uploadBufferSize);

        // Copy row by row with proper alignment
        var alignedPitch = (pitch + 255) & ~255u; // Align to 256 bytes
        for (var y = 0; y < height; y++)
        {
            var srcRow = srcData.Slice(y * (int)pitch, (int)pitch);
            var dstRow = dstSpan.Slice(y * (int)alignedPitch, (int)pitch);
            srcRow.CopyTo(dstRow);
        }

        uploadBuffer.Get()->Unmap(0, null);

        // We'll copy the texture data during Execute phase when we have access to command list
        // Store the upload buffer for later use
        _uploadBuffer = uploadBuffer.Move();
        _textureWidth = width;
        _textureHeight = height;
        _texturePitch = alignedPitch;

        // Create SRV descriptor heap
        var srvHeapDesc = new DescriptorHeapDescription
        {
            Type = DescriptorHeapType.CbvSrvUav,
            NumDescriptors = 1,
            Flags = DescriptorHeapFlags.ShaderVisible
        };

        device->CreateDescriptorHeap(&srvHeapDesc, __uuidof<ID3D12DescriptorHeap>(), _srvHeap.GetVoidAddressOf());

        // Get descriptor handles
        _srvHandle = _srvHeap.Get()->GetCPUDescriptorHandleForHeapStart();
        _srvGpuHandle = _srvHeap.Get()->GetGPUDescriptorHandleForHeapStart();
        _srvDescriptorSize = device->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);

        // Create SRV
        var srvDesc = new ShaderResourceViewDescription
        {
            Format = Format.R8G8B8A8Unorm,
            ViewDimension = Win32.Graphics.Direct3D12.SrvDimension.Texture2D,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Texture2D = new Texture2DSrv
            {
                MostDetailedMip = 0,
                MipLevels = 1,
                PlaneSlice = 0,
                ResourceMinLODClamp = 0.0f
            }
        };

        device->CreateShaderResourceView(_textureResource.Get(), &srvDesc, _srvHandle);
    }

    private static ulong GetRequiredIntermediateSize(ID3D12Resource* destinationResource, uint firstSubresource, uint numSubresources)
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;
        var resourceDesc = destinationResource->GetDesc();

        ulong requiredSize = 0;
        device->GetCopyableFootprints(&resourceDesc, firstSubresource, numSubresources, 0, null, null, null, &requiredSize);

        return requiredSize;
    }

    public void Execute(CommandList cmd)
    {
        var commandList = cmd.NativeCommandList.Ptr;

        // Set root signature and pipeline state
        commandList->SetGraphicsRootSignature(_material!.Shader.RootSignature);
        commandList->SetPipelineState(_material.Shader.PipelineState);

        // Set descriptor heap
        var heaps = stackalloc ID3D12DescriptorHeap*[1];
        heaps[0] = _srvHeap.Get();
        commandList->SetDescriptorHeaps(1, heaps);

        // Bind texture descriptor table directly
        if (_material.Shader.Textures.Count > 0)
        {
            var textureInfo = _material.Shader.Textures[0];
            commandList->SetGraphicsRootDescriptorTable(textureInfo.RootParameterIndex, _srvGpuHandle);
        }

        // Draw mesh
        commandList->IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        commandList->IASetVertexBuffers(0, 1, _mesh!.VertexBufferView);
        commandList->IASetIndexBuffer(_mesh.IndexBufferView);
        commandList->DrawIndexedInstanced(_mesh.IndexCount, 1, 0, 0, 0);
    }

    public void Dispose()
    {
        _mesh?.Dispose();
        _shader?.Dispose();
        _material?.Dispose();

        _textureResource.Dispose();
        _uploadBuffer.Dispose();
        _srvHeap.Dispose();
    }
}
