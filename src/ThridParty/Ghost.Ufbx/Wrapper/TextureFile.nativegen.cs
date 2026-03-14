namespace Ghost.Ufbx;

public unsafe struct TextureFile
{
    private ufbx_texture_file* _ptr;

    internal TextureFile(ufbx_texture_file* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public uint Index => _ptr->index;

    public ReadOnlySpan<byte> FilenameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->filename);
    public string Filename => NativeWrapperHelpers.GetString(_ptr->filename);

    public ReadOnlySpan<byte> AbsoluteFilenameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->absolute_filename);
    public string AbsoluteFilename => NativeWrapperHelpers.GetString(_ptr->absolute_filename);

    public ReadOnlySpan<byte> RelativeFilenameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->relative_filename);
    public string RelativeFilename => NativeWrapperHelpers.GetString(_ptr->relative_filename);

    public ReadOnlySpan<byte> RawFilename => NativeWrapperHelpers.AsSpan(_ptr->raw_filename);

    public ReadOnlySpan<byte> RawAbsoluteFilename => NativeWrapperHelpers.AsSpan(_ptr->raw_absolute_filename);

    public ReadOnlySpan<byte> RawRelativeFilename => NativeWrapperHelpers.AsSpan(_ptr->raw_relative_filename);

    public ReadOnlySpan<byte> Content => NativeWrapperHelpers.AsSpan(_ptr->content);

    internal ufbx_texture_file* GetUnsafePtr() => _ptr;
}
