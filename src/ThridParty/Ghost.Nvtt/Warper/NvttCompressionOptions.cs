namespace Ghost.Nvtt.Warper;

/// <summary>
/// Controls how a surface is compressed - format, quality, pixel layout and
/// optional quantization settings.
/// </summary>
public sealed unsafe class NvttCompressionOptionsHandle : IDisposable
{
    private NvttCompressionOptions* _ptr;

    /// <summary>Raw pointer - use only when calling the native API directly.</summary>
    public NvttCompressionOptions* Ptr => _ptr;

    // -------------------------------------------------------------------------
    // Construction / destruction
    // -------------------------------------------------------------------------

    public NvttCompressionOptionsHandle() => _ptr = Api.nvttCreateCompressionOptions();

    public void Dispose()
    {
        if (_ptr != null)
        {
            Api.nvttDestroyCompressionOptions(_ptr);
            _ptr = null;
        }
    }

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    /// <summary>Target compressed format (e.g. BC1, BC7, ASTC …).</summary>
    public NvttFormat Format
    {
        set { ThrowIfDisposed(); Api.nvttSetCompressionOptionsFormat(_ptr, value); }
    }

    /// <summary>Compression quality preset.</summary>
    public NvttQuality Quality
    {
        set { ThrowIfDisposed(); Api.nvttSetCompressionOptionsQuality(_ptr, value); }
    }

    /// <summary>Pixel type for uncompressed RGB(A) output.</summary>
    public NvttPixelType PixelType
    {
        set { ThrowIfDisposed(); Api.nvttSetCompressionOptionsPixelType(_ptr, value); }
    }

    /// <summary>Row-pitch alignment in bytes for uncompressed output.</summary>
    public int PitchAlignment
    {
        set { ThrowIfDisposed(); Api.nvttSetCompressionOptionsPitchAlignment(_ptr, value); }
    }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>Resets all options to their default values.</summary>
    public void Reset()
    {
        ThrowIfDisposed();
        Api.nvttResetCompressionOptions(_ptr);
    }

    /// <summary>
    /// Sets per-channel importance weights used during block-compression error
    /// minimisation.
    /// </summary>
    public void SetColorWeights(float red, float green, float blue, float alpha = 1f)
    {
        ThrowIfDisposed();
        Api.nvttSetCompressionOptionsColorWeights(_ptr, red, green, blue, alpha);
    }

    /// <summary>
    /// Configures a custom uncompressed pixel format by specifying the bit-depth
    /// and per-channel bit masks.
    /// </summary>
    public void SetPixelFormat(uint bitCount, uint rMask, uint gMask, uint bMask, uint aMask)
    {
        ThrowIfDisposed();
        Api.nvttSetCompressionOptionsPixelFormat(_ptr, bitCount, rMask, gMask, bMask, aMask);
    }

    /// <summary>
    /// Enables or disables dithering and binary-alpha quantisation.
    /// </summary>
    /// <param name="colorDithering">Dither RGB channels.</param>
    /// <param name="alphaDithering">Dither the alpha channel.</param>
    /// <param name="binaryAlpha">Snap alpha to 0 or 255.</param>
    /// <param name="alphaThreshold">Threshold used when <paramref name="binaryAlpha"/> is true.</param>
    public void SetQuantization(bool colorDithering, bool alphaDithering, bool binaryAlpha,
                                int alphaThreshold = 127)
    {
        ThrowIfDisposed();
        Api.nvttSetCompressionOptionsQuantization(
            _ptr,
            NvttInterop.ToNvtt(colorDithering),
            NvttInterop.ToNvtt(alphaDithering),
            NvttInterop.ToNvtt(binaryAlpha),
            alphaThreshold);
    }

    /// <summary>Returns the D3D9 FourCC / D3DFORMAT value for the current settings.</summary>
    public uint GetD3D9Format()
    {
        ThrowIfDisposed();
        return Api.nvttGetCompressionOptionsD3D9Format(_ptr);
    }

    // -------------------------------------------------------------------------

    private void ThrowIfDisposed()
    {
        if (_ptr == null)
        {
            throw new ObjectDisposedException(nameof(NvttCompressionOptionsHandle));
        }
    }
}
