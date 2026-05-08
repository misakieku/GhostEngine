namespace Ghost.TestCore;

public class TestRunner
{
    public static void Run<T>()
        where T : ITest, new()
    {
        var test = new T();

        try
        {
            test.Setup();
            test.Run();
            test.Cleanup();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with exception: {ex.Message}");
        }

        Console.WriteLine("Test completed.");
    }

    public static void Run<T>(int iteration)
        where T : ITest, new()
    {
        var test = new T();
        var i = 0;

        try
        {
            test.Setup();

            iteration = iteration < 1 ? 1 : iteration;
            for (i = 0; i < iteration; i++)
            {
                test.Run();
            }

            test.Cleanup();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed at iteration {i} with exception: {ex.Message}");
        }

        Console.WriteLine($"Test completed after {iteration} iterations.");
    }
}