using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public class PipelineKeywordTests
{
    [TestMethod]
    public void TestKeywordMask128_BitOperations()
    {
        var mask = KeywordMask128.Empty;
        Assert.IsTrue(mask.IsEmpty);
        Assert.IsFalse(mask.Has(0));
        Assert.IsFalse(mask.Has(63));
        Assert.IsFalse(mask.Has(64));
        Assert.IsFalse(mask.Has(127));

        mask = mask.Set(0);
        mask = mask.Set(63);
        mask = mask.Set(64);
        mask = mask.Set(127);

        Assert.IsFalse(mask.IsEmpty);
        Assert.IsTrue(mask.Has(0));
        Assert.IsTrue(mask.Has(63));
        Assert.IsTrue(mask.Has(64));
        Assert.IsTrue(mask.Has(127));
        Assert.IsFalse(mask.Has(1));
        Assert.IsFalse(mask.Has(65));

        mask = mask.Unset(63);
        mask = mask.Unset(64);
        Assert.IsFalse(mask.Has(63));
        Assert.IsFalse(mask.Has(64));
        Assert.IsTrue(mask.Has(0));
        Assert.IsTrue(mask.Has(127));

        var maskA = KeywordMask128.Empty.Set(5).Set(70);
        var maskB = KeywordMask128.Empty.Set(70).Set(100);

        var combined = maskA | maskB;
        Assert.IsTrue(combined.Has(5));
        Assert.IsTrue(combined.Has(70));
        Assert.IsTrue(combined.Has(100));

        var intersected = maskA & maskB;
        Assert.IsFalse(intersected.Has(5));
        Assert.IsTrue(intersected.Has(70));
        Assert.IsFalse(intersected.Has(100));

        var xor = maskA ^ maskB;
        Assert.IsTrue(xor.Has(5));
        Assert.IsFalse(xor.Has(70));
        Assert.IsTrue(xor.Has(100));

        var inverted = ~maskA;
        Assert.IsFalse(inverted.Has(5));
        Assert.IsTrue(inverted.Has(0));
        Assert.IsFalse(inverted.Has(70));
        Assert.IsTrue(inverted.Has(71));
    }

    [TestMethod]
    public void TestPipelineKeywordRegistry_RegisterAndParseMask()
    {
        var kwPcf = PipelineKeywordRegistry.GetOrRegister("_SHADOW_PCF");
        var kwVsm = PipelineKeywordRegistry.GetOrRegister("_SHADOW_VSM");
        var kwRis = PipelineKeywordRegistry.GetOrRegister("_SHADOW_RIS");

        Assert.AreEqual("_SHADOW_PCF", kwPcf.Name);
        Assert.AreEqual("_SHADOW_VSM", kwVsm.Name);
        Assert.AreEqual("_SHADOW_RIS", kwRis.Name);

        Assert.IsTrue(kwPcf.Id >= 0 && kwPcf.Id < 128);
        Assert.IsTrue(kwVsm.Id >= 0 && kwVsm.Id < 128);
        Assert.IsTrue(kwRis.Id >= 0 && kwRis.Id < 128);
        Assert.AreNotEqual(kwPcf.Id, kwVsm.Id);
        Assert.AreNotEqual(kwVsm.Id, kwRis.Id);

        // Repeated registration should return identical ID
        var kwPcf2 = PipelineKeywordRegistry.GetOrRegister("_SHADOW_PCF");
        Assert.AreEqual(kwPcf.Id, kwPcf2.Id);

        // Parsing semicolon-delimited keywords
        var parsedSingle = PipelineKeywordRegistry.ParseMask("_SHADOW_PCF");
        Assert.AreEqual(kwPcf.Mask, parsedSingle);

        var parsedMulti = PipelineKeywordRegistry.ParseMask("_SHADOW_PCF;_SHADOW_VSM");
        var expectedCombined = kwPcf.Mask | kwVsm.Mask;
        Assert.AreEqual(expectedCombined, parsedMulti);

        var parsedEmpty = PipelineKeywordRegistry.ParseMask(string.Empty);
        Assert.AreEqual(KeywordMask128.Empty, parsedEmpty);

        var parsedNull = PipelineKeywordRegistry.ParseMask(null);
        Assert.AreEqual(KeywordMask128.Empty, parsedNull);
    }

    [TestMethod]
    public unsafe void TestShaderLibrary_MultiVariantResolution()
    {
        var library = new ShaderLibrary(null, null, null);
        try
        {
            var kwPcf = PipelineKeywordRegistry.GetOrRegister("_SHADOW_PCF_TEST");
            var kwVsm = PipelineKeywordRegistry.GetOrRegister("_SHADOW_VSM_TEST");
            var kwRis = PipelineKeywordRegistry.GetOrRegister("_SHADOW_RIS_TEST");
            var kwFog = PipelineKeywordRegistry.GetOrRegister("_FOG_HIGH_TEST");

            var supportedMask = kwPcf.Mask | kwVsm.Mask | kwRis.Mask;

            var dummyCodePcf = new byte[] { 1, 2, 3, 4 };
            var dummyCodeVsm = new byte[] { 5, 6, 7, 8 };
            var dummyCodeRis = new byte[] { 9, 10, 11, 12 };

            fixed (byte* pPcf = dummyCodePcf, pVsm = dummyCodeVsm, pRis = dummyCodeRis)
            {
                var variant0 = new CompiledVariantData(kwPcf.Mask, new[] { new ShaderByteCode { pCode = pPcf, size = (ulong)dummyCodePcf.Length } });
                var variant1 = new CompiledVariantData(kwVsm.Mask, new[] { new ShaderByteCode { pCode = pVsm, size = (ulong)dummyCodeVsm.Length } });
                var variant2 = new CompiledVariantData(kwRis.Mask, new[] { new ShaderByteCode { pCode = pRis, size = (ulong)dummyCodeRis.Length } });

                var passData = new CompiledPassData(supportedMask, new[] { variant0, variant1, variant2 });

                const ulong shaderId = 0x12345678ABCD;
                var publishResult = library.PublishCompiledGeneration(shaderId, new ReadOnlySpan<CompiledPassData>(ref passData));
                Assert.IsTrue(publishResult.IsSuccess);

                // Query with exact PCF keyword
                var hashPcf = library.GetCompiledHash(shaderId, 0, kwPcf.Mask).GetValueOrThrow();
                var cachePcf = library.GetCompiledCache(shaderId, 0, kwPcf.Mask).GetValueOrThrow();
                Assert.AreEqual(hashPcf, cachePcf.compiledHash);

                // Query with exact VSM keyword
                var hashVsm = library.GetCompiledHash(shaderId, 0, kwVsm.Mask).GetValueOrThrow();
                var cacheVsm = library.GetCompiledCache(shaderId, 0, kwVsm.Mask).GetValueOrThrow();
                Assert.AreEqual(hashVsm, cacheVsm.compiledHash);
                Assert.AreNotEqual(hashPcf, hashVsm);

                // Query with exact RIS keyword
                var hashRis = library.GetCompiledHash(shaderId, 0, kwRis.Mask).GetValueOrThrow();
                var cacheRis = library.GetCompiledCache(shaderId, 0, kwRis.Mask).GetValueOrThrow();
                Assert.AreEqual(hashRis, cacheRis.compiledHash);
                Assert.AreNotEqual(hashPcf, hashRis);
                Assert.AreNotEqual(hashVsm, hashRis);

                // Query with unrelated keyword (FOG_HIGH):
                // Supported mask filters it to Empty, which falls back to the default variant (index 0 / PCF)
                var hashWithFog = library.GetCompiledHash(shaderId, 0, kwFog.Mask).GetValueOrThrow();
                Assert.AreEqual(hashPcf, hashWithFog);

                // Query with combined (VSM | FOG_HIGH):
                // Supported mask filters out FOG_HIGH, leaving VSM, matching VSM variant
                var hashVsmAndFog = library.GetCompiledHash(shaderId, 0, kwVsm.Mask | kwFog.Mask).GetValueOrThrow();
                Assert.AreEqual(hashVsm, hashVsmAndFog);
            }
        }
        finally
        {
            library.Dispose();
        }
    }

    [TestMethod]
    public unsafe void TestShaderLibrary_ToggleKeywordResolution()
    {
        var library = new ShaderLibrary(null, null, null);
        try
        {
            var kwShadowLow = PipelineKeywordRegistry.GetOrRegister("_SHADOW_LOW_TOGGLE");
            var kwShadowHigh = PipelineKeywordRegistry.GetOrRegister("_SHADOW_HIGH_TOGGLE");
            var kwColored = PipelineKeywordRegistry.GetOrRegister("_COLORED_SHADOW_TOGGLE");
            var kwUnrelated = PipelineKeywordRegistry.GetOrRegister("_UNRELATED_TOGGLE");

            var supportedToggleOnly = kwColored.Mask;
            var supportedMixed = kwShadowLow.Mask | kwShadowHigh.Mask | kwColored.Mask;

            var codeOff = new byte[] { 1, 1, 1, 1 };
            var codeOn = new byte[] { 2, 2, 2, 2 };
            var codeLowOff = new byte[] { 3, 3, 3, 3 };
            var codeLowOn = new byte[] { 4, 4, 4, 4 };
            var codeHighOff = new byte[] { 5, 5, 5, 5 };
            var codeHighOn = new byte[] { 6, 6, 6, 6 };

            fixed (byte* pOff = codeOff, pOn = codeOn, pLowOff = codeLowOff, pLowOn = codeLowOn, pHighOff = codeHighOff, pHighOn = codeHighOn)
            {
                var passToggleOnly = new CompiledPassData(supportedToggleOnly, new[]
                {
                    new CompiledVariantData(KeywordMask128.Empty, new[] { new ShaderByteCode { pCode = pOff, size = (ulong)codeOff.Length } }),
                    new CompiledVariantData(kwColored.Mask, new[] { new ShaderByteCode { pCode = pOn, size = (ulong)codeOn.Length } }),
                });

                const ulong toggleShaderId = 0x223344556677ul;
                Assert.IsTrue(library.PublishCompiledGeneration(toggleShaderId, new ReadOnlySpan<CompiledPassData>(ref passToggleOnly)).IsSuccess);

                var hashOff = library.GetCompiledHash(toggleShaderId, 0, KeywordMask128.Empty).GetValueOrThrow();
                var hashOn = library.GetCompiledHash(toggleShaderId, 0, kwColored.Mask).GetValueOrThrow();
                var hashUnrelated = library.GetCompiledHash(toggleShaderId, 0, kwUnrelated.Mask).GetValueOrThrow();
                var hashBoth = library.GetCompiledHash(toggleShaderId, 0, kwColored.Mask | kwUnrelated.Mask).GetValueOrThrow();

                Assert.AreNotEqual(hashOff, hashOn);
                Assert.AreEqual(hashOff, hashUnrelated);
                Assert.AreEqual(hashOn, hashBoth);

                var passMixed = new CompiledPassData(supportedMixed, new[]
                {
                    new CompiledVariantData(kwShadowLow.Mask, new[] { new ShaderByteCode { pCode = pLowOff, size = (ulong)codeLowOff.Length } }),
                    new CompiledVariantData(kwShadowLow.Mask | kwColored.Mask, new[] { new ShaderByteCode { pCode = pLowOn, size = (ulong)codeLowOn.Length } }),
                    new CompiledVariantData(kwShadowHigh.Mask, new[] { new ShaderByteCode { pCode = pHighOff, size = (ulong)codeHighOff.Length } }),
                    new CompiledVariantData(kwShadowHigh.Mask | kwColored.Mask, new[] { new ShaderByteCode { pCode = pHighOn, size = (ulong)codeHighOn.Length } }),
                });

                const ulong mixedShaderId = 0x334455667788ul;
                Assert.IsTrue(library.PublishCompiledGeneration(mixedShaderId, new ReadOnlySpan<CompiledPassData>(ref passMixed)).IsSuccess);

                var hLowOff = library.GetCompiledHash(mixedShaderId, 0, kwShadowLow.Mask).GetValueOrThrow();
                var hLowOn = library.GetCompiledHash(mixedShaderId, 0, kwShadowLow.Mask | kwColored.Mask).GetValueOrThrow();
                var hHighOff = library.GetCompiledHash(mixedShaderId, 0, kwShadowHigh.Mask).GetValueOrThrow();
                var hHighOn = library.GetCompiledHash(mixedShaderId, 0, kwShadowHigh.Mask | kwColored.Mask).GetValueOrThrow();

                Assert.AreNotEqual(hLowOff, hLowOn);
                Assert.AreNotEqual(hLowOff, hHighOff);
                Assert.AreNotEqual(hHighOff, hHighOn);
                Assert.AreNotEqual(hLowOn, hHighOn);
            }
        }
        finally
        {
            library.Dispose();
        }
    }
}
