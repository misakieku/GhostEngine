using Microsoft.UI.Reactor;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.AssetBaker;

internal class Program
{
    static void Main(string[] args)
    {
        AllocationManager.Initialize(AllocationManagerDesc.Default);

        // Launches the Reactor Application with a standard developer dashboard resolution
        ReactorApp.Run<App>("Ghost.AssetBaker", width: 1200, height: 800);

        AllocationManager.Dispose();
    }
}
