namespace Ghost.Ufbx;

public unsafe readonly ref struct BoneList
{
    private readonly ufbx_bone** _data;
    public int Count { get; }

    internal BoneList(ufbx_bone** data, nuint count)
    {
        _data = data;
        Count = checked((int)count);
    }

    public Bone this[int index]
    {
        get
        {
            NativeWrapperHelpers.ThrowIfOutOfRange(index, Count);
            return new(_data[index]);
        }
    }

    public Enumerator GetEnumerator() => new(_data, Count);

    public unsafe ref struct Enumerator
    {
        private readonly ufbx_bone** _data;
        private readonly int _count;
        private int _index;

        internal Enumerator(ufbx_bone** data, int count)
        {
            _data = data;
            _count = count;
            _index = -1;
        }

        public Bone Current => new(_data[_index]);

        public bool MoveNext()
        {
            var next = _index + 1;
            if (next >= _count)
            {
                return false;
            }

            _index = next;
            return true;
        }
    }
}
