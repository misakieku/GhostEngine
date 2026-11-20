using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Streaming;

internal unsafe class MaterialAssetEntry : AssetEntry, ILoadableAssetEntry
{
    private Handle<Material> _actualHandle;
    private Handle<Shader> _shaderHandle;

    public Handle<Material> MaterialHandle => _actualHandle;

    public MaterialAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, Guid[] dependencies, string assetName)
        : base(manager, resourceDatabase, resourceManager, assetId, AssetType.Material, dependencies, assetName)
    {
    }

    public override void ReadAssetData(Span<byte> dst)
    {
        Logger.DebugAssert(dst.Length == sizeof(Handle<Material>));
        Logger.DebugAssert(_actualHandle.IsValid);

        ref var address = ref MemoryMarshal.GetReference(dst);
        Unsafe.WriteUnaligned(ref address, _actualHandle);
    }

    public override void ReadAssetData<T>(ref T dst)
    {
        Logger.DebugAssert(typeof(T) == typeof(Handle<Material>));
        Logger.DebugAssert(_actualHandle.IsValid);

        dst = Unsafe.BitCast<Handle<Material>, T>(_actualHandle);
    }

    protected override void OnReleaseResource()
    {
        if (_actualHandle.IsValid)
        {
            ResourceManager.ReleaseMaterial(_actualHandle);
            _actualHandle = Handle<Material>.Invalid;
        }

        _shaderHandle = Handle<Shader>.Invalid;
    }

    public Result OnLoadContent([Owner] ref AssetPayload payload)
    {
        try
        {
            if (payload.Size < sizeof(MaterialContentHeader))
            {
                return Result.Failure("Material payload is too small to contain a header.");
            }

            var header = payload.Read<MaterialContentHeader>(0);
            if (header.magic != MaterialContentHeader.MAGIC)
            {
                return Result.Failure($"Unexpected material header magic: 0x{header.magic:X8}.");
            }

            if (header.version != MaterialContentHeader.VERSION)
            {
                return Result.Failure($"Unsupported material header version {header.version}.");
            }

            var shaderHandle = Handle<Shader>.Invalid;
            if (Manager.ShaderVariants != null && Manager.ShaderVariants.TryGetShaderHandle(header.shaderId, out var directShaderHandle))
            {
                shaderHandle = directShaderHandle;
            }
            else if (Manager.TryGetEntry(header.shaderId, out var shaderEntry))
            {
                shaderEntry.ReadAssetData(ref shaderHandle);
            }

            if (shaderHandle.IsInvalid)
            {
                return Result.Failure($"Failed to resolve shader handle for shader ID {header.shaderId}.");
            }

            var shaderRef = ResourceManager.GetShaderReference(shaderHandle);
            var expectedSize = shaderRef.IsSuccess ? (int)shaderRef.Value.PropertyBufferSize : (int)header.propertyBufferSize;
            var propBytes = expectedSize > 0 ? BuildPropertyBuffer(ref payload, in header, expectedSize) : Array.Empty<byte>();

            if (_actualHandle.IsValid && ResourceManager.HasMaterial(_actualHandle) && _shaderHandle == shaderHandle)
            {
                if (propBytes.Length > 0)
                {
                    var err = ResourceManager.SetRawMaterialProperty(_actualHandle, propBytes);
                    if (err != Error.None)
                    {
                        return Result.Failure($"Failed to update material property cache: {err}.");
                    }
                }

                IncrementContentVersion();
                return Result.Success();
            }

            var oldHandle = _actualHandle;
            var newHandle = ResourceManager.CreateMaterial(shaderHandle, AssetName);
            if (newHandle.IsInvalid)
            {
                return Result.Failure($"Failed to create material in ResourceManager for '{AssetName}'.");
            }

            if (propBytes.Length > 0)
            {
                var err = ResourceManager.SetRawMaterialProperty(newHandle, propBytes);
                if (err != Error.None)
                {
                    ResourceManager.ReleaseMaterial(newHandle);
                    return Result.Failure($"Failed to set material property cache: {err}.");
                }
            }

            _actualHandle = newHandle;
            _shaderHandle = shaderHandle;

            if (oldHandle.IsValid)
            {
                ResourceManager.ReleaseMaterial(oldHandle);
            }

            IncrementContentVersion();
            return Result.Success();
        }
        finally
        {
            payload.Dispose();
        }
    }

    private byte[] BuildPropertyBuffer(ref AssetPayload payload, in MaterialContentHeader header, int expectedSize)
    {
        var propBytes = new byte[expectedSize];
        var copySize = Math.Min((int)header.propertyBufferSize, expectedSize);
        if (copySize > 0)
        {
            var propSlice = payload.Slice(header.propertyBufferOffset, copySize);
            propSlice.CopyTo(propBytes.AsSpan(0, copySize));
        }

        if (header.textureFixupCount > 0 || header.samplerFixupCount > 0)
        {
            fixed (byte* pBuffer = propBytes)
            {
                for (var i = 0; i < header.textureFixupCount; i++)
                {
                    var fixupOffset = header.textureFixupOffset + i * sizeof(MaterialTextureFixup);
                    var fixup = payload.Read<MaterialTextureFixup>(fixupOffset);

                    if (fixup.textureGuid != Guid.Empty && fixup.byteOffset + sizeof(uint) <= (uint)expectedSize)
                    {
                        if (Manager.TryGetEntry(fixup.textureGuid, out var texEntry))
                        {
                            var texHandle = default(Handle<GPUTexture>);
                            texEntry.ReadAssetData(ref texHandle);

                            var srvIndex = ResourceDatabase.GetBindlessIndex(texHandle.AsResource());
                            *(uint*)(pBuffer + fixup.byteOffset) = srvIndex;
                        }
                    }
                }

                for (var i = 0; i < header.samplerFixupCount; i++)
                {
                    var fixupOffset = header.samplerFixupOffset + i * sizeof(MaterialSamplerFixup);
                    var fixup = payload.Read<MaterialSamplerFixup>(fixupOffset);

                    if (fixup.byteOffset + sizeof(uint) <= (uint)expectedSize)
                    {
                        var samplerType = (uint)fixup.samplerType < (uint)StaticSamplerType.Count
                            ? fixup.samplerType
                            : StaticSamplerType.TrilinearRepeat;
                        var samplerIndex = ResourceManager.StaticSampler.GetSampler(samplerType);
                        *(uint*)(pBuffer + fixup.byteOffset) = samplerIndex;
                    }
                }
            }
        }

        return propBytes;
    }
}
