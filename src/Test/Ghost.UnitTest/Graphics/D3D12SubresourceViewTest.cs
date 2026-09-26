#if GHOST_UNITTEST

using Ghost.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.Graphics;

[TestClass]
[DoNotParallelize]
public class D3D12SubresourceViewTest
{
    [TestMethod]
    public void TestSubresource_UavPerMipCreatedByDefault()
    {
        using var graphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc());
        var resourceAllocator = graphicsEngine.ResourceAllocator;
        var resourceDatabase = graphicsEngine.ResourceDatabase;

        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = 256,
            Height = 256,
            MipLevels = 4,
            Format = TextureFormat.R32_Float,
            Usage = TextureUsage.UnorderedAccess | TextureUsage.ShaderResource,
        };

        var texHandle = resourceAllocator.CreateTexture(in desc);
        Assert.IsTrue(texHandle.IsValid);

        try
        {
            var resHandle = texHandle.AsResource();

            // Main bindless UAV index (AllSubresources)
            var mainUav = resourceDatabase.GetBindlessIndex(resHandle, BindlessAccess.UnorderedAccess, IResourceDatabase.AllSubresources);
            Assert.AreNotEqual(uint.MaxValue, mainUav, "Main UAV must be valid.");

            // Main bindless SRV index
            var mainSrv = resourceDatabase.GetBindlessIndex(resHandle, BindlessAccess.ShaderResource, IResourceDatabase.AllSubresources);
            Assert.AreNotEqual(uint.MaxValue, mainSrv, "Main SRV must be valid.");

            // Per-mip UAV indices
            var mipUavs = new uint[4];
            for (var mip = 0u; mip < 4; mip++)
            {
                mipUavs[mip] = resourceDatabase.GetBindlessIndex(resHandle, BindlessAccess.UnorderedAccess, mip);
                Assert.AreNotEqual(uint.MaxValue, mipUavs[mip], $"Mip {mip} UAV must be valid.");
            }

            // Each mip UAV should be unique
            for (var i = 0; i < 4; i++)
            {
                for (var j = i + 1; j < 4; j++)
                {
                    Assert.AreNotEqual(mipUavs[i], mipUavs[j], $"Mip {i} and {j} UAVs must be distinct.");
                }
            }

            // Out-of-bounds subresource index
            var invalidSub = resourceDatabase.GetBindlessIndex(resHandle, BindlessAccess.UnorderedAccess, 4);
            Assert.AreEqual(uint.MaxValue, invalidSub, "Out-of-bounds subresource must return uint.MaxValue.");

            // Batch fetch via GetBindlessIndices
            Span<uint> requestedSubs = stackalloc uint[5] { 0, 1, 2, 3, 4 };
            Span<uint> batchUavs = stackalloc uint[5];
            resourceDatabase.GetBindlessIndices(resHandle, requestedSubs, batchUavs, BindlessAccess.UnorderedAccess);
            for (var mip = 0; mip < 4; mip++)
            {
                Assert.AreEqual(mipUavs[mip], batchUavs[mip], $"Batch UAV index for mip {mip} must match single fetch.");
            }
            Assert.AreEqual(uint.MaxValue, batchUavs[4], "Batch out-of-bounds must be uint.MaxValue.");
        }
        finally
        {
            resourceDatabase.ReleaseResourceImmediately(texHandle.AsResource());
        }
    }

    [TestMethod]
    public void TestSubresource_CreatePerMipSrvFlag()
    {
        using var graphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc());
        var resourceAllocator = graphicsEngine.ResourceAllocator;
        var resourceDatabase = graphicsEngine.ResourceDatabase;

        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = 128,
            Height = 128,
            MipLevels = 3,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsage.ShaderResource,
        };

        var additional = new AdditionalTextureDesc
        {
            ViewCreationFlags = TextureViewCreationFlags.CreatePerMipSrv,
        };

        var texHandle = resourceAllocator.CreateTexture(in desc, additionalDesc: additional);
        Assert.IsTrue(texHandle.IsValid);

        try
        {
            var resHandle = texHandle.AsResource();

            var mip0Srv = resourceDatabase.GetBindlessIndex(resHandle, BindlessAccess.ShaderResource, 0);
            var mip1Srv = resourceDatabase.GetBindlessIndex(resHandle, BindlessAccess.ShaderResource, 1);
            var mip2Srv = resourceDatabase.GetBindlessIndex(resHandle, BindlessAccess.ShaderResource, 2);

            Assert.AreNotEqual(uint.MaxValue, mip0Srv, "Mip 0 SRV must be valid.");
            Assert.AreNotEqual(uint.MaxValue, mip1Srv, "Mip 1 SRV must be valid.");
            Assert.AreNotEqual(uint.MaxValue, mip2Srv, "Mip 2 SRV must be valid.");
            Assert.AreNotEqual(mip0Srv, mip1Srv, "Mip 0 and 1 SRV must be distinct.");
            Assert.AreNotEqual(mip1Srv, mip2Srv, "Mip 1 and 2 SRV must be distinct.");
        }
        finally
        {
            resourceDatabase.ReleaseResourceImmediately(texHandle.AsResource());
        }
    }

    [TestMethod]
    public void TestSubresource_NoSubresourceFlags_FastPath()
    {
        using var graphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc());
        var resourceAllocator = graphicsEngine.ResourceAllocator;
        var resourceDatabase = graphicsEngine.ResourceDatabase;

        // Texture with 1 mip and no UAV -> no subresource views created
        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = 64,
            Height = 64,
            MipLevels = 1,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsage.ShaderResource,
        };

        var texHandle = resourceAllocator.CreateTexture(in desc);
        Assert.IsTrue(texHandle.IsValid);

        try
        {
            var resHandle = texHandle.AsResource();

            var mainSrv = resourceDatabase.GetBindlessIndex(resHandle, BindlessAccess.ShaderResource, IResourceDatabase.AllSubresources);
            Assert.AreNotEqual(uint.MaxValue, mainSrv);

            // Fast path when subResourceView is not created: requesting subResource = 0 returns main srv
            var defaultSrv = resourceDatabase.GetBindlessIndex(resHandle, BindlessAccess.ShaderResource, 0);
            Assert.AreEqual(mainSrv, defaultSrv);
        }
        finally
        {
            resourceDatabase.ReleaseResourceImmediately(texHandle.AsResource());
        }
    }

    [TestMethod]
    public void TestSubresource_SwapPreservesSubresourceBindings()
    {
        using var graphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc());
        var resourceAllocator = graphicsEngine.ResourceAllocator;
        var resourceDatabase = graphicsEngine.ResourceDatabase;

        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = 128,
            Height = 128,
            MipLevels = 3,
            Format = TextureFormat.R32_Float,
            Usage = TextureUsage.UnorderedAccess | TextureUsage.ShaderResource,
        };

        var texA = resourceAllocator.CreateTexture(in desc);
        var texB = resourceAllocator.CreateTexture(in desc);
        Assert.IsTrue(texA.IsValid && texB.IsValid);

        try
        {
            var resA = texA.AsResource();
            var resB = texB.AsResource();

            var aMip0 = resourceDatabase.GetBindlessIndex(resA, BindlessAccess.UnorderedAccess, 0);
            var aMip1 = resourceDatabase.GetBindlessIndex(resA, BindlessAccess.UnorderedAccess, 1);
            var bMip0 = resourceDatabase.GetBindlessIndex(resB, BindlessAccess.UnorderedAccess, 0);
            var bMip1 = resourceDatabase.GetBindlessIndex(resB, BindlessAccess.UnorderedAccess, 1);

            Assert.AreNotEqual(aMip0, bMip0);

            var swapResult = resourceDatabase.Swap(resA, resB);
            Assert.AreEqual(Error.None, swapResult);

            // Bindless indices for resA and resB slots should remain unchanged
            Assert.AreEqual(aMip0, resourceDatabase.GetBindlessIndex(resA, BindlessAccess.UnorderedAccess, 0));
            Assert.AreEqual(aMip1, resourceDatabase.GetBindlessIndex(resA, BindlessAccess.UnorderedAccess, 1));
            Assert.AreEqual(bMip0, resourceDatabase.GetBindlessIndex(resB, BindlessAccess.UnorderedAccess, 0));
            Assert.AreEqual(bMip1, resourceDatabase.GetBindlessIndex(resB, BindlessAccess.UnorderedAccess, 1));
        }
        finally
        {
            resourceDatabase.ReleaseResourceImmediately(texA.AsResource());
            resourceDatabase.ReleaseResourceImmediately(texB.AsResource());
        }
    }
}

#endif
