using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Ufbx;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Ghost.AssetForge.Core.Bakers;

public unsafe partial class MeshBaker
{
    private static readonly string[] s_supportedTextureExtensions = [".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr"];

    private sealed class MaterialResolutionState
    {
        public string ModelVirtualDir { get; }
        public string ExtractRootVirtualDir { get; }
        public ufbx_scene* Scene { get; }
        public MeshBakeSettings Settings { get; }
        public AssetBakerContext Context { get; }
        public HashSet<Guid> BakedSubMaterials { get; } = new();
        public Dictionary<string, Guid> ExtractedMaterialGuids { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<IntPtr, Guid> ResolvedTextureGuids { get; } = new();

        public MaterialResolutionState(string sourceFilePath, ufbx_scene* scene, MeshBakeSettings settings, AssetBakerContext ctx)
        {
            Scene = scene;
            Settings = settings;
            Context = ctx;

            var virtualModelPath = ResolveModelVirtualPath(sourceFilePath, ctx);
            var slash = virtualModelPath.LastIndexOf('/');
            ModelVirtualDir = slash >= 0 ? virtualModelPath[..slash] : string.Empty;

            var modelStem = SanitizeAssetFileName(Path.GetFileNameWithoutExtension(virtualModelPath), "Model");
            var extractFolder = $"{modelStem}_Extracted";
            ExtractRootVirtualDir = string.IsNullOrEmpty(ModelVirtualDir) ? extractFolder : $"{ModelVirtualDir}/{extractFolder}";
        }
    }

    private static Guid ResolveOrBakeMaterial(ufbx_material* pMat, string matName, Guid assetId, MaterialResolutionState state)
    {
        var scalarValues = ReadMaterialScalars(pMat);
        var textureGuids = ResolveMaterialTextures(pMat, state);

        if (state.Settings.ExtractMaterials)
        {
            if (state.ExtractedMaterialGuids.TryGetValue(matName, out var cachedMatGuid))
            {
                return cachedMatGuid;
            }

            var safeMatName = SanitizeAssetFileName(matName, "Material");
            var matVirtualPath = $"{state.ExtractRootVirtualDir}/Materials/{safeMatName}.gmat";

            var doc = new MaterialAssetDocument
            {
                Shader = MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID
            };

            foreach (var (propName, propVal) in scalarValues)
            {
                if (propVal is float f)
                {
                    doc.Properties[propName] = JsonSerializer.SerializeToElement(f, MaterialAssetDocumentContext.Default.Single);
                }
                else if (propVal is float3 f3)
                {
                    doc.Properties[propName] = JsonSerializer.SerializeToElement(new[] { f3.x, f3.y, f3.z }, MaterialAssetDocumentContext.Default.SingleArray);
                }
            }

            foreach (var (slotName, texGuid) in textureGuids)
            {
                if (texGuid != Guid.Empty)
                {
                    doc.Textures[slotName] = texGuid;
                }
            }

            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(doc, MaterialAssetDocumentContext.Default.MaterialAssetDocument);
            var extractedGuid = state.Context.ImportAsset(matVirtualPath, jsonBytes, overwrite: false);
            state.ExtractedMaterialGuids[matName] = extractedGuid;
            return extractedGuid;
        }

        var matSubPath = $"Materials/{matName}";
        var matSubGuid = GuidUtility.DeriveSubAssetGuid(assetId, matSubPath);
        if (state.BakedSubMaterials.Add(matSubGuid))
        {
            BakeSubAssetMaterial(matSubPath, scalarValues, textureGuids, state.Context);
        }

        return matSubGuid;
    }

    private static void ExtractRemainingEmbeddedTextures(MaterialResolutionState state)
    {
        if (!state.Settings.ExtractEmbeddedTextures || state.Scene == null)
        {
            return;
        }

        for (var i = 0u; i < state.Scene->textures.count; i++)
        {
            var pTex = state.Scene->textures.data[i];
            if (pTex != null)
            {
                ResolveTextureGuid(pTex, $"Texture_{i}", state);
            }
        }
    }

