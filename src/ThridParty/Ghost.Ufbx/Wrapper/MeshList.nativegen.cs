namespace Ghost.Ufbx;

public unsafe readonly ref struct MeshList
{
    private readonly ufbx_mesh** _data;
    public int Count { get; }

    internal MeshList(ufbx_mesh** data, nuint count)
    {
        _data = data;
        Count = checked((int)count);
    }

    public Mesh this[int index]
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
        private readonly ufbx_mesh** _data;
        private readonly int _count;
        private int _index;

        internal Enumerator(ufbx_mesh** data, int count)
        {
            _data = data;
            _count = count;
            _index = -1;
        }

        public Mesh Current => new(_data[_index]);

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
