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

        var (propertyBytes, fixups, samplerFixups) = MaterialPropertyPacker.Pack(semantics, doc.Properties, doc.Textures);

        ctx.AddAssetDependency(shaderGuid);
        foreach (var fixup in fixups)
        {
            if (fixup.textureGuid != Guid.Empty)
            {
                ctx.AddAssetDependency(fixup.textureGuid);
            }
        }

        var propertyBufferOffset = (uint)Marshal.SizeOf<MaterialContentHeader>();
        var textureFixupOffset = propertyBufferOffset + (uint)propertyBytes.Length;
        var samplerFixupOffset = textureFixupOffset + (uint)(fixups.Count * Marshal.SizeOf<MaterialTextureFixup>());

        var header = new MaterialContentHeader
        {
            magic = MaterialContentHeader.MAGIC,
            version = MaterialContentHeader.VERSION,
            shaderId = shaderGuid,
            propertyBufferSize = (uint)propertyBytes.Length,
            textureFixupCount = (uint)fixups.Count,
            samplerFixupCount = (uint)samplerFixups.Count,
            propertyBufferOffset = propertyBufferOffset,
            textureFixupOffset = textureFixupOffset,
            samplerFixupOffset = samplerFixupOffset
        };

        var headerSpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1));
        dst.Write(headerSpan);
        dst.Write(propertyBytes);

        if (fixups.Count > 0)
        {
            var fixupSpan = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(fixups));
            dst.Write(fixupSpan);
        }

        if (samplerFixups.Count > 0)
        {
            var samplerSpan = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(samplerFixups));
            dst.Write(samplerSpan);
        }
    }
}
