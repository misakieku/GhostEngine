using Ghost.Core.Utilities;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.UnitTest.Graphics;

public partial class RenderGraphTest
{
    [TestMethod]
    public void TestSyncMarkerRoundTrip_PreservesDependencyPayloadsAcrossStream()
    {
        using var scope = AllocationManager.CreateStackScope();
        var writer = new BufferWriter(512, scope.AllocationHandle);

        RGCommandStream.WriteSyncMarker(ref writer, CommandQueueType.Compute, new int[] { 0 }, nextCommandBufferId: 1);
        RGCommandStream.WriteSyncMarker(ref writer, CommandQueueType.Graphics, ReadOnlySpan<int>.Empty, nextCommandBufferId: 2);
        RGCommandStream.WriteSyncMarker(ref writer, CommandQueueType.Graphics, new int[] { 1, 2 }, nextCommandBufferId: 3);

        var reader = new SpanReader(writer.AsSpan());

        Assert.AreEqual(RGExecutionOpType.CommandBufferSyncPoint, reader.Read<RGExecutionOpType>());
        var computeMarker = RGCommandStream.ReadSyncMarker(ref reader);
        Assert.AreEqual(CommandQueueType.Compute, computeMarker.NextCommandBufferType);
        Assert.IsTrue(computeMarker.ProducerCommandBufferIds.SequenceEqual(new int[] { 0 }));

        Assert.AreEqual(RGExecutionOpType.CommandBufferSyncPoint, reader.Read<RGExecutionOpType>());
        var independentGraphicsMarker = RGCommandStream.ReadSyncMarker(ref reader);
        Assert.AreEqual(CommandQueueType.Graphics, independentGraphicsMarker.NextCommandBufferType);
        Assert.AreEqual(0, independentGraphicsMarker.ProducerCommandBufferIds.Length);

        Assert.AreEqual(RGExecutionOpType.CommandBufferSyncPoint, reader.Read<RGExecutionOpType>());
        var joinMarker = RGCommandStream.ReadSyncMarker(ref reader);
        Assert.AreEqual(CommandQueueType.Graphics, joinMarker.NextCommandBufferType);
        Assert.IsTrue(joinMarker.ProducerCommandBufferIds.SequenceEqual(new int[] { 1, 2 }));

        Assert.AreEqual(0, reader.RemainingBytes);
        writer.Dispose();
    }

    [TestMethod]
    public void TestSyncMarkerValidation_EnforcesEarlierUniqueProducerIds()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => RGCommandStream.ValidateProducerIds(new int[] { 1 }, nextCommandBufferId: 1),
            "A producer ID equal to the next command buffer ID must be rejected.");
        Assert.ThrowsExactly<ArgumentException>(
            () => RGCommandStream.ValidateProducerIds(new int[] { 5 }, nextCommandBufferId: 3),
            "A producer ID greater than the next command buffer ID must be rejected.");
        Assert.ThrowsExactly<ArgumentException>(
            () => RGCommandStream.ValidateProducerIds(new int[] { -1 }, nextCommandBufferId: 1),
            "A negative producer ID must be rejected.");
        Assert.ThrowsExactly<ArgumentException>(
            () => RGCommandStream.ValidateProducerIds(new int[] { 0, 1, 0 }, nextCommandBufferId: 3),
            "Duplicate producer IDs within one marker must be rejected.");

        RGCommandStream.ValidateProducerIds(new int[] { 0, 1, 2 }, nextCommandBufferId: 3);
        RGCommandStream.ValidateProducerIds(ReadOnlySpan<int>.Empty, nextCommandBufferId: 0);
        RGCommandStream.ValidateProducerIds(new int[] { 0 }, nextCommandBufferId: 1);
    }
}
