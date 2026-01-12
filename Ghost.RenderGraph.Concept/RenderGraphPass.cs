using Ghost.Core;
using System.IO;

namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Represents different types of render passes.
/// </summary>
public enum RenderPassType : byte
{
    Raster,
    Compute
}

/// <summary>
/// Base class for render passes.
/// Uses pooling to avoid allocations after the first frame.
/// </summary>
internal abstract class RenderGraphPassBase
{
    public string name = string.Empty;
    public int index;
    public RenderPassType type;
    public bool allowCulling = true;
    public bool asyncCompute;

    public TextureAccess depthAccess;
    public TextureAccess[] colorAccess = new TextureAccess[8];
    public int maxColorIndex = -1;

    public List<Identifier<RGResource>> randomAccess = new(8);

    // Resource dependencies
    public readonly List<Identifier<RGResource>> resourceReads = new(8);
    public readonly List<Identifier<RGResource>> resourceWrites = new(4);
    public readonly List<Identifier<RGResource>> resourceCreates = new(4);
    
    // Execution state
    public bool culled;
    public bool hasSideEffects;

    public abstract void Execute(RenderContext context);
    public abstract void Clear();
    public abstract bool HasRenderFunc();

    public virtual void Reset(RenderGraphObjectPool pool)
    {
        name = string.Empty;
        index = -1;
        type = RenderPassType.Raster;
        allowCulling = true;
        asyncCompute = false;
        
        depthAccess = default;
        colorAccess.AsSpan().Clear();
        maxColorIndex = -1;

        randomAccess.Clear();

        resourceReads.Clear();
        resourceWrites.Clear();
        resourceCreates.Clear();
        culled = false;
        hasSideEffects = false;
    }
}

internal abstract class RenderGraphPassT<TPassData, TRenderContext> : RenderGraphPassBase
    where TPassData : class, new()
{
    public TPassData passData = null!;
    public Action<TPassData, TRenderContext>? renderFunc;

    public void Init(int index, TPassData passData, string name, RenderPassType type)
    {
        this.index = index;
        this.passData = passData;
        this.name = name;
        this.type = type;
    }

    public sealed override bool HasRenderFunc()
    {
        return renderFunc != null;
    }

    public override void Clear()
    {
        passData = null!;
        renderFunc = null;
    }

    public override void Reset(RenderGraphObjectPool pool)
    {
        base.Reset(pool);
        pool.Return(passData);
        Clear();
    }
}

internal sealed class RasterRenderGraphPass<TPassData> : RenderGraphPassT<TPassData, RasterRenderContext>
    where TPassData : class, new()
{
    public override void Execute(RenderContext context)
    {
        renderFunc!(passData, context.RasterContext);
    }

    public override void Reset(RenderGraphObjectPool pool)
    {
        base.Reset(pool);
        pool.Return(this);
    }
}

internal sealed class ComputeRenderGraphPass<TPassData> : RenderGraphPassT<TPassData, ComputeRenderContext>
    where TPassData : class, new()
{
    public override void Execute(RenderContext context)
    {
        renderFunc!(passData, context.ComputeContext);
    }

    public override void Reset(RenderGraphObjectPool pool)
    {
        base.Reset(pool);
        pool.Return(this);
    }
}