namespace Ghost.Test.Core;

public interface ITest
{
    void Setup();

    void Run();

    void Cleanup();
}
