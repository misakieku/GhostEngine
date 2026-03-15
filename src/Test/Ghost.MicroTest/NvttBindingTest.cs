using Ghost.Nvtt;
using Ghost.Test.Core;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

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
///   7. Mip chain       — generates and compresses the full pMip chain.
///   8. Error callback  — installs a global message callback and verifies
///                        it doesn't crash.
///   9. Output to file  — re-runs the full pipeline writing a real .dds file
///                        to the system temp folder.
/// </summary>
internal sealed unsafe class NvttBindingTest : ITest
{
    private const string _IMAGE_PATH = @"C:/Users/Misaki/Downloads/Screenshot 2024-07-20 035047.png";
    private static ReadOnlySpan<byte> ImagePathByte => @"C:/Users/Misaki/Downloads/Screenshot 2024-07-20 035047.png"u8;

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
        var version = Api.nvttVersion();
        Assert(version > 0, $"Expected version > 0, got {version}");
        Console.WriteLine($"OK  (version = {version >> 16}.{(version >> 8) & 0xFF}.{version & 0xFF})");

        // ---- Test 2: CUDA support query (must not crash) ----------------------
        Console.Write("[Test 2] IsCudaSupported ... ");
        var cuda = Api.nvttIsCudaSupported();
        Console.WriteLine($"OK  (cuda = {cuda})");

        // ---- Test 3: Global message callback ----------------------------------
        Console.Write("[Test 3] SetMessageCallback ... ");
        var callbackFired = 0;
        var token = Api.nvttSetMessageCallback(&CallBack, &callbackFired);

        Console.WriteLine($"OK  (no crash, callback fired {callbackFired} times during install)");

        // ---- Test 4: Surface creation + load ----------------------------------
        Console.Write("[Test 4] NvttSurface.Load ... ");
        Assert(File.Exists(_IMAGE_PATH),
            $"Image not found: '{_IMAGE_PATH}'. Edit _IMAGE_PATH before running.");

        var pSurface = NvttSurface.Create();

        NvttBoolean hasAlpha;
        var loaded = pSurface->Load(ImagePathByte, &hasAlpha, false, null);

        Assert(loaded, "nvttSurfaceLoad returned false");
        Assert(pSurface != null, "Surface is null after load");
        Assert(pSurface->Width() > 0 && pSurface->Height() > 0,
            $"Bad dimensions after load: {pSurface->Width()}x{pSurface->Height()}");

        Console.WriteLine($"OK  ({pSurface->Width()}x{pSurface->Height()}, hasAlpha={hasAlpha})");

        // ---- Test 5: Resize to power-of-two ≤ 512 ----------------------------
        Console.Write("[Test 5] ResizeMakeSquare ... ");
        pSurface->ResizeMakeSquare(512,
            NvttRoundMode.NVTT_RoundMode_ToPreviousPowerOfTwo,
            NvttResizeFilter.NVTT_ResizeFilter_Box, null);

        Assert(pSurface->Width() <= 512 && pSurface->Height() <= 512,
            $"Expected ≤512 after resize, got {pSurface->Width()}x{pSurface->Height()}");
        Assert(IsPowerOfTwo(pSurface->Width()) && IsPowerOfTwo(pSurface->Height()),
            $"Expected power-of-two after resize, got {pSurface->Width()}x{pSurface->Height()}");

        Console.WriteLine($"OK  ({pSurface->Width()}x{pSurface->Height()})");

        // ---- Test 6: sRGB → linear conversion ---------------------------------
        Console.Write("[Test 6] ToLinearFromSrgb ... ");
        pSurface->ToLinearFromSrgb(null); // must not crash
        Console.WriteLine("OK");

        // ---- Test 7: CountMipmaps ---------------------------------------------
        Console.Write("[Test 7] CountMipmaps ... ");

        var mipCount = pSurface->CountMipmaps(1);
        var expectedMax = (int)Math.Log2(Math.Max(pSurface->Width(), pSurface->Height())) + 1;

        Assert(mipCount > 0 && mipCount <= expectedMax,
            $"Unexpected mip count: {mipCount} (expected 1..{expectedMax})");

        Console.WriteLine($"OK  ({mipCount} levels)");

        // ---- Test 8: In-memory BC7 compression + pMip chain -------------------
        Console.Write("[Test 8] Compress BC7 in-memory ... ");
        var totalBytesReceived = 0L;
        var imagesBegun = 0;

        var pCompOpts = NvttCompressionOptions.Create();
        pCompOpts->SetFormat(NvttFormat.NVTT_Format_BC7);
        pCompOpts->SetQuality(NvttQuality.NVTT_Quality_Fastest);

