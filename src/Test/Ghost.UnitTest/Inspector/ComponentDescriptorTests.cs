using Ghost.Core.Attributes;
using Ghost.Editor.Core.Inspector;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Misaki.HighPerformance.Mathematics;
using Ghost.Entities;
using System.Runtime.InteropServices;

namespace Ghost.UnitTest.Inspector;

[TestClass]
public unsafe class ComponentDescriptorTests
{
    private struct TestComponent : IComponent
    {
        public int IntValue;

        [HideInInspector]
        public float HiddenFloat;

        [InspectorName("Custom Name")]
        public double DoubleValue;

        [ReadOnlyInInspector]
        public bool IsReadOnly;

        public float3 Position;
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
            IntValue = 42,
            DoubleValue = 3.1415,
            IsReadOnly = true,
            Position = new float3(1, 2, 3)
        };

        var pInt = descriptor.Properties[0];
        var pDouble = descriptor.Properties[1];

        // 1. Read
        object? readInt = pInt.ReadBoxed(&comp);
        Assert.AreEqual(42, readInt);

        object? readDouble = pDouble.ReadBoxed(&comp);
        Assert.AreEqual(3.1415, readDouble);

        // 2. Write
        pInt.WriteBoxed(&comp, 99);
        Assert.AreEqual(99, comp.IntValue);

        pDouble.WriteBoxed(&comp, 1.23);
        Assert.AreEqual(1.23, comp.DoubleValue);
    }
}
