using Ghost.Entities.Test;
using Ghost.Test.Core;
using Misaki.HighPerformance.LowLevel.Buffer;

AllocationManager.EnableDebugLayer();
TestRunner.Run<SystemTest>();
AllocationManager.Dispose();
