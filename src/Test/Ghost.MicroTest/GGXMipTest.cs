using Ghost.AssetForge.Core.Bakers;
using Ghost.Core;
using Ghost.MicroTest.Core;
using Ghost.Nvtt;
using Ghost.StbI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ghost.MicroTest;

internal class GGXMipTest : ITest
{
    public void Setup()
    {
        AllocationManager.Initialize();
    }

    public void Cleanup()
    {
        AllocationManager.Dispose();
    }



    public unsafe void Run()
    {
        var inputPath = @"src/Test/TestGame/Assets/Textures/resting_place_4k.hdr";
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"File not found: {inputPath}");
            return;
        }

        Console.WriteLine($"Loading HDR: {inputPath}...");
        using var stream = File.OpenRead(inputPath);
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);

        int width, height, channels;
        var pixels = StbIApi.LoadfFromMemory(bytes, &width, &height, &channels, 4);

        if (pixels == null)
        {
            Console.WriteLine("Failed to decode HDR image.");
            return;
        }

        Console.WriteLine($"Loaded HDR: {width}x{height}, channels={channels}");

        var dumpDir = Path.Combine(Directory.GetCurrentDirectory(), "dump_cubemap");
        Directory.CreateDirectory(dumpDir);

        try
        {
            var edge = 512;
            Console.WriteLine($"Projecting equirectangular map to cubemap with edge={edge}...");
            var maxCubeMips = (int)Math.Floor(Math.Log2(edge)) + 1;
            var pixelsPerFace = edge * edge;
            var faceSize = pixelsPerFace * 4;
            using var baseCubeData = new UnsafeArray<float>(faceSize * 6, AllocationHandle.TLSF);

            TextureBaker.ProjectEquirectangularToCubemap(pixels, width, height, (float*)baseCubeData.GetUnsafePtr(), edge);

            var baseFace0Path = Path.Combine(dumpDir, "base_face0.hdr");
            fixed (byte* pPath = Encoding.UTF8.GetBytes(baseFace0Path + "\0"))
            {
                StbIApi.WriteHdr((sbyte*)pPath, edge, edge, 4, (float*)baseCubeData.GetUnsafePtr());
            }
            Console.WriteLine($"Saved base cubemap face 0 to: {baseFace0Path}");

            static void SaveHdrAsPng(string path, int w, int h, float* hdrData)
            {
                var ldr = new byte[w * h * 4];
                for (int i = 0; i < w * h; i++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        float v = hdrData[i * 4 + c];
                        float mapped = v / (1.0f + v);
                        float srgb = MathF.Pow(Math.Clamp(mapped, 0.0f, 1.0f), 1.0f / 2.2f);
                        ldr[i * 4 + c] = (byte)(srgb * 255.0f);
                    }
                    ldr[i * 4 + 3] = 255;
                }
                fixed (byte* pPath = Encoding.UTF8.GetBytes(path + "\0"))
                fixed (byte* pLdr = ldr)
                {
                    StbIApi.WritePng((sbyte*)pPath, w, h, 4, pLdr, w * 4);
                }
            }

            for (var f = 0; f < 6; f++)
            {
                var pFaceData = (float*)baseCubeData.GetUnsafePtr() + f * faceSize;
                SaveHdrAsPng(Path.Combine(dumpDir, $"base_face{f}.png"), edge, edge, pFaceData);
            }

            // Now test GenerateMipHDRI with a limited edge (e.g. 128 or 256 for quick test)
            var textureInfo = new TextureInfo
            {
                pixelData = (IntPtr)pixels,
                width = width,
                height = height,
                depth = 1,
                bitsPerChannel = 32,
                colorComponents = 4,
                isCube = true
            };

            Console.WriteLine("Running GenerateMipHDRI...");
            var mipLevels = TextureBaker.GenerateMipHDRI(textureInfo, baseCubeData, edge, Math.Min(4, maxCubeMips));

            for (var level = 0; level < Math.Min(4, maxCubeMips); level++)
            {
                var w = mipLevels[level].width;
                var outFace0Hdr = Path.Combine(dumpDir, $"mip_{level}_face0.hdr");
                var outFace0Png = Path.Combine(dumpDir, $"mip_{level}_face0.png");
                fixed (byte* pPath = Encoding.UTF8.GetBytes(outFace0Hdr + "\0"))
                {
                    StbIApi.WriteHdr((sbyte*)pPath, w, w, 4, (float*)mipLevels[level].data.GetUnsafePtr());
                }
                SaveHdrAsPng(outFace0Png, w, w, (float*)mipLevels[level].data.GetUnsafePtr());
                Console.WriteLine($"Saved mip {level} face 0 (size {w}x{w}) to: {outFace0Png}");
                mipLevels[level].data.Dispose();
            }
            mipLevels.Dispose();

            Console.WriteLine("Done! Check dump_cubemap directory.");
        }
        finally
        {
            StbIApi.ImageFree(pixels);
        }
    }
}
