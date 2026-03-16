namespace Ghost.Nvtt
{
    //public enum NvttBoolean
    //{
    //    NVTT_False,
    //    NVTT_True,
    //}

    // NOTE: The native NVTT API uses an enum for boolean values, but we want to expose it as a struct that can be implicitly converted to/from C# bool for better ergonomics.
    // Since the memory layout of a struct with a single int field is the same as an enum, this should be safe for interop.
    public readonly struct NvttBoolean : IEquatable<NvttBoolean>
    {
        public static NvttBoolean NVTT_False => new(0);
        public static NvttBoolean NVTT_True => new(1);

        private readonly int _value;

        public NvttBoolean(int value)
        {
            _value = value;
        }

        public bool Equals(NvttBoolean other)
        {
            return _value == other._value;
        }

        public override bool Equals(object? obj)
        {
            return obj is NvttBoolean && Equals((NvttBoolean)obj);
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        public static bool operator ==(NvttBoolean left, NvttBoolean right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NvttBoolean left, NvttBoolean right)
        {
            return !(left == right);
        }

        public static implicit operator NvttBoolean(bool value)
        {
            return value ? NVTT_True : NVTT_False;
        }

        public static implicit operator bool(NvttBoolean value)
        {
            return value._value != 0;
        }
    }
}
