using Ghost.Data.Models;
using System.Text.Json.Serialization;

namespace Ghost.Data;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(TemplateInfo))]
internal partial class JsonContext : JsonSerializerContext
{
}