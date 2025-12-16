namespace Ghost.Entities;

public readonly struct TimeData
{
    public int FrameCount
    {
        get; init;
    }

    public float DeltaTime
    {
        get; init;
    }

    public double ElapsedTime
    {
        get; init;
    }
}
