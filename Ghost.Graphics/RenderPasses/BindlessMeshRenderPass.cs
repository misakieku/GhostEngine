using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.Data;
using Ghost.Graphics.Utilities;
using StbImageSharp;
using System.Runtime.InteropServices;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D.Dxc;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.RenderPasses;

/// <summary>
/// Modern bindless texture implementation using SM 6.6 with ResourceDescriptorHeap
/// and D3D12_ROOT_SIGNATURE_FLAG_CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED
/// </summary>
internal unsafe class BindlessMeshRenderPass : IRenderPass
{
    private const string _HLSL_SOURCE = @"
cbuffer ConstantBuffer : register(b0)
{
    float4 _Color;
    uint _TextureIndex1;
    uint _TextureIndex2;
    uint _TextureIndex3;
    uint _TextureIndex4;
};

// SM 6.6 approach - no need for explicit texture arrays
// ResourceDescriptorHeap is a global descriptor heap access mechanism
// This allows direct indexing into any texture in the bound descriptor heap
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
    // SM 6.6 Modern Bindless Approach:
    // ResourceDescriptorHeap[index] directly accesses any texture in the heap
    // No need for explicit texture arrays or descriptor table binding
    // This is enabled by D3D12_ROOT_SIGNATURE_FLAG_CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED
    Texture2D tex1 = ResourceDescriptorHeap[_TextureIndex1];
    Texture2D tex2 = ResourceDescriptorHeap[_TextureIndex2];
    Texture2D tex3 = ResourceDescriptorHeap[_TextureIndex3];
    Texture2D tex4 = ResourceDescriptorHeap[_TextureIndex4];
    
    // Sample the textures normally
    float4 color1 = tex1.Sample(_MainSampler, input.uv.xy);
    float4 color2 = tex2.Sample(_MainSampler, input.uv.xy);
    float4 color3 = tex3.Sample(_MainSampler, input.uv.xy);
    float4 color4 = tex4.Sample(_MainSampler, input.uv.xy);
    
    // Blend all textures together (simple average)
    float4 blendedColor = (color1 + color2 + color3 + color4) * 0.25f;
    
