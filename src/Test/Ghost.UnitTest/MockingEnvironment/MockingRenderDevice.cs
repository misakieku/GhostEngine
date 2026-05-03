using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingRenderDevice : IRenderDevice
{
    private readonly MockingCommandQueue _commandQueue = new MockingCommandQueue(CommandQueueType.Graphics);
    private readonly MockingCommandQueue _computeCommandQueue = new MockingCommandQueue(CommandQueueType.Compute);
    private readonly MockingCommandQueue _copyCommandQueue = new MockingCommandQueue(CommandQueueType.Copy);

    public ICommandQueue GraphicsQueue => _commandQueue;

    public ICommandQueue ComputeQueue => _computeCommandQueue;

    public ICommandQueue CopyQueue => _copyCommandQueue;

    public FeatureSupport FeatureSupport => (FeatureSupport)~0;

    public string Name
    {
        get; set;
    } = "MockingRenderDevice";

    public void Dispose()
    {
    }
}
