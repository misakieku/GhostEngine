using System.Text.Json;

namespace Ghost.Engine.Resources;

public static class StaticResource
{
    public static readonly JsonSerializerOptions defaultSerializerOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        IgnoreReadOnlyProperties = true,
    };
}