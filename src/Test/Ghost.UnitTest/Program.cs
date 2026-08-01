#if !GHOST_UNITTEST

using BenchmarkDotNet.Running;
using Ghost.UnitTest.Benchmarks.Graphics;

namespace Ghost.UnitTest;

public class Program
{
    private static void Main(string[] args)
    {
        BenchmarkRunner.Run<RenderGraphBenchmark>();
        //var rg = new RenderGraphBenchmark();
        //rg.Setup();

        //var sw = new System.Diagnostics.Stopwatch();
        //sw.Start();
        //for (var i = 0; i < 1; i++)
        //{
        //    rg.Compile_Cold_CacheMiss();
        //}
        //sw.Stop();
        //rg.Cleanup();

        //Console.WriteLine($"{sw.Elapsed.TotalMilliseconds / 1} ms");
    }
}

#endif