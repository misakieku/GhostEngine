namespace Ghost.Test.Core;

public class TestRunner
{
    public static void Run<T>()
        where T : ITest, new()
    {
        var test = new T();
        test.Setup();
        test.Run();
        test.Cleanup();
    }

    public static void Run<T>(int iteration)
        where T : ITest, new()
    {
        var test = new T();
        test.Setup();

        iteration = iteration < 1 ? 1 : iteration;
        for (var i = 0; i < iteration; i++)
        {
            test.Run();
        }

        test.Cleanup();
    }
}