namespace Ghost.Nvtt;

/// <summary>
/// Wrapper around an nvtt batch list — a list of (surface, face, mipmap,
/// outputOptions) tuples passed to <see cref="NvttContext.CompressBatch"/>.
/// </summary>
public sealed unsafe class NvttBatchListHandle : IDisposable
{
    private NvttBatchList* _ptr;

    /// <summary>Raw pointer - use only when calling the native API directly.</summary>
    public NvttBatchList* Ptr => _ptr;

    // -------------------------------------------------------------------------
    // Construction / destruction
    // -------------------------------------------------------------------------

    public NvttBatchListHandle() => _ptr = Api.nvttCreateBatchList();

    public void Dispose()
    {
        if (_ptr != null)
        {
            Api.nvttDestroyBatchList(_ptr);
            _ptr = null;
        }
    }

    // -------------------------------------------------------------------------
    // Mutation
    // -------------------------------------------------------------------------

    /// <summary>Removes all items from the list.</summary>
    public void Clear()
    {
        ThrowIfDisposed();
        Api.nvttBatchListClear(_ptr);
    }

    /// <summary>
    /// Appends an entry.  The <paramref name="surface"/> and
    /// <paramref name="outputOptions"/> must remain alive for the duration of
    /// any subsequent <see cref="NvttContext.CompressBatch"/> call.
    /// </summary>
    public void Append(NvttSurfaceHandle surface, int face, int mipmap,
        NvttOutputOptionsHandle outputOptions)
    {
        ThrowIfDisposed();
        Api.nvttBatchListAppend(_ptr, surface.Ptr, face, mipmap,
            outputOptions.Ptr);
    }

    // -------------------------------------------------------------------------
    // Query
    // -------------------------------------------------------------------------

    /// <summary>Number of items currently in the list.</summary>
    public uint Count
    {
        get { ThrowIfDisposed(); return Api.nvttBatchListGetSize(_ptr); }
    }

    /// <summary>
    /// Returns the raw pointers for item <paramref name="index"/>.
    /// The pointers are borrowed - do NOT dispose them.
    /// </summary>
    public void GetItem(uint index,
        out NvttSurface* surface, out int face, out int mipmap,
        out NvttOutputOptions* outputOptions)
    {
        ThrowIfDisposed();
        NvttSurface* s;
        NvttOutputOptions* o;
        int f, m;
        Api.nvttBatchListGetItem(_ptr, index, &s, &f, &m, &o);
        surface = s;
        face = f;
        mipmap = m;
        outputOptions = o;
    }

    // -------------------------------------------------------------------------

    private void ThrowIfDisposed()
    {
        if (_ptr == null)
        {
            throw new ObjectDisposedException(nameof(NvttBatchListHandle));
        }
    }
}
