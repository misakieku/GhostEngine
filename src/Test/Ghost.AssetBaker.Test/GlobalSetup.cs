using Microsoft.VisualStudio.TestTools.UnitTesting;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.AssetForge.Test;

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
