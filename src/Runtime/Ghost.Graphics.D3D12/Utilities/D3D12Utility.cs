using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.DXGI_Alias;

namespace Ghost.Graphics.D3D12.Utilities;

internal static unsafe class D3D12Utility
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly ref struct IID_PPV
    {
        public readonly Guid* iid;
        public readonly void** ppv;

        public IID_PPV(Guid* iid, void** ppv)
        {
            this.iid = iid;
            this.ppv = ppv;
        }

        public void Deconstruct(out Guid* iid, out void** ppv)
        {
            iid = this.iid;
            ppv = this.ppv;
        }
    }

    public static Guid* IID_NULL
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IID.IID_NULL));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IID_PPV IID_PPV_ARGS<T>(ComPtr<T>* comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        return new IID_PPV(__uuidof<T>(), (void**)comPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Attach<T>(ref this UniquePtr<T> uPtr, T* other)
        where T : unmanaged, IUnknown.Interface
    {
        var ptr = uPtr.Get();
        if (ptr != null)
        {
            var refCount = ptr->Release();
            Debug.Assert((refCount != 0) || (ptr != other));
        }

        uPtr = new UniquePtr<T>(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose<T>(ref this UniquePtr<T> uPtr)
        where T : unmanaged, IUnknown.Interface
    {
        var ptr = uPtr.Detach();
        if (ptr != null)
        {
            var refCount = ptr->Release();
            Debug.Assert((refCount != 0));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result ToResult(this HRESULT hr, [CallerArgumentExpression(nameof(hr))] string? op = null)
    {
        if (hr.SUCCEEDED)
        {
            return Result.Success();
        }

        return Result.Failure($"{op} failed with code {hr}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void** ReleaseAndGetVoidAddressOf<T>(ref this ComPtr<T> comPtr)
        where T : unmanaged, IUnknown.Interface
    {
        return (void**)comPtr.ReleaseAndGetAddressOf();
    }

    public static void SetName<T>(ref this T obj, ReadOnlySpan<char> name)
        where T : unmanaged, ID3D12Object.Interface
    {
        if (name.IsEmpty)
        {
            return;
        }

        fixed (char* pName = name)
        {
            obj.SetName(pName);
        }
    }

    public static void SetName(ref this D3D12MA_Allocation obj, ReadOnlySpan<char> name)
    {
        if (name.IsEmpty)
        {
            return;
        }

        fixed (char* pName = name)
        {
            obj.SetName(pName);
        }
    }

    public static TextureDimension ToTextureDimension(this D3D12_RESOURCE_DIMENSION dimension)
    {
        return dimension switch
        {
            D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE1D => TextureDimension.Texture2D,
            D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D => TextureDimension.Texture2D,
            D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE3D => TextureDimension.Texture3D,
            _ => throw new NotSupportedException($"Resource dimension {dimension} is not supported."),
        };
    }

    public static DXGI_FORMAT ToDXGIFormat(this TextureFormat format)
    {
        return format switch
        {
            TextureFormat.Unknown => DXGI_FORMAT_UNKNOWN,
            TextureFormat.R8G8B8A8_UNorm => DXGI_FORMAT_R8G8B8A8_UNORM,
            TextureFormat.B8G8R8A8_UNorm => DXGI_FORMAT_B8G8R8A8_UNORM,
            TextureFormat.R16G16B16A16_Float => DXGI_FORMAT_R16G16B16A16_FLOAT,
            TextureFormat.R32G32B32A32_Float => DXGI_FORMAT_R32G32B32A32_FLOAT,
            TextureFormat.D24_UNorm_S8_UInt => DXGI_FORMAT_D24_UNORM_S8_UINT,
            TextureFormat.D32_Float => DXGI_FORMAT_D32_FLOAT,
            _ => throw new NotSupportedException($"Texture format {format} is not supported."),
        };
    }

    public static TextureFormat ToTextureFormat(this DXGI_FORMAT format)
    {
        return format switch
        {
            DXGI_FORMAT_R8G8B8A8_UNORM => TextureFormat.R8G8B8A8_UNorm,
            DXGI_FORMAT_B8G8R8A8_UNORM => TextureFormat.B8G8R8A8_UNorm,
            DXGI_FORMAT_R16G16B16A16_FLOAT => TextureFormat.R16G16B16A16_Float,
            DXGI_FORMAT_R32G32B32A32_FLOAT => TextureFormat.R32G32B32A32_Float,
            DXGI_FORMAT_D24_UNORM_S8_UINT => TextureFormat.D24_UNorm_S8_UInt,
            DXGI_FORMAT_D32_FLOAT => TextureFormat.D32_Float,
            _ => throw new NotSupportedException($"DXGI format {format} is not supported.")
        };
    }

    public static D3D12_RESOURCE_STATES ToD3D12States(this ResourceState state)
    {
        var d3dStates = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON;

        if (state.HasFlag(ResourceState.VertexAndConstantBuffer))
        {
            d3dStates |= D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER;
        }

        if (state.HasFlag(ResourceState.IndexBuffer))
        {
            d3dStates |= D3D12_RESOURCE_STATE_INDEX_BUFFER;
        }

        if (state.HasFlag(ResourceState.RenderTarget))
        {
            d3dStates |= D3D12_RESOURCE_STATE_RENDER_TARGET;
        }

        if (state.HasFlag(ResourceState.UnorderedAccess))
        {
            d3dStates |= D3D12_RESOURCE_STATE_UNORDERED_ACCESS;
        }

        if (state.HasFlag(ResourceState.DepthWrite))
        {
            d3dStates |= D3D12_RESOURCE_STATE_DEPTH_WRITE;
        }

        if (state.HasFlag(ResourceState.DepthRead))
        {
            d3dStates |= D3D12_RESOURCE_STATE_DEPTH_READ;
        }

        if (state.HasFlag(ResourceState.PixelShaderResource))
        {
            d3dStates |= D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;
        }

        if (state.HasFlag(ResourceState.CopyDest))
        {
            d3dStates |= D3D12_RESOURCE_STATE_COPY_DEST;
        }

        if (state.HasFlag(ResourceState.CopySource))
        {
            d3dStates |= D3D12_RESOURCE_STATE_COPY_SOURCE;
        }

        if (state.HasFlag(ResourceState.GenericRead))
        {
            d3dStates |= D3D12_RESOURCE_STATE_GENERIC_READ;
        }

        if (state.HasFlag(ResourceState.IndirectArgument))
        {
            d3dStates |= D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT;
        }

        if (state.HasFlag(ResourceState.NonPixelShaderResource))
        {
            d3dStates |= D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE;
        }

        return d3dStates;
    }

    public static ResourceState ToResourceState(this D3D12_RESOURCE_STATES states)
    {
        var resourceState = ResourceState.Common;

        if (states.HasFlag(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER))
        {
            resourceState |= ResourceState.VertexAndConstantBuffer;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_INDEX_BUFFER))
        {
            resourceState |= ResourceState.IndexBuffer;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_RENDER_TARGET))
        {
            resourceState |= ResourceState.RenderTarget;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_UNORDERED_ACCESS))
        {
            resourceState |= ResourceState.UnorderedAccess;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_DEPTH_WRITE))
        {
            resourceState |= ResourceState.DepthWrite;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_DEPTH_READ))
        {
            resourceState |= ResourceState.DepthRead;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE))
        {
            resourceState |= ResourceState.PixelShaderResource;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_COPY_DEST))
        {
            resourceState |= ResourceState.CopyDest;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_COPY_SOURCE))
        {
            resourceState |= ResourceState.CopySource;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_GENERIC_READ))
        {
            resourceState |= ResourceState.GenericRead;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT))
        {
            resourceState |= ResourceState.IndirectArgument;
        }

        if (states.HasFlag(D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE))
        {
            resourceState |= ResourceState.NonPixelShaderResource;
        }

        return resourceState;
    }

    public static D3D12_FILTER ToD3D12Filter(this TextureFilterMode filterMode)
    {
        return filterMode switch
        {
            TextureFilterMode.Point => D3D12_FILTER_MIN_MAG_MIP_POINT,
            TextureFilterMode.Bilinear => D3D12_FILTER_MIN_MAG_LINEAR_MIP_POINT,
            TextureFilterMode.Trilinear => D3D12_FILTER_MIN_MAG_MIP_LINEAR,
            TextureFilterMode.Anisotropic => D3D12_FILTER_ANISOTROPIC,
            _ => throw new ArgumentException($"Unknown texture filter mode: {filterMode}")
        };
    }

    public static D3D12_TEXTURE_ADDRESS_MODE ToD3D12TextureAddressMode(this TextureAddressMode addressMode)
    {
        return addressMode switch
        {
            TextureAddressMode.Repeat => D3D12_TEXTURE_ADDRESS_MODE_WRAP,
            TextureAddressMode.Mirror => D3D12_TEXTURE_ADDRESS_MODE_MIRROR,
            TextureAddressMode.Clamp => D3D12_TEXTURE_ADDRESS_MODE_CLAMP,
            TextureAddressMode.Border => D3D12_TEXTURE_ADDRESS_MODE_BORDER,
            TextureAddressMode.MirrorOnce => D3D12_TEXTURE_ADDRESS_MODE_MIRROR_ONCE,
            _ => throw new ArgumentException($"Unknown texture address mode: {addressMode}")
        };
    }

    public static D3D12_COMPARISON_FUNC ToD3D12ComparisonFunc(this ComparisonFunction func)
    {
        return func switch
        {
            ComparisonFunction.Never => D3D12_COMPARISON_FUNC_NEVER,
            ComparisonFunction.Less => D3D12_COMPARISON_FUNC_LESS,
            ComparisonFunction.Equal => D3D12_COMPARISON_FUNC_EQUAL,
            ComparisonFunction.LessEqual => D3D12_COMPARISON_FUNC_LESS_EQUAL,
            ComparisonFunction.Greater => D3D12_COMPARISON_FUNC_GREATER,
            ComparisonFunction.NotEqual => D3D12_COMPARISON_FUNC_NOT_EQUAL,
            ComparisonFunction.GreaterEqual => D3D12_COMPARISON_FUNC_GREATER_EQUAL,
            ComparisonFunction.Always => D3D12_COMPARISON_FUNC_ALWAYS,
            _ => throw new ArgumentException($"Unknown comparison function: {func}")
        };
    }

    public static D3D12_COMPARISON_FUNC ToD3DCompare(this ZTest z)
    {
        return z switch
        {
            ZTest.Disabled => D3D12_COMPARISON_FUNC_NEVER,
            ZTest.Less => D3D12_COMPARISON_FUNC_LESS,
            ZTest.LessEqual => D3D12_COMPARISON_FUNC_LESS_EQUAL,
            ZTest.Equal => D3D12_COMPARISON_FUNC_EQUAL,
            ZTest.GreaterEqual => D3D12_COMPARISON_FUNC_GREATER_EQUAL,
            ZTest.Greater => D3D12_COMPARISON_FUNC_GREATER,
            ZTest.NotEqual => D3D12_COMPARISON_FUNC_NOT_EQUAL,
            ZTest.Always => D3D12_COMPARISON_FUNC_ALWAYS,
            _ => D3D12_COMPARISON_FUNC_LESS_EQUAL
        };
    }

    public static D3D12_COMMAND_LIST_TYPE ToCommandListType(CommandBufferType type)
    {
        return type switch
        {
            CommandBufferType.Graphics => D3D12_COMMAND_LIST_TYPE_DIRECT,
            CommandBufferType.Compute => D3D12_COMMAND_LIST_TYPE_COMPUTE,
            CommandBufferType.Copy => D3D12_COMMAND_LIST_TYPE_COPY,
            _ => throw new ArgumentException($"Unknown command buffer type: {type}")
        };
    }

    public static D3D12_RESOURCE_FLAGS ToD3D12ResourceFlag(this TextureUsage usage)
    {
        var flags = D3D12_RESOURCE_FLAG_NONE;

        if (usage.HasFlag(TextureUsage.RenderTarget))
        {
            flags |= D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
        }

        if (usage.HasFlag(TextureUsage.DepthStencil))
        {
            flags |= D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
        }

        if (usage.HasFlag(TextureUsage.UnorderedAccess))
        {
            flags |= D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
        }

        return flags;
    }

    public static D3D12_RESOURCE_DESC ToD3D12ResourceDesc(this in TextureDesc desc)
    {
        var dxgiFormat = desc.Format.ToDXGIFormat();
        var maxDimension = Math.Max(desc.Width, Math.Max(desc.Height, desc.Slice));
        var mipLevels = desc.MipLevels == 0
            ? (ushort)(1 + Math.Floor(Math.Log2(maxDimension)))
            : (ushort)desc.MipLevels;

        var resourceFlags = desc.Usage.ToD3D12ResourceFlag();
        return desc.Dimension switch
        {
            TextureDimension.Texture2D => D3D12_RESOURCE_DESC.Tex2D(
                                dxgiFormat,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                flags: resourceFlags),
            TextureDimension.Texture3D => D3D12_RESOURCE_DESC.Tex3D(
                                dxgiFormat,
                                desc.Width,
                                desc.Height,
                                (ushort)desc.Slice,
                                flags: resourceFlags),
            TextureDimension.TextureCube => D3D12_RESOURCE_DESC.Tex2D(
                                dxgiFormat,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                arraySize: 6,
                                flags: resourceFlags),
            TextureDimension.Texture2DArray => D3D12_RESOURCE_DESC.Tex2D(
                                dxgiFormat,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                arraySize: (ushort)desc.Slice,
                                flags: resourceFlags),
            TextureDimension.TextureCubeArray => D3D12_RESOURCE_DESC.Tex2D(
                                dxgiFormat,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                arraySize: (ushort)(desc.Slice * 6),
                                flags: resourceFlags),
            _ => throw new ArgumentException($"Unsupported texture dimension: {desc.Dimension}"),
        };
    }

    public static D3D12_RESOURCE_FLAGS ToD3D12ResourceFlag(this BufferUsage usage)
    {
        var flags = D3D12_RESOURCE_FLAG_NONE;

        if (usage.HasFlag(BufferUsage.Raw) || usage.HasFlag(BufferUsage.UnorderedAccess))
        {
            flags |= D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
        }

        return flags;
    }

    public static D3D12_RESOURCE_DESC ToD3D12ResourceDesc(this in BufferDesc desc)
    {
        var alignedSize = desc.Size;
        if (desc.Usage.HasFlag(BufferUsage.Constant))
        {
            // D3D12 CBV size must be 256-byte aligned
            alignedSize = (uint)(desc.Size + 255) & ~255u;
        }

        var resourceFlags = desc.Usage.ToD3D12ResourceFlag();
        return D3D12_RESOURCE_DESC.Buffer(alignedSize, resourceFlags);
    }


    public static ResourceDesc ToResourceDesc(this D3D12_RESOURCE_DESC desc)
    {
        if (desc.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER)
        {
            return ResourceDesc.Buffer(new BufferDesc
            {
                Size = (uint)desc.Width,
                Stride = 0,
                Usage = BufferUsage.None,
                MemoryType = ResourceMemoryType.Default
            });
        }
        else
        {
            return ResourceDesc.Texture(new TextureDesc
            {
                Width = (uint)desc.Width,
                Height = desc.Height,
                Slice = desc.DepthOrArraySize,
                Format = desc.Format.ToTextureFormat(),
                Dimension = desc.Dimension.ToTextureDimension(),
                MipLevels = desc.MipLevels,
                Usage = TextureUsage.None,
            });
        }
    }


    public static D3D12_RASTERIZER_DESC D3D12_RASTERIZER_DESC_CREATE(
        D3D12_FILL_MODE fillMode,
        D3D12_CULL_MODE cullMode,
        bool frontCounterClockwise = false,
        int depthBias = D3D12_DEFAULT_DEPTH_BIAS,
        float depthBiasClamp = D3D12_DEFAULT_DEPTH_BIAS_CLAMP,
        float slopeScaledDepthBias = D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS,
        bool depthClipEnable = true,
        bool multisampleEnable = true,
        bool antialiasedLineEnable = false,
        uint forcedSampleCount = 0,
        D3D12_CONSERVATIVE_RASTERIZATION_MODE conservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF)
    {
        return new D3D12_RASTERIZER_DESC
        {
            FillMode = fillMode,
            CullMode = cullMode,
            FrontCounterClockwise = frontCounterClockwise ? TRUE : FALSE,
            DepthBias = depthBias,
            DepthBiasClamp = depthBiasClamp,
            SlopeScaledDepthBias = slopeScaledDepthBias,
            DepthClipEnable = depthClipEnable ? TRUE : FALSE,
            MultisampleEnable = multisampleEnable ? TRUE : FALSE,
            AntialiasedLineEnable = antialiasedLineEnable ? TRUE : FALSE,
            ForcedSampleCount = forcedSampleCount,
            ConservativeRaster = conservativeRaster
        };
    }

    public static D3D12_RASTERIZER_DESC D3D12_RASTERIZER_DESC_CULL_NONE => D3D12_RASTERIZER_DESC_CREATE(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_NONE);
    public static D3D12_RASTERIZER_DESC D3D12_RASTERIZER_DESC_CULL_CLOCKWISE => D3D12_RASTERIZER_DESC_CREATE(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_FRONT);
    public static D3D12_RASTERIZER_DESC D3D12_RASTERIZER_DESC_CULL_COUNTER_CLOCKWISE => D3D12_RASTERIZER_DESC_CREATE(D3D12_FILL_MODE_SOLID, D3D12_CULL_MODE_BACK);
    public static D3D12_RASTERIZER_DESC D3D12_RASTERIZER_DESC_WIREFRAME => D3D12_RASTERIZER_DESC_CREATE(D3D12_FILL_MODE_WIREFRAME, D3D12_CULL_MODE_NONE);


    public static D3D12_BLEND_DESC D3D12_BLEND_DESC_CREATE(D3D12_BLEND srcBlend, D3D12_BLEND destBlend)
    {
        var blendDesc = new D3D12_BLEND_DESC
        {
            AlphaToCoverageEnable = false,
            IndependentBlendEnable = false
        };

        for (var i = 0; i < D3D12_SIMULTANEOUS_RENDER_TARGET_COUNT; i++)
        {
            blendDesc.RenderTarget[i].BlendEnable = srcBlend != D3D12_BLEND_ONE || destBlend != D3D12_BLEND_ZERO;
            blendDesc.RenderTarget[i].LogicOp = D3D12_LOGIC_OP_NOOP;
            blendDesc.RenderTarget[i].SrcBlend = srcBlend;
            blendDesc.RenderTarget[i].DestBlend = destBlend;
            blendDesc.RenderTarget[i].BlendOp = D3D12_BLEND_OP_ADD;
            blendDesc.RenderTarget[i].SrcBlendAlpha = srcBlend;
            blendDesc.RenderTarget[i].DestBlendAlpha = destBlend;
            blendDesc.RenderTarget[i].BlendOpAlpha = D3D12_BLEND_OP_ADD;
            blendDesc.RenderTarget[i].RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE_ALL;
        }

        return blendDesc;
    }

    public static D3D12_BLEND_DESC D3D12_BLEND_DESC_OPAQUE => D3D12_BLEND_DESC_CREATE(D3D12_BLEND_ONE, D3D12_BLEND_ZERO);
    public static D3D12_BLEND_DESC D3D12_BLEND_DESC_ALPHA_BLEND => D3D12_BLEND_DESC_CREATE(D3D12_BLEND_SRC_ALPHA, D3D12_BLEND_INV_SRC_ALPHA);
    public static D3D12_BLEND_DESC D3D12_BLEND_DESC_ADDITIVE => D3D12_BLEND_DESC_CREATE(D3D12_BLEND_SRC_ALPHA, D3D12_BLEND_ONE);
    public static D3D12_BLEND_DESC D3D12_BLEND_DESC_MULTIPLY => D3D12_BLEND_DESC_CREATE(D3D12_BLEND_DEST_COLOR, D3D12_BLEND_ZERO);
    public static D3D12_BLEND_DESC D3D12_BLEND_DESC_PREMULTIPLIED => D3D12_BLEND_DESC_CREATE(D3D12_BLEND_ONE, D3D12_BLEND_INV_SRC_ALPHA);
    public static D3D12_BLEND_DESC D3D12_BLEND_DESC_NON_PREMULTIPLIED => D3D12_BLEND_DESC_CREATE(D3D12_BLEND_SRC_ALPHA, D3D12_BLEND_INV_SRC_ALPHA);


    public static D3D12_DEPTH_STENCIL_DESC D3D12_DEPTH_STENCIL_DESC_CREATE(
        bool depthEnable,
        bool depthWriteEnable,
        D3D12_COMPARISON_FUNC depthFunc,
        bool stencilEnable = false,
        byte stencilReadMask = D3D12_DEFAULT_STENCIL_READ_MASK,
        byte stencilWriteMask = D3D12_DEFAULT_STENCIL_WRITE_MASK,
        D3D12_STENCIL_OP frontStencilFailOp = D3D12_STENCIL_OP_KEEP,
        D3D12_STENCIL_OP frontStencilDepthFailOp = D3D12_STENCIL_OP_KEEP,
        D3D12_STENCIL_OP frontStencilPassOp = D3D12_STENCIL_OP_KEEP,
        D3D12_COMPARISON_FUNC frontStencilFunc = D3D12_COMPARISON_FUNC_ALWAYS,
        D3D12_STENCIL_OP backStencilFailOp = D3D12_STENCIL_OP_KEEP,
        D3D12_STENCIL_OP backStencilDepthFailOp = D3D12_STENCIL_OP_KEEP,
        D3D12_STENCIL_OP backStencilPassOp = D3D12_STENCIL_OP_KEEP,
        D3D12_COMPARISON_FUNC backStencilFunc = D3D12_COMPARISON_FUNC_ALWAYS)
    {
        return new D3D12_DEPTH_STENCIL_DESC
        {
            DepthEnable = depthEnable,
            DepthWriteMask = depthWriteEnable ? D3D12_DEPTH_WRITE_MASK_ALL : D3D12_DEPTH_WRITE_MASK_ZERO,
            DepthFunc = depthFunc,
            StencilEnable = stencilEnable,
            StencilReadMask = stencilReadMask,
            StencilWriteMask = stencilWriteMask,
            FrontFace = D3D12_DEPTH_STENCILOP_DESC_CREATE(frontStencilFailOp, frontStencilDepthFailOp, frontStencilPassOp, frontStencilFunc),
            BackFace = D3D12_DEPTH_STENCILOP_DESC_CREATE(backStencilFailOp, backStencilDepthFailOp, backStencilPassOp, backStencilFunc)
        };
    }

    public static D3D12_DEPTH_STENCIL_DESC D3D12_DEPTH_STENCIL_DESC_NONE => D3D12_DEPTH_STENCIL_DESC_CREATE(false, false, D3D12_COMPARISON_FUNC_LESS_EQUAL);
    public static D3D12_DEPTH_STENCIL_DESC D3D12_DEPTH_STENCIL_DESC_READ => D3D12_DEPTH_STENCIL_DESC_CREATE(true, false, D3D12_COMPARISON_FUNC_LESS_EQUAL);
    public static D3D12_DEPTH_STENCIL_DESC D3D12_DEPTH_STENCIL_DESC_REVERSE_Z => D3D12_DEPTH_STENCIL_DESC_CREATE(true, true, D3D12_COMPARISON_FUNC_GREATER_EQUAL);
    public static D3D12_DEPTH_STENCIL_DESC D3D12_DEPTH_STENCIL_DESC_READ_REVERSE_Z => D3D12_DEPTH_STENCIL_DESC_CREATE(true, false, D3D12_COMPARISON_FUNC_GREATER_EQUAL);


    public static D3D12_DEPTH_STENCILOP_DESC D3D12_DEPTH_STENCILOP_DESC_CREATE(
        D3D12_STENCIL_OP stencilFailOp,
        D3D12_STENCIL_OP stencilDepthFailOp,
        D3D12_STENCIL_OP stencilPassOp,
        D3D12_COMPARISON_FUNC stencilFunc)
    {
        return new D3D12_DEPTH_STENCILOP_DESC
        {
            StencilFailOp = stencilFailOp,
            StencilDepthFailOp = stencilDepthFailOp,
            StencilPassOp = stencilPassOp,
            StencilFunc = stencilFunc
        };
    }

    public static D3D12_DEPTH_STENCILOP_DESC D3D12_DEPTH_STENCILOP_DESC_DEFAULT => D3D12_DEPTH_STENCILOP_DESC_CREATE(D3D12_STENCIL_OP_KEEP, D3D12_STENCIL_OP_KEEP, D3D12_STENCIL_OP_KEEP, D3D12_COMPARISON_FUNC_ALWAYS);
}
