using System.Text.Json;

namespace Ghost.Engine.Resources;

public static class StaticResource
{
    public static JsonSerializerOptions defaultSerializerOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        IgnoreReadOnlyProperties = true,
    };
}