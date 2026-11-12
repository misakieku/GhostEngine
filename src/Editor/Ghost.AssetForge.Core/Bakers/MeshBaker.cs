using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.MeshOptimizer;
using Ghost.Ufbx;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ghost.AssetForge.Core.Bakers;

[AssetBaker(Extensions = [".fbx", ".obj"], Type = AssetType.EntityPrefab, SettingsType = typeof(MeshBakeSettings), SettingsVersion = MeshBakeSettings.VERSION)]
public unsafe partial class MeshBaker : IAssetBaker
{
    public bool CompressOutput => false;

    private sealed class SerializedEntityRecord
    {
        public int parentIndex;
        public int firstChildIndex;
        public int nextSiblingIndex;
        public readonly List<(int typeIndex, byte[] data)> components = new();
    }

    public Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        if (settings is not MeshBakeSettings meshSettings)
        {
            throw new ArgumentException("Invalid settings type. Expected MeshBakeSettings.", nameof(settings));
        }

        var parseResult = MeshProcessor.ParseModelScene(src, meshSettings, AllocationHandle.Persistent);
        if (parseResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to parse model '{src}': {parseResult.Message}");
        }

        using var modelScene = parseResult.Value;
        var assetId = ctx.AssetId != Guid.Empty ? ctx.AssetId : Guid.NewGuid();

        // Step 1: Bake each mesh into a sub-asset
        var meshDependencies = new int[modelScene.Meshes.Count];
        var dependencies = new List<Guid>();
        var bakedMaterials = new HashSet<Guid>();

        for (var i = 0; i < modelScene.Meshes.Count; i++)
        {
            var mesh = modelScene.Meshes[i];
            cancellationToken.ThrowIfCancellationRequested();

            using var meshletDataPtr = MeshProcessor.BuildMeshlets(
                mesh.vertices.AsReadOnly(),
                mesh.indices.AsReadOnly(),
                mesh.materialParts.AsReadOnly(),
                meshSettings,
                AllocationHandle.Persistent);

            var pMeshletData = meshletDataPtr.Get();
            MeshProcessor.BuildClusterLodHierarchy(pMeshletData, AllocationHandle.Persistent);

            var subPath = string.IsNullOrWhiteSpace(mesh.Name) ? $"Mesh_{i}" : mesh.Name;
            using var subStream = ctx.AddSubAsset(subPath, AssetType.Mesh);
            WriteMeshContent(subStream, mesh, pMeshletData, meshSettings);

            var meshSubGuid = GuidUtility.DeriveSubAssetGuid(assetId, subPath);
            meshDependencies[i] = GetDependencyIndex(dependencies, meshSubGuid, ctx);
        }

        // Step 2: Build node hierarchy and entities
        var componentTypes = new List<string>();
        var entities = new List<SerializedEntityRecord>();
        var meshMaterialFixups = new List<PrefabMeshMaterialFixup>();

        BuildPrefabEntities(modelScene, assetId, meshSettings, ctx, meshDependencies, dependencies, componentTypes, entities, meshMaterialFixups, bakedMaterials);

        // Step 3: Write EntityPrefab to primary dst
        WritePrefabContent(dst, componentTypes, entities, meshMaterialFixups);

