using Ghost.Nvtt.Native;
using System.Runtime.InteropServices;

namespace Ghost.Nvtt;

/// <summary>
/// Wrapper around a single 2-D / 3-D / cube-face image surface used as input
/// to the compression pipeline.
///
/// Most mutating methods accept an optional <paramref name="tc"/> timing
/// context.  Pass <c>null</c> (the default) to skip timing.
/// </summary>
public sealed unsafe class NvttSurfaceHandle : IDisposable
{
    private NvttSurface* _ptr;

    /// <summary>Raw pointer – use only when calling the native API directly.</summary>
    public NvttSurface* Ptr => _ptr;

    // -------------------------------------------------------------------------
    // Construction / destruction
    // -------------------------------------------------------------------------
    
    public NvttSurfaceHandle() => _ptr = Api.nvttCreateSurface();

    /// <summary>Wraps an existing raw pointer (takes ownership; will destroy on dispose).</summary>
    internal NvttSurfaceHandle(NvttSurface* existing) => _ptr = existing;

    public void Dispose()
    {
        if (_ptr != null)
        {
            Api.nvttDestroySurface(_ptr);
            _ptr = null;
        }
    }

    // -------------------------------------------------------------------------
    // Clone / sub-image
    // -------------------------------------------------------------------------

    /// <summary>Returns a deep copy of this surface.</summary>
    public NvttSurfaceHandle Clone()
    {
        ThrowIfDisposed();
        return new NvttSurfaceHandle(Api.nvttSurfaceClone(_ptr));
    }

    /// <summary>
    /// Extracts a rectangular sub-region into a new <see cref="NvttSurfaceHandle"/>.
    /// </summary>
    public NvttSurfaceHandle CreateSubImage(
        int x0, int x1, int y0, int y1, int z0, int z1,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        return new NvttSurfaceHandle(Api.nvttSurfaceCreateSubImage(_ptr, x0, x1, y0, y1, z0, z1, tc));
    }

    // -------------------------------------------------------------------------
    // Read-only properties
    // -------------------------------------------------------------------------

    /// <summary>Returns <c>true</c> when the surface holds no data.</summary>
    public bool IsNull
    {
        get { ThrowIfDisposed(); return NvttInterop.ToBool(Api.nvttSurfaceIsNull(_ptr)); }
    }

