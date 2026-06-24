namespace Ghost.MicroTest.Core;

public interface ITest
{
    void Setup();

    void Run();

    void Cleanup();
}
