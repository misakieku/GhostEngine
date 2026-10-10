using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RHI;

public static class RHIUtility
{
    public const int MAX_RENDER_TARGETS = 8;
    public const ulong SHADER_ID_MASK = 0xFFFFFFFFFFFFFFF0ul;
    public const ulong PIPELINE_KEY_MASK = 0xFFFFFFFFFFFFFFF0ul;
    public const ulong GRAPHICS_PIPELINE_KEY_FLAG = 0x1ul;
    public const ulong COMPUTE_PIPELINE_KEY_FLAG = 0x2ul;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetBytesPerPixel(this TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8_UNorm => 1,
            TextureFormat.R8_SNorm => 1,
            TextureFormat.R16_UNorm => 2,
            TextureFormat.R16_SNorm => 2,
            TextureFormat.R16_Float => 2,
            TextureFormat.R32_UInt => 4,
            TextureFormat.R32_SInt => 4,

            TextureFormat.R8G8_UNorm => 2,
            TextureFormat.R8G8_SNorm => 2,
            TextureFormat.R16G16_UNorm => 4,
            TextureFormat.R16G16_SNorm => 4,
            TextureFormat.R16G16_Float => 4,
            TextureFormat.R32G32_Float => 8,

            TextureFormat.R8G8B8A8_SRGB => 4,
            TextureFormat.R8G8B8A8_UNorm => 4,
            TextureFormat.R8G8B8A8_SNorm => 4,
            TextureFormat.B8G8R8A8_UNorm => 4,
            TextureFormat.R11G11B10_Float => 4,

            TextureFormat.R10G10B10A2_UNorm => 4,
            TextureFormat.R16G16B16A16_Float => 8,
            TextureFormat.R32G32B32A32_Float => 16,

            TextureFormat.D24_UNorm_S8_UInt => 4,
            TextureFormat.D32_Float => 4,

            TextureFormat.R32_Float => 4,
            TextureFormat.R32G32_UInt => 8,

            TextureFormat.R8G8B8A8_Typeless => 4,
            TextureFormat.R16G16B16A16_Typeless => 8,
            TextureFormat.R32G32B32A32_Typeless => 16,
            TextureFormat.R32_Typeless => 4,
            TextureFormat.R24G8_Typeless => 4,
            _ => throw new NotSupportedException($"Texture format {format} is not supported."),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDepthStencilFormat(this TextureFormat format)
    {
        return format == TextureFormat.D24_UNorm_S8_UInt || format == TextureFormat.D32_Float;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStencilFormat(this TextureFormat format)
    {
        return format == TextureFormat.D24_UNorm_S8_UInt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompressedFormat(this TextureFormat format)
    {
        return format is >= TextureFormat.BC1_UNorm and <= TextureFormat.BC7_UNorm_SRGB;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTypelessFormat(this TextureFormat format)
    {
        return format is >= TextureFormat.R8G8B8A8_Typeless and <= TextureFormat.R24G8_Typeless;
    }

    public static void GetSurfaceInfo(this TextureFormat format, uint width, uint height, out uint rowPitch, out uint slicePitch, out uint rowCount)
    {
        var bc = false;
        var packed = false;
        var planar = false;
        var bpe = 0u;

        switch (format)
        {
            case TextureFormat.BC1_UNorm:
            case TextureFormat.BC1_UNorm_SRGB:
            case TextureFormat.BC4_UNorm:
            case TextureFormat.BC4_SNorm:
                bc = true;
                bpe = 8;
                break;

            case TextureFormat.BC2_UNorm:
            case TextureFormat.BC2_UNorm_SRGB:
            case TextureFormat.BC3_UNorm:
            case TextureFormat.BC3_UNorm_SRGB:
            case TextureFormat.BC5_UNorm:
            case TextureFormat.BC5_SNorm:
            case TextureFormat.BC6H_UF16:
            case TextureFormat.BC6H_SF16:
            case TextureFormat.BC7_UNorm:
            case TextureFormat.BC7_UNorm_SRGB:
                bc = true;
                bpe = 16;
                break;

            default:
                break;
        }

        if (bc)
        {
            var numBlocksWide = 0u;
            if (width > 0)
            {
                numBlocksWide = Math.Max(1u, (width + 3) / 4u);
            }

            var numBlocksHigh = 0u;
            if (height > 0)
            {
                numBlocksHigh = Math.Max(1u, (height + 3) / 4u);
            }

            rowPitch = numBlocksWide * bpe;
            rowCount = numBlocksHigh;
            slicePitch = rowPitch * numBlocksHigh;
        }
        else if (packed)
        {
            rowPitch = ((width + 1u) >> 1) * bpe;
            rowCount = height;
            slicePitch = rowPitch * height;
        }
        else if (planar)
        {
            rowPitch = ((width + 1u) >> 1) * bpe;
            slicePitch = (rowPitch * height) + ((rowPitch * height + 1) >> 1);
            rowCount = height + ((height + 1u) >> 1);
        }
        else
        {
            var bpp = GetBytesPerPixel(format) * 8;
            rowPitch = (width * bpp + 7) / 8; // round up to nearest byte
            rowCount = height;
            slicePitch = rowPitch * height;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetShaderID(string shaderName)
    {
        var hash = XxHash64.HashToUInt64(MemoryMarshal.AsBytes(shaderName.AsSpan()));
        return hash & SHADER_ID_MASK;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetPassID(ulong shaderID, int passIndex)
    {
        Logger.DebugAssert(passIndex >= 0 && passIndex < 16, "Pass index must be between 0 and 15 to fit within the shader ID mask.");
        return shaderID | ((ulong)passIndex & 0xFul);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key64<ShaderPass> CreateShaderPassKey(ulong passID, ulong compiledHash)
    {
        return Hash.Combine64(passID, compiledHash);
    }

    public static unsafe Key128<PipelineState> CreateGraphicsPipelineKey(ulong passId, ulong compiledHash, PipelineState pipelineState, PassAttachmentHash passAttachmentHash)
    {
        // Order-sensitive 128-bit mix. Cheap and stable, avoids span hashing.
        static ulong Mix64(ulong x)
        {
            x ^= x >> 30;
            x *= 0xBF58476D1CE4E5B9ul;
            x ^= x >> 27;
            x *= 0x94D049BB133111EBul;
            x ^= x >> 31;
            return x;
        }

        var mLo = compiledHash;
        var mHi = pipelineState.GetHashCode64();

        var pPasskey = (ulong*)&passAttachmentHash.value;
        var pLo = pPasskey[0];
        var pHi = pPasskey[1];

        // Distinct constants + cross-feeding to reduce structural collisions.
        var hi = Mix64(mHi ^ (pHi + 0xC2B2AE3D27D4EB4Ful) ^ (pLo * 0x165667B19E3779F9ul) ^ passId);
        var lo = Mix64(mLo ^ (pLo + 0x9E3779B97F4A7C15ul) ^ (mHi * 0xD6E8FEB86659FD93ul) ^ (passId * 0xA24BAED4963EE407ul));

        lo = lo & PIPELINE_KEY_MASK | GRAPHICS_PIPELINE_KEY_FLAG; // Ensure graphics pipeline keys are distinguishable from compute pipeline keys.

        return new Key128<PipelineState>(new UInt128(hi, lo));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Key128<PipelineState> CreateComputePipelineKey(ulong passId, ulong compiledHash)
    {
        // Include the stable entry-point identity so two entries with identical bytecode cannot alias.
        var hi = compiledHash ^ (passId + 0x9E3779B97F4A7C15ul);
        var lo = compiledHash ^ (passId * 0xD6E8FEB86659FD93ul);
        lo = lo & PIPELINE_KEY_MASK | COMPUTE_PIPELINE_KEY_FLAG; // Ensure compute pipeline keys are distinguishable from graphics pipeline keys.
        return new Key128<PipelineState>(new UInt128(hi, lo));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetStringFromHash(UInt128 key, Span<char> destination)
    {
        return key.TryFormat(destination, out var _, "X16");
    }

    public static TextureFormat FromDxgiFormat(uint format)
    {
        return format switch
        {
            0 => TextureFormat.Unknown, // DXGI_FORMAT_UNKNOWN

            61 => TextureFormat.R8_UNorm, // DXGI_FORMAT_R8_UNORM
            63 => TextureFormat.R8_SNorm, // DXGI_FORMAT_R8_SNORM
            56 => TextureFormat.R16_UNorm, // DXGI_FORMAT_R16_UNORM
            58 => TextureFormat.R16_SNorm, // DXGI_FORMAT_R16_SNORM
            54 => TextureFormat.R16_Float, // DXGI_FORMAT_R16_FLOAT
            41 => TextureFormat.R32_Float, // DXGI_FORMAT_R32_FLOAT
            42 => TextureFormat.R32_UInt, // DXGI_FORMAT_R32_UINT
            43 => TextureFormat.R32_SInt, // DXGI_FORMAT_R32_SINT

            49 => TextureFormat.R8G8_UNorm, // DXGI_FORMAT_R8G8_UNORM
            51 => TextureFormat.R8G8_SNorm, // DXGI_FORMAT_R8G8_SNORM
            35 => TextureFormat.R16G16_UNorm, // DXGI_FORMAT_R16G16_UNORM
            37 => TextureFormat.R16G16_SNorm, // DXGI_FORMAT_R16G16_SNORM
            34 => TextureFormat.R16G16_Float, // DXGI_FORMAT_R16G16_FLOAT
            16 => TextureFormat.R32G32_Float, // DXGI_FORMAT_R32G32_FLOAT
            17 => TextureFormat.R32G32_UInt, // DXGI_FORMAT_R32G32_UINT

            28 => TextureFormat.R8G8B8A8_UNorm, // DXGI_FORMAT_R8G8B8A8_UNORM
            29 => TextureFormat.R8G8B8A8_SRGB, // DXGI_FORMAT_R8G8B8A8_UNORM_SRGB
            31 => TextureFormat.R8G8B8A8_SNorm, // DXGI_FORMAT_R8G8B8A8_SNORM
            87 => TextureFormat.B8G8R8A8_UNorm, // DXGI_FORMAT_B8G8R8A8_UNORM
            26 => TextureFormat.R11G11B10_Float, // DXGI_FORMAT_R11G11B10_FLOAT

            24 => TextureFormat.R10G10B10A2_UNorm, // DXGI_FORMAT_R10G10B10A2_UNORM

            10 => TextureFormat.R16G16B16A16_Float, // DXGI_FORMAT_R16G16B16A16_FLOAT
            2 => TextureFormat.R32G32B32A32_Float, // DXGI_FORMAT_R32G32B32A32_FLOAT

            45 => TextureFormat.D24_UNorm_S8_UInt, // DXGI_FORMAT_D24_UNORM_S8_UINT
            40 => TextureFormat.D32_Float, // DXGI_FORMAT_D32_FLOAT

            27 => TextureFormat.R8G8B8A8_Typeless, // DXGI_FORMAT_R8G8B8A8_TYPELESS
            9 => TextureFormat.R16G16B16A16_Typeless, // DXGI_FORMAT_R16G16B16A16_TYPELESS
            1 => TextureFormat.R32G32B32A32_Typeless, // DXGI_FORMAT_R32G32B32A32_TYPELESS
            39 => TextureFormat.R32_Typeless, // DXGI_FORMAT_R32_TYPELESS
            44 => TextureFormat.R24G8_Typeless, // DXGI_FORMAT_R24G8_TYPELESS

            71 => TextureFormat.BC1_UNorm, // DXGI_FORMAT_BC1_UNORM
            72 => TextureFormat.BC1_UNorm_SRGB, // DXGI_FORMAT_BC1_UNORM_SRGB
            74 => TextureFormat.BC2_UNorm, // DXGI_FORMAT_BC2_UNORM
            75 => TextureFormat.BC2_UNorm_SRGB, // DXGI_FORMAT_BC2_UNORM_SRGB
            77 => TextureFormat.BC3_UNorm, // DXGI_FORMAT_BC3_UNORM
            78 => TextureFormat.BC3_UNorm_SRGB, // DXGI_FORMAT_BC3_UNORM_SRGB
            80 => TextureFormat.BC4_UNorm, // DXGI_FORMAT_BC4_UNORM
            81 => TextureFormat.BC4_SNorm, // DXGI_FORMAT_BC4_SNORM
            83 => TextureFormat.BC5_UNorm, // DXGI_FORMAT_BC5_UNORM
            84 => TextureFormat.BC5_SNorm, // DXGI_FORMAT_BC5_SNORM
            95 => TextureFormat.BC6H_UF16, // DXGI_FORMAT_BC6H_UF16
            96 => TextureFormat.BC6H_SF16, // DXGI_FORMAT_BC6H_SF16
            98 => TextureFormat.BC7_UNorm, // DXGI_FORMAT_BC7_UNORM
            99 => TextureFormat.BC7_UNorm_SRGB, // DXGI_FORMAT_BC7_UNORM_SRGB
            _ => throw new NotSupportedException($"DXGI format value {format} is not supported.")
        };
    }
}
