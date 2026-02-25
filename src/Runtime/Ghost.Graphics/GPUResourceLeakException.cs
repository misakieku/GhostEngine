namespace Ghost.Graphics;

internal unsafe class GPUResourceLeakException : Exception
{
    public GPUResourceLeakException(uint refCount, void* address, string name)
        : base($"GPU resource leak detected! Resource '{name}' at address {(UIntPtr)address} has a reference count of {refCount} when it should be 0. This indicates that the resource was not properly released before being destroyed, which can lead to memory leaks and other issues. Please ensure that all references to this resource are released appropriately.")
    {
    }

    public static void ThrowIfRefCountNonZero(uint refCount, void* address, string name)
    {
        if (refCount != 0)
        {
            throw new GPUResourceLeakException(refCount, address, name);
        }
    }
}