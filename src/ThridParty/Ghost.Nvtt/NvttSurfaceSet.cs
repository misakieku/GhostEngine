namespace Ghost.Nvtt;

/// <summary>
/// Wrapper around an nvtt surface set — a collection of faces and mip levels
/// loaded from a DDS file or built programmatically.
/// </summary>
public sealed unsafe class NvttSurfaceSetHandle : IDisposable
{
    private NvttSurfaceSet* _ptr;

    /// <summary>Raw pointer – use only when calling the native API directly.</summary>
    public NvttSurfaceSet* Ptr => _ptr;

    // -------------------------------------------------------------------------
    // Construction / destruction
    // -------------------------------------------------------------------------

    public NvttSurfaceSetHandle() => _ptr = Api.nvttCreateSurfaceSet();

    public void Dispose()
    {
        if (_ptr != null)
        {
            Api.nvttDestroySurfaceSet(_ptr);
            _ptr = null;
        }
    }

    // -------------------------------------------------------------------------
    // Read-only properties
    // -------------------------------------------------------------------------

    /// <summary>Texture dimensionality stored in this set.</summary>
    public NvttTextureType TextureType
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceSetGetTextureType(_ptr); }
    }

    /// <summary>Number of faces (1 for 2-D / 3-D, 6 for cube maps).</summary>
    public int FaceCount
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceSetGetFaceCount(_ptr); }
    }

    /// <summary>Number of mip levels.</summary>
    public int MipmapCount
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceSetGetMipmapCount(_ptr); }
    }

    /// <summary>Width of the base (mip 0) image in pixels.</summary>
    public int Width
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceSetGetWidth(_ptr); }
    }

    /// <summary>Height of the base (mip 0) image in pixels.</summary>
    public int Height
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceSetGetHeight(_ptr); }
    }

    /// <summary>Depth of the base (mip 0) image (1 for 2-D textures).</summary>
    public int Depth
    {
        get { ThrowIfDisposed(); return Api.nvttSurfaceSetGetDepth(_ptr); }
    }

    // -------------------------------------------------------------------------
    // Surface access
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the raw <see cref="NvttSurface"/> pointer for the given face
    /// and mip level.  The pointer is owned by this surface set – do NOT dispose
    /// it.
    /// </summary>
    public NvttSurface* GetSurfacePtr(int faceId, int mipId, bool expectSigned = false)
    {
        ThrowIfDisposed();
        return Api.nvttSurfaceSetGetSurface(_ptr, faceId, mipId,
            NvttInterop.ToNvtt(expectSigned));
    }

    // -------------------------------------------------------------------------
    // Load / Save
    // -------------------------------------------------------------------------

    /// <summary>Resets the surface set to an empty state.</summary>
    public void Reset()
    {
        ThrowIfDisposed();
        Api.nvttResetSurfaceSet(_ptr);
    }

    /// <summary>Loads from a DDS file.  Returns <c>false</c> on failure.</summary>
    public bool LoadDDS(string fileName, bool forceNormal = false)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[NvttInterop._MAX_STACK_PATH];
        var utf8 = NvttInterop.ToUtf8(fileName, buf);
        fixed (byte* p = utf8)
        {
            return NvttInterop.ToBool(
                Api.nvttSurfaceSetLoadDDS(_ptr, (sbyte*)p,
                    NvttInterop.ToNvtt(forceNormal)));
        }
    }

    /// <summary>Loads from a managed byte array containing DDS data.  Returns <c>false</c> on failure.</summary>
    public bool LoadDDSFromMemory(ReadOnlySpan<byte> data, bool forceNormal = false)
    {
        ThrowIfDisposed();
        fixed (byte* p = data)
        {
            return NvttInterop.ToBool(
                Api.nvttSurfaceSetLoadDDSFromMemory(_ptr, p, (ulong)data.Length,
                    NvttInterop.ToNvtt(forceNormal)));
        }
    }

    /// <summary>Saves a single face/mip as an image file.  Returns <c>false</c> on failure.</summary>
    public bool SaveImage(string fileName, int faceId, int mipId)
    {
        ThrowIfDisposed();
        Span<byte> buf = stackalloc byte[NvttInterop._MAX_STACK_PATH];
        var utf8 = NvttInterop.ToUtf8(fileName, buf);
        fixed (byte* p = utf8)
        {
            return NvttInterop.ToBool(
                Api.nvttSurfaceSetSaveImage(_ptr, (sbyte*)p, faceId, mipId));
        }
    }

    // -------------------------------------------------------------------------

    private void ThrowIfDisposed()
    {
        if (_ptr == null)
        {
            throw new ObjectDisposedException(nameof(NvttSurfaceSetHandle));
        }
    }
}
