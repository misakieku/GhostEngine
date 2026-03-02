using System.Diagnostics;

namespace Ghost.Graphics.Core;

public struct RenderingLayerMask
{
    private static readonly Dictionary<string, uint> _layerNameToBit = new (32);
    private static readonly Dictionary<uint, string> _bitToLayerName = new (32);

    internal static void SetLayerName(int layerIndex, string name)
    {
        Debug.Assert(layerIndex >= 0 && layerIndex < 32, "Layer index must be between 0 and 31.");

        var bit = 1u << layerIndex;
        _layerNameToBit[name] = bit;
        _bitToLayerName[bit] = name;
    }

    public static uint GetLayerBit(string name)
    {
        if (_layerNameToBit.TryGetValue(name, out var bit))
        {
            return bit;
        }

        return ~0u;
    }

    public uint value;

    public static implicit operator uint(RenderingLayerMask mask) => mask.value;
    public static implicit operator RenderingLayerMask(uint value) => new RenderingLayerMask { value = value };
}
