namespace Ghost.Graphics.RHI;

public unsafe interface ICommandSignature : IRHIObject
{
    IntPtr NativePointer { get; }
}