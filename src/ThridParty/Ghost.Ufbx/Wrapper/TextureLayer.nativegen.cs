namespace Ghost.Ufbx;

public unsafe struct TextureLayer
{
    private ufbx_texture_layer* _ptr;

    internal TextureLayer(ufbx_texture_layer* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public bool HasTexture => _ptr->texture != null;
    public Texture Texture => _ptr->texture != null ? new(_ptr->texture) : throw new InvalidOperationException("Texture is null.");

    public ufbx_blend_mode BlendMode => _ptr->blend_mode;

    public float Alpha => _ptr->alpha;

    internal ufbx_texture_layer* GetUnsafePtr() => _ptr;
}
