using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics.RenderPipeline;

public interface IRenderPipeline : IDisposable
{
    void AddRenderList(RenderList renderList, string key);

    void Render(RenderContext ctx, ReadOnlySpan<RenderRequest> requests);
}

public abstract class RenderPipelineBase : IRenderPipeline
{
    protected readonly Dictionary<string, RenderList> _renderLists = new();

    private bool _disposed;

    ~RenderPipelineBase()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void AddRenderList(RenderList renderList, string key)
    {
        ThrowIfDisposed();

        ref var existingList = ref CollectionsMarshal.GetValueRefOrAddDefault(_renderLists, key, out var exists);
        if (!exists)
        {
            existingList = renderList;
        }
        else
        {
            existingList.Append(renderList);
        }
    }

    public abstract void Render(RenderContext ctx, ReadOnlySpan<RenderRequest> requests);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Dispose(true);

        foreach (var list in _renderLists.Values)
        {
            list.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public virtual void Dispose(bool disposing)
    {
    }
}
