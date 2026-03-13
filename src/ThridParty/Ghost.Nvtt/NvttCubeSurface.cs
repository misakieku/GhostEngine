namespace Ghost.Nvtt;

/// <summary>
/// Wrapper around an nvtt cube-map surface (six faces, optional mip chain).
///
/// Methods that return a new <see cref="NvttCubeSurface"/> transfer ownership
/// to the caller; dispose the result when done.
/// </summary>
public sealed unsafe class NvttCubeSurfaceHandle : IDisposable
{
    private NvttCubeSurface* _ptr;

    /// <summary>Raw pointer - use only when calling the native API directly.</summary>
    public NvttCubeSurface* Ptr => _ptr;

    // -------------------------------------------------------------------------
    // Construction / destruction
    // -------------------------------------------------------------------------

    public NvttCubeSurfaceHandle() => _ptr = Api.nvttCreateCubeSurface();

    /// <summary>Wraps an existing raw pointer (takes ownership; will destroy on dispose).</summary>
    internal NvttCubeSurfaceHandle(NvttCubeSurface* existing) => _ptr = existing;

    public void Dispose()
    {
        if (_ptr != null)
        {
            Api.nvttDestroyCubeSurface(_ptr);
            _ptr = null;
        }
    }

    // -------------------------------------------------------------------------
    // Read-only properties
    // -------------------------------------------------------------------------

    /// <summary>Returns <c>true</c> when the cube surface holds no data.</summary>
    public bool IsNull
    {
        get { ThrowIfDisposed(); return NvttInterop.ToBool(Api.nvttCubeSurfaceIsNull(_ptr)); }
    }

    /// <summary>Side length in pixels of each face.</summary>
    public int EdgeLength
    {
        get { ThrowIfDisposed(); return Api.nvttCubeSurfaceEdgeLength(_ptr); }
    }

    /// <summary>Number of mip levels stored in this cube surface.</summary>
    public int MipmapCount
    {
        get { ThrowIfDisposed(); return Api.nvttCubeSurfaceCountMipmaps(_ptr); }
    }