    /// <summary>Image width in pixels.</summary>
    public int Width
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceWidth(_ptr); }
    }

    /// <summary>Image height in pixels.</summary>
    public int Height
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceHeight(_ptr); }
    }

    /// <summary>Image depth (1 for 2-D textures).</summary>
    public int Depth
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceDepth(_ptr); }
    }

    /// <summary>Texture dimensionality.</summary>
    public NvttTextureType TextureType
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceType(_ptr); }
    }

    /// <summary>Whether the surface contains a normal map.</summary>
    public bool IsNormalMap
    {
        get { ThrowIfDisposed(); return NvttInterop.ToBool(Api.nvttSurfaceIsNormalMap(_ptr)); }
    }

    // -------------------------------------------------------------------------
    // Settable properties
    // -------------------------------------------------------------------------

    /// <summary>UV wrap mode used when filtering near edges.</summary>
    public NvttWrapMode WrapMode
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceWrapMode(_ptr); }
    }

    /// <summary>Alpha mode interpretation.</summary>
    public NvttAlphaMode AlphaMode
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceAlphaMode(_ptr); }
    }

    // -------------------------------------------------------------------------
    // Query methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the number of mip levels that can be generated down to
    /// <paramref name="minSize"/> pixels on the smallest side.
    /// </summary>
    public int CountMipmaps(int minSize = 1)
    {
        ThrowIfDisposed();
        return Api.nvttSurfaceCountMipmaps(_ptr, minSize);
    }

    /// <summary>Alpha-test coverage for the given reference value and channel.</summary>
    public float AlphaTestCoverage(float alphaRef, int alphaChannel = 3)
    {
        ThrowIfDisposed();
        return Api.nvttSurfaceAlphaTestCoverage(_ptr, alphaRef, alphaChannel);
    }

    /// <summary>Per-channel average luminance.</summary>
    public float Average(int channel, int alphaChannel = 3, float gamma = 2.2f)
    {
        ThrowIfDisposed();
        return Api.nvttSurfaceAverage(_ptr, channel, alphaChannel, gamma);
    }

    /// <summary>
    /// Returns a pointer to the raw float data for all four channels interleaved.
    /// The span is valid only while this surface is alive.
    /// </summary>
    public Span<float> Data
    {
        get
        {
            ThrowIfDisposed();
            float* p = Api.nvttSurfaceData(_ptr);
            int count = Width * Height * Depth * 4;
            return p == null ? Span<float>.Empty : new Span<float>(p, count);
        }
    }

    /// <summary>
    /// Returns a pointer to the raw float data for a single channel (0=R,1=G,2=B,3=A).
    /// The span is valid only while this surface is alive.
    /// </summary>
    public Span<float> Channel(int index)
    {
        ThrowIfDisposed();
        float* p = Api.nvttSurfaceChannel(_ptr, index);
        int count = Width * Height * Depth;
        return p == null ? Span<float>.Empty : new Span<float>(p, count);
    }

    /// <summary>Populates a histogram array for the given channel.</summary>
    public void Histogram(int channel, float rangeMin, float rangeMax, Span<int> bins,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        fixed (int* b = bins)
        {
            Api.nvttSurfaceHistogram(_ptr, channel, rangeMin, rangeMax, bins.Length, b, tc);
        }
    }

    /// <summary>Returns the minimum and maximum values of a channel.</summary>
    public void Range(int channel, out float min, out float max,
        int alphaChannel = 3, float alphaRef = 0f,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        float lo, hi;
        Api.nvttSurfaceRange(_ptr, channel, &lo, &hi, alphaChannel, alphaRef, tc);
        min = lo;
        max = hi;
    }

    // -------------------------------------------------------------------------
    // Load / Save
    // -------------------------------------------------------------------------

    /// <summary>Loads an image from disk.  Returns <c>false</c> on failure.</summary>
    public bool Load(string fileName, out bool hasAlpha, bool expectSigned = false,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[NvttInterop._MAX_STACK_PATH];
        var utf8 = NvttInterop.ToUtf8(fileName, buf);
        fixed (byte* p = utf8)
        {
            Ghost.Nvtt.Native.NvttBoolean nvAlpha;
            bool ok = NvttInterop.ToBool(
                Api.nvttSurfaceLoad(_ptr, (sbyte*)p, &nvAlpha,
                    NvttInterop.ToNvtt(expectSigned), tc));
            hasAlpha = NvttInterop.ToBool(nvAlpha);
            return ok;
        }
    }

    /// <summary>Loads an image from a managed byte array.  Returns <c>false</c> on failure.</summary>
    public bool LoadFromMemory(ReadOnlySpan<byte> data, out bool hasAlpha,
        bool expectSigned = false, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        fixed (byte* p = data)
        {
            Ghost.Nvtt.Native.NvttBoolean nvAlpha;
            bool ok = NvttInterop.ToBool(
                Api.nvttSurfaceLoadFromMemory(_ptr, p, (ulong)data.Length,
                    &nvAlpha, NvttInterop.ToNvtt(expectSigned), tc));
            hasAlpha = NvttInterop.ToBool(nvAlpha);
            return ok;
        }
    }

    /// <summary>Saves the surface to disk.  Returns <c>false</c> on failure.</summary>
    public bool Save(string fileName, bool hasAlpha = false, bool hdr = false,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[NvttInterop._MAX_STACK_PATH];
        var utf8 = NvttInterop.ToUtf8(fileName, buf);
        fixed (byte* p = utf8)
        {
            return NvttInterop.ToBool(
                Api.nvttSurfaceSave(_ptr, (sbyte*)p,
                    NvttInterop.ToNvtt(hasAlpha), NvttInterop.ToNvtt(hdr), tc));
        }
    }

    // -------------------------------------------------------------------------
    // Set image data
    // -------------------------------------------------------------------------

    /// <summary>Allocates an empty surface of the given dimensions.</summary>
    public bool SetImage(int w, int h, int d = 1, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(Api.nvttSurfaceSetImage(_ptr, w, h, d, tc));
    }

    /// <summary>Sets the surface from interleaved RGBA data.</summary>
    public bool SetImageData(NvttInputFormat format, int w, int h, int d,
        ReadOnlySpan<byte> data, bool unsignedToSigned = false,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        fixed (byte* p = data)
        {
            return NvttInterop.ToBool(
                Api.nvttSurfaceSetImageData(_ptr, format, w, h, d, p,
                    NvttInterop.ToNvtt(unsignedToSigned), tc));
        }
    }

    /// <summary>Sets the surface from separate RGBA channel planes.</summary>
    public bool SetImageRGBA(NvttInputFormat format, int w, int h, int d,
        ReadOnlySpan<byte> r, ReadOnlySpan<byte> g,
        ReadOnlySpan<byte> b, ReadOnlySpan<byte> a,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        fixed (byte* pr = r, pg = g, pb = b, pa = a)
        {
            return NvttInterop.ToBool(
                Api.nvttSurfaceSetImageRGBA(_ptr, format, w, h, d, pr, pg, pb, pa, tc));
        }
    }

    /// <summary>Sets the surface from a compressed 2-D image.</summary>
    public bool SetImage2D(NvttFormat format, int w, int h,
        ReadOnlySpan<byte> data, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        fixed (byte* p = data)
        {
            return NvttInterop.ToBool(
                Api.nvttSurfaceSetImage2D(_ptr, format, w, h, p, tc));
        }
    }

    /// <summary>Sets the surface from a compressed 3-D image.</summary>
    public bool SetImage3D(NvttFormat format, int w, int h, int d,
        ReadOnlySpan<byte> data, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        fixed (byte* p = data)
        {
            return NvttInterop.ToBool(
                Api.nvttSurfaceSetImage3D(_ptr, format, w, h, d, p, tc));
        }
    }

    // -------------------------------------------------------------------------
    // Resize / mipmap
    // -------------------------------------------------------------------------

    /// <summary>Resizes the surface to the exact dimensions given.</summary>
    public void Resize(int w, int h, int d, NvttResizeFilter filter,
        float filterWidth = 1f, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Api.nvttSurfaceResize(_ptr, w, h, d, filter, filterWidth, null, tc);
    }

    /// <summary>Resizes so that the longest extent is at most <paramref name="maxExtent"/>.</summary>
    public void ResizeMax(int maxExtent, NvttRoundMode mode, NvttResizeFilter filter,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Api.nvttSurfaceResizeMax(_ptr, maxExtent, mode, filter, tc);
    }

    /// <summary>Resizes to a square texture with side at most <paramref name="maxExtent"/>.</summary>
    public void ResizeMakeSquare(int maxExtent, NvttRoundMode mode, NvttResizeFilter filter,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Api.nvttSurfaceResizeMakeSquare(_ptr, maxExtent, mode, filter, tc);
    }

    /// <summary>Pads or crops the canvas to the given dimensions without resampling.</summary>
    public void CanvasSize(int w, int h, int d, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Api.nvttSurfaceCanvasSize(_ptr, w, h, d, tc);
    }

    /// <summary>Returns <c>true</c> if a next mip level can still be generated.</summary>
    public bool CanMakeNextMipmap(int minSize = 1)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(Api.nvttSurfaceCanMakeNextMipmap(_ptr, minSize));
    }

    /// <summary>Generates the next mip level in place (downsamples by 2).</summary>
    public bool BuildNextMipmap(NvttMipmapFilter filter, int minSize = 1,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(
            Api.nvttSurfaceBuildNextMipmapDefaults(_ptr, filter, minSize, tc));
    }

    /// <summary>Generates the next mip level using a solid colour.</summary>
    public bool BuildNextMipmapSolidColor(float r, float g, float b, float a,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        float* color = stackalloc float[4] { r, g, b, a };
        return NvttInterop.ToBool(
            Api.nvttSurfaceBuildNextMipmapSolidColor(_ptr, color, tc));
    }

    // -------------------------------------------------------------------------
    // Colour-space conversions
    // -------------------------------------------------------------------------

    /// <summary>Converts from sRGB to linear (per-channel).</summary>
    public void ToLinearFromSrgb(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToLinearFromSrgb(_ptr, tc);
    }

    /// <summary>Converts from linear to sRGB (clamped).</summary>
    public void ToSrgb(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToSrgb(_ptr, tc);
    }

    /// <summary>Converts from sRGB to linear (unclamped).</summary>
    public void ToLinearFromSrgbUnclamped(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToLinearFromSrgbUnclamped(_ptr, tc);
    }

    /// <summary>Converts from linear to sRGB (unclamped).</summary>
    public void ToSrgbUnclamped(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToSrgbUnclamped(_ptr, tc);
    }

    /// <summary>Applies gamma expansion (toLinear) to all channels.</summary>
    public void ToLinear(float gamma, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToLinear(_ptr, gamma, tc);
    }

    /// <summary>Applies gamma compression to all channels.</summary>
    public void ToGamma(float gamma, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToGamma(_ptr, gamma, tc);
    }

    /// <summary>Applies gamma expansion to a single channel.</summary>
    public void ToLinearChannel(int channel, float gamma, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToLinearChannel(_ptr, channel, gamma, tc);
    }

    /// <summary>Applies gamma compression to a single channel.</summary>
    public void ToGammaChannel(int channel, float gamma, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToGammaChannel(_ptr, channel, gamma, tc);
    }

    // -------------------------------------------------------------------------
    // Pixel operations
    // -------------------------------------------------------------------------

    /// <summary>Applies a 4×4 colour transform matrix plus per-channel offset.</summary>
    public void Transform(
        float[] w0, float[] w1, float[] w2, float[] w3, float[] offset,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        fixed (float* pw0 = w0, pw1 = w1, pw2 = w2, pw3 = w3, po = offset)
        {
            Api.nvttSurfaceTransform(_ptr, pw0, pw1, pw2, pw3, po, tc);
        }
    }

    /// <summary>Rearranges channels: result[0]=src[r], result[1]=src[g], etc.</summary>
    public void Swizzle(int r, int g, int b, int a, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceSwizzle(_ptr, r, g, b, a, tc);
    }

    /// <summary>Applies <c>x = x * scale + bias</c> to a single channel.</summary>
    public void ScaleBias(int channel, float scale, float bias,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceScaleBias(_ptr, channel, scale, bias, tc);
    }

    /// <summary>Clamps a channel to [<paramref name="low"/>, <paramref name="high"/>].</summary>
    public void Clamp(int channel, float low, float high, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceClamp(_ptr, channel, low, high, tc);
    }

    /// <summary>Blends toward a constant RGBA colour by factor <paramref name="t"/>.</summary>
    public void Blend(float r, float g, float b, float a, float t,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceBlend(_ptr, r, g, b, a, t, tc);
    }

    /// <summary>Multiplies RGB by alpha.</summary>
    public void PremultiplyAlpha(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfacePremultiplyAlpha(_ptr, tc);
    }

    /// <summary>Divides RGB by alpha (with epsilon guard against divide-by-zero).</summary>
    public void DemultiplyAlpha(float epsilon = 1e-6f, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceDemultiplyAlpha(_ptr, epsilon, tc);
    }

    /// <summary>Converts to greyscale by weighted sum of channels.</summary>
    public void ToGreyScale(float redScale, float greenScale, float blueScale,
        float alphaScale, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Api.nvttSurfaceToGreyScale(_ptr, redScale, greenScale, blueScale, alphaScale, tc);
    }

    /// <summary>Fills the edge border of the surface with the given colour.</summary>
    public void SetBorder(float r, float g, float b, float a,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceSetBorder(_ptr, r, g, b, a, tc);
    }

    /// <summary>Fills the entire surface with a constant colour.</summary>
    public void Fill(float r, float g, float b, float a, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceFill(_ptr, r, g, b, a, tc);
    }

    /// <summary>Scales alpha so that alpha-test coverage matches the given target.</summary>
    public void ScaleAlphaToCoverage(float coverage, float alphaRef = 0.5f,
        int alphaChannel = 3, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Api.nvttSurfaceScaleAlphaToCoverage(_ptr, coverage, alphaRef, alphaChannel, tc);
    }

    // -------------------------------------------------------------------------
    // HDR encodings
    // -------------------------------------------------------------------------

    /// <summary>Encodes to RGBM (RGB * M, M in alpha).</summary>
    public void ToRGBM(float range = 6f, float threshold = 0.25f,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToRGBM(_ptr, range, threshold, tc);
    }

    /// <summary>Decodes from RGBM back to linear HDR.</summary>
    public void FromRGBM(float range = 6f, float threshold = 0.25f,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceFromRGBM(_ptr, range, threshold, tc);
    }

    /// <summary>Encodes to RGBE (Radiance HDR format).</summary>
    public void ToRGBE(int mantissaBits = 9, int exponentBits = 5,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToRGBE(_ptr, mantissaBits, exponentBits, tc);
    }

    /// <summary>Decodes from RGBE back to linear HDR.</summary>
    public void FromRGBE(int mantissaBits = 9, int exponentBits = 5,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceFromRGBE(_ptr, mantissaBits, exponentBits, tc);
    }

    /// <summary>Converts to YCoCg colour space.</summary>
    public void ToYCoCg(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToYCoCg(_ptr, tc);
    }

    /// <summary>Converts from YCoCg back to RGB.</summary>
    public void FromYCoCg(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceFromYCoCg(_ptr, tc);
    }

    // -------------------------------------------------------------------------
    // Normal-map operations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates a normal map from the surface (treated as a height map)
    /// using four blur kernel sizes.
    /// </summary>
    public void ToNormalMap(float sm, float medium, float big, float large,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Api.nvttSurfaceToNormalMap(_ptr, sm, medium, big, large, tc);
    }

    /// <summary>Re-normalises all normal vectors in the surface.</summary>
    public void NormalizeNormalMap(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceNormalizeNormalMap(_ptr, tc);
    }

    /// <summary>Applies a normal-space transform.</summary>
    public void TransformNormals(NvttNormalTransform xform,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceTransformNormals(_ptr, xform, tc);
    }

    /// <summary>Reconstructs normals from a packed representation.</summary>
    public void ReconstructNormals(NvttNormalTransform xform,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceReconstructNormals(_ptr, xform, tc);
    }

    /// <summary>Packs normals into [0,1] range using <c>n*scale+bias</c>.</summary>
    public void PackNormals(float scale = 0.5f, float bias = 0.5f,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfacePackNormals(_ptr, scale, bias, tc);
    }

    /// <summary>Expands packed normals back to [-1,1] range.</summary>
    public void ExpandNormals(float scale = 2f, float bias = -1f,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceExpandNormals(_ptr, scale, bias, tc);
    }

    /// <summary>
    /// Creates a Toksvig specular power map from a normal map.
    /// Caller owns the returned surface.
    /// </summary>
    public NvttSurfaceHandle CreateToksvigMap(float power, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        return new NvttSurfaceHandle(Api.nvttSurfaceCreateToksvigMap(_ptr, power, tc));
    }

    // -------------------------------------------------------------------------
    // Flip
    // -------------------------------------------------------------------------

    /// <summary>Flips the surface along the X axis.</summary>
    public void FlipX(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceFlipX(_ptr, tc);
    }

    /// <summary>Flips the surface along the Y axis.</summary>
    public void FlipY(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceFlipY(_ptr, tc);
    }

    /// <summary>Flips the surface along the Z axis.</summary>
    public void FlipZ(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceFlipZ(_ptr, tc);
    }

    // -------------------------------------------------------------------------
    // Channel copy / add
    // -------------------------------------------------------------------------

    /// <summary>Copies a single channel from another surface.</summary>
    public bool CopyChannel(NvttSurfaceHandle src, int srcChannel, int dstChannel,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(
            Api.nvttSurfaceCopyChannel(_ptr, src.Ptr, srcChannel, dstChannel, tc));
    }

    /// <summary>Adds a scaled channel from another surface into this one.</summary>
    public bool AddChannel(NvttSurfaceHandle src, int srcChannel, int dstChannel,
        float scale = 1f, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(
            Api.nvttSurfaceAddChannel(_ptr, src.Ptr, srcChannel, dstChannel, scale, tc));
    }

    /// <summary>Copies a rectangular region from another surface into this one.</summary>
    public bool Copy(NvttSurfaceHandle src,
        int xsrc, int ysrc, int zsrc,
        int xsize, int ysize, int zsize,
        int xdst, int ydst, int zdst,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        return NvttInterop.ToBool(
            Api.nvttSurfaceCopy(_ptr, src.Ptr,
                xsrc, ysrc, zsrc, xsize, ysize, zsize, xdst, ydst, zdst, tc));
    }

    // -------------------------------------------------------------------------
    // GPU transfer
    // -------------------------------------------------------------------------

    /// <summary>Uploads the surface to the GPU (CUDA). <paramref name="performCopy"/> clones instead of moving.</summary>
    public void ToGPU(bool performCopy = false, NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Api.nvttSurfaceToGPU(_ptr, NvttInterop.ToNvtt(performCopy), tc);
    }

    /// <summary>Downloads the surface back to CPU memory.</summary>
    public void ToCPU(NvttTimingContext* tc = null)
    {
        ThrowIfDisposed(); Api.nvttSurfaceToCPU(_ptr, tc);
    }

    // -------------------------------------------------------------------------
    // Quantize / binarize
    // -------------------------------------------------------------------------

    /// <summary>Quantizes a channel to the given bit depth.</summary>
    public void Quantize(int channel, int bits,
        bool exactEndPoints = false, bool dither = false,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Api.nvttSurfaceQuantize(_ptr, channel, bits,
            NvttInterop.ToNvtt(exactEndPoints), NvttInterop.ToNvtt(dither), tc);
    }

    /// <summary>Binarizes a channel using a threshold.</summary>
    public void Binarize(int channel, float threshold, bool dither = false,
        NvttTimingContext* tc = null)
    {
        ThrowIfDisposed();
        Api.nvttSurfaceBinarize(_ptr, channel, threshold,
            NvttInterop.ToNvtt(dither), tc);
    }

    // -------------------------------------------------------------------------

    private void ThrowIfDisposed()
    {
        if (_ptr == null)
        {
            throw new ObjectDisposedException(nameof(NvttSurfaceHandle));
        }
    }
}
