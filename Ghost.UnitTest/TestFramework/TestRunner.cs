namespace Ghost.UnitTest.TestFramework;

internal class TestRunner
{
    public static void Run<T>()
        where T : ITest, new()
    {
        var test = new T();
        test.Run();
    }
}