    // -------------------------------------------------------------------------
    // Load / Save
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads a cube map from disk.
    /// <paramref name="mipmap"/> selects which mip level to load (-1 = all).
    /// Returns <c>false</c> on failure.
    /// </summary>
    public bool Load(string fileName, int mipmap = 0)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[NvttInterop._MAX_STACK_PATH];
        var utf8 = NvttInterop.ToUtf8(fileName, buf);
        fixed (byte* p = utf8)
        {
            return NvttInterop.ToBool(Api.nvttCubeSurfaceLoad(_ptr, (sbyte*)p, mipmap));
        }
    }

    /// <summary>Loads a cube map from a managed byte array.  Returns <c>false</c> on failure.</summary>
    public bool LoadFromMemory(ReadOnlySpan<byte> data, int mipmap = 0)
    {
        ThrowIfDisposed();
        fixed (byte* p = data)
        {
            return NvttInterop.ToBool(
                Api.nvttCubeSurfaceLoadFromMemory(_ptr, p, (ulong)data.Length, mipmap));
        }
    }

    /// <summary>Saves the cube map to disk.  Returns <c>false</c> on failure.</summary>
    public bool Save(string fileName)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[NvttInterop._MAX_STACK_PATH];
        var utf8 = NvttInterop.ToUtf8(fileName, buf);
        fixed (byte* p = utf8)
        {
            return NvttInterop.ToBool(Api.nvttCubeSurfaceSave(_ptr, (sbyte*)p));
        }
    }

    // -------------------------------------------------------------------------
    // Face access
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the raw <see cref="NvttSurface"/> pointer for the given face
    /// (0–5).  The pointer is owned by this cube surface - do NOT dispose it.
    /// </summary>
    public NvttSurface* FacePtr(int face)
    {
        ThrowIfDisposed();
        return Api.nvttCubeSurfaceFace(_ptr, face);
    }

    // -------------------------------------------------------------------------
    // Fold / Unfold
    // -------------------------------------------------------------------------

    /// <summary>
    /// Folds a cross-layout <see cref="NvttSurface"/> into this cube surface.
    /// </summary>
    public void Fold(NvttSurfaceHandle img, NvttCubeLayout layout)
    {
        ThrowIfDisposed();
        Api.nvttCubeSurfaceFold(_ptr, img.Ptr, layout);
    }

    /// <summary>
    /// Unfolds the cube surface into a flat cross-layout image.
    /// Caller owns the returned surface.
    /// </summary>
    public NvttSurfaceHandle Unfold(NvttCubeLayout layout)
    {
        ThrowIfDisposed();
        return new NvttSurfaceHandle(Api.nvttCubeSurfaceUnfold(_ptr, layout));
    }

    // -------------------------------------------------------------------------
    // Query methods
    // -------------------------------------------------------------------------

    /// <summary>Returns the per-channel average for the given channel index.</summary>
    public float Average(int channel)
    {
        ThrowIfDisposed();
        return Api.nvttCubeSurfaceAverage(_ptr, channel);
    }

    /// <summary>Returns the min and max values of a channel across all faces.</summary>
    public void Range(int channel, out float min, out float max)
    {
        ThrowIfDisposed();
        float lo, hi;
        Api.nvttCubeSurfaceRange(_ptr, channel, &lo, &hi);
        min = lo;
        max = hi;
    }

    // -------------------------------------------------------------------------
    // Pixel operations
    // -------------------------------------------------------------------------

    /// <summary>Clamps a channel to [<paramref name="low"/>, <paramref name="high"/>].</summary>
    public void Clamp(int channel, float low, float high)
    {
        ThrowIfDisposed();
        Api.nvttCubeSurfaceClamp(_ptr, channel, low, high);
    }

    /// <summary>Applies gamma expansion (toLinear) to all faces.</summary>
    public void ToLinear(float gamma)
    {
        ThrowIfDisposed();
        Api.nvttCubeSurfaceToLinear(_ptr, gamma);
    }

    /// <summary>Applies gamma compression to all faces.</summary>
    public void ToGamma(float gamma)
    {
        ThrowIfDisposed();
        Api.nvttCubeSurfaceToGamma(_ptr, gamma);
    }

    // -------------------------------------------------------------------------
    // Filtering (return new owned NvttCubeSurface)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes an irradiance-filtered cube map of the given <paramref name="size"/>.
    /// Caller owns the result.
    /// </summary>
    public NvttCubeSurfaceHandle IrradianceFilter(int size, EdgeFixup fixup = EdgeFixup.NVTT_EdgeFixup_None)
    {
        ThrowIfDisposed();
        return new NvttCubeSurfaceHandle(Api.nvttCubeSurfaceIrradianceFilter(_ptr, size, fixup));
    }

    /// <summary>
    /// Computes a cosine-power (specular) filtered cube map.
    /// Caller owns the result.
    /// </summary>
    public NvttCubeSurfaceHandle CosinePowerFilter(int size, float cosinePower,
        EdgeFixup fixup = EdgeFixup.NVTT_EdgeFixup_None)
    {
        ThrowIfDisposed();
        return new NvttCubeSurfaceHandle(
            Api.nvttCubeSurfaceCosinePowerFilter(_ptr, size, cosinePower, fixup));
    }

    /// <summary>
    /// Resamples the cube map to the given <paramref name="size"/> using fast bilinear resampling.
    /// Caller owns the result.
    /// </summary>
    public NvttCubeSurfaceHandle FastResample(int size, EdgeFixup fixup = EdgeFixup.NVTT_EdgeFixup_None)
    {
        ThrowIfDisposed();
        return new NvttCubeSurfaceHandle(Api.nvttCubeSurfaceFastResample(_ptr, size, fixup));
    }

    // -------------------------------------------------------------------------

    private void ThrowIfDisposed()
    {
        if (_ptr == null)
        {
            throw new ObjectDisposedException(nameof(NvttCubeSurfaceHandle));
        }
    }
}
