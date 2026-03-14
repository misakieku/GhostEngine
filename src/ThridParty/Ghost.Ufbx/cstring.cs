using System.Runtime.InteropServices;
using System.Text;

namespace Ghost.Ufbx;

public unsafe struct cstring : IDisposable, IEquatable<cstring>
{
    public byte* ptr;
    public int length;

    public cstring(byte* ptr, nuint length)
    {
        if (length == 0)
        {
            return;
        }

        this.ptr = (byte*)NativeMemory.Alloc(length);
        this.length = (int)length;
    }

    public cstring(ReadOnlySpan<char> str)
    {
        if (str.Length == 0)
        {
            return;
        }

        length = Encoding.UTF8.GetByteCount(str);
        ptr = (byte*)NativeMemory.Alloc((nuint)length);
        fixed (char* p = str)
        {
            Encoding.UTF8.GetBytes(p, str.Length, ptr, length);
        }
    }

    public cstring(ReadOnlySpan<byte> str)
    {
        if (str.Length == 0)
        {
            return;
        }

        length = str.Length;
        ptr = (byte*)NativeMemory.Alloc((nuint)length);
        fixed (byte* p = str)
        {
            NativeMemory.Copy(p, ptr, (nuint)length);
        }
    }

    public cstring(cstring other)
    {
        if (other.length == 0)
        {
            return;
        }

        length = other.length;
        ptr = (byte*)NativeMemory.Alloc((nuint)length);
        NativeMemory.Copy(other.ptr, ptr, (nuint)length);
    }

    public void Dispose()
    {
        if (ptr != null)
        {
            NativeMemory.Free(ptr);
        }

        ptr = null;
        length = 0;
    }

    public readonly bool Equals(cstring other)
    {
        return length == other.length && ptr == other.ptr;
    }

    public override bool Equals(object? obj)
    {
        return obj is cstring cstring && Equals(cstring);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine((nint)ptr, length);
    }

    public override readonly string ToString()
    {
        return Encoding.UTF8.GetString(ptr, length);
    }

    public static bool operator ==(cstring left, cstring right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(cstring left, cstring right)
    {
        return !(left == right);
    }
}
