namespace Ghost.Graphics.Contracts;

internal interface IRenderPass : IDisposable
{
    void Initialize(ICommandBuffer cmb);
    void Execute(ICommandBuffer cmb);
}