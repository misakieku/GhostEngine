using System.Runtime.InteropServices;

namespace Ghost.Nvtt;

/// <summary>
/// Static helpers wrapping global nvtt functions (version, CUDA detection,
/// error utilities, image comparison, mipmap helpers).
/// </summary>
public static unsafe class NvttGlobal
{
    // -------------------------------------------------------------------------
    // Library info
    // -------------------------------------------------------------------------

    /// <summary>Returns the nvtt library version as a packed uint (major*10000 + minor*100 + patch).</summary>
    public static uint Version => Api.nvttVersion();

    /// <summary>Returns <c>true</c> when a CUDA-capable GPU is available.</summary>
    public static bool IsCudaSupported
        => NvttInterop.ToBool(Api.nvttIsCudaSupported());

    // -------------------------------------------------------------------------
    // Error strings
    // -------------------------------------------------------------------------

    /// <summary>Returns a human-readable string for <paramref name="error"/>.</summary>
    public static string? ErrorString(NvttError error)
        => NvttInterop.FromUtf8(Api.nvttErrorString(error));

    // -------------------------------------------------------------------------
    // Global message callback
    //
    // The delegate type must be kept alive as long as the callback is registered.
    // Store the returned token and dispose it to clear the callback.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Delegate type for the global nvtt message callback.
    /// </summary>
    public delegate void MessageCallback(
        NvttSeverity severity, NvttError error, string? description);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void NativeMessageCallback(
        NvttSeverity severity, NvttError error, sbyte* description, void* userData);

    /// <summary>
    /// A registration token returned by <see cref="SetMessageCallback"/>.
    /// Dispose to clear the callback and release the pinned delegate.
    /// </summary>
    public sealed class MessageCallbackToken : IDisposable
    {
        private NativeMessageCallback? _pinned;

        internal MessageCallbackToken(NativeMessageCallback pinned)
            => _pinned = pinned;

        public void Dispose()
        {
            if (_pinned != null)
            {
                // Clear the callback by registering null.
                Api.nvttSetMessageCallback(null, null);
                _pinned = null;
            }
        }
    }

    /// <summary>
    /// Registers a managed message callback that nvtt calls for warnings and errors.
    /// Returns a token; dispose the token to unregister.
    /// </summary>
    public static MessageCallbackToken SetMessageCallback(MessageCallback callback)
    {
        void native(NvttSeverity sev, NvttError err, sbyte* descPtr, void* _)
        {
            var desc = NvttInterop.FromUtf8(descPtr);
            callback(sev, err, desc);
        }

        var fp = Marshal.GetFunctionPointerForDelegate(native);
        Api.nvttSetMessageCallback(
            (delegate* unmanaged[Cdecl]<NvttSeverity, NvttError, sbyte*, void*, void>)fp,
            null);

        return new MessageCallbackToken(native);
    }

    // -------------------------------------------------------------------------
    // Image comparison (error metrics)
    // -------------------------------------------------------------------------

    /// <summary>RMS per-channel colour error between two surfaces.</summary>
    public static float RmsError(NvttSurfaceHandle reference, NvttSurfaceHandle img,
        NvttTimingContext* tc = null)
        => Api.nvttRmsError(reference.Ptr, img.Ptr, tc);

    /// <summary>RMS alpha-channel error between two surfaces.</summary>
    public static float RmsAlphaError(NvttSurfaceHandle reference, NvttSurfaceHandle img,
        NvttTimingContext* tc = null)
        => Api.nvttRmsAlphaError(reference.Ptr, img.Ptr, tc);

    /// <summary>RMS CIE-Lab perceptual error between two surfaces.</summary>
    public static float RmsCIELabError(NvttSurfaceHandle reference, NvttSurfaceHandle img,
        NvttTimingContext* tc = null)
        => Api.nvttRmsCIELabError(reference.Ptr, img.Ptr, tc);

    /// <summary>Angular error between two normal-map surfaces.</summary>
    public static float AngularError(NvttSurfaceHandle reference, NvttSurfaceHandle img,
        NvttTimingContext* tc = null)
        => Api.nvttAngularError(reference.Ptr, img.Ptr, tc);

    /// <summary>
    /// Tone-mapped RMS error.  Useful for HDR comparisons.
    /// </summary>
    public static float RmsToneMappedError(NvttSurfaceHandle reference, NvttSurfaceHandle img,
        float exposure, NvttTimingContext* tc = null)
        => Api.nvttRmsToneMappedError(reference.Ptr, img.Ptr, exposure, tc);

    // -------------------------------------------------------------------------
    // Difference image
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a new surface containing the scaled per-pixel difference.
    /// Caller owns the returned surface.
    /// </summary>
    public static NvttSurfaceHandle Diff(NvttSurfaceHandle reference, NvttSurfaceHandle img,
        float scale, NvttTimingContext* tc = null)
        => new NvttSurfaceHandle(Api.nvttDiff(reference.Ptr, img.Ptr, scale, tc));

    // -------------------------------------------------------------------------
    // Histogram
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a new surface containing a histogram visualisation of
    /// <paramref name="img"/> at the given dimensions.
    /// Caller owns the returned surface.
    /// </summary>
    public static NvttSurfaceHandle Histogram(NvttSurfaceHandle img, int width, int height,
        NvttTimingContext* tc = null)
        => new NvttSurfaceHandle(Api.nvttHistogram(img.Ptr, width, height, tc));

    /// <summary>
    /// Returns a new surface containing a histogram visualisation with an
    /// explicit value range.
    /// Caller owns the returned surface.
    /// </summary>
    public static NvttSurfaceHandle HistogramRange(NvttSurfaceHandle img,
        float minRange, float maxRange, int width, int height,
        NvttTimingContext* tc = null)
        => new NvttSurfaceHandle(
            Api.nvttHistogramRange(img.Ptr, minRange, maxRange, width, height, tc));

    // -------------------------------------------------------------------------
    // Extent / mipmap helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes the target extent (power-of-two rounding, texture type clamping,
    /// etc.) for a texture of the given dimensions.
    /// Modifies <paramref name="width"/>, <paramref name="height"/> and
    /// <paramref name="depth"/> in place.
    /// </summary>
    public static void GetTargetExtent(ref int width, ref int height, ref int depth,
        int maxExtent, NvttRoundMode roundMode, NvttTextureType textureType,
        NvttTimingContext* tc = null)
    {
        fixed (int* pw = &width, ph = &height, pd = &depth)
        {
            Api.nvttGetTargetExtent(pw, ph, pd, maxExtent, roundMode, textureType, tc);
        }
    }

    /// <summary>
    /// Returns the number of mip levels that can be generated for the given
    /// base dimensions.
    /// </summary>
    public static int CountMipmaps(int w, int h, int d,
        NvttTimingContext* tc = null)
        => Api.nvttCountMipmaps(w, h, d, tc);
}
