using Ghost.Core;
using Ghost.Editor.Core.Services;

namespace Ghost.UnitTest;

[TestClass]
public class UndoServiceTests
{
    private class TestGhostObject : GhostObject
    {
        public string Data { get; set; } = "Initial";

        public TestGhostObject()
        {
        }

        public override void SerializeState(BinaryWriter writer)
        {
            writer.Write(Data);
        }

        public override void DeserializeState(BinaryReader reader)
        {
            Data = reader.ReadString();
        }
    }

    private EditorWorldService _worldService = null!;
    private UndoService _undoService = null!;

    [TestInitialize]
    public void Setup()
    {
        _worldService = new EditorWorldService();
        _undoService = new UndoService(_worldService);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _worldService.Dispose();
    }

    [TestMethod]
    public void TestObjectStateUndoRedo()
    {
        var obj = new TestGhostObject();
        obj.Data = "State 1";

        _undoService.RecordObject(obj, "Change Data");
        obj.Data = "State 2";

        _undoService.PerformUndo();
        Assert.AreEqual("State 1", obj.Data);

        _undoService.PerformRedo();
        Assert.AreEqual("State 2", obj.Data);
    }

    [TestMethod]
    public void TestTransactionGrouping()
    {
        var obj = new TestGhostObject();

        _undoService.BeginTransaction("Slider Drag");
        _undoService.RecordObject(obj, "Drag Start");
        obj.Data = "Drag 1";

        _undoService.RecordObject(obj, "Drag Mid");
        obj.Data = "Drag 2";

        _undoService.RecordObject(obj, "Drag End");
        obj.Data = "Drag Final";
        _undoService.EndTransaction();

        // Perform undo should jump all the way back to "Initial"
        _undoService.PerformUndo();
        Assert.AreEqual("Initial", obj.Data);

        _undoService.PerformRedo();
        Assert.AreEqual("Drag Final", obj.Data);
    }

    [TestMethod]
    public void TestRingBufferOverflow()
    {
        // Internal capacity is 50. Let's push 60 items.
        var obj = new TestGhostObject();

        for (var i = 0; i < 60; i++)
        {
            _undoService.RecordObject(obj, $"Action {i}");
            obj.Data = $"State {i}";
        }

        // We can only undo 50 times.
        for (var i = 0; i < 50; i++)
        {
            _undoService.PerformUndo();
        }

        // It should have stopped at State 9 because State 0-9 were overwritten in the buffer.
        Assert.AreEqual("State 9", obj.Data);
    }
}
