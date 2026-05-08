using System.Runtime.InteropServices;

namespace Ghost.Core.Graphics;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Texture2DHandle
{
    public readonly uint DescriptorIndex;

    public Texture2DHandle(uint descriptorIndex)
    {
        DescriptorIndex = descriptorIndex;
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct Texture3DHandle
{
    public readonly uint DescriptorIndex;

    public Texture3DHandle(uint descriptorIndex)
    {
        DescriptorIndex = descriptorIndex;
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct BufferHandle
{
    public readonly uint DescriptorIndex;

    public BufferHandle(uint descriptorIndex)
    {
        DescriptorIndex = descriptorIndex;
    }
}
