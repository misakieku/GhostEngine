namespace Ghost.Nvtt;

/// <summary>
/// Wrapper around the nvtt compression context — the central object that drives
/// the compression pipeline.
/// </summary>
public sealed unsafe class NvttContextHandle : IDisposable
{
    private NvttContext* _ptr;

    /// <summary>Raw pointer - use only when calling the native API directly.</summary>
    public NvttContext* Ptr => _ptr;

    // -------------------------------------------------------------------------
    // Construction / destruction
    // -------------------------------------------------------------------------

    public NvttContextHandle() => _ptr = Api.nvttCreateContext();

    public void Dispose()
    {
        if (_ptr != null)
        {
            Api.nvttDestroyContext(_ptr);
            _ptr = null;
        }
    }

    // -------------------------------------------------------------------------
    // CUDA acceleration
    // -------------------------------------------------------------------------

    /// <summary>Enables or disables CUDA-accelerated compression.</summary>
    public void SetCudaAcceleration(bool enable)
    {
        ThrowIfDisposed();
        Api.nvttSetContextCudaAcceleration(_ptr, NvttInterop.ToNvtt(enable));
    }

    /// <summary>Returns <c>true</c> if CUDA acceleration is currently enabled.</summary>
    public bool IsCudaAccelerationEnabled
    {
        get
        {
            ThrowIfDisposed();
            return NvttInterop.ToBool(Api.nvttContextIsCudaAccelerationEnabled(_ptr));
        }
    }

    // -------------------------------------------------------------------------
    // Timing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enables or disables internal timing collection.
    /// <paramref name="detailLevel"/> controls the granularity (0 = off, higher = more detail).
    /// </summary>
    public void EnableTiming(bool enable, int detailLevel = 1)
    {
        ThrowIfDisposed();
        Api.nvttContextEnableTiming(_ptr, NvttInterop.ToNvtt(enable), detailLevel);
    }

    /// <summary>
    /// Returns the timing context owned by this nvtt context.
    /// The pointer is borrowed - do NOT dispose it separately.
    /// Returns <c>null</c> if timing was never enabled.
    /// </summary>
    public NvttTimingContext* GetTimingContextPtr()
    {
        ThrowIfDisposed();
        return Api.nvttContextGetTimingContext(_ptr);
    }

    // -------------------------------------------------------------------------
    // Estimate size
    // -------------------------------------------------------------------------

    /// <summary>
    /// Estimates the compressed size in bytes for <paramref name="mipmapCount"/>
    /// mip levels of <paramref name="img"/> using <paramref name="compressionOptions"/>.
    /// </summary>
    public int EstimateSize(NvttSurfaceHandle img, int mipmapCount,
        NvttCompressionOptionsHandle compressionOptions)
    {
        ThrowIfDisposed();
        return Api.nvttContextEstimateSize(_ptr, img.Ptr, mipmapCount,
            compressionOptions.Ptr);
    }

    /// <summary>Estimates the compressed size for a cube map.</summary>
    public int EstimateSizeCube(NvttCubeSurfaceHandle img, int mipmapCount,
        NvttCompressionOptionsHandle compressionOptions)
    {
        ThrowIfDisposed();
        return Api.nvttContextEstimateSizeCube(_ptr, img.Ptr, mipmapCount,
            compressionOptions.Ptr);
    }

    /// <summary>Estimates the compressed size for raw-data dimensions.</summary>
    public int EstimateSizeData(int w, int h, int d, int mipmapCount,
        NvttCompressionOptionsHandle compressionOptions)
    {
        ThrowIfDisposed();
        return Api.nvttContextEstimateSizeData(_ptr, w, h, d, mipmapCount,
            compressionOptions.Ptr);
    }

    // -------------------------------------------------------------------------
    // Output header
    // -------------------------------------------------------------------------