        return Task.CompletedTask;
    }

    private static int GetDependencyIndex(List<Guid> dependencies, Guid assetGuid, AssetBakerContext ctx)
    {
        for (var i = 0; i < dependencies.Count; i++)
        {
            if (dependencies[i] == assetGuid)
            {
                return i;
            }
        }
        var idx = dependencies.Count;
        dependencies.Add(assetGuid);
        ctx.AddAssetDependency(assetGuid);
        return idx;
    }

    private static int GetComponentTypeIndex(List<string> componentTypes, string typeName)
    {
        for (var i = 0; i < componentTypes.Count; i++)
        {
            if (componentTypes[i] == typeName)
            {
                return i;
            }
        }
        var idx = componentTypes.Count;
        componentTypes.Add(typeName);
        return idx;
    }

    private static void BuildPrefabEntities(
        ParsedModelScene modelScene,
        Guid assetId,
        MeshBakeSettings meshSettings,
        AssetBakerContext ctx,
        int[] meshDependencies,
        List<Guid> dependencies,
        List<string> componentTypes,
        List<SerializedEntityRecord> entities,
        List<PrefabMeshMaterialFixup> meshMaterialFixups,
        HashSet<Guid> bakedMaterials)
    {
        var localTransformType = GetComponentTypeIndex(componentTypes, PrefabComponentTypeNames.LOCAL_TRANSFORM);
        var localToWorldType = GetComponentTypeIndex(componentTypes, PrefabComponentTypeNames.LOCAL_TO_WORLD);
        var meshInstanceType = GetComponentTypeIndex(componentTypes, PrefabComponentTypeNames.MESH_INSTANCE);
        var dirLightType = GetComponentTypeIndex(componentTypes, PrefabComponentTypeNames.DIRECTIONAL_LIGHT);
        var punctualLightType = GetComponentTypeIndex(componentTypes, PrefabComponentTypeNames.PUNCTUAL_LIGHT);
        var cameraType = GetComponentTypeIndex(componentTypes, PrefabComponentTypeNames.CAMERA);

        var scene = modelScene.Scene;
        var rootNode = scene != null ? scene->root_node : null;

        var nodeOrder = new List<IntPtr>();
        var nodeToIndex = new Dictionary<IntPtr, int>();

        if (rootNode != null)
        {
            var queue = new Queue<IntPtr>();
            queue.Enqueue((IntPtr)rootNode);
            nodeToIndex[(IntPtr)rootNode] = 0;
            nodeOrder.Add((IntPtr)rootNode);

            while (queue.Count > 0)
            {
                var cur = (ufbx_node*)queue.Dequeue();
                for (var c = 0u; c < cur->children.count; c++)
                {
                    var child = cur->children.data[c];
                    var childPtr = (IntPtr)child;
                    if (!nodeToIndex.ContainsKey(childPtr))
                    {
                        nodeToIndex[childPtr] = nodeOrder.Count;
                        nodeOrder.Add(childPtr);
                        queue.Enqueue(childPtr);
                    }
                }
            }
        }

        var referencedMeshes = new HashSet<int>();

        for (var i = 0; i < nodeOrder.Count; i++)
        {
            var node = (ufbx_node*)nodeOrder[i];
            var record = new SerializedEntityRecord();

            if (node == rootNode)
            {
                record.parentIndex = -1;
            }
            else if (node->parent != null && nodeToIndex.TryGetValue((IntPtr)node->parent, out var pIdx))
            {
                record.parentIndex = pIdx;
            }
            else
            {
                record.parentIndex = 0;
            }

            if (node->children.count > 0 && nodeToIndex.TryGetValue((IntPtr)node->children.data[0], out var fcIdx))
            {
                record.firstChildIndex = fcIdx;
            }
            else
            {
                record.firstChildIndex = -1;
            }

            record.nextSiblingIndex = -1;
            if (node->parent != null)
            {
                var parent = node->parent;
                for (var c = 0u; c < parent->children.count; c++)
                {
                    if (parent->children.data[c] == node && c + 1 < parent->children.count)
                    {
                        if (nodeToIndex.TryGetValue((IntPtr)parent->children.data[c + 1], out var nsIdx))
                        {
                            record.nextSiblingIndex = nsIdx;
                        }
                        break;
                    }
                }
            }

            var t = node->local_transform.translation;
            var r = node->local_transform.rotation;
            var s = node->local_transform.scale;

            var localTransform = (i == 0)
                ? new PrefabLocalTransform { position = float3.zero, rotation = quaternion.identity, scale = new float3(1.0f) }
                : new PrefabLocalTransform { position = new float3(t.x, t.y, t.z), rotation = new quaternion(r.x, r.y, r.z, r.w), scale = new float3(s.x, s.y, s.z) };

            var localToWorld = new PrefabLocalToWorld { matrix = ComputeTRS(localTransform.position, localTransform.rotation, localTransform.scale) };

            record.components.Add((localTransformType, StructToBytes(localTransform)));
            record.components.Add((localToWorldType, StructToBytes(localToWorld)));

            if (node->mesh != null && modelScene.MeshPointerToIndex.TryGetValue((IntPtr)node->mesh, out var meshIdx))
            {
                referencedMeshes.Add(meshIdx);
                AddMeshComponents(node->mesh, i, meshIdx, assetId, meshSettings, ctx, meshDependencies, dependencies, bakedMaterials, meshInstanceType, record, meshMaterialFixups);
            }

            if (node->light != null)
            {
                AddLightComponent(node->light, dirLightType, punctualLightType, record);
            }

            if (node->camera != null)
            {
                AddCameraComponent(node->camera, cameraType, record);
            }

            entities.Add(record);
        }

        // Attach any unreferenced meshes as child entities of root entity 0
        for (var m = 0; m < modelScene.Meshes.Count; m++)
        {
            if (referencedMeshes.Contains(m))
            {
                continue;
            }

            var childIndex = entities.Count;
            var childRecord = new SerializedEntityRecord
            {
                parentIndex = 0,
                firstChildIndex = -1,
                nextSiblingIndex = -1
            };

            var localTransform = new PrefabLocalTransform { position = float3.zero, rotation = quaternion.identity, scale = new float3(1.0f) };
            var localToWorld = new PrefabLocalToWorld { matrix = float4x4.identity };
            childRecord.components.Add((localTransformType, StructToBytes(localTransform)));
            childRecord.components.Add((localToWorldType, StructToBytes(localToWorld)));

            var pMesh = scene != null && scene->meshes.count > (uint)m ? scene->meshes.data[m] : null;
            if (pMesh != null)
            {
                AddMeshComponents(pMesh, childIndex, m, assetId, meshSettings, ctx, meshDependencies, dependencies, bakedMaterials, meshInstanceType, childRecord, meshMaterialFixups);
            }

            if (entities.Count > 0 && entities[0].firstChildIndex < 0)
            {
                entities[0].firstChildIndex = childIndex;
            }

            entities.Add(childRecord);
        }
    }

    private static void AddMeshComponents(
        ufbx_mesh* pMesh,
        int entityIndex,
        int meshIdx,
        Guid assetId,
        MeshBakeSettings meshSettings,
        AssetBakerContext ctx,
        int[] meshDependencies,
        List<Guid> dependencies,
        HashSet<Guid> bakedMaterials,
        int meshInstanceType,
        SerializedEntityRecord record,
        List<PrefabMeshMaterialFixup> meshMaterialFixups)
    {
        var matCount = pMesh->materials.count > 0 ? (int)pMesh->materials.count : 1;
        var matStartDepIndex = dependencies.Count;

        for (var m = 0; m < matCount; m++)
        {
            var pMat = pMesh->materials.count > (uint)m ? pMesh->materials.data[m] : null;
            var matName = pMat != null ? pMat->name.ToString() : $"Material_{m}";
            if (string.IsNullOrWhiteSpace(matName))
            {
                matName = $"Material_{m}";
            }

            Guid matGuid;
            if (meshSettings.MaterialSlotRemap.TryGetValue(matName, out var remapped) && remapped != Guid.Empty)
            {
                matGuid = remapped;
            }
            else
            {
                var matSubPath = $"Materials/{matName}";
                matGuid = GuidUtility.DeriveSubAssetGuid(assetId, matSubPath);

                if (!bakedMaterials.Contains(matGuid))
                {
                    bakedMaterials.Add(matGuid);
                    BakeSubAssetMaterial(pMat, matSubPath, ctx);
                }
            }

            GetDependencyIndex(dependencies, matGuid, ctx);
        }

        meshMaterialFixups.Add(new PrefabMeshMaterialFixup
        {
            entityIndex = entityIndex,
            meshDependencyIndex = meshDependencies[meshIdx],
            materialCount = matCount,
            materialDependencyStartIndex = matStartDepIndex
        });

        var meshInstance = new PrefabMeshInstance
        {
            mesh = 0,
            materialPalette = -1,
            renderingLayerMask = uint.MaxValue,
            shadowCastingMode = 1,
            staticShadowCaster = true
        };

        record.components.Add((meshInstanceType, StructToBytes(meshInstance)));
    }

    private static void BakeSubAssetMaterial(ufbx_material* pMat, string matSubPath, AssetBakerContext ctx)
    {
        var semantics = MaterialPropertyPacker.GetShaderSemantics(MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID, ctx.AssetDirectories);
        if (semantics == null)
        {
            throw new InvalidOperationException("Failed to resolve or parse StandardLit shader for sub-asset material baking.");
        }

        var (propBytes, matFixups) = MaterialPropertyPacker.Pack(
            semantics,
            jsonProperties: null,
            jsonTextures: null,
            valueFallback: name =>
            {
                if (pMat == null) return null;
                return name switch
                {
                    "baseColor" => pMat->pbr.base_color.has_value
                        ? new float3(pMat->pbr.base_color.value_vec3.x, pMat->pbr.base_color.value_vec3.y, pMat->pbr.base_color.value_vec3.z)
                        : pMat->fbx.diffuse_color.has_value
                            ? new float3(pMat->fbx.diffuse_color.value_vec3.x, pMat->fbx.diffuse_color.value_vec3.y, pMat->fbx.diffuse_color.value_vec3.z)
                            : null,
                    "roughness" => pMat->pbr.roughness.has_value ? (object)pMat->pbr.roughness.value_real : null,
                    "metallic" => pMat->pbr.metalness.has_value ? (object)pMat->pbr.metalness.value_real : null,
                    "emissionColor" => pMat->pbr.emission_color.has_value
                        ? new float3(pMat->pbr.emission_color.value_vec3.x, pMat->pbr.emission_color.value_vec3.y, pMat->pbr.emission_color.value_vec3.z)
                        : null,
                    _ => null
                };
            });

        var matDeps = new List<Guid>();
        matDeps.Add(MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID);
        foreach (var fixup in matFixups)
        {
            if (fixup.textureGuid != Guid.Empty && !matDeps.Contains(fixup.textureGuid))
            {
                matDeps.Add(fixup.textureGuid);
            }
        }

        using var matStream = ctx.AddSubAsset(matSubPath, AssetType.Material, CollectionsMarshal.AsSpan(matDeps));
        var matHeader = new MaterialContentHeader
        {
            magic = MaterialContentHeader.MAGIC,
            version = MaterialContentHeader.VERSION,
            shaderId = MaterialPropertyPacker.STANDARD_LIT_SHADER_GUID,
            propertyBufferSize = (uint)propBytes.Length,
            textureFixupCount = (uint)matFixups.Count,
            propertyBufferOffset = Marshal.SizeOf<MaterialContentHeader>(),
            textureFixupOffset = Marshal.SizeOf<MaterialContentHeader>() + propBytes.Length
        };

        matStream.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref matHeader, 1)));
        matStream.Write(propBytes);
        if (matFixups.Count > 0)
        {
            matStream.Write(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(matFixups)));
        }
    }

    private static void AddLightComponent(ufbx_light* light, int dirLightType, int punctualLightType, SerializedEntityRecord record)
    {
        if (light->type == ufbx_light_type.UFBX_LIGHT_DIRECTIONAL)
        {
            var dir = PrefabDirectionalLight.Default;
            dir.color = new float3(light->color.x, light->color.y, light->color.z);
            dir.intensity = light->intensity;
            dir.castShadows = light->cast_shadows;
            record.components.Add((dirLightType, StructToBytes(dir)));
        }
        else if (light->type == ufbx_light_type.UFBX_LIGHT_SPOT)
        {
            var spot = PrefabPunctualLight.CreateSpot(
                new float3(light->color.x, light->color.y, light->color.z),
                light->intensity,
                10.0f,
                math.radians(light->inner_angle),
                math.radians(light->outer_angle));
            record.components.Add((punctualLightType, StructToBytes(spot)));
        }
        else
        {
            var point = PrefabPunctualLight.CreatePoint(
                new float3(light->color.x, light->color.y, light->color.z),
                light->intensity,
                10.0f);
            record.components.Add((punctualLightType, StructToBytes(point)));
        }
    }

    private static void AddCameraComponent(ufbx_camera* camera, int cameraType, SerializedEntityRecord record)
    {
        var cam = PrefabCamera.Default;
        if (camera->near_plane > 0) cam.nearClipPlane = camera->near_plane;
        if (camera->far_plane > 0) cam.farClipPlane = camera->far_plane;
        if (camera->focal_length_mm > 0) cam.focalLength = camera->focal_length_mm;
        if (camera->film_size_inch.x > 0 && camera->film_size_inch.y > 0)
        {
            cam.sensorSize = new float2(camera->film_size_inch.x * 25.4f, camera->film_size_inch.y * 25.4f);
        }
        record.components.Add((cameraType, StructToBytes(cam)));
    }

    private static byte[] StructToBytes<T>(scoped in T value) where T : unmanaged
    {
        var bytes = new byte[sizeof(T)];
        fixed (byte* p = bytes)
        {
            *(T*)p = value;
        }
        return bytes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4x4 ComputeTRS(float3 position, quaternion rotation, float3 scale)
    {
        var r = new float3x3(rotation);
        return new float4x4(
            r[0][0] * scale.x, r[1][0] * scale.y, r[2][0] * scale.z, position.x,
            r[0][1] * scale.x, r[1][1] * scale.y, r[2][1] * scale.z, position.y,
            r[0][2] * scale.x, r[1][2] * scale.y, r[2][2] * scale.z, position.z,
            0.0f, 0.0f, 0.0f, 1.0f);
    }

    private static void WritePrefabContent(Stream dst, List<string> componentTypes, List<SerializedEntityRecord> entities, List<PrefabMeshMaterialFixup> meshMaterialFixups)
    {
        var assetStartOffset = dst.Position;
        var header = new PrefabContentHeader
        {
            magic = PrefabContentHeader.MAGIC,
            version = PrefabContentHeader.VERSION,
            entityCount = entities.Count,
            reserved0 = 0,
            componentTypeCount = componentTypes.Count,
            meshMaterialFixupCount = meshMaterialFixups.Count,
            reserved1 = 0
        };

        var headerPlaceholder = new byte[Marshal.SizeOf<PrefabContentHeader>()];
        dst.Write(headerPlaceholder);

        header.componentTypesOffset = dst.Position - assetStartOffset;
        for (var i = 0; i < componentTypes.Count; i++)
        {
            var utf8 = Encoding.UTF8.GetBytes(componentTypes[i]);
            var len = (ushort)utf8.Length;
            dst.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref len, 1)));
            dst.Write(utf8);
        }

        header.entitiesOffset = dst.Position - assetStartOffset;
        for (var i = 0; i < entities.Count; i++)
        {
            var e = entities[i];
            var eHeader = new PrefabEntityHeader
            {
                parentIndex = e.parentIndex,
                firstChildIndex = e.firstChildIndex,
                nextSiblingIndex = e.nextSiblingIndex,
                componentCount = e.components.Count
            };
            dst.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref eHeader, 1)));

            for (var c = 0; c < e.components.Count; c++)
            {
                var comp = e.components[c];
                var cHeader = new PrefabComponentHeader
                {
                    componentTypeIndex = comp.typeIndex,
                    dataSize = comp.data.Length
                };
                dst.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref cHeader, 1)));
                dst.Write(comp.data);
            }
        }

        header.meshMaterialFixupsOffset = dst.Position - assetStartOffset;
        if (meshMaterialFixups.Count > 0)
        {
            dst.Write(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(meshMaterialFixups)));
        }

        var endPos = dst.Position;
        dst.Position = assetStartOffset;
        dst.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));
        dst.Position = endPos;
    }

    private static void WriteMeshContent(Stream stream, ParsedMesh mesh, MeshletMeshData* meshletData, MeshBakeSettings settings)
    {
        var assetStartOffset = stream.Position;

        // Bundle meshlets, local packed vertices, and micro-triangles into 128KB pages
        using var packedPages = PagePacker.PackPages(meshletData, AllocationHandle.Persistent);

        var pageDirectory = packedPages.pageDirectory;
        var pageBuffers = packedPages.pageBuffers;
        var reorderedMeshlets = packedPages.reorderedMeshlets;

        var header = new MeshContentHeader
        {
            magic = MeshContentHeader.MAGIC,
            version = MeshContentHeader.VERSION,
            meshletCount = reorderedMeshlets.Count,
            meshletGroupCount = meshletData->groups.Count,
            meshletHierarchyNodeCount = meshletData->hierarchyNodes.Count,
            materialSlotCount = meshletData->materialSlotCount,
            lodLevelCount = meshletData->lodLevelCount,
            pageCount = pageDirectory.Count,
            rootPageCount = pageDirectory.Count > 0 && pageDirectory[0].isRootPage != 0 ? 1 : 0,
            rootMeshletCount = pageDirectory.Count > 0 && pageDirectory[0].isRootPage != 0 ? pageDirectory[0].meshletCount : 0,
            boundsMin = mesh.boundingBox.Min,
            boundsMax = mesh.boundingBox.Max,
            boundingSphereCenter = (mesh.boundingBox.Min + mesh.boundingBox.Max) * 0.5f,
            boundingSphereRadius = Misaki.HighPerformance.Mathematics.math.length(mesh.boundingBox.Max - (mesh.boundingBox.Min + mesh.boundingBox.Max) * 0.5f),
        };

        // Write header placeholder
        stream.Write(header);

        // Hierarchy DAG nodes
        header.hierarchyOffset = stream.Position - assetStartOffset;
        stream.Write(meshletData->hierarchyNodes.AsSpan());
        header.hierarchySize = stream.Position - assetStartOffset - header.hierarchyOffset;

        // Meshlet Groups
        header.groupOffset = stream.Position - assetStartOffset;
        stream.Write(meshletData->groups.AsSpan());
        header.groupSize = stream.Position - assetStartOffset - header.groupOffset;

        // Reordered Meshlets table
        header.meshletOffset = stream.Position - assetStartOffset;
        stream.Write(reorderedMeshlets.AsSpan());
        header.meshletSize = stream.Position - assetStartOffset - header.meshletOffset;

        // Page Directory Table placeholder
        var pageDirStartPos = stream.Position;
        header.pageDirectoryOffset = stream.Position - assetStartOffset;
        stream.Write(pageDirectory.AsSpan());
        header.pageDirectorySize = stream.Position - assetStartOffset - header.pageDirectoryOffset;

        // Root Page 0 (uncompressed payload)
        if (pageBuffers.Length > 0 && pageDirectory[0].isRootPage != 0)
        {
            header.rootPageOffset = stream.Position - assetStartOffset;
            stream.Write(pageBuffers[0]);
            header.rootPageSize = stream.Position - assetStartOffset - header.rootPageOffset;

            ref var p0 = ref pageDirectory[0];
            p0.fileOffset = (ulong)header.rootPageOffset;
        }

        // Bulk Pages 1..N
        if (pageBuffers.Length > 1)
        {
            header.bulkPagesOffset = stream.Position - assetStartOffset;
            for (var p = 1; p < pageBuffers.Length; p++)
            {
                ref var dirEntry = ref pageDirectory[p];
                dirEntry.fileOffset = (ulong)(stream.Position - assetStartOffset);
                stream.Write(pageBuffers[p]);
                dirEntry.compressedSize = (uint)pageBuffers[p].Length;
                dirEntry.uncompressedSize = (uint)pageBuffers[p].Length;
            }
            header.bulkPagesSize = stream.Position - assetStartOffset - header.bulkPagesOffset;
        }

        // Material Parts
        header.materialPartOffset = stream.Position - assetStartOffset;
        WriteMaterialParts(stream, mesh.materialParts.AsSpan());
        header.materialPartSize = stream.Position - assetStartOffset - header.materialPartOffset;

        // Ray Tracing Proxy Geometry
        using var proxyMesh = BuildRayTracingProxy(mesh, settings, AllocationHandle.Persistent);
        header.rayTracingProxyOffset = stream.Position - assetStartOffset;
        header.rayTracingProxyVertexCount = proxyMesh.positions.Count;
        header.rayTracingProxyIndexCount = proxyMesh.indices.Count;
        stream.Write(proxyMesh.positions.AsSpan());
        stream.Write(proxyMesh.indices.AsSpan());
        header.rayTracingProxySize = stream.Position - assetStartOffset - header.rayTracingProxyOffset;

        var endOfAsset = stream.Position;

        // Backpatch Page Directory with real file offsets
        stream.Position = pageDirStartPos;
        stream.Write(pageDirectory.AsSpan());

        // Backpatch Header with all real offsets and sizes
        stream.Position = assetStartOffset;
        stream.Write(header);
        stream.Position = endOfAsset;
        stream.Flush();
    }

    internal struct RayTracingProxyMesh : IDisposable
    {
        public UnsafeList<float3> positions;
        public UnsafeList<uint> indices;

        public void Dispose()
        {
            positions.Dispose();
            indices.Dispose();
        }
    }

    private static RayTracingProxyMesh BuildRayTracingProxy(ParsedMesh mesh, MeshBakeSettings settings, AllocationHandle allocationHandle)
    {
        var pVertices = (Vertex*)mesh.vertices.GetUnsafePtr();
        var triCount = mesh.indices.Count / 3;

        var percent = Math.Clamp(settings.FallbackPercentage, 0.0f, 1.0f);

        if (percent >= 1.0f || triCount <= 1000)
        {
            var fallbackProxy = new RayTracingProxyMesh
            {
                positions = new UnsafeList<float3>(mesh.vertices.Count, allocationHandle),
                indices = new UnsafeList<uint>(mesh.indices.Count, allocationHandle)
            };
            for (var i = 0; i < mesh.vertices.Count; i++)
            {
                fallbackProxy.positions.Add(pVertices[i].position);
            }
            fallbackProxy.indices.AddRange(mesh.indices.AsSpan());
            return fallbackProxy;
        }

        nuint targetIndexCount;
        float targetError;

        if (percent > 0.0f)
        {
            targetIndexCount = (nuint)Math.Max(3, (int)(mesh.indices.Count * percent));
            targetError = 1.0f;
        }
        else
        {
            targetIndexCount = (nuint)Math.Max(3, (int)(mesh.indices.Count * 0.1f));
            targetError = Math.Max(1e-4f, settings.FallbackRelativeError);
        }

        using var destIndices = new UnsafeArray<uint>(mesh.indices.Count, allocationHandle);
        var resultError = 0.0f;

        var simplifiedIndexCount = MeshOptApi.Simplify(
            (uint*)destIndices.GetUnsafePtr(),
            (uint*)mesh.indices.GetUnsafePtr(),
            (nuint)mesh.indices.Count,
            (float*)&pVertices->position,
            (nuint)mesh.vertices.Count,
            (nuint)sizeof(Vertex),
            targetIndexCount,
            targetError,
            percent > 0.0f ? SimplifyOptions.None : SimplifyOptions.LockBorder,
            &resultError);

        if (simplifiedIndexCount == 0 || simplifiedIndexCount > (nuint)mesh.indices.Count)
        {
            var fallbackProxy = new RayTracingProxyMesh
            {
                positions = new UnsafeList<float3>(mesh.vertices.Count, allocationHandle),
                indices = new UnsafeList<uint>(mesh.indices.Count, allocationHandle)
            };
            for (var i = 0; i < mesh.vertices.Count; i++)
            {
                fallbackProxy.positions.Add(pVertices[i].position);
            }
            fallbackProxy.indices.AddRange(mesh.indices.AsSpan());
            return fallbackProxy;
        }

        using var remap = new UnsafeArray<uint>(mesh.vertices.Count, allocationHandle);
        var numUniqueVertices = (uint)MeshOptApi.GenerateVertexRemap(
            (uint*)remap.GetUnsafePtr(),
            (uint*)destIndices.GetUnsafePtr(),
            simplifiedIndexCount,
            pVertices,
            (nuint)mesh.vertices.Count,
            (nuint)sizeof(Vertex));

        var proxy = new RayTracingProxyMesh
        {
            positions = new UnsafeList<float3>((int)numUniqueVertices, allocationHandle),
            indices = new UnsafeList<uint>((int)simplifiedIndexCount, allocationHandle)
        };
        proxy.positions.UnsafeSetCount((int)numUniqueVertices);
        proxy.indices.UnsafeSetCount((int)simplifiedIndexCount);

        for (var i = 0; i < mesh.vertices.Count; i++)
        {
            var newIdx = remap[i];
            if (newIdx != ~0u && newIdx < numUniqueVertices)
            {
                proxy.positions[(int)newIdx] = pVertices[i].position;
            }
        }

        MeshOptApi.RemapIndexBuffer(
            (uint*)proxy.indices.GetUnsafePtr(),
            (uint*)destIndices.GetUnsafePtr(),
            simplifiedIndexCount,
            (uint*)remap.GetUnsafePtr());

        MeshOptApi.OptimizeVertexCache(
            (uint*)proxy.indices.GetUnsafePtr(),
            (uint*)proxy.indices.GetUnsafePtr(),
            simplifiedIndexCount,
            numUniqueVertices);

        return proxy;
    }

    private static void WriteMaterialParts(Stream stream, ReadOnlySpan<MaterialPartInfo> parts)
    {
        if (parts.IsEmpty)
        {
            return;
        }

        var buffer = parts.Length <= 64
            ? stackalloc MeshContentMaterialPart[parts.Length]
            : new MeshContentMaterialPart[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            buffer[i] = new MeshContentMaterialPart
            {
                materialIndex = parts[i].materialIndex,
                indexStart = parts[i].indexStart,
                indexCount = parts[i].indexCount,
                vertexStart = parts[i].vertexStart,
                vertexCount = parts[i].vertexCount,
            };
        }

        stream.Write(MemoryMarshal.AsBytes(buffer));
    }
}