    return blendedColor * _Color;
}
";

    private Mesh? _mesh;

    // Direct D3D12 resources for modern bindless implementation
    private ComPtr<ID3D12PipelineState> _pipelineState;
    private ComPtr<ID3D12RootSignature> _rootSignature;
    private ComPtr<ID3D12Resource> _constantBuffer;
    private void* _constantBufferMappedData;

    // Bindless texture resources
    private ComPtr<ID3D12Resource>[] _textureResources;
    private ComPtr<ID3D12DescriptorHeap> _bindlessHeap;
    private ComPtr<ID3D12DescriptorHeap> _samplerHeap;
    private CpuDescriptorHandle _bindlessHeapStart;
    private GpuDescriptorHandle _bindlessHeapGpuStart;
    private uint _descriptorSize;

    // Texture upload resources
    private ComPtr<ID3D12Resource>[] _uploadBuffers;
    private uint[] _textureWidths;
    private uint[] _textureHeights;
    private uint[] _texturePitches;
    private bool _texturesUploaded = false;

    // Texture file paths for this demo
    private readonly string[] _textureFiles = [
        "C:/Users/Misaki/Downloads/Im/Icon.png",
        "C:/Users/Misaki/Downloads/Im/Backdrop.jpg",
        "C:/Users/Misaki/Downloads/Im/101167591_p0.png",
        "C:/Users/Misaki/Downloads/Im/yande.re 1134666 blue_archive nakamasa_ichika sugarhigh.jpg"
    ];

    // Constant buffer data structure
    private struct ConstantBufferData
    {
        public System.Numerics.Vector4 Color;
        public uint TextureIndex1;
        public uint TextureIndex2;
        public uint TextureIndex3;
        public uint TextureIndex4;
    }

    public void Initialize(CommandList cmd)
    {
        _mesh = MeshBuilder.CreateCube(0.75f);
        _mesh.UploadMeshData();

        CreateModernBindlessShader();
        CreateConstantBuffer();
        CreateBindlessTextures();
        UploadTextureData(cmd);
        UpdateConstantBuffer();
    }

    private void CreateModernBindlessShader()
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

        var vsCode = CompileShaderDXC(_HLSL_SOURCE, "VSMain", "vs_6_6");
        var psCode = CompileShaderDXC(_HLSL_SOURCE, "PSMain", "ps_6_6");

        CreateModernBindlessRootSignature();
        CreatePipelineState(vsCode, psCode);
    }

    private byte[] CompileShaderDXC(string source, string entryPoint, string profile)
    {
        using ComPtr<IDxcCompiler3> compiler = default;
        using ComPtr<IDxcUtils> utils = default;

        // Create DXC compiler and utils
        DxcCreateInstance(CLSID_DxcCompiler, __uuidof<IDxcCompiler3>(), compiler.GetVoidAddressOf());
        DxcCreateInstance(CLSID_DxcUtils, __uuidof<IDxcUtils>(), utils.GetVoidAddressOf());

        // Create source blob
        using ComPtr<IDxcBlobEncoding> sourceBlob = default;
        var sourceBytes = System.Text.Encoding.UTF8.GetBytes(source);
        fixed (byte* sourceBytesPtr = sourceBytes)
        {
            utils.Get()->CreateBlob(sourceBytesPtr, (uint)sourceBytes.Length, DXC_CP_UTF8, sourceBlob.GetAddressOf());
        }

        // Prepare compilation arguments
        var argsArray = new string[]
        {
            "-T", profile,           // Target profile (vs_6_6, ps_6_6)
            "-E", entryPoint,        // Entry point
            "-HV", "2021",           // HLSL version 2021 (required for SM 6.6)
            "-enable-16bit-types",   // Enable 16-bit types
            "-O3",                   // Optimization level
            "-Qstrip_debug",         // Strip debug info for release
            "-Qstrip_reflect"        // Strip reflection info for release
        };

        // Convert to wide strings (DXC expects LPCWSTR)
        var wideArgs = new nuint[argsArray.Length];
        var argPointers = new IntPtr[argsArray.Length];

        for (var i = 0; i < argsArray.Length; i++)
        {
            argPointers[i] = Marshal.StringToHGlobalUni(argsArray[i]);
            wideArgs[i] = (nuint)argPointers[i];
        }

        try
        {
            // Compile shader
            using ComPtr<IDxcResult> result = default;
            fixed (nuint* argsPtr = wideArgs)
            {
                var buffer = new DxcBuffer
                {
                    Ptr = sourceBlob.Get()->GetBufferPointer(),
                    Size = sourceBlob.Get()->GetBufferSize(),
                    Encoding = DXC_CP_UTF8
                };

                compiler.Get()->Compile(&buffer, (char**)argsPtr, (uint)argsArray.Length, null, __uuidof<IDxcResult>(), result.GetVoidAddressOf());
            }

            // Check compilation result
            HResult hrStatus;
            result.Get()->GetStatus(&hrStatus);
            if (hrStatus.Failure)
            {
                // Get error messages
                using ComPtr<IDxcBlobEncoding> errorBlob = default;
                result.Get()->GetErrorBuffer(errorBlob.GetAddressOf());

                if (errorBlob.Get() != null)
                {
                    var errorMessage = Marshal.PtrToStringUni((IntPtr)errorBlob.Get()->GetBufferPointer());
                    throw new Exception($"DXC shader compilation failed: {errorMessage}");
                }
                else
                {
                    throw new Exception("DXC shader compilation failed with unknown error");
                }
            }

            // Get compiled bytecode
            using ComPtr<IDxcBlob> bytecodeBlob = default;
            result.Get()->GetResult(bytecodeBlob.GetAddressOf());

            if (bytecodeBlob.Get() == null)
            {
                throw new Exception("DXC compilation succeeded but no bytecode was produced");
            }

            // Copy bytecode to managed array
            var bytecodeSize = (int)bytecodeBlob.Get()->GetBufferSize();
            var bytecode = new byte[bytecodeSize];

            fixed (byte* bytecodePtr = bytecode)
            {
                Buffer.MemoryCopy(bytecodeBlob.Get()->GetBufferPointer(), bytecodePtr, bytecodeSize, bytecodeSize);
            }

            return bytecode;
        }
        finally
        {
            // Free allocated wide strings
            for (var i = 0; i < argPointers.Length; i++)
            {
                Marshal.FreeHGlobal(argPointers[i]);
            }
        }
    }

    private void CreateModernBindlessRootSignature()
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

        // Modern approach: Only CBV + Sampler, no descriptor tables needed
        // The CBV_SRV_UAV_HEAP_DIRECTLY_INDEXED flag allows direct heap access
        var rootParameters = new RootParameter1[2];

        // CBV for constant buffer
        rootParameters[0] = new RootParameter1
        {
            ParameterType = RootParameterType.Cbv,
            ShaderVisibility = ShaderVisibility.All,
            Descriptor = new RootDescriptor1(0, 0), // b0
        };

        // Sampler descriptor table (still needed for samplers)
        var samplerRanges = new DescriptorRange1[1];
        samplerRanges[0] = new DescriptorRange1
        {
            RangeType = DescriptorRangeType.Sampler,
            NumDescriptors = 1,
            BaseShaderRegister = 0, // s0
            RegisterSpace = 0,
            Flags = DescriptorRangeFlags.None,
            OffsetInDescriptorsFromTableStart = 0
        };

        fixed (DescriptorRange1* samplerRangesPtr = samplerRanges)
        {
            rootParameters[1] = new RootParameter1
            {
                ParameterType = RootParameterType.DescriptorTable,
                ShaderVisibility = ShaderVisibility.All,
                DescriptorTable = new RootDescriptorTable1(1, samplerRangesPtr)
            };
        }

        // Create root signature with the modern flag
        fixed (RootParameter1* rootParamsPtr = rootParameters)
        {
            var rootSignatureDesc = new RootSignatureDescription1
            {
                NumParameters = (uint)rootParameters.Length,
                pParameters = rootParamsPtr,
                NumStaticSamplers = 0,
                pStaticSamplers = null,
                // Key difference: Use the modern flag for direct heap indexing
                Flags = RootSignatureFlags.AllowInputAssemblerInputLayout |
                       RootSignatureFlags.CbvSrvUavHeapDirectlyIndexed
            };

            var versionedDesc = new VersionedRootSignatureDescription
            {
                Version = RootSignatureVersion.V1_1,
                Desc_1_1 = rootSignatureDesc
            };

            using ComPtr<ID3DBlob> signature = default;
            using ComPtr<ID3DBlob> error = default;

            D3D12SerializeVersionedRootSignature(&versionedDesc, signature.GetAddressOf(), error.GetAddressOf());

            device->CreateRootSignature(0, signature.Get()->GetBufferPointer(), signature.Get()->GetBufferSize(), __uuidof<ID3D12RootSignature>(), _rootSignature.GetVoidAddressOf());
        }
    }

    private void CreatePipelineState(byte[] vsCode, byte[] psCode)
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

        fixed (byte* vsPtr = vsCode)
        fixed (byte* psPtr = psCode)
        {

            var psoDesc = new GraphicsPipelineStateDescription
            {
                pRootSignature = _rootSignature.Get(),
                VS = new ShaderBytecode(vsPtr, (nuint)vsCode.Length),
                PS = new ShaderBytecode(psPtr, (nuint)psCode.Length),
                InputLayout = D3D12PipelineResource.InputLayoutDescription,
                RasterizerState = RasterizerDescription.CullNone,
                BlendState = BlendDescription.Opaque,
                DepthStencilState = DepthStencilDescription.Default,
                SampleMask = uint.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                NumRenderTargets = 1,
                SampleDesc = new SampleDescription(1, 0),
                DSVFormat = Format.Unknown,
            };

            // Fix: Use the correct swap chain back buffer format
            psoDesc.RTVFormats[0] = D3D12PipelineResource.SWAP_CHAIN_BACK_BUFFER_FORMAT;

            device->CreateGraphicsPipelineState(&psoDesc, __uuidof<ID3D12PipelineState>(), _pipelineState.GetVoidAddressOf());
        }
    }

    private void CreateConstantBuffer()
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

        // Create constant buffer
        var cbSize = (uint)((sizeof(ConstantBufferData) + 255) & ~255); // Align to 256 bytes
        var heapProps = new HeapProperties(HeapType.Upload);
        var bufferDesc = ResourceDescription.Buffer(cbSize);

        device->CreateCommittedResource(
            &heapProps,
            HeapFlags.None,
            &bufferDesc,
            ResourceStates.GenericRead,
            null,
            __uuidof<ID3D12Resource>(),
            _constantBuffer.GetVoidAddressOf()
        );

        // Map constant buffer
        fixed (void** mappedDataPtr = &_constantBufferMappedData)
        {
            _constantBuffer.Get()->Map(0, null, mappedDataPtr);
        }
    }

    private void UpdateConstantBuffer()
    {
        var cbData = new ConstantBufferData
        {
            Color = new(1.0f, 1.0f, 1.0f, 1.0f),
            TextureIndex1 = 0,
            TextureIndex2 = 1,
            TextureIndex3 = 2,
            TextureIndex4 = 3
        };

        *(ConstantBufferData*)_constantBufferMappedData = cbData;
    }

    private void CreateBindlessTextures()
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;
        var textureCount = _textureFiles.Length;

        // Initialize arrays
        _textureResources = new ComPtr<ID3D12Resource>[textureCount];
        _uploadBuffers = new ComPtr<ID3D12Resource>[textureCount];
        _textureWidths = new uint[textureCount];
        _textureHeights = new uint[textureCount];
        _texturePitches = new uint[textureCount];

        // Create bindless descriptor heap
        var bindlessHeapDesc = new DescriptorHeapDescription
        {
            Type = DescriptorHeapType.CbvSrvUav,
            NumDescriptors = 1000,
            Flags = DescriptorHeapFlags.ShaderVisible
        };

        device->CreateDescriptorHeap(&bindlessHeapDesc, __uuidof<ID3D12DescriptorHeap>(), _bindlessHeap.GetVoidAddressOf());

        // Create sampler heap
        var samplerHeapDesc = new DescriptorHeapDescription
        {
            Type = DescriptorHeapType.Sampler,
            NumDescriptors = 1,
            Flags = DescriptorHeapFlags.ShaderVisible
        };

        device->CreateDescriptorHeap(&samplerHeapDesc, __uuidof<ID3D12DescriptorHeap>(), _samplerHeap.GetVoidAddressOf());

        // Get descriptor handles
        _bindlessHeapStart = _bindlessHeap.Get()->GetCPUDescriptorHandleForHeapStart();
        _bindlessHeapGpuStart = _bindlessHeap.Get()->GetGPUDescriptorHandleForHeapStart();
        _descriptorSize = device->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);

        // Create sampler
        var samplerDesc = new SamplerDescription
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            MipLODBias = 0,
            MaxAnisotropy = 1,
            MinLOD = 0,
            MaxLOD = float.MaxValue
        };

        // Set border color manually
        samplerDesc.BorderColor[0] = 0;
        samplerDesc.BorderColor[1] = 0;
        samplerDesc.BorderColor[2] = 0;
        samplerDesc.BorderColor[3] = 0;

        var samplerHandle = _samplerHeap.Get()->GetCPUDescriptorHandleForHeapStart();
        device->CreateSampler(&samplerDesc, samplerHandle);

        // Load and create textures
        for (var i = 0; i < textureCount; i++)
        {
            CreateTextureResource(i);
        }
    }

    private void CreateTextureResource(int index)
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;

        // Load image data
        using var stream = new FileStream(_textureFiles[index], FileMode.Open, FileAccess.Read);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        var width = (uint)image.Width;
        var height = (uint)image.Height;
        uint bytesPerPixel = 4;
        var pitch = width * bytesPerPixel;

        // Store texture dimensions
        _textureWidths[index] = width;
        _textureHeights[index] = height;
        _texturePitches[index] = (pitch + 255) & ~255u;

        // Create texture resource
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
            _textureResources[index].GetVoidAddressOf()
        );

        // Create upload buffer
        var uploadBufferSize = GetRequiredIntermediateSize(_textureResources[index].Get(), 0, 1);
        var uploadHeapProps = new HeapProperties(HeapType.Upload);
        var uploadBufferDesc = ResourceDescription.Buffer(uploadBufferSize);

        device->CreateCommittedResource(
            &uploadHeapProps,
            HeapFlags.None,
            &uploadBufferDesc,
            ResourceStates.GenericRead,
            null,
            __uuidof<ID3D12Resource>(),
            _uploadBuffers[index].GetVoidAddressOf()
        );

        // Map and copy texture data
        void* mappedData = null;
        _uploadBuffers[index].Get()->Map(0, null, &mappedData);

        var srcData = image.Data.AsSpan();
        var dstSpan = new Span<byte>(mappedData, (int)uploadBufferSize);

        // Copy row by row with proper alignment
        for (var y = 0; y < height; y++)
        {
            var srcRow = srcData.Slice(y * (int)pitch, (int)pitch);
            var dstRow = dstSpan.Slice(y * (int)_texturePitches[index], (int)pitch);
            srcRow.CopyTo(dstRow);
        }

        _uploadBuffers[index].Get()->Unmap(0, null);

        // Create SRV in the bindless heap
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

        // Calculate SRV handle for this texture
        var srvHandle = new CpuDescriptorHandle
        {
            ptr = _bindlessHeapStart.ptr + (nuint)(index * _descriptorSize)
        };

        device->CreateShaderResourceView(_textureResources[index].Get(), &srvDesc, srvHandle);
    }

    private void UploadTextureData(CommandList cmd)
    {
        var commandList = cmd.NativeCommandList.Ptr;

        for (var i = 0; i < _textureResources.Length; i++)
        {
            // Copy from upload buffer to texture
            var srcLocation = new TextureCopyLocation(_uploadBuffers[i].Get(), new PlacedSubresourceFootprint
            {
                Offset = 0,
                Footprint = new SubresourceFootprint
                {
                    Format = Format.R8G8B8A8Unorm,
                    Width = _textureWidths[i],
                    Height = _textureHeights[i],
                    Depth = 1,
                    RowPitch = _texturePitches[i]
                }
            });

            var dstLocation = new TextureCopyLocation(_textureResources[i].Get(), 0);

            commandList->CopyTextureRegion(&dstLocation, 0, 0, 0, &srcLocation, null);

            // Transition texture to shader resource
            var barrier = new ResourceBarrier
            {
                Type = ResourceBarrierType.Transition,
                Flags = ResourceBarrierFlags.None,
                Transition = new ResourceTransitionBarrier
                {
                    pResource = _textureResources[i].Get(),
                    StateBefore = ResourceStates.CopyDest,
                    StateAfter = ResourceStates.PixelShaderResource,
                    Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES
                }
            };
            commandList->ResourceBarrier(1, &barrier);
        }

        _texturesUploaded = true;
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
        commandList->SetGraphicsRootSignature(_rootSignature.Get());
        commandList->SetPipelineState(_pipelineState.Get());

        // Set descriptor heaps - this is crucial for modern bindless
        var heaps = stackalloc ID3D12DescriptorHeap*[2];
        heaps[0] = _bindlessHeap.Get();  // CBV_SRV_UAV heap for direct indexing
        heaps[1] = _samplerHeap.Get();   // Sampler heap
        commandList->SetDescriptorHeaps(2, heaps);

        // Bind constant buffer
        commandList->SetGraphicsRootConstantBufferView(0, _constantBuffer.Get()->GetGPUVirtualAddress());

        // Bind sampler descriptor table
        var samplerGpuHandle = _samplerHeap.Get()->GetGPUDescriptorHandleForHeapStart();
        commandList->SetGraphicsRootDescriptorTable(1, samplerGpuHandle);

        // No need to explicitly bind texture descriptor table!
        // The textures are accessed directly via ResourceDescriptorHeap[index]

        // Draw mesh
        commandList->IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        commandList->IASetVertexBuffers(0, 1, _mesh!.VertexBufferView);
        commandList->IASetIndexBuffer(_mesh.IndexBufferView);
        commandList->DrawIndexedInstanced(_mesh.IndexCount, 1, 0, 0, 0);
    }

    public void Dispose()
    {
        _mesh?.Dispose();

        // Unmap constant buffer
        if (_constantBuffer.Get() != null)
        {
            _constantBuffer.Get()->Unmap(0, null);
        }

        _pipelineState.Dispose();
        _rootSignature.Dispose();
        _constantBuffer.Dispose();

        // Dispose texture resources
        if (_textureResources != null)
        {
            for (var i = 0; i < _textureResources.Length; i++)
            {
                _textureResources[i].Dispose();
            }
        }

        // Dispose upload buffers
        if (_uploadBuffers != null)
        {
            for (var i = 0; i < _uploadBuffers.Length; i++)
            {
                _uploadBuffers[i].Dispose();
            }
        }

        _bindlessHeap.Dispose();
        _samplerHeap.Dispose();
    }
}