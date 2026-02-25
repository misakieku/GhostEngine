using BenchmarkDotNet.Running;
using Ghost.Entities.Test;

//AllocationManager.EnableDebugLayer();
//TestRunner.Run<SerializationTest>();
//AllocationManager.Dispose();

BenchmarkRunner.Run<QueryBenchmark>();
//var test = new QueryBenchmark();
//test.Setup();
//test.QueryEntities();
//test.Cleanup();