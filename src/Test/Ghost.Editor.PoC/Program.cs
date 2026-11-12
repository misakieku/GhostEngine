using Ghost.Editor.PoC.Tests;
using Ghost.Editor.PoC.UI;
using Microsoft.UI.Reactor;

namespace Ghost.Editor.PoC;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--headless-test")
        {
            return EditorSyncHeadlessRunner.Run();
        }

        ReactorApp.Run<EditorPoCApp>("GhostEngine — Minimal Editor PoC", width: 1024, height: 600);
        return 0;
    }
}
