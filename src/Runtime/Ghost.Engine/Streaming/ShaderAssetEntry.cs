using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

internal unsafe class ShaderAssetEntry : AssetEntry, ILoadableAssetEntry
{
    private Handle<Shader> _actualHandle;
    public Handle<Shader> _tempHandle;

    public ShaderAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies)
    {
    }

    public override void ReadAssetData(Span<byte> dst)
    {
        Logger.DebugAssert(dst.Length == sizeof(Handle<Shader>));
        Logger.DebugAssert(_actualHandle.IsValid);

        ref var address = ref MemoryMarshal.GetReference(dst);
        Unsafe.WriteUnaligned(ref address, _actualHandle);
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        Logger.DebugAssert(typeof(T) == typeof(Handle<Shader>));
        Logger.DebugAssert(_actualHandle.IsValid);

        dst = Unsafe.BitCast<Handle<Shader>, T>(_actualHandle);
    }

    public Result OnLoadContent(Stream contentStream)
    {
        throw new NotImplementedException();
    }
}
