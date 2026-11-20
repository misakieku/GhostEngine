using Hexa.NET.ImGui;
using System.Text;

namespace Ghost.Studio.UI;

/// <summary>
/// Shared Dear ImGui drag-and-drop payload identifiers and UTF-8 string payload helpers.
/// </summary>
internal static unsafe class EditorDragDrop
{
    public const string ASSET_PAYLOAD = "GHOST_PROJECT_ASSET";
    public const string ENTITY_PAYLOAD = "GHOST_HIERARCHY_ENTITY";

    public static void SetStringPayload(string payloadType, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        Span<byte> buffer = byteCount <= 1024 ? stackalloc byte[byteCount] : new byte[byteCount];
        Encoding.UTF8.GetBytes(value, buffer);
        fixed (byte* ptr = buffer)
        {
            ImGui.SetDragDropPayload(payloadType, ptr, (nuint)buffer.Length);
        }
    }

    public static bool TryAcceptStringPayload(string payloadType, out string value)
    {
        var payload = ImGui.AcceptDragDropPayload(payloadType);
        if (!payload.IsNull && payload.Data != null && payload.DataSize > 0)
        {
            value = Encoding.UTF8.GetString((byte*)payload.Data, payload.DataSize);
            return true;
        }

        value = string.Empty;
        return false;
    }
}
