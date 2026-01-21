using Ghost.Core;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Represents different types of render passes.
/// </summary>
public enum RenderPassType : byte
{
    Raster,
    Compute,
    Unsafe
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
    public readonly List<Identifier<RGResource>>[] resourceReads = new List<Identifier<RGResource>>[(int)RenderGraphResourceType.Count];
    public readonly List<Identifier<RGResource>>[] resourceWrites = new List<Identifier<RGResource>>[(int)RenderGraphResourceType.Count];
    public readonly List<Identifier<RGResource>>[] resourceCreates = new List<Identifier<RGResource>>[(int)RenderGraphResourceType.Count];

    // Buffer usage hints (maps buffer resource ID to hint)
    public readonly Dictionary<int, BufferHint> bufferHints = new(8);

    // Execution state
    public bool culled;
    public bool hasSideEffects;

    public RenderGraphPassBase()
    {
        for (int i = 0; i < (int)RenderGraphResourceType.Count; i++)
        {
            resourceReads[i] = new List<Identifier<RGResource>>(8);
            resourceWrites[i] = new List<Identifier<RGResource>>(4);
            resourceCreates[i] = new List<Identifier<RGResource>>(4);
        }
    }

    public abstract void Execute(RenderGraphContext context);
    public abstract bool HasRenderFunc();
    public abstract int GetRenderFuncHashCode();

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

        for (var i = 0; i < (int)RenderGraphResourceType.Count; i++)
        {
            resourceReads[i].Clear();
            resourceWrites[i].Clear();
            resourceCreates[i].Clear();
        }

        bufferHints.Clear();

        culled = false;
        hasSideEffects = false;
    }
}

internal abstract class RenderGraphPass<TPassData, TRenderContext> : RenderGraphPassBase
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

    public override int GetRenderFuncHashCode()
    {
        if (renderFunc == null)
        {
            return 0;
        }

        var methodHashCode = RuntimeHelpers.GetHashCode(renderFunc.Method);
        return renderFunc.Target == null ? methodHashCode : methodHashCode ^ RuntimeHelpers.GetHashCode(renderFunc.Target); // static deleget does not have target
    }

    public override void Reset(RenderGraphObjectPool pool)
    {
        base.Reset(pool);
        pool.Return(passData);

        passData = null!;
        renderFunc = null;
    }
}

internal sealed class RasterRenderGraphPass<TPassData> : RenderGraphPass<TPassData, IRasterRenderContext>
    where TPassData : class, new()
{
    public override void Execute(RenderGraphContext context)
    {
        renderFunc!(passData, context);
    }

    public override void Reset(RenderGraphObjectPool pool)
    {
        base.Reset(pool);
        pool.Return(this);
    }
}

internal sealed class ComputeRenderGraphPass<TPassData> : RenderGraphPass<TPassData, IComputeRenderContext>
    where TPassData : class, new()
{
    public override void Execute(RenderGraphContext context)
    {
        renderFunc!(passData, context);
    }

    public override void Reset(RenderGraphObjectPool pool)
    {
        base.Reset(pool);
        pool.Return(this);
    }
}

internal sealed class UnsafeRenderGraphPass<TPassData> : RenderGraphPass<TPassData, IUnsafeRenderContext>
    where TPassData : class, new()
{
    public override void Execute(RenderGraphContext context)
    {
        renderFunc!(passData, context);
    }

    public override void Reset(RenderGraphObjectPool pool)
    {
        base.Reset(pool);
        pool.Return(this);
    }
}
