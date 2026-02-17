using Ghost.Entities;
using Misaki.HighPerformance.Jobs;

namespace Ghost.Engine;

public interface IEngineContext : IDisposable
{
    JobScheduler JobScheduler { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal class EngineEntryAttribute : Attribute
{
}

[EngineEntry]
internal sealed partial class EngineCore : IEngineContext
{
    private readonly JobScheduler _jobScheduler;

    public JobScheduler JobScheduler => _jobScheduler;

    public EngineCore()
    {
        _jobScheduler = new JobScheduler(Environment.ProcessorCount - 2); // We -2 here, one for main thread, one for render thread

        ComponentRegistry.GetOrRegisterComponentID<ManagedEntityRef>();
    }

    public void Init()
    {
    }

    public void Dispose()
    {
        _jobScheduler.Dispose();
    }
}