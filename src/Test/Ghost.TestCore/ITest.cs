namespace Ghost.TestCore;

public interface ITest
{
    void Setup();

    void Run();

    void Cleanup();
}
