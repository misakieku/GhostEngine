namespace Ghost.Engine;

public enum SceneLoadingType
{
    Single = 0,
    Additive = 1,
}

public enum ShadowCastingMode : uint
{
    Off,
    On,
    TwoSided,
    ShadowsOnly
}
