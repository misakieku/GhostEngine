namespace Ghost.Test.Core;

public interface ITest
{
    public void Setup();

    public void Run();

    public void Cleanup();
}
