using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingCommandAllocator : ICommandAllocator
{
    public string Name
    {
        get; set;
    } = "MockCommandAllocator";

    public void Reset()
    {
    }

    public void Dispose()
    {
    }
}
