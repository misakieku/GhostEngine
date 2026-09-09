using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public class ShaderLibraryTest
{
    private class MockPipelineLibrary : IPipelineLibrary
    {
        public List<ulong> EvictedHashes { get; } = new();

        public Result<Key128<PipelineState>> CreateComputePipeline(scoped in ComputePSODesc desc) => Result<Key128<PipelineState>>.Failure();
        public Result<Key128<PipelineState>> CreateGraphicsPipeline(scoped in GraphicsPSODesc desc) => Result<Key128<PipelineState>>.Failure();

        public void EvictStalePipelines(ulong compiledHash)
        {
            EvictedHashes.Add(compiledHash);
        }

        public bool HasPipelineStateObject(UInt128 key) => false;
        public void SaveLibraryToDisk(string filePath) { }
        public void BeginFrame(ulong submittedFrame) { }
        public void EndFrame(ulong completedFrame) { }
        public void Dispose() { }
    }

    private class MockShaderCompilationBridge : IShaderCompilationBridge
    {
        public List<(ulong id, int passIndex)> Requests { get; } = new();
        public event ShaderCompiledHandler? OnShaderCompiled;
        public event Action<ulong>? OnShaderInvalidated;

        public void RequestCompilation(ulong shaderId, int passIndex)
        {
            Requests.Add((shaderId, passIndex));
        }

        public void TriggerCompiled(ulong shaderId, int passIndex, ReadOnlySpan<ShaderByteCode> byteCodes)
        {
            OnShaderCompiled?.Invoke(shaderId, passIndex, byteCodes);
        }

        public void TriggerInvalidated(ulong shaderId)
        {
            OnShaderInvalidated?.Invoke(shaderId);
        }

        public void Dispose()
        {
        }
    }

    [TestMethod]
    public unsafe void TestInvalidateShaderCache_EvictsPipelinesAndClearsCache()
    {
        // Arrange
        var mockPipelineLibrary = new MockPipelineLibrary();
        using var shaderLibrary = new ShaderLibrary(null, mockPipelineLibrary, "TestShaderCache");

        ulong testShaderId = 12345;
        var testPassIndex = 0;

        // Create some dummy bytecode to cache
        var fakeData = new byte[] { 1, 2, 3, 4 };
        var expectedHash = 0UL;

        fixed (byte* pData = fakeData)
        {
            var byteCode = new ShaderByteCode
            {
                pCode = pData,
                size = (ulong)fakeData.Length
            };

            var byteCodes = new Span<ShaderByteCode>(ref byteCode);

            // Compute hash that should be generated (only bytecode)
            var dataSpan = new ReadOnlySpan<byte>(byteCode.pCode, (int)byteCode.size);
            expectedHash = System.IO.Hashing.XxHash64.HashToUInt64(dataSpan);

            // Act: Cache it
            shaderLibrary.CacheCompiledResult(testShaderId, testPassIndex, byteCodes);
        }

        // Verify it was cached successfully
        var cachedResult = shaderLibrary.GetCompiledCache(testShaderId, testPassIndex);
        Assert.IsTrue(cachedResult.IsSuccess, "Shader should be cached");
        Assert.AreEqual(expectedHash, cachedResult.Value.compiledHash);

        // Act: Invalidate
        shaderLibrary.InvalidateShaderCache(testShaderId);

        // Assert: EvictStalePipelines should be called
        Assert.HasCount(1, mockPipelineLibrary.EvictedHashes);
        Assert.AreEqual(expectedHash, mockPipelineLibrary.EvictedHashes[0]);

        // Assert: Cache should be cleared
        var cachedResultAfter = shaderLibrary.GetCompiledCache(testShaderId, testPassIndex);
        Assert.IsFalse(cachedResultAfter.IsSuccess, "Cache should be invalidated");
    }

    [TestMethod]
    public void TestGetCompiledHash_TriggersCompilationRequest()
    {
        // Arrange
        var mockBridge = new MockShaderCompilationBridge();
        using var shaderLibrary = new ShaderLibrary(mockBridge, null, "TestShaderCache");
        var testShaderId = 555UL;
        var passIndex = 1;

        // Act
        var result = shaderLibrary.GetCompiledHash(testShaderId, passIndex);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(Error.NotFound, result.Error);
        Assert.HasCount(1, mockBridge.Requests);
        Assert.AreEqual(testShaderId, mockBridge.Requests[0].id);
        Assert.AreEqual(passIndex, mockBridge.Requests[0].passIndex);
    }

    [TestMethod]
    public unsafe void TestOnShaderCompiled_UpdatesHashCache()
    {
        // Arrange
        var mockBridge = new MockShaderCompilationBridge();
        using var shaderLibrary = new ShaderLibrary(mockBridge, null, "TestShaderCache");

        var fakeData = new byte[] { 1, 2, 3, 4 };
        var expectedHash = 0UL;

        // Act
        fixed (byte* pData = fakeData)
        {
            var byteCode = new ShaderByteCode { pCode = pData, size = (ulong)fakeData.Length };

            // Compute expected hash of bytecode
            var dataSpan = new ReadOnlySpan<byte>(pData, fakeData.Length);
            expectedHash = System.IO.Hashing.XxHash64.HashToUInt64(dataSpan);

            mockBridge.TriggerCompiled(0, 0, new ReadOnlySpan<ShaderByteCode>(ref byteCode));
        }

        // Assert
        var result = shaderLibrary.GetCompiledHash(0, 0);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(expectedHash, result.Value);
    }

    [TestMethod]
    public void TestGetCompiledCache_HandlesIndexOutOfBounds()
    {
        // Arrange
        using var shaderLibrary = new ShaderLibrary(null, null, "TestShaderCache");
        var testShaderId = 111UL;

        // Act
        var result = shaderLibrary.GetCompiledCache(testShaderId, 99);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(Error.NotFound, result.Error);
    }

    [TestMethod]
    public void PipelineKeysIncludePassIdentityAndPipelineKind()
    {
        const ulong compiledHash = 0x123456789ABCDEF0UL;
        var firstPassId = ShaderIdentity.GetPassId(ShaderIdentity.GetShaderId("First"), 0);
        var secondPassId = ShaderIdentity.GetPassId(ShaderIdentity.GetShaderId("Second"), 0);
        var attachments = new PassAttachmentHash(new TextureFormat[] { TextureFormat.R8G8B8A8_UNorm }, TextureFormat.D32_Float);

        var firstGraphics = RHIUtility.CreateGraphicsPipelineKey(firstPassId, compiledHash, PipelineState.Default, attachments);
        var secondGraphics = RHIUtility.CreateGraphicsPipelineKey(secondPassId, compiledHash, PipelineState.Default, attachments);
        var firstCompute = RHIUtility.CreateComputePipelineKey(firstPassId, compiledHash);
        var secondCompute = RHIUtility.CreateComputePipelineKey(secondPassId, compiledHash);

        Assert.AreNotEqual(firstGraphics, secondGraphics);
        Assert.AreNotEqual(firstCompute, secondCompute);
        Assert.AreNotEqual(firstGraphics, firstCompute);
    }

    [TestMethod]
    public unsafe void CachedBytecodeOffsetsAreRelativeToBytecodePayload()
    {
        using var shaderLibrary = new ShaderLibrary(null, null, "TestShaderCache");
        var meshCode = new byte[] { 1, 2, 3 };
        var pixelCode = new byte[] { 4, 5 };

        fixed (byte* pMeshCode = meshCode)
        fixed (byte* pPixelCode = pixelCode)
        {
            var byteCodes = stackalloc ShaderByteCode[2];
            byteCodes[0] = new ShaderByteCode { pCode = pMeshCode, size = (ulong)meshCode.Length };
            byteCodes[1] = new ShaderByteCode { pCode = pPixelCode, size = (ulong)pixelCode.Length };
            shaderLibrary.CacheCompiledResult(42, 0, new ReadOnlySpan<ShaderByteCode>(byteCodes, 2));
        }

        var result = shaderLibrary.GetCompiledCache(42, 0);
        Assert.IsTrue(result.IsSuccess);
        ShaderLibrary.ParseCacheData(result.Value.byteCode, out _, out var offsets, out var bytes);

        Assert.AreEqual(2, offsets.Length);
        Assert.AreEqual(0UL, offsets[0]);
        Assert.AreEqual(3UL, offsets[1]);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, bytes.ToArray());
    }

    [TestMethod]
    public unsafe void PublishCompiledGenerationAtomicallySwapsPassesAndDefersCacheRetirement()
    {
        var pipelineLibrary = new MockPipelineLibrary();
        using var shaderLibrary = new ShaderLibrary(null, pipelineLibrary, "TestShaderCache");
        var firstMesh = new byte[] { 1, 2 };
        var firstPixel = new byte[] { 3 };
        var secondCompute = new byte[] { 4, 5, 6 };
        var reloadedMesh = new byte[] { 7 };
        var reloadedPixel = new byte[] { 8, 9 };
        var reloadedCompute = new byte[] { 10 };

        fixed (byte* pFirstMesh = firstMesh)
        fixed (byte* pFirstPixel = firstPixel)
        fixed (byte* pSecondCompute = secondCompute)
        {
            var byteCodes = stackalloc ShaderByteCode[3];
            byteCodes[0] = new ShaderByteCode { pCode = pFirstMesh, size = (ulong)firstMesh.Length };
            byteCodes[1] = new ShaderByteCode { pCode = pFirstPixel, size = (ulong)firstPixel.Length };
            byteCodes[2] = new ShaderByteCode { pCode = pSecondCompute, size = (ulong)secondCompute.Length };
            var offsets = stackalloc int[] { 0, 2, 3 };
            Assert.IsTrue(shaderLibrary.PublishCompiledGeneration(42, new ReadOnlySpan<int>(offsets, 3), new ReadOnlySpan<ShaderByteCode>(byteCodes, 3)).IsSuccess);
        }

        var firstPassHash = shaderLibrary.GetCompiledHash(42, 0).Value;
        var secondPassHash = shaderLibrary.GetCompiledHash(42, 1).Value;
        shaderLibrary.BeginFrame(7);

        fixed (byte* pReloadedMesh = reloadedMesh)
        fixed (byte* pReloadedPixel = reloadedPixel)
        fixed (byte* pReloadedCompute = reloadedCompute)
        {
            var byteCodes = stackalloc ShaderByteCode[3];
            byteCodes[0] = new ShaderByteCode { pCode = pReloadedMesh, size = (ulong)reloadedMesh.Length };
            byteCodes[1] = new ShaderByteCode { pCode = pReloadedPixel, size = (ulong)reloadedPixel.Length };
            byteCodes[2] = new ShaderByteCode { pCode = pReloadedCompute, size = (ulong)reloadedCompute.Length };
            var offsets = stackalloc int[] { 0, 2, 3 };
            Assert.IsTrue(shaderLibrary.PublishCompiledGeneration(42, new ReadOnlySpan<int>(offsets, 3), new ReadOnlySpan<ShaderByteCode>(byteCodes, 3)).IsSuccess);
        }

        Assert.AreNotEqual(firstPassHash, shaderLibrary.GetCompiledHash(42, 0).Value);
        Assert.AreNotEqual(secondPassHash, shaderLibrary.GetCompiledHash(42, 1).Value);
        CollectionAssert.AreEquivalent(new ulong[] { firstPassHash, secondPassHash }, pipelineLibrary.EvictedHashes);
        Assert.AreEqual(1, shaderLibrary.RetiredCacheCount);

        shaderLibrary.EndFrame(6);
        Assert.AreEqual(1, shaderLibrary.RetiredCacheCount);
        shaderLibrary.EndFrame(7);
        Assert.AreEqual(0, shaderLibrary.RetiredCacheCount);
    }

    [TestMethod]
    public unsafe void InvalidCompiledGenerationPreservesPublishedPasses()
    {
        var pipelineLibrary = new MockPipelineLibrary();
        using var shaderLibrary = new ShaderLibrary(null, pipelineLibrary, "TestShaderCache");
        var initialCode = new byte[] { 1, 2, 3 };
        var replacementCode = new byte[] { 4 };

        fixed (byte* pInitialCode = initialCode)
        {
            var byteCode = new ShaderByteCode { pCode = pInitialCode, size = (ulong)initialCode.Length };
            var offsets = stackalloc int[] { 0, 1 };
            Assert.IsTrue(shaderLibrary.PublishCompiledGeneration(12, new ReadOnlySpan<int>(offsets, 2), new ReadOnlySpan<ShaderByteCode>(ref byteCode)).IsSuccess);
        }

        var publishedHash = shaderLibrary.GetCompiledHash(12, 0).Value;
        fixed (byte* pReplacementCode = replacementCode)
        {
            var byteCode = new ShaderByteCode { pCode = pReplacementCode, size = (ulong)replacementCode.Length };
            var invalidOffsets = stackalloc int[] { 0, 2 };
            var result = shaderLibrary.PublishCompiledGeneration(12, new ReadOnlySpan<int>(invalidOffsets, 2), new ReadOnlySpan<ShaderByteCode>(ref byteCode));
            Assert.IsTrue(result.IsFailure);
        }

        Assert.AreEqual(publishedHash, shaderLibrary.GetCompiledHash(12, 0).Value);
        Assert.IsEmpty(pipelineLibrary.EvictedHashes);
        Assert.AreEqual(0, shaderLibrary.RetiredCacheCount);
    }

    [TestMethod]
    public unsafe void ComputeGenerationPublishesEachEntryPointAsIndependentCache()
    {
        using var shaderLibrary = new ShaderLibrary(null, null, "TestShaderCache");
        var firstEntry = new byte[] { 1, 2 };
        var secondEntry = new byte[] { 3, 4, 5 };

        fixed (byte* pFirstEntry = firstEntry)
        fixed (byte* pSecondEntry = secondEntry)
        {
            var byteCodes = stackalloc ShaderByteCode[2];
            byteCodes[0] = new ShaderByteCode { pCode = pFirstEntry, size = (ulong)firstEntry.Length };
            byteCodes[1] = new ShaderByteCode { pCode = pSecondEntry, size = (ulong)secondEntry.Length };
            var offsets = stackalloc int[] { 0, 1, 2 };
            Assert.IsTrue(shaderLibrary.PublishCompiledGeneration(84, new ReadOnlySpan<int>(offsets, 3), new ReadOnlySpan<ShaderByteCode>(byteCodes, 2)).IsSuccess);
        }

        var firstCache = shaderLibrary.GetCompiledCache(84, 0);
        var secondCache = shaderLibrary.GetCompiledCache(84, 1);
        Assert.IsTrue(firstCache.IsSuccess);
        Assert.IsTrue(secondCache.IsSuccess);
        ShaderLibrary.ParseCacheData(firstCache.Value.byteCode, out _, out _, out var firstBytes);
        ShaderLibrary.ParseCacheData(secondCache.Value.byteCode, out _, out _, out var secondBytes);
        CollectionAssert.AreEqual(firstEntry, firstBytes.ToArray());
        CollectionAssert.AreEqual(secondEntry, secondBytes.ToArray());
    }
}
