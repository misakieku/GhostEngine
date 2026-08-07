using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingCommandAllocator : ICommandAllocator
{
    public CommandBufferType Type
    {
        get;
    }

    public string Name
    {
        get; set;
    } = "MockCommandAllocator";

    public MockingCommandAllocator(CommandBufferType type)
    {
        Type = type;
    }

    public void Reset()
    {
    }

    public void Dispose()
    {
    }
}