    private static void BakeSubAssetMaterial(string matSubPath, Dictionary<string, object> scalarValues, Dictionary<string, Guid> textureGuids, AssetBakerContext ctx)
    {
        var semantics = MaterialPropertyPacker.GetShaderSemantics(MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID, ctx.AssetDirectories);
        if (semantics == null)
        {
            throw new InvalidOperationException("Failed to resolve or parse StandardLit shader for sub-asset material baking.");
        }

        var (propBytes, matFixups, samplerFixups) = MaterialPropertyPacker.Pack(
            semantics,
            jsonProperties: null,
            jsonTextures: null,
            valueFallback: name => scalarValues.TryGetValue(name, out var val) ? val : null,
            textureFallback: name => textureGuids.TryGetValue(name, out var guid) && guid != Guid.Empty ? guid : null);

        var matDeps = new List<Guid> { MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID };
        foreach (var fixup in matFixups)
        {
            if (fixup.textureGuid != Guid.Empty && !matDeps.Contains(fixup.textureGuid))
            {
                matDeps.Add(fixup.textureGuid);
            }
        }

        var propertyBufferOffset = Marshal.SizeOf<MaterialContentHeader>();
        var textureFixupOffset = propertyBufferOffset + propBytes.Length;
        var samplerFixupOffset = textureFixupOffset + matFixups.Count * Marshal.SizeOf<MaterialTextureFixup>();

        using var matStream = ctx.AddSubAsset(matSubPath, AssetType.Material, CollectionsMarshal.AsSpan(matDeps));
        var matHeader = new MaterialContentHeader
        {
            magic = MaterialContentHeader.MAGIC,
            version = MaterialContentHeader.VERSION,
            shaderId = MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID,
            propertyBufferSize = (uint)propBytes.Length,
            textureFixupCount = (uint)matFixups.Count,
            samplerFixupCount = (uint)samplerFixups.Count,
            propertyBufferOffset = propertyBufferOffset,
            textureFixupOffset = textureFixupOffset,
            samplerFixupOffset = samplerFixupOffset
        };

        matStream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref matHeader, 1)));
        matStream.Write(propBytes);
        if (matFixups.Count > 0)
        {
            matStream.Write(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(matFixups)));
        }

        if (samplerFixups.Count > 0)
        {
            matStream.Write(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(samplerFixups)));
        }
    }

    private static Dictionary<string, object> ReadMaterialScalars(ufbx_material* pMat)
    {
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (pMat == null)
        {
            return values;
        }

        if (pMat->pbr.base_color.has_value)
        {
            var c = pMat->pbr.base_color.value_vec3;
            values["baseColor"] = new float3(c.x, c.y, c.z);
        }
        else if (pMat->fbx.diffuse_color.has_value)
        {
            var c = pMat->fbx.diffuse_color.value_vec3;
            values["baseColor"] = new float3(c.x, c.y, c.z);
        }

        if (pMat->pbr.roughness.has_value)
        {
            values["roughness"] = pMat->pbr.roughness.value_real;
        }

        if (pMat->pbr.metalness.has_value)
        {
            values["metallic"] = pMat->pbr.metalness.value_real;
        }

        if (pMat->pbr.emission_color.has_value)
        {
            var e = pMat->pbr.emission_color.value_vec3;
            values["emissionColor"] = new float3(e.x, e.y, e.z);
        }
        else if (pMat->fbx.emission_color.has_value)
        {
            var e = pMat->fbx.emission_color.value_vec3;
            values["emissionColor"] = new float3(e.x, e.y, e.z);
        }

        return values;
    }

    private static Dictionary<string, Guid> ResolveMaterialTextures(ufbx_material* pMat, MaterialResolutionState state)
    {
        var textures = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (pMat == null)
        {
            return textures;
        }

        TryBindMaterialTextureSlot(textures, "baseColorMap", SelectTexture(pMat->pbr.base_color, pMat->fbx.diffuse_color), "baseColor", state);
        TryBindMaterialTextureSlot(textures, "normalMap", SelectTexture(pMat->pbr.normal_map, pMat->fbx.normal_map, pMat->fbx.bump), "normal", state);
        TryBindMaterialTextureSlot(textures, "ormMap", SelectTexture(pMat->pbr.ambient_occlusion, pMat->pbr.roughness, pMat->pbr.metalness), "orm", state);
        TryBindMaterialTextureSlot(textures, "emissionMap", SelectTexture(pMat->pbr.emission_color, pMat->fbx.emission_color), "emission", state);
        return textures;
    }

    private static ufbx_texture* SelectTexture(in ufbx_material_map primary, in ufbx_material_map secondary)
    {
        if (primary.texture != null && primary.texture_enabled)
        {
            return primary.texture;
        }

        if (secondary.texture != null && secondary.texture_enabled)
        {
            return secondary.texture;
        }

        return null;
    }

    private static ufbx_texture* SelectTexture(in ufbx_material_map primary, in ufbx_material_map secondary, in ufbx_material_map tertiary)
    {
        var first = SelectTexture(primary, secondary);
        if (first != null)
        {
            return first;
        }

        return tertiary.texture != null && tertiary.texture_enabled ? tertiary.texture : null;
    }

    private static void TryBindMaterialTextureSlot(Dictionary<string, Guid> textures, string slotName, ufbx_texture* pTex, string fallbackPrefix, MaterialResolutionState state)
    {
        if (pTex == null)
        {
            return;
        }

        var fallbackName = $"{fallbackPrefix}_{pTex->element_id}";
        var texGuid = ResolveTextureGuid(pTex, fallbackName, state);
        if (texGuid != Guid.Empty)
        {
            textures[slotName] = texGuid;
        }
    }

    private static Guid ResolveTextureGuid(ufbx_texture* pTex, string fallbackStem, MaterialResolutionState state)
    {
        var fileTex = UnwrapFileTexture(pTex);
        if (fileTex == null)
        {
            return Guid.Empty;
        }

        var key = (IntPtr)fileTex;
        if (state.ResolvedTextureGuids.TryGetValue(key, out var cachedGuid))
        {
            return cachedGuid;
        }

        // First follow Unity-style lookup against project asset directories without copying files outside the project.
        foreach (var candidateVirtualPath in BuildExternalTextureCandidatePaths(fileTex, state))
        {
            if (state.Context.TryResolveProjectAssetGuid(candidateVirtualPath, out var existingGuid, out _))
            {
                state.ResolvedTextureGuids[key] = existingGuid;
                return existingGuid;
            }
        }

        if (state.Settings.ExtractEmbeddedTextures && TryGetEmbeddedTextureBytes(fileTex, state.Scene, out var embeddedData))
        {
            var rawName = GetPreferredTextureName(fileTex, fallbackStem);
            var stem = SanitizeAssetFileName(Path.GetFileNameWithoutExtension(rawName), fallbackStem);
            var ext = ResolveEmbeddedTextureExtension(rawName, embeddedData);
            var targetVirtualPath = $"{state.ExtractRootVirtualDir}/Textures/{stem}{ext}";

            var extractedGuid = state.Context.ImportAsset(targetVirtualPath, embeddedData, overwrite: false);
            state.ResolvedTextureGuids[key] = extractedGuid;
            return extractedGuid;
        }

        state.ResolvedTextureGuids[key] = Guid.Empty;
        return Guid.Empty;
    }

    private static ufbx_texture* UnwrapFileTexture(ufbx_texture* pTex)
    {
        if (pTex == null)
        {
            return null;
        }

        if (pTex->type != ufbx_texture_type.UFBX_TEXTURE_FILE && pTex->file_textures.count > 0 && pTex->file_textures.data[0] != null)
        {
            return pTex->file_textures.data[0];
        }

        return pTex;
    }

    private static List<string> BuildExternalTextureCandidatePaths(ufbx_texture* pTex, MaterialResolutionState state)
    {
        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var relName = pTex->relative_filename.ToString();
        var fileNamePath = pTex->filename.ToString();
        var candidatesRaw = new[] { relName, fileNamePath };

        for (var i = 0; i < candidatesRaw.Length; i++)
        {
            var raw = candidatesRaw[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var normalized = raw.Replace('\\', '/');
            if (!Path.IsPathRooted(normalized))
            {
                var relToModel = CombineAndNormalizeVirtualPath(state.ModelVirtualDir, normalized);
                if (!string.IsNullOrEmpty(relToModel) && seen.Add(relToModel))
                {
                    results.Add(relToModel);
                }

                var trimmedRoot = normalized.TrimStart('/');
                if (!trimmedRoot.StartsWith("../", StringComparison.Ordinal) && seen.Add(trimmedRoot))
                {
                    results.Add(trimmedRoot);
                }
            }

            var leafFileName = Path.GetFileName(normalized);
            if (!string.IsNullOrWhiteSpace(leafFileName))
            {
                var inExtracted = $"{state.ExtractRootVirtualDir}/Textures/{leafFileName}";
                if (seen.Add(inExtracted))
                {
                    results.Add(inExtracted);
                }

                var inModelTextures = CombineAndNormalizeVirtualPath(state.ModelVirtualDir, $"Textures/{leafFileName}");
                if (!string.IsNullOrEmpty(inModelTextures) && seen.Add(inModelTextures))
                {
                    results.Add(inModelTextures);
                }

                var nextToModel = CombineAndNormalizeVirtualPath(state.ModelVirtualDir, leafFileName);
                if (!string.IsNullOrEmpty(nextToModel) && seen.Add(nextToModel))
                {
                    results.Add(nextToModel);
                }
            }
        }

        return results;
    }

    private static bool TryGetEmbeddedTextureBytes(ufbx_texture* pTex, ufbx_scene* scene, out ReadOnlySpan<byte> bytes)
    {
        if (pTex->content.size > 0 && pTex->content.data != null && pTex->content.size <= int.MaxValue)
        {
            bytes = new ReadOnlySpan<byte>(pTex->content.data, (int)pTex->content.size);
            return true;
        }

        if (pTex->video != null && pTex->video->content.size > 0 && pTex->video->content.data != null && pTex->video->content.size <= int.MaxValue)
        {
            bytes = new ReadOnlySpan<byte>(pTex->video->content.data, (int)pTex->video->content.size);
            return true;
        }

        if (scene != null && pTex->has_file && pTex->file_index < scene->texture_files.count)
        {
            var blob = scene->texture_files.data[pTex->file_index].content;
            if (blob.size > 0 && blob.data != null && blob.size <= int.MaxValue)
            {
                bytes = new ReadOnlySpan<byte>(blob.data, (int)blob.size);
                return true;
            }
        }

        bytes = default;
        return false;
    }

    private static string GetPreferredTextureName(ufbx_texture* pTex, string fallbackStem)
    {
        var rel = pTex->relative_filename.ToString();
        if (!string.IsNullOrWhiteSpace(rel))
        {
            return Path.GetFileName(rel.Replace('\\', '/'));
        }

        var file = pTex->filename.ToString();
        if (!string.IsNullOrWhiteSpace(file))
        {
            return Path.GetFileName(file.Replace('\\', '/'));
        }

        var name = pTex->name.ToString();
        return string.IsNullOrWhiteSpace(name) ? fallbackStem : name;
    }

    private static string ResolveEmbeddedTextureExtension(string rawName, ReadOnlySpan<byte> data)
    {
        var ext = Path.GetExtension(rawName);
        if (!string.IsNullOrEmpty(ext))
        {
            for (var i = 0; i < s_supportedTextureExtensions.Length; i++)
            {
                if (string.Equals(ext, s_supportedTextureExtensions[i], StringComparison.OrdinalIgnoreCase))
                {
                    return s_supportedTextureExtensions[i];
                }
            }
        }

        if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        {
            return ".png";
        }

        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return ".jpg";
        }

        if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D)
        {
            return ".bmp";
        }

        if (data.Length >= 10 && data[0] == (byte)'#' && data[1] == (byte)'?')
        {
            return ".hdr";
        }

        return ".png";
    }

    private static string ResolveModelVirtualPath(string sourceFilePath, AssetBakerContext ctx)
    {
        if (!string.IsNullOrWhiteSpace(ctx.RelativePath))
        {
            return ctx.RelativePath.Replace('\\', '/').TrimStart('/');
        }

        var fullSrc = Path.GetFullPath(sourceFilePath);
        for (var i = ctx.AssetDirectories.Count - 1; i >= 0; i--)
        {
            var root = Path.GetFullPath(ctx.AssetDirectories[i]);
            var rootWithSep = root.EndsWith(Path.DirectorySeparatorChar) || root.EndsWith(Path.AltDirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;
            if (fullSrc.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetRelativePath(root, fullSrc).Replace('\\', '/');
            }
        }

        return Path.GetFileName(fullSrc);
    }

    private static string CombineAndNormalizeVirtualPath(string baseDir, string relativePath)
    {
        var combined = string.IsNullOrEmpty(baseDir) ? relativePath : $"{baseDir}/{relativePath}";
        var segments = combined.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new List<string>(segments.Length);

        for (var i = 0; i < segments.Length; i++)
        {
            var seg = segments[i];
            if (seg == ".")
            {
                continue;
            }

            if (seg == "..")
            {
                if (stack.Count == 0)
                {
                    return string.Empty;
                }

                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            stack.Add(seg);
        }

        return string.Join('/', stack);
    }

    private static string SanitizeAssetFileName(string name, string fallback)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return fallback;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var buffer = name.Length <= 256 ? stackalloc char[name.Length] : new char[name.Length];

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '/' || c == '\\' || c == ':' || Array.IndexOf(invalidChars, c) >= 0)
            {
                buffer[i] = '_';
            }
            else
            {
                buffer[i] = c;
            }
        }

        var cleaned = new string(buffer).Trim().Trim('.');
        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }
}
