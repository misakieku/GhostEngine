using Ghost.Nvtt;
using Ghost.Nvtt.Warper;
using Ghost.Test.Core;

namespace Ghost.MicroTest;

/// <summary>
/// Validates the NVTT binding + wrapper layer end-to-end.
///
/// Tests performed:
///   1. Version query   — confirms the native DLL loads.
///   2. Surface load    — loads an image from disk via NvttSurface.Load.
///   3. Resize          — resizes to a power-of-two no larger than 512.
///   4. sRGB conversion — converts to linear colour space.
///   5. Mipmap count    — verifies CountMipmaps() returns a sensible value.
///   6. Compression     — compresses to BC7 with in-memory output.
///   7. Mip chain       — generates and compresses the full mip chain.
///   8. Error callback  — installs a global message callback and verifies
///                        it doesn't crash.
///   9. Output to file  — re-runs the full pipeline writing a real .dds file
///                        to the system temp folder.
/// </summary>
internal sealed unsafe class NvttBindingTest : ITest
{
    private const string _IMAGE_PATH = @"C:/Users/Misaki/Downloads/Screenshot 2024-07-20 035047.png";

    private string _outputDdsPath = string.Empty;

    public void Setup()
    {
        _outputDdsPath = Path.Combine(Path.GetTempPath(), $"nvtt_test_{Guid.NewGuid():N}.dds");
        Console.WriteLine("[NvttBindingTest] Setup complete.");
        Console.WriteLine($"[NvttBindingTest] Input image : {_IMAGE_PATH}");
        Console.WriteLine($"[NvttBindingTest] Output DDS  : {_outputDdsPath}");
    }

