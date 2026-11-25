using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.D3D12;

public partial struct D3D12_SHADER_RESOURCE_VIEW_DESC
{
    public DXGI_FORMAT Format;

    public D3D12_SRV_DIMENSION ViewDimension;

    public uint Shader4ComponentMapping;

    [NativeTypeName("__AnonymousRecord_d3d12_L3958_C5")]
    public _Anonymous_e__Union Anonymous;

    [UnscopedRef]
    public ref D3D12_BUFFER_SRV Buffer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Buffer;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX1D_SRV Texture1D
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture1D;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX1D_ARRAY_SRV Texture1DArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture1DArray;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX2D_SRV Texture2D
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture2D;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX2D_ARRAY_SRV Texture2DArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture2DArray;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX2DMS_SRV Texture2DMS
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture2DMS;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX2DMS_ARRAY_SRV Texture2DMSArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture2DMSArray;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEX3D_SRV Texture3D
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.Texture3D;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEXCUBE_SRV TextureCube
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.TextureCube;
        }
    }

    [UnscopedRef]
    public ref D3D12_TEXCUBE_ARRAY_SRV TextureCubeArray
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.TextureCubeArray;
        }
    }

    [UnscopedRef]
    public ref D3D12_RAYTRACING_ACCELERATION_STRUCTURE_SRV RaytracingAccelerationStructure
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.RaytracingAccelerationStructure;
        }
    }

    [UnscopedRef]
    public ref D3D12_BUFFER_SRV_BYTE_OFFSET BufferByteOffset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return ref Anonymous.BufferByteOffset;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _Anonymous_e__Union
    {
        [FieldOffset(0)]
        public D3D12_BUFFER_SRV Buffer;

        [FieldOffset(0)]
        public D3D12_TEX1D_SRV Texture1D;

        [FieldOffset(0)]
        public D3D12_TEX1D_ARRAY_SRV Texture1DArray;

        [FieldOffset(0)]
        public D3D12_TEX2D_SRV Texture2D;

        [FieldOffset(0)]
        public D3D12_TEX2D_ARRAY_SRV Texture2DArray;

        [FieldOffset(0)]
        public D3D12_TEX2DMS_SRV Texture2DMS;

        [FieldOffset(0)]
        public D3D12_TEX2DMS_ARRAY_SRV Texture2DMSArray;

        [FieldOffset(0)]
        public D3D12_TEX3D_SRV Texture3D;

        [FieldOffset(0)]
        public D3D12_TEXCUBE_SRV TextureCube;

        [FieldOffset(0)]
        public D3D12_TEXCUBE_ARRAY_SRV TextureCubeArray;

        [FieldOffset(0)]
        public D3D12_RAYTRACING_ACCELERATION_STRUCTURE_SRV RaytracingAccelerationStructure;

        [FieldOffset(0)]
        public D3D12_BUFFER_SRV_BYTE_OFFSET BufferByteOffset;
    }
}
