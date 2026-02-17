using BenchmarkDotNet.Running;
using Ghost.Entities.Test;
using Ghost.Test.Core;
using Misaki.HighPerformance.LowLevel.Buffer;

//AllocationManager.EnableDebugLayer();
//TestRunner.Run<SerializationTest>();
//AllocationManager.Dispose();

BenchmarkRunner.Run<QueryBenchmark>();
//var test = new QueryBenchmark();
//test.Setup();
//test.QueryEntities();
//test.Cleanup();