namespace Ghost.Nvtt.Warper;

/// <summary>
/// Wraps an nvtt timing context that records per-operation wall-clock times.
/// Obtain one from <see cref="NvttContext.TimingContext"/> or create a
/// standalone instance and pass it as the optional <c>tc</c> parameter on
/// surface methods.
/// </summary>
public sealed unsafe class NvttTimingContextHandle : IDisposable
{
    private NvttTimingContext* _ptr;

    /// <summary>Raw pointer - use only when calling the native API directly.</summary>
    public NvttTimingContext* Ptr => _ptr;

    // -------------------------------------------------------------------------
    // Construction / destruction
    // -------------------------------------------------------------------------

    /// <summary>Creates a timing context at the specified detail level (0 = off, higher = more detail).</summary>
    public NvttTimingContextHandle(int detailLevel = 1)
        => _ptr = Api.nvttCreateTimingContext(detailLevel);

    /// <summary>Wraps an already-owned native pointer (ownership transferred to this object).</summary>
    internal NvttTimingContextHandle(NvttTimingContext* owned) => _ptr = owned;

    public void Dispose()
    {
        if (_ptr != null)
        {
            Api.nvttDestroyTimingContext(_ptr);
            _ptr = null;
        }
    }

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets the detail level (0 = disabled; higher values record more sub-operations).
    /// </summary>
    public int DetailLevel
    {
        set
        {
            ThrowIfDisposed();
            Api.nvttTimingContextSetDetailLevel(_ptr, value);
        }
    }

    /// <summary>Number of timing records captured so far.</summary>
    public int RecordCount
    {
        get
        {
            ThrowIfDisposed();
            return Api.nvttTimingContextGetRecordCount(_ptr);
        }
    }

    // -------------------------------------------------------------------------
    // Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the description and elapsed seconds for the record at
    /// <paramref name="index"/>.
    /// </summary>
    public (string description, double seconds) GetRecord(int index)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[256];
        double seconds;
        fixed (byte* p = buf)
        {
            Api.nvttTimingContextGetRecordSafe(_ptr, index, (sbyte*)p, (nuint)buf.Length, &seconds);
            var desc = NvttInterop.FromUtf8((sbyte*)p) ?? string.Empty;
            return (desc, seconds);
        }
    }

    /// <summary>Prints all timing records to stdout via the native library.</summary>
    public void PrintRecords()
    {
        ThrowIfDisposed();
        Api.nvttTimingContextPrintRecords(_ptr);
    }

    // -------------------------------------------------------------------------

    private void ThrowIfDisposed()
    {
        if (_ptr == null)
        {
            throw new ObjectDisposedException(nameof(NvttTimingContextHandle));
        }
    }
}
