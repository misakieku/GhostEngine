#if !GHOST_UNITTEST

using BenchmarkDotNet.Running;

namespace Ghost.UnitTest;

public class Program
{
    private static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

#endif