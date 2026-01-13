using Ghost.RenderGraph.Concept;
using Ghost.RenderGraph.Concept.Benchmark;

#if !DEBUG

BenchmarkDotNet.Running.BenchmarkRunner.Run<RenderGraphBenchmark>();
return;

var renderGraph = new RenderGraph();
const int _ITERATION = 500000;
for (var i = 0; i < _ITERATION; i++)
{
    RenderGraphBenchmark.ExecuteGraph(renderGraph);
}

GC.Collect();
GC.WaitForPendingFinalizers();
//Thread.Sleep(1000); // Leave a gap in visual studio allocations timeline
var sw = new System.Diagnostics.Stopwatch();
var gcBefore = GC.GetAllocatedBytesForCurrentThread();
sw.Start();

for (var i = 0; i < _ITERATION; i++)
{
    RenderGraphBenchmark.ExecuteGraph(renderGraph);
}

sw.Stop();
var gcAfter = GC.GetAllocatedBytesForCurrentThread();

Console.WriteLine($"{sw.Elapsed.TotalNanoseconds / _ITERATION} ns (per iteration)");
Console.WriteLine($"GC Allocated Bytes: {(gcAfter - gcBefore) / _ITERATION} bytes (per iteration)");
#else
var renderGraph = new RenderGraph();

// Run twice to demonstrate cache hit
Console.WriteLine("=== FRAME 1 (Cache Miss Expected) ===");
RenderGraphBenchmark.ExecuteGraph(renderGraph);

Console.WriteLine("\n\n=== FRAME 2 (Cache Hit Expected) ===");
RenderGraphBenchmark.ExecuteGraph(renderGraph);
#endif
