using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Ghost.AssetForge.Core.Bakers;

[AssetBaker(Extensions = [".gmat"], Type = AssetType.Material, SettingsType = typeof(MaterialBakeSettings), SettingsVersion = MaterialBakeSettings.VERSION)]
public class MaterialBaker : IAssetBaker
{
    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        var jsonText = await File.ReadAllTextAsync(src, cancellationToken).ConfigureAwait(false);
        var doc = JsonSerializer.Deserialize(jsonText, MaterialAssetDocumentContext.Default.MaterialAssetDocument) ?? new MaterialAssetDocument();

        var shaderGuid = doc.Shader != Guid.Empty ? doc.Shader : MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID;
        var semantics = MaterialPropertyPacker.GetShaderSemantics(shaderGuid, ctx.AssetDirectories);
        if (semantics == null)
        {
            throw new InvalidOperationException($"Failed to resolve or parse shader for GUID '{shaderGuid}'.");
        }

        var (propertyBytes, fixups) = MaterialPropertyPacker.Pack(semantics, doc.Properties, doc.Textures);

        ctx.AddAssetDependency(shaderGuid);
        foreach (var fixup in fixups)
        {
            if (fixup.textureGuid != Guid.Empty)
            {
                ctx.AddAssetDependency(fixup.textureGuid);
            }
        }

        var header = new MaterialContentHeader
        {
            magic = MaterialContentHeader.MAGIC,
            version = MaterialContentHeader.VERSION,
            shaderId = shaderGuid,
            propertyBufferSize = (uint)propertyBytes.Length,
            textureFixupCount = (uint)fixups.Count,
            propertyBufferOffset = (uint)Marshal.SizeOf<MaterialContentHeader>(),
            textureFixupOffset = (uint)(Marshal.SizeOf<MaterialContentHeader>() + propertyBytes.Length)
        };

        var headerSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1));
        dst.Write(headerSpan);
        dst.Write(propertyBytes);

        if (fixups.Count > 0)
        {
            var fixupSpan = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(fixups));
            dst.Write(fixupSpan);
        }
    }
}
