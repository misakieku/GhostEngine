#if !GHOST_UNITTEST

using BenchmarkDotNet.Running;
using Ghost.UnitTest.Benchmarks.Graphics;

namespace Ghost.UnitTest;

public class Program
{
    private static void Main(string[] args)
    {
        //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        BenchmarkRunner.Run<RenderGraphBenchmark>();
    }
}

#endif