    /// <summary>
    /// Writes the DDS / KTX header to <paramref name="outputOptions"/>.
    /// Must be called once before compressing mip levels.
    /// Returns <c>false</c> on failure.
    /// </summary>
    public bool OutputHeader(NvttSurfaceHandle img, int mipmapCount,
        NvttCompressionOptionsHandle compressionOptions, NvttOutputOptionsHandle outputOptions)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(
            Api.nvttContextOutputHeader(_ptr, img.Ptr, mipmapCount,
                compressionOptions.Ptr, outputOptions.Ptr));
    }

    /// <summary>Writes the header for a cube-map texture.</summary>
    public bool OutputHeaderCube(NvttCubeSurfaceHandle img, int mipmapCount,
        NvttCompressionOptionsHandle compressionOptions, NvttOutputOptionsHandle outputOptions)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(
            Api.nvttContextOutputHeaderCube(_ptr, img.Ptr, mipmapCount,
                compressionOptions.Ptr, outputOptions.Ptr));
    }

    /// <summary>Writes the header using explicit dimensions instead of a surface.</summary>
    public bool OutputHeaderData(NvttTextureType type, int w, int h, int d,
        int mipmapCount, bool isNormalMap,
        NvttCompressionOptionsHandle compressionOptions, NvttOutputOptionsHandle outputOptions)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(
            Api.nvttContextOutputHeaderData(_ptr, type, w, h, d, mipmapCount,
                NvttInterop.ToNvtt(isNormalMap),
                compressionOptions.Ptr, outputOptions.Ptr));
    }

    // -------------------------------------------------------------------------
    // Compress
    // -------------------------------------------------------------------------

    /// <summary>
    /// Compresses a single face/mip of <paramref name="img"/> and sends the
    /// result to <paramref name="outputOptions"/>.
    /// Returns <c>false</c> on failure.
    /// </summary>
    public bool Compress(NvttSurfaceHandle img, int face, int mipmap,
        NvttCompressionOptionsHandle compressionOptions, NvttOutputOptionsHandle outputOptions)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(
            Api.nvttContextCompress(_ptr, img.Ptr, face, mipmap,
                compressionOptions.Ptr, outputOptions.Ptr));
    }

    /// <summary>Compresses a single mip of a cube-map face.</summary>
    public bool CompressCube(NvttCubeSurfaceHandle img, int mipmap,
        NvttCompressionOptionsHandle compressionOptions, NvttOutputOptionsHandle outputOptions)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(
            Api.nvttContextCompressCube(_ptr, img.Ptr, mipmap,
                compressionOptions.Ptr, outputOptions.Ptr));
    }

    /// <summary>Compresses a single mip from a raw float RGBA buffer.</summary>
    public bool CompressData(int w, int h, int d, int face, int mipmap,
        ReadOnlySpan<float> rgba,
        NvttCompressionOptionsHandle compressionOptions, NvttOutputOptionsHandle outputOptions)
    {
        ThrowIfDisposed();
        fixed (float* p = rgba)
        {
            return NvttInterop.ToBool(
                Api.nvttContextCompressData(_ptr, w, h, d, face, mipmap, p,
                    compressionOptions.Ptr, outputOptions.Ptr));
        }
    }

    /// <summary>
    /// Compresses a batch of (surface, face, mipmap, outputOptions) entries
    /// using the shared <paramref name="compressionOptions"/>.
    /// Returns <c>false</c> on failure.
    /// </summary>
    public bool CompressBatch(NvttBatchListHandle batchList,
        NvttCompressionOptionsHandle compressionOptions)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(
            Api.nvttContextCompressBatch(_ptr, batchList.Ptr,
                compressionOptions.Ptr));
    }

    // -------------------------------------------------------------------------
    // Quantize
    // -------------------------------------------------------------------------

    /// <summary>
    /// Quantizes <paramref name="surface"/> in place according to
    /// <paramref name="compressionOptions"/> (useful before compressing
    /// to formats that only support limited bit depths).
    /// </summary>
    public void Quantize(NvttSurfaceHandle surface, NvttCompressionOptionsHandle compressionOptions)
    {
        ThrowIfDisposed();
        Api.nvttContextQuantize(_ptr, surface.Ptr, compressionOptions.Ptr);
    }

    // -------------------------------------------------------------------------

    private void ThrowIfDisposed()
    {
        if (_ptr == null)
        {
            throw new ObjectDisposedException(nameof(NvttContextHandle));
        }
    }
}
