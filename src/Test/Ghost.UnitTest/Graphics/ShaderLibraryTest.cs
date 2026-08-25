using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;

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
}
