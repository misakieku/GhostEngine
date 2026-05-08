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

        public Result<Key128<PipelineState>> CreateComputePipeline(ref readonly ComputePSODesc desc) => Result<Key128<PipelineState>>.Failure();
        public Result<Key128<PipelineState>> CreateGraphicsPipeline(ref readonly GraphicsPSODesc desc) => Result<Key128<PipelineState>>.Failure();

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
        public List<(ulong id, int passIndex, Key64<ShaderVariant> variantKey)> Requests { get; } = new();
        public event Action<Key64<ShaderVariant>, ulong>? OnShaderVariantCompiled;

        public void RequestCompilation(ulong shaderId, int passIndex, Key64<ShaderVariant> variantKey)
        {
            Requests.Add((shaderId, passIndex, variantKey));
        }

        public void TriggerCompiled(Key64<ShaderVariant> variantKey, ulong newHash)
        {
            OnShaderVariantCompiled?.Invoke(variantKey, newHash);
        }
    }

    [TestInitialize]
    public void Setup()
    {
        AllocationManager.Initialize(AllocationManagerDesc.Default);
    }

    [TestCleanup]
    public void Cleanup()
    {
        AllocationManager.Dispose();
    }

    [TestMethod]
    public unsafe void TestInvalidateShaderCache_EvictsPipelinesAndClearsCache()
    {
        // Arrange
        using var shaderLibrary = new ShaderLibrary(null, "TestShaderCache");
        var mockPipelineLibrary = new MockPipelineLibrary();

        ulong testShaderId = 12345;
        var testPassIndex = 0;
        var variantKey = new Key64<ShaderVariant>(999);

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
            shaderLibrary.CacheCompiledResult(testShaderId, testPassIndex, variantKey, byteCodes);
        }

        // Verify it was cached successfully
        var cachedResult = shaderLibrary.GetCompiledCache(testShaderId, testPassIndex);
        Assert.IsTrue(cachedResult.IsSuccess, "Shader should be cached");
        Assert.AreEqual(expectedHash, cachedResult.Value.compiledHash);

        // Act: Invalidate
        shaderLibrary.InvalidateShaderCache(testShaderId, mockPipelineLibrary);

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
        using var shaderLibrary = new ShaderLibrary(mockBridge, "TestShaderCache");
        var testShaderId = 555UL;
        var passIndex = 1;
        var variantKey = new Key64<ShaderVariant>(777);

        // Act
        var result = shaderLibrary.GetCompiledHash(testShaderId, passIndex, variantKey);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(Error.NotFound, result.Error);
        Assert.HasCount(1, mockBridge.Requests);
        Assert.AreEqual(testShaderId, mockBridge.Requests[0].id);
        Assert.AreEqual(passIndex, mockBridge.Requests[0].passIndex);
        Assert.AreEqual(variantKey, mockBridge.Requests[0].variantKey);
    }

    [TestMethod]
    public void TestOnVariantCompiled_UpdatesHashCache()
    {
        // Arrange
        var mockBridge = new MockShaderCompilationBridge();
        using var shaderLibrary = new ShaderLibrary(mockBridge, "TestShaderCache");
        var variantKey = new Key64<ShaderVariant>(123);
        ulong newHash = 0xABCDE;

        // Act
        mockBridge.TriggerCompiled(variantKey, newHash);

        // Assert
        var result = shaderLibrary.GetCompiledHash(0, 0, variantKey);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(newHash, result.Value);
    }

    [TestMethod]
    public void TestGetCompiledCache_HandlesIndexOutOfBounds()
    {
        // Arrange
        using var shaderLibrary = new ShaderLibrary(null, "TestShaderCache");
        ulong testShaderId = 111;
        var scope = AllocationManager.CreateStackScope();

        // Act
        var result = shaderLibrary.GetCompiledCache(testShaderId, 99);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(Error.NotFound, result.Error);

        scope.Dispose();
    }
}
