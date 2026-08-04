#if !GHOST_UNITTEST

using BenchmarkDotNet.Running;
using Ghost.UnitTest.Benchmarks.Graphics;

namespace Ghost.UnitTest;

public class Program
{
    private static void Main(string[] args)
    {
        //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        //BenchmarkRunner.Run<RenderGraphBenchmark>();
        var rg = new RenderGraphBenchmark();
        rg.Setup();
        rg.Compile_Cold_CacheMiss();
        rg.Cleanup();
    }
}

#endif