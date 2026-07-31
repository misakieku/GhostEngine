#if !GHOST_UNITTEST

using BenchmarkDotNet.Running;
using Ghost.UnitTest.Benchmarks.Graphics;
using Misaki.HighPerformance.LowLevel.Buffer;

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
        //for (int i = 0; i < 1024000; i++)
        //{
        //    rg.Compile_Warm_CacheHit();
        //    rg.IterationCleanup();
        //}
        //sw.Stop();
        //rg.Cleanup();

        //Console.WriteLine($"{sw.Elapsed.TotalMilliseconds / 1024000} ms");
    }
}

#endif