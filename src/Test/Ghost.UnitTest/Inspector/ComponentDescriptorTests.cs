using Ghost.Core.Attributes;
using Ghost.Editor.Core.Inspector;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.UnitTest.Inspector;

[TestClass]
public unsafe class ComponentDescriptorTests
{
    private struct TestComponent : IComponent
    {
        public int intValue;

        [HideInInspector]
        public float hiddenFloat;

        [InspectorName("Custom Name")]
        public double doubleValue;

        [ReadOnlyInInspector]
        public bool isReadOnly;

        public float3 position;
    }

    [TestMethod]
    public void Test_ComponentDescriptor_GeneratesCorrectOffsets()
    {
        ComponentRegistry.GetOrRegisterComponentID<TestComponent>();
        var descriptor = ComponentDescriptorRegistry.GetOrCreate(typeof(TestComponent));

        Assert.IsNotNull(descriptor);
        Assert.AreEqual("TestComponent", descriptor.DisplayName);
        Assert.AreEqual(4, descriptor.Properties.Length, "Should have exactly 4 visible properties (HiddenFloat is ignored).");

        var p0 = descriptor.Properties[0];
        Assert.AreEqual("IntValue", p0.Name);
        Assert.AreEqual(typeof(int), p0.FieldType);
        Assert.AreEqual(0, p0.OffsetInComponent);

        var p1 = descriptor.Properties[1];
        Assert.AreEqual("Custom Name", p1.DisplayName);
        Assert.AreEqual(typeof(double), p1.FieldType);
        // Offset of double after int+float is 8 (with alignment)
        Assert.AreEqual((int)Marshal.OffsetOf<TestComponent>("DoubleValue"), p1.OffsetInComponent);

        var p2 = descriptor.Properties[2];
        Assert.AreEqual("IsReadOnly", p2.Name);
        Assert.IsTrue(p2.IsReadOnly);

        var p3 = descriptor.Properties[3];
        Assert.AreEqual("Position", p3.Name);
        Assert.AreEqual(typeof(float3), p3.FieldType);
        Assert.IsNull(p3.Children); // float3 is a primitive so it has no children
    }

    [TestMethod]
    public void Test_PropertyDescriptor_ReadWriteBoxed()
    {
        ComponentRegistry.GetOrRegisterComponentID<TestComponent>();
        var descriptor = ComponentDescriptorRegistry.GetOrCreate(typeof(TestComponent));

        var comp = new TestComponent
        {
            intValue = 42,
            doubleValue = 3.1415,
            isReadOnly = true,
            position = new float3(1, 2, 3)
        };

        var pInt = descriptor.Properties[0];
        var pDouble = descriptor.Properties[1];

        // 1. Read
        var readInt = pInt.ReadBoxed(&comp);
        Assert.AreEqual(42, readInt);

        var readDouble = pDouble.ReadBoxed(&comp);
        Assert.AreEqual(3.1415, readDouble);

        // 2. Write
        pInt.WriteBoxed(&comp, 99);
        Assert.AreEqual(99, comp.intValue);

        pDouble.WriteBoxed(&comp, 1.23);
        Assert.AreEqual(1.23, comp.doubleValue);
    }
}
