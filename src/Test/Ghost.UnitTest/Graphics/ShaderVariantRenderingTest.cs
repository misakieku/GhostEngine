using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public sealed class ShaderVariantRenderingTest
{
    private sealed class VariantSource : IShaderVariantSource
    {
        private readonly ShaderVariantDispatchInfo[] _variants;
        private readonly bool[] _ready;

        public VariantSource(ShaderVariantDispatchInfo[] variants, bool[] ready)
        {
            _variants = variants;
            _ready = ready;
        }

        public ReadOnlySpan<ShaderVariantDispatchInfo> GetDispatchVariants(PassSemantic semantic)
        {
            return semantic == PassSemantic.DeferredTexturing ? _variants : ReadOnlySpan<ShaderVariantDispatchInfo>.Empty;
        }

        public bool IsBytecodeReady(int denseIndex)
        {
            return (uint)denseIndex < (uint)_ready.Length && _ready[denseIndex];
        }
    }

    private sealed class CommandSignature : ICommandSignature
    {
        public string Name { get; set; } = nameof(CommandSignature);
        public IntPtr NativePointer => IntPtr.Zero;

        public void Dispose()
        {
        }
    }

    private sealed class ComputeContext : IComputeRenderContext
    {
        private readonly Handle<Shader> _rejectedShader;

        public List<Handle<Shader>> BindAttempts { get; } = new();
        public List<(uint maxCommandCount, ulong argumentOffset, ulong countOffset)> Executions { get; } = new();

        public ResourceManager ResourceManager => null!;
        public IResourceDatabase ResourceDatabase => null!;
        public float2 RelativeScale => default;

        public ComputeContext(Handle<Shader> rejectedShader)
        {
            _rejectedShader = rejectedShader;
        }

        public Handle<GPUResource> GetActualResource(Identifier<RGResource> resource) => Handle<GPUResource>.Invalid;
        public Handle<GPUTexture> GetActualTexture(Identifier<RGTexture> texture) => Handle<GPUTexture>.Invalid;
        public Handle<GPUBuffer> GetActualBuffer(Identifier<RGBuffer> buffer) => Handle<GPUBuffer>.Invalid;

        public void SetActiveCompute(Handle<ComputeShader> computeShader, int entryIndex)
        {
        }

        public bool TrySetActiveShaderPass(Handle<Shader> shader, PassSemantic semantic)
        {
            BindAttempts.Add(shader);
            return shader != _rejectedShader;
        }

        public void DispatchCompute(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
        {
        }

        public void ExecuteIndirect(ICommandSignature commandSignature, uint maxCommandCount, Handle<GPUBuffer> argumentBuffer, ulong argumentOffset, Handle<GPUBuffer> countBuffer, ulong countBufferOffset)
        {
            Executions.Add((maxCommandCount, argumentOffset, countBufferOffset));
        }
    }

    [TestMethod]
    public void ExecuteIndirectComputeSkipsUnavailableVariantsAndUsesDenseSlots()
    {
        var firstShader = new Handle<Shader>(1, 1);
        var rejectedShader = new Handle<Shader>(2, 1);
        var unavailableShader = new Handle<Shader>(3, 1);
        var source = new VariantSource(
            new ShaderVariantDispatchInfo[]
            {
                new ShaderVariantDispatchInfo(1, firstShader),
                new ShaderVariantDispatchInfo(3, rejectedShader),
                new ShaderVariantDispatchInfo(5, unavailableShader),
            },
            new bool[] { false, true, false, true, false, false });
        var context = new ComputeContext(rejectedShader);
        using var commandSignature = new CommandSignature();

        var executeCount = ShaderVariantRendering.ExecuteIndirectCompute(
            source,
            context,
            PassSemantic.DeferredTexturing,
            commandSignature,
            64,
            Handle<GPUBuffer>.Invalid,
            128,
            256,
            Handle<GPUBuffer>.Invalid,
            16,
            sizeof(uint));

        Assert.AreEqual(1, executeCount);
        Assert.HasCount(2, context.BindAttempts);
        Assert.HasCount(1, context.Executions);
        Assert.AreEqual((64u, 384UL, 20UL), context.Executions[0]);
    }
}
