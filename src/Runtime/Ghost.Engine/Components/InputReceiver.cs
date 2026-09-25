using Ghost.Entities;

namespace Ghost.Engine.Components;

/// <summary>
/// Declares that an entity receives input and links it to an InputMap profile.
/// </summary>
public struct InputReceiver : IComponentData
{
    public int ProfileId;
    public bool Enabled;

    public InputReceiver(int profileId, bool enabled = true)
    {
        ProfileId = profileId;
        Enabled = enabled;
    }
}
