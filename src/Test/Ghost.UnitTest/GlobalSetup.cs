using Misaki.HighPerformance.LowLevel.Buffer;

[assembly: DoNotParallelize]

namespace Ghost.UnitTest;

[TestClass]
public static class GlobalSetup
{
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        AllocationManager.Initialize();
    }

    [AssemblyCleanup]
    public static void Cleanup(TestContext context)
    {
        AllocationManager.Dispose();
    }
}
