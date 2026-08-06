using Ghost.Core;
using System.Collections;

namespace Ghost.Graphics.RenderGraphModule;

internal readonly struct RenderGraphResourceSet : IEnumerable<Identifier<RGResource>>
{
    private readonly List<Identifier<RGResource>> _resources;

    public int Count => _resources.Count;

    public RenderGraphResourceSet(int capacity)
    {
        _resources = new List<Identifier<RGResource>>(capacity);
    }

    public bool Add(Identifier<RGResource> resource)
    {
        for (var i = 0; i < _resources.Count; i++)
        {
            if (_resources[i] == resource)
            {
                return false;
            }
        }

        _resources.Add(resource);
        return true;
    }

    public bool Contains(Identifier<RGResource> resource)
    {
        for (var i = 0; i < _resources.Count; i++)
        {
            if (_resources[i] == resource)
            {
                return true;
            }
        }

        return false;
    }

    public void Clear()
    {
        _resources.Clear();
    }

    public List<Identifier<RGResource>>.Enumerator GetEnumerator()
    {
        return _resources.GetEnumerator();
    }

    IEnumerator<Identifier<RGResource>> IEnumerable<Identifier<RGResource>>.GetEnumerator()
    {
        return _resources.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _resources.GetEnumerator();
    }
}
