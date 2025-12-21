using Ghost.Entities;
using Misaki.HighPerformance.Jobs;

namespace Ghost.Engine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal class EngineEntryAttribute : Attribute
{
}

internal partial class EngineCoreImpl : IDisposable
{
    internal readonly JobScheduler _jobScheduler;

    internal EngineCoreImpl()
    {
        _jobScheduler = new JobScheduler(Environment.ProcessorCount - 2); // We -2 here, one for main thread, one for render thread
    }

    internal void IncrementCPUFenceValue()
    {
        //GraphicsPipeline.SignalCPUReady();
    }

    public void Dispose()
    {
        _jobScheduler.Dispose();
        JobScheduler.ReleaseTempAllocator();
    }
}

[EngineEntry]
public static partial class EngineCore
{
    internal static readonly EngineCoreImpl s_impl;

    public static JobScheduler JobScheduler => s_impl._jobScheduler;

    static EngineCore()
    {
        s_impl = new EngineCoreImpl();

        ComponentRegistry.GetOrRegisterComponentID<ManagedEntityRef>();
    }

    internal static void Init()
    {
    }

    internal static void Dispose()
    {
        s_impl.Dispose();
    }
}