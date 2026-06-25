using Microsoft.UI.Reactor;

namespace Ghost.AssetBaker;

internal class Program
{
    static void Main(string[] args)
    {
        // Launches the Reactor Application with a standard developer dashboard resolution
        ReactorApp.Run<App>("Ghost.AssetBaker", width: 1200, height: 800);
    }
}
