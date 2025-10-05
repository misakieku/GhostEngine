namespace Ghost.Shader;

public enum ZTestOptions
{
    Disabled,
    Less,
    LessEqual,
    Equal,
    GreaterEqual,
    Greater,
    NotEqual,
    Always
}

public enum ZWriteOptions
{
    Off,
    On
}

public enum CullOptions
{
    Off,
    Front,
    Back
}

public enum BlendOptions
{
    Opaque,
    Alpha,
    Additive,
    Multiply,
    PremultipliedAlpha
}