        var pOutOpts = NvttOutputOptions.Create();
        pOutOpts->SetOutputHeader(true);
        pOutOpts->SetSrgbFlag(true);
        pOutOpts->SetContainer(NvttContainer.NVTT_Container_DDS10);

        pOutOpts->SetOutputHandler(
            (size, w, h, d, face, mip) =>
            {
                imagesBegun++;
            },
            (ptr, len) =>
            {
                totalBytesReceived += len;
                return true;
            },
            null
        );
        pOutOpts->SetErrorHandler(err =>
            Console.WriteLine($"/n         [NVTT Error] {err}"));

        var pCtx = NvttContext.Create();
        pCtx->SetCudaAcceleration(false); // CPU only for the test

        var pMip = pSurface->Clone();
        var headerOk = pCtx->OutputHeader(pMip, mipCount, pCompOpts, pOutOpts);
        Assert(headerOk, "OutputHeader returned false");

        for (var level = 0; level < mipCount; level++)
        {
            var compressOk = pCtx->Compress(pMip, face: 0, mipmap: level, pCompOpts, pOutOpts);
            Assert(compressOk, $"Compress returned false at mip level {level}");

            if (level + 1 < mipCount)
            {
                pMip->BuildNextMipmapDefaults(NvttMipmapFilter.NVTT_MipmapFilter_Kaiser, 1, null);
            }
        }

        Assert(imagesBegun == mipCount,
            $"Expected {mipCount} beginImage callbacks, got {imagesBegun}");
        Assert(totalBytesReceived > 0,
            $"No bytes received from output handler");
        Console.WriteLine($"OK  ({imagesBegun} mips, {totalBytesReceived:N0} bytes total)");

        // ---- Test 9: EstimateSize consistency ---------------------------------

        Console.Write("[Test 9] EstimateSize ... ");
        var estimated = pCtx->EstimateSize(pSurface, mipCount, pCompOpts);

        // Estimate can differ from actual due to header overhead; just sanity-check it's > 0.
        Assert(estimated > 0, $"EstimateSize returned {estimated}");
        Console.WriteLine($"OK  (estimated = {estimated:N0} bytes, actual = {totalBytesReceived:N0} bytes)");

        // ---- Test 10: Output to real DDS file ---------------------------------
        Console.Write("[Test 10] Compress to file ... ");
        var pOutOptsFile = NvttOutputOptions.Create();
        pOutOptsFile->SetOutputHeader(true);
        pOutOptsFile->SetSrgbFlag(true);
        pOutOptsFile->SetContainer(NvttContainer.NVTT_Container_DDS10);
        pOutOptsFile->SetFileName(Encoding.UTF8.GetBytes(_outputDdsPath));

        var pCtxFile = NvttContext.Create();
        var pMipFile = pSurface->Clone();

        var fileHeaderOk = pCtxFile->OutputHeader(pMipFile, mipCount, pCompOpts, pOutOptsFile);
        Assert(fileHeaderOk, "File OutputHeader returned false");

        for (var level = 0; level < mipCount; level++)
        {
            var ok = pCtxFile->Compress(pMipFile, face: 0, mipmap: level, pCompOpts, pOutOptsFile);
            Assert(ok, $"File Compress returned false at level {level}");

            if (level + 1 < mipCount)
            {
                pMipFile->BuildNextMipmapDefaults(NvttMipmapFilter.NVTT_MipmapFilter_Kaiser, 1, null);
            }
        }

        Assert(File.Exists(_outputDdsPath), $"DDS output file was not created: {_outputDdsPath}");

        var fileSize = new FileInfo(_outputDdsPath).Length;
        Assert(fileSize > 128, $"DDS file suspiciously small: {fileSize} bytes");

        Console.WriteLine($"OK  ({fileSize:N0} bytes → {_outputDdsPath})");

        Console.WriteLine("[NvttBindingTest] All tests PASSED.");

        pMipFile->Dispose();
        pCtxFile->Dispose();
        pOutOptsFile->Dispose();
        pMip->Dispose();
        pCtx->Dispose();
        pOutOpts->Dispose();
        pCompOpts->Dispose();
        pSurface->Dispose();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void CallBack(NvttSeverity severity, NvttError error, sbyte* msg, void* userData)
    {
        (*(int*)userData)++;

        int i = 0;
        while (msg[i] != 0)
        {
            i++;
        }

        Console.WriteLine($"/n         [NVTT] [{severity}] {error}: {Encoding.UTF8.GetString((byte*)msg, i)}");
    }

    public void Cleanup()
    {
        Console.WriteLine($"[NvttBindingTest] Output DDS left at: {_outputDdsPath}");
    }

    // -------------------------------------------------------------------------

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"[ASSERTION FAILED] {message}");
        }
    }

    private static bool IsPowerOfTwo(int n)
        => n > 0 && (n & (n - 1)) == 0;
}
