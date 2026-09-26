using System;
using Ghost.Engine.RenderPipeline;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public class TileLightCullingTests
{
    private static void PackTileLights(Span<uint> tileDwords, ReadOnlySpan<uint> lightIndices, uint dwords)
    {
        uint maxLights = 2u * dwords - 1u;
        uint count = (uint)Math.Min(lightIndices.Length, (int)maxLights);

        // Slot 0: bits 0..15 = count, bits 16..31 = light 0
        uint firstLight = (count > 0) ? lightIndices[0] : 0u;
        tileDwords[0] = (count & 0xFFFFu) | (firstLight << 16);

        // Slots 1..dwords-1: pairs of 16-bit indices
        for (uint k = 1; k < dwords; ++k)
        {
            uint idxA = 2u * k - 1u;
            uint idxB = 2u * k;
            uint lightA = (idxA < count) ? lightIndices[(int)idxA] : 0u;
            uint lightB = (idxB < count) ? lightIndices[(int)idxB] : 0u;
            tileDwords[(int)k] = (lightA & 0xFFFFu) | (lightB << 16);
        }
    }

    private static uint UnpackTileLightCount(ReadOnlySpan<uint> tileDwords)
    {
        return tileDwords[0] & 0xFFFFu;
    }

    private static (uint lightIndex, bool valid) UnpackTileLight(ReadOnlySpan<uint> tileDwords, uint lightOffset, uint dwords)
    {
        if (lightOffset == 0)
        {
            uint dword0 = tileDwords[0];
            uint count = dword0 & 0xFFFFu;
            if (count > 0)
            {
                return (dword0 >> 16, true);
            }
            return (0, false);
        }

        uint dwordIndex = (lightOffset + 1u) / 2u;
        if (dwordIndex < dwords)
        {
            uint raw = tileDwords[(int)dwordIndex];
            uint index = ((lightOffset + 1u) & 1u) != 0 ? (raw >> 16) : (raw & 0xFFFFu);
            return (index, true);
        }

        return (0, false);
    }

    [TestMethod]
    public void TestTileGridDimensions_1080p()
    {
        var renderSize = new uint2(1920, 1080);
        var tilesX = (renderSize.x + 15u) / 16u;
        var tilesY = (renderSize.y + 15u) / 16u;

        Assert.AreEqual(120u, tilesX);
        Assert.AreEqual(68u, tilesY);

        var totalTiles = tilesX * tilesY;
        Assert.AreEqual(8160u, totalTiles);

        // 16 DWORDs per tile = 64 bytes per tile
        var size16 = totalTiles * 16u * 4u;
        Assert.AreEqual(522240u, size16); // ~510 KB

        // 32 DWORDs per tile = 128 bytes per tile
        var size32 = totalTiles * 32u * 4u;
        Assert.AreEqual(1044480u, size32); // ~1.02 MB
    }

    [TestMethod]
    public void TestTilePacking_EmptyTile()
    {
        Span<uint> tile16 = stackalloc uint[16];
        PackTileLights(tile16, ReadOnlySpan<uint>.Empty, 16);

        Assert.AreEqual(0u, UnpackTileLightCount(tile16));
        var (lightIndex, valid) = UnpackTileLight(tile16, 0, 16);
        Assert.IsFalse(valid);
        Assert.AreEqual(0u, lightIndex);
    }

    [TestMethod]
    public void TestTilePacking_SingleLight()
    {
        Span<uint> tile16 = stackalloc uint[16];
        Span<uint> lights = stackalloc uint[] { 42 };
        PackTileLights(tile16, lights, 16);

        Assert.AreEqual(1u, UnpackTileLightCount(tile16));

        var (light0, valid0) = UnpackTileLight(tile16, 0, 16);
        Assert.IsTrue(valid0);
        Assert.AreEqual(42u, light0);

        var (light1, valid1) = UnpackTileLight(tile16, 1, 16);
        Assert.IsTrue(valid1);
        Assert.AreEqual(0u, light1); // index 1 was not set
    }

    [TestMethod]
    public void TestTilePacking_FullCapacity_16Dwords_31Lights()
    {
        Span<uint> tile16 = stackalloc uint[16];
        Span<uint> lights = stackalloc uint[31];
        for (var i = 0; i < 31; ++i)
        {
            lights[i] = (uint)(100 + i);
        }

        PackTileLights(tile16, lights, 16);

        Assert.AreEqual(31u, UnpackTileLightCount(tile16));

        for (uint i = 0; i < 31; ++i)
        {
            var (light, valid) = UnpackTileLight(tile16, i, 16);
            Assert.IsTrue(valid, $"Light offset {i} should be valid");
            Assert.AreEqual(100u + i, light, $"Light offset {i} mismatch");
        }

        var (_, outOfBounds) = UnpackTileLight(tile16, 31, 16);
        Assert.IsFalse(outOfBounds);
    }

    [TestMethod]
    public void TestTilePacking_FullCapacity_32Dwords_63Lights()
    {
        Span<uint> tile32 = stackalloc uint[32];
        Span<uint> lights = stackalloc uint[63];
        for (var i = 0; i < 63; ++i)
        {
            lights[i] = (uint)(500 + i);
        }

        PackTileLights(tile32, lights, 32);

        Assert.AreEqual(63u, UnpackTileLightCount(tile32));

        for (uint i = 0; i < 63; ++i)
        {
            var (light, valid) = UnpackTileLight(tile32, i, 32);
            Assert.IsTrue(valid, $"Light offset {i} should be valid");
            Assert.AreEqual(500u + i, light, $"Light offset {i} mismatch");
        }

        var (_, outOfBounds) = UnpackTileLight(tile32, 63, 32);
        Assert.IsFalse(outOfBounds);
    }

    [TestMethod]
    public void TestTilePacking_ClampingExceedsCapacity()
    {
        Span<uint> tile16 = stackalloc uint[16];
        Span<uint> lights = stackalloc uint[50];
        for (var i = 0; i < 50; ++i)
        {
            lights[i] = (uint)(i + 1);
        }

        PackTileLights(tile16, lights, 16);

        // Max lights for 16 dwords is 31
        Assert.AreEqual(31u, UnpackTileLightCount(tile16));

        for (uint i = 0; i < 31; ++i)
        {
            var (light, valid) = UnpackTileLight(tile16, i, 16);
            Assert.IsTrue(valid);
            Assert.AreEqual(i + 1u, light);
        }
    }

    [TestMethod]
    public void TestReversedZ_LinearEyeDepthReconstruction()
    {
        float nearClip = 0.1f;
        float farClip = 1000.0f;

        // Reversed-Z projection matrix elements (from RenderPipeline.cs):
        // m_22 = near / (near - far)
        // m_23 = (far * near) / (far - near)
        float m22 = nearClip / (nearClip - farClip);
        float m23 = (farClip * nearClip) / (farClip - nearClip);

        // Near plane device depth = 1.0f
        float linearNear = m23 / (1.0f - m22);
        Assert.AreEqual(nearClip, linearNear, 1e-4f);

        // Far plane device depth = 0.0f
        float linearFar = m23 / (0.0f - m22);
        Assert.AreEqual(farClip, linearFar, 1e-2f);

        // Intermediate depth: device depth 0.5f
        float linearMid = m23 / (0.5f - m22);
        Assert.IsGreaterThan(nearClip, linearMid);
        Assert.IsLessThan(farClip, linearMid);
    }
}


