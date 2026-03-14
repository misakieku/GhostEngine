namespace Ghost.Ufbx;

public unsafe struct AudioClip
{
    private ufbx_audio_clip* _ptr;

    internal AudioClip(ufbx_audio_clip* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

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

    public Element Element => new((ufbx_element*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->element));

    public ReadOnlySpan<byte> NameBytes => NativeWrapperHelpers.AsByteSpan(_ptr->name);
    public string Name => NativeWrapperHelpers.GetString(_ptr->name);

    public Props Props => new((ufbx_props*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->props));

    public uint ElementId => _ptr->element_id;

    public uint TypedId => _ptr->typed_id;

    internal ufbx_audio_clip* GetUnsafePtr() => _ptr;
}
