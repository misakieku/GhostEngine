namespace Ghost.MicroTest.Core;

public static class Assert
{
    public static void AreEqual<T>(T expected, T actual, string message = "")
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new AssertFailedException($"Assert.AreEqual failed. Expected: {expected}, Actual: {actual}. {message}");
        }
    }

    public static void IsTrue(bool condition, string message = "")
    {
        if (!condition)
        {
            throw new AssertFailedException($"Assert.IsTrue failed. {message}");
        }
    }

    public static void IsFalse(bool condition, string message = "")
    {
        if (condition)
        {
            throw new AssertFailedException($"Assert.IsFalse failed. {message}");
        }
    }

    public static void Fail(string message = "")
    {
        throw new AssertFailedException($"Assert.Fail: {message}");
    }
}