    public void Run()
    {
        // ---- Test 1: Version ---------------------------------------------------
        Console.Write("[Test 1] nvttVersion ... ");
        var version = NvttGlobal.Version;
        Assert(version > 0, $"Expected version > 0, got {version}");
        Console.WriteLine($"OK  (version = {version >> 16}.{(version >> 8) & 0xFF}.{version & 0xFF})");

        // ---- Test 2: CUDA support query (must not crash) ----------------------
        Console.Write("[Test 2] IsCudaSupported ... ");
        var cuda = NvttGlobal.IsCudaSupported;
        Console.WriteLine($"OK  (cuda = {cuda})");

        // ---- Test 3: Global message callback ----------------------------------
        Console.Write("[Test 3] SetMessageCallback ... ");
        var callbackFired = 0;
        using (var token = NvttGlobal.SetMessageCallback((severity, error, msg) =>
        {
            callbackFired++;
            Console.WriteLine($"/n         [NVTT] [{severity}] {error}: {msg}");
        }))
        {
            // Just install + dispose — no assertion needed; must not throw.
        }
        Console.WriteLine($"OK  (no crash, callback fired {callbackFired} times during install)");

        // ---- Test 4: Surface creation + load ----------------------------------
        Console.Write("[Test 4] NvttSurface.Load ... ");
        Assert(File.Exists(_IMAGE_PATH),
            $"Image not found: '{_IMAGE_PATH}'. Edit _IMAGE_PATH before running.");

        using var surface = new NvttSurfaceHandle();
        var loaded = surface.Load(_IMAGE_PATH, out var hasAlpha);
        Assert(loaded, "nvttSurfaceLoad returned false");
        Assert(!surface.IsNull, "Surface is null after load");
        Assert(surface.Width > 0 && surface.Height > 0,
            $"Bad dimensions after load: {surface.Width}x{surface.Height}");
        Console.WriteLine($"OK  ({surface.Width}x{surface.Height}, hasAlpha={hasAlpha})");

        // ---- Test 5: Resize to power-of-two ≤ 512 ----------------------------
        Console.Write("[Test 5] ResizeMakeSquare ... ");
        surface.ResizeMakeSquare(512,
            NvttRoundMode.NVTT_RoundMode_ToPreviousPowerOfTwo,
            NvttResizeFilter.NVTT_ResizeFilter_Box);
        Assert(surface.Width <= 512 && surface.Height <= 512,
            $"Expected ≤512 after resize, got {surface.Width}x{surface.Height}");
        Assert(IsPowerOfTwo(surface.Width) && IsPowerOfTwo(surface.Height),
            $"Expected power-of-two after resize, got {surface.Width}x{surface.Height}");
        Console.WriteLine($"OK  ({surface.Width}x{surface.Height})");

        // ---- Test 6: sRGB → linear conversion ---------------------------------
        Console.Write("[Test 6] ToLinearFromSrgb ... ");
        surface.ToLinearFromSrgb(); // must not crash
        Console.WriteLine("OK");

        // ---- Test 7: CountMipmaps ---------------------------------------------
        Console.Write("[Test 7] CountMipmaps ... ");
        var mipCount = surface.CountMipmaps();
        var expectedMax = (int)Math.Log2(Math.Max(surface.Width, surface.Height)) + 1;
        Assert(mipCount > 0 && mipCount <= expectedMax,
            $"Unexpected mip count: {mipCount} (expected 1..{expectedMax})");
        Console.WriteLine($"OK  ({mipCount} levels)");

        // ---- Test 8: In-memory BC7 compression + mip chain -------------------
        Console.Write("[Test 8] Compress BC7 in-memory ... ");
        long totalBytesReceived = 0;
        var imagesBegun = 0;

        using var compOpts = new NvttCompressionOptionsHandle();
        compOpts.Format = NvttFormat.NVTT_Format_BC7;
        compOpts.Quality = NvttQuality.NVTT_Quality_Fastest;

        using var outOpts = new NvttOutputOptionsHandle();
        outOpts.OutputHeader = true;
        outOpts.Srgb = true;
        outOpts.Container = NvttContainer.NVTT_Container_DDS10;

        outOpts.SetOutputHandler(
            beginImage: (size, w, h, d, face, mip) =>
            {
                imagesBegun++;
            },
            outputData: (ptr, len) =>
            {
                totalBytesReceived += len;
                return true;
            },
            endImage: null
        );
        outOpts.SetErrorHandler(err =>
            Console.WriteLine($"/n         [NVTT Error] {err}"));

        using var ctx = new NvttContextHandle();
        ctx.SetCudaAcceleration(false); // CPU only for the test

        using var mip = surface.Clone();
        var headerOk = ctx.OutputHeader(mip, mipCount, compOpts, outOpts);
        Assert(headerOk, "OutputHeader returned false");

        for (var level = 0; level < mipCount; level++)
        {
            var compressOk = ctx.Compress(mip, face: 0, mipmap: level, compOpts, outOpts);
            Assert(compressOk, $"Compress returned false at mip level {level}");

            if (level + 1 < mipCount)
                mip.BuildNextMipmap(NvttMipmapFilter.NVTT_MipmapFilter_Kaiser);
        }

        Assert(imagesBegun == mipCount,
            $"Expected {mipCount} beginImage callbacks, got {imagesBegun}");
        Assert(totalBytesReceived > 0,
            $"No bytes received from output handler");
        Console.WriteLine($"OK  ({imagesBegun} mips, {totalBytesReceived:N0} bytes total)");

        // ---- Test 9: EstimateSize consistency ---------------------------------
        Console.Write("[Test 9] EstimateSize ... ");
        var estimated = ctx.EstimateSize(surface, mipCount, compOpts);
        // Estimate can differ from actual due to header overhead; just sanity-check it's > 0.
        Assert(estimated > 0, $"EstimateSize returned {estimated}");
        Console.WriteLine($"OK  (estimated = {estimated:N0} bytes, actual = {totalBytesReceived:N0} bytes)");

        // ---- Test 10: Output to real DDS file ---------------------------------
        Console.Write("[Test 10] Compress to file ... ");
        using var outOptsFile = new NvttOutputOptionsHandle();
        outOptsFile.OutputHeader = true;
        outOptsFile.Srgb = true;
        outOptsFile.Container = NvttContainer.NVTT_Container_DDS10;
        outOptsFile.FileName = _outputDdsPath;

        using var ctxFile = new NvttContextHandle();
        using var mipFile = surface.Clone();

        var fileHeaderOk = ctxFile.OutputHeader(mipFile, mipCount, compOpts, outOptsFile);
        Assert(fileHeaderOk, "File OutputHeader returned false");

        for (var level = 0; level < mipCount; level++)
        {
            var ok = ctxFile.Compress(mipFile, face: 0, mipmap: level, compOpts, outOptsFile);
            Assert(ok, $"File Compress returned false at level {level}");

            if (level + 1 < mipCount)
                mipFile.BuildNextMipmap(NvttMipmapFilter.NVTT_MipmapFilter_Kaiser);
        }

        Assert(File.Exists(_outputDdsPath),
            $"DDS output file was not created: {_outputDdsPath}");
        var fileSize = new FileInfo(_outputDdsPath).Length;
        Assert(fileSize > 128, $"DDS file suspiciously small: {fileSize} bytes");
        Console.WriteLine($"OK  ({fileSize:N0} bytes → {_outputDdsPath})");

        Console.WriteLine("/n[NvttBindingTest] All tests PASSED.");
    }

    public void Cleanup()
    {
        // Leave the DDS file in place so you can inspect it.
        Console.WriteLine($"/n[NvttBindingTest] Output DDS left at: {_outputDdsPath}");
    }

    // -------------------------------------------------------------------------

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException($"[ASSERTION FAILED] {message}");
    }

    private static bool IsPowerOfTwo(int n)
        => n > 0 && (n & (n - 1)) == 0;
}
