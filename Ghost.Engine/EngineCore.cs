using Ghost.Entities;
using Misaki.HighPerformance.Jobs;

namespace Ghost.Engine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal class EngineEntryAttribute : Attribute
{
}

[EngineEntry]
public partial class EngineCore
{
    private readonly JobScheduler _jobScheduler;

    public JobScheduler JobScheduler => _jobScheduler;

    internal EngineCore()
    {
        _jobScheduler = new JobScheduler(Environment.ProcessorCount - 2); // We -2 here, one for main thread, one for render thread

        ComponentRegistry.GetOrRegisterComponentID<ManagedEntityRef>();
    }

    internal void Init()
    {
    }

    internal void Dispose()
    {
        _jobScheduler.Dispose();
    }
}