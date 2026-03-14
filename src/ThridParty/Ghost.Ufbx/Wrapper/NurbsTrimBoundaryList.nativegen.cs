namespace Ghost.Ufbx;

public unsafe readonly ref struct NurbsTrimBoundaryList
{
    private readonly ufbx_nurbs_trim_boundary** _data;
    public int Count { get; }

    internal NurbsTrimBoundaryList(ufbx_nurbs_trim_boundary** data, nuint count)
    {
        _data = data;
        Count = checked((int)count);
    }

    public NurbsTrimBoundary this[int index]
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
        private readonly ufbx_nurbs_trim_boundary** _data;
        private readonly int _count;
        private int _index;

        internal Enumerator(ufbx_nurbs_trim_boundary** data, int count)
        {
            _data = data;
            _count = count;
            _index = -1;
        }

        public NurbsTrimBoundary Current => new(_data[_index]);

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
