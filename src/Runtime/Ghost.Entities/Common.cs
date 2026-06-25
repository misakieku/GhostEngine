namespace Ghost.Entities;

public readonly struct TimeData
{
    public int FrameIndex
    {
        get; init;
    }

    public float DeltaTime
    {
        get; init;
    }

    public float ElapsedTime
    {
        get; init;
    }
}
