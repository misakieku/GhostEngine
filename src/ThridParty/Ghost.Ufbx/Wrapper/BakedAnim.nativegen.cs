namespace Ghost.Ufbx;

public unsafe struct BakedAnim
{
    private ufbx_baked_anim* _ptr;

    internal BakedAnim(ufbx_baked_anim* ptr)
    {
        _ptr = ptr;
    }

    public bool IsNull => _ptr == null;

    public void RetainBakedAnim()
    {
        Api.ufbx_retain_baked_anim(_ptr);
    }

    public void FreeBakedAnim()
    {
        Api.ufbx_free_baked_anim(_ptr);
    }

    public BakedNode FindBakedNodeByTypedId(uint typedId)
    {
        return new(Api.ufbx_find_baked_node_by_typed_id(_ptr, typedId));
    }

    public BakedNode FindBakedNode(Node node)
    {
        return new(Api.ufbx_find_baked_node(_ptr, node.GetUnsafePtr()));
    }

    public BakedElement FindBakedElementByElementId(uint elementId)
    {
        return new(Api.ufbx_find_baked_element_by_element_id(_ptr, elementId));
    }

    public BakedElement FindBakedElement(Element element)
    {
        return new(Api.ufbx_find_baked_element(_ptr, element.GetUnsafePtr()));
    }

    public ReadOnlySpan<ufbx_baked_node> Nodes => _ptr->nodes.data == null ? ReadOnlySpan<ufbx_baked_node>.Empty : new ReadOnlySpan<ufbx_baked_node>(_ptr->nodes.data, checked((int)_ptr->nodes.count));

    public ReadOnlySpan<ufbx_baked_element> Elements => _ptr->elements.data == null ? ReadOnlySpan<ufbx_baked_element>.Empty : new ReadOnlySpan<ufbx_baked_element>(_ptr->elements.data, checked((int)_ptr->elements.count));

    public double PlaybackTimeBegin => _ptr->playback_time_begin;

    public double PlaybackTimeEnd => _ptr->playback_time_end;

    public double PlaybackDuration => _ptr->playback_duration;

    public double KeyTimeMin => _ptr->key_time_min;

    public double KeyTimeMax => _ptr->key_time_max;

    public BakedAnimMetadata Metadata => new((ufbx_baked_anim_metadata*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _ptr->metadata));

    internal ufbx_baked_anim* GetUnsafePtr() => _ptr;
}
