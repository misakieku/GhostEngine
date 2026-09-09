using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.Services;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public sealed class MaterialPaletteStoreTest
{
    [TestMethod]
    public void FirstInsertedPaletteUsesNonZeroValidSlot()
    {
        using var store = new MaterialPaletteStore();
        var material = new Handle<Material>(0, 1);

        var paletteIndex = store.InsertOrGet([material]);

        Assert.AreNotEqual(0, paletteIndex);
        Assert.IsTrue(store.IsValid(paletteIndex));
        Assert.AreEqual(material, store.GetMaterial(paletteIndex, 0));
    }
}
