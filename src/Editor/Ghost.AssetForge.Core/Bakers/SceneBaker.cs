using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Ghost.AssetForge.Core.Bakers;

[AssetBaker(Extensions = [".gscene"], Type = AssetType.Scene, SettingsType = typeof(SceneBakeSettings), SettingsVersion = SceneBakeSettings.VERSION)]
public sealed class SceneBaker : IAssetBaker
{
    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        var jsonText = await File.ReadAllTextAsync(src, cancellationToken).ConfigureAwait(false);
        var doc = JsonSerializer.Deserialize(jsonText, SceneAssetDocumentContext.Default.SceneAssetDocument)
            ?? throw new InvalidDataException($"Failed to deserialize scene document '{src}'.");

        if (doc.Version != SceneAssetDocument.CURRENT_VERSION)
        {
            throw new InvalidDataException($"Unsupported .gscene version {doc.Version} in '{src}'.");
        }

        var builder = new SceneBinaryBuilder(ctx, doc);
        builder.Build();
        builder.WriteTo(dst);
    }

    private sealed class SceneBinaryBuilder
    {
        private readonly AssetBakerContext _ctx;
        private readonly SceneAssetDocument _doc;
        private readonly Dictionary<Guid, SceneEntityTarget> _guidToTarget = new();
        private readonly Dictionary<Guid, int> _depToIndex = new();
        private readonly Dictionary<string, int> _stringToIndex = new(StringComparer.Ordinal);

        private readonly List<string> _strings = new();
        private readonly List<SceneEntityHeader> _entities = new();
        private readonly List<ScenePrefabSpawnHeader> _prefabInstances = new();
        private readonly List<SceneComponentHeader> _components = new();
        private readonly List<SceneFieldRecord> _fields = new();
        private readonly List<SceneVolumeOverrideHeader> _volumeOverrides = new();
        private readonly List<int> _materialDependencyIndices = new();
        private readonly List<ScenePrefabNameOverrideHeader> _nameOverrides = new();
        private readonly List<SceneAddedComponentHeader> _addedComponents = new();
        private readonly List<SceneRemovedComponentHeader> _removedComponents = new();
        private readonly List<ScenePropertyOverrideHeader> _propertyOverrides = new();
        private readonly List<byte> _dataBlob = new();

        public SceneBinaryBuilder(AssetBakerContext ctx, SceneAssetDocument doc)
        {
            _ctx = ctx;
            _doc = doc;
        }

        public void Build()
        {
            IndexEntityTargets();

            for (var i = 0; i < _doc.Entities.Count; i++)
            {
                var entity = _doc.Entities[i];
                var (nameOffset, nameLen) = WriteUtf8String(entity.Name);
                var compStart = _components.Count;

                for (var c = 0; c < entity.Components.Count; c++)
                {
                    var comp = entity.Components[c];
                    _components.Add(BuildComponentRecord(comp.Type, comp.Fields, comp.VolumeOverrides));
                }

                _entities.Add(new SceneEntityHeader
                {
                    parentTarget = ResolveTarget(entity.ParentGuid),
                    nameOffset = nameOffset,
                    nameByteLength = nameLen,
                    componentStartIndex = compStart,
                    componentCount = entity.Components.Count
                });
            }

            for (var i = 0; i < _doc.PrefabInstances.Count; i++)
            {
                var prefab = _doc.PrefabInstances[i];
                var prefabDepIndex = RegisterAssetDependency(prefab.PrefabGuid);

                var nameStart = _nameOverrides.Count;
                if (prefab.NameOverrides != null)
                {
                    foreach (var kvp in prefab.NameOverrides)
                    {
                        var (offset, len) = WriteUtf8String(kvp.Value);
                        _nameOverrides.Add(new ScenePrefabNameOverrideHeader
                        {
                            templateEntityIndex = kvp.Key,
                            nameOffset = offset,
                            nameByteLength = len
                        });
                    }
                }
                var nameCount = _nameOverrides.Count - nameStart;

                var addedStart = _addedComponents.Count;
                if (prefab.AddedComponents != null)
                {
                    for (var a = 0; a < prefab.AddedComponents.Count; a++)
                    {
                        var added = prefab.AddedComponents[a];
                        var templateIndex = ResolvePrefabTemplateIndex(prefab, added.TargetGuid);
                        if (templateIndex < 0)
                        {
                            continue;
                        }

                        var compRecord = BuildComponentRecord(added.Component, added.Fields, added.VolumeOverrides);
                        _addedComponents.Add(new SceneAddedComponentHeader
                        {
                            templateEntityIndex = templateIndex,
                            component = compRecord
                        });
                    }
                }
                var addedCount = _addedComponents.Count - addedStart;

                var removedStart = _removedComponents.Count;
                if (prefab.RemovedComponents != null)
                {
                    for (var r = 0; r < prefab.RemovedComponents.Count; r++)
                    {
                        var removed = prefab.RemovedComponents[r];
                        var templateIndex = ResolvePrefabTemplateIndex(prefab, removed.TargetGuid);
                        if (templateIndex < 0)
                        {
                            continue;
                        }

                        _removedComponents.Add(new SceneRemovedComponentHeader
                        {
                            templateEntityIndex = templateIndex,
                            typeNameIndex = InternString(removed.Component)
                        });
                    }
                }
                var removedCount = _removedComponents.Count - removedStart;

                var propStart = _propertyOverrides.Count;
                if (prefab.PropertyOverrides != null)
                {
                    for (var p = 0; p < prefab.PropertyOverrides.Count; p++)
                    {
                        var prop = prefab.PropertyOverrides[p];
                        var templateIndex = ResolvePrefabTemplateIndex(prefab, prop.TargetGuid);
                        if (templateIndex < 0)
                        {
                            continue;
                        }

                        _propertyOverrides.Add(new ScenePropertyOverrideHeader
                        {
                            templateEntityIndex = templateIndex,
                            componentTypeNameIndex = InternString(prop.Component),
                            field = EncodeField(prop.Field, prop.Value)
                        });
                    }
                }
                var propCount = _propertyOverrides.Count - propStart;

                _prefabInstances.Add(new ScenePrefabSpawnHeader
                {
                    prefabDependencyIndex = prefabDepIndex,
                    parentTarget = ResolveTarget(prefab.ParentGuid),
                    templateEntityCount = prefab.NodeGuids.Count,
                    nameOverrideStartIndex = nameStart,
                    nameOverrideCount = nameCount,
                    addedComponentStartIndex = addedStart,
                    addedComponentCount = addedCount,
                    removedComponentStartIndex = removedStart,
                    removedComponentCount = removedCount,
                    propertyOverrideStartIndex = propStart,
                    propertyOverrideCount = propCount
                });
            }
        }

        public void WriteTo(Stream dst)
        {
            var stringBytes = BuildStringTableBytes();

            var offset = (long)Marshal.SizeOf<SceneContentHeader>();
            var stringsOffset = offset;
            offset += stringBytes.Length;

            var entitiesOffset = offset;
            offset += (long)_entities.Count * Marshal.SizeOf<SceneEntityHeader>();

            var prefabInstancesOffset = offset;
            offset += (long)_prefabInstances.Count * Marshal.SizeOf<ScenePrefabSpawnHeader>();

            var componentsOffset = offset;
            offset += (long)_components.Count * Marshal.SizeOf<SceneComponentHeader>();

            var fieldsOffset = offset;
            offset += (long)_fields.Count * Marshal.SizeOf<SceneFieldRecord>();

            var volumeOverridesOffset = offset;
            offset += (long)_volumeOverrides.Count * Marshal.SizeOf<SceneVolumeOverrideHeader>();

            var materialDependencyIndicesOffset = offset;
            offset += (long)_materialDependencyIndices.Count * sizeof(int);

            var nameOverridesOffset = offset;
            offset += (long)_nameOverrides.Count * Marshal.SizeOf<ScenePrefabNameOverrideHeader>();

            var addedComponentsOffset = offset;
            offset += (long)_addedComponents.Count * Marshal.SizeOf<SceneAddedComponentHeader>();

            var removedComponentsOffset = offset;
            offset += (long)_removedComponents.Count * Marshal.SizeOf<SceneRemovedComponentHeader>();

            var propertyOverridesOffset = offset;
            offset += (long)_propertyOverrides.Count * Marshal.SizeOf<ScenePropertyOverrideHeader>();

            var dataBlobOffset = offset;

            var header = new SceneContentHeader
            {
                magic = SceneContentHeader.MAGIC,
                version = SceneContentHeader.VERSION,
                stringCount = _strings.Count,
                entityCount = _entities.Count,
                prefabInstanceCount = _prefabInstances.Count,
                componentCount = _components.Count,
                fieldCount = _fields.Count,
                volumeOverrideCount = _volumeOverrides.Count,
                materialDependencyIndexCount = _materialDependencyIndices.Count,
                nameOverrideCount = _nameOverrides.Count,
                addedComponentCount = _addedComponents.Count,
                removedComponentCount = _removedComponents.Count,
                propertyOverrideCount = _propertyOverrides.Count,
                dataBlobSize = _dataBlob.Count,
                stringsOffset = stringsOffset,
                entitiesOffset = entitiesOffset,
                prefabInstancesOffset = prefabInstancesOffset,
                componentsOffset = componentsOffset,
                fieldsOffset = fieldsOffset,
                volumeOverridesOffset = volumeOverridesOffset,
                materialDependencyIndicesOffset = materialDependencyIndicesOffset,
                nameOverridesOffset = nameOverridesOffset,
                addedComponentsOffset = addedComponentsOffset,
                removedComponentsOffset = removedComponentsOffset,
                propertyOverridesOffset = propertyOverridesOffset,
                dataBlobOffset = dataBlobOffset
            };

            dst.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref header, 1)));
            dst.Write(stringBytes);
            WriteList(dst, _entities);
            WriteList(dst, _prefabInstances);
            WriteList(dst, _components);
            WriteList(dst, _fields);
            WriteList(dst, _volumeOverrides);
            WriteList(dst, _materialDependencyIndices);
            WriteList(dst, _nameOverrides);
            WriteList(dst, _addedComponents);
            WriteList(dst, _removedComponents);
            WriteList(dst, _propertyOverrides);
            if (_dataBlob.Count > 0)
            {
                dst.Write(CollectionsMarshal.AsSpan(_dataBlob));
            }
        }

        private void IndexEntityTargets()
        {
            for (var i = 0; i < _doc.Entities.Count; i++)
            {
                var guid = _doc.Entities[i].Guid;
                if (guid != Guid.Empty)
                {
                    _guidToTarget[guid] = new SceneEntityTarget { kind = SceneTargetKind.Standalone, index = i, subIndex = -1 };
                }
            }

            for (var p = 0; p < _doc.PrefabInstances.Count; p++)
            {
                var prefab = _doc.PrefabInstances[p];
                foreach (var kvp in prefab.NodeGuids)
                {
                    if (kvp.Value != Guid.Empty)
                    {
                        _guidToTarget[kvp.Value] = new SceneEntityTarget { kind = SceneTargetKind.PrefabNode, index = p, subIndex = kvp.Key };
                    }
                }
            }
        }

        private SceneEntityTarget ResolveTarget(Guid? guid)
        {
            if (guid.HasValue && guid.Value != Guid.Empty && _guidToTarget.TryGetValue(guid.Value, out var target))
            {
                return target;
            }

            return SceneEntityTarget.Invalid;
        }

        private static int ResolvePrefabTemplateIndex(ScenePrefabInstanceRecord prefab, Guid targetGuid)
        {
            foreach (var kvp in prefab.NodeGuids)
            {
                if (kvp.Value == targetGuid)
                {
                    return kvp.Key;
                }
            }

            return -1;
        }

        private SceneComponentHeader BuildComponentRecord(string typeName, Dictionary<string, JsonElement> fields, List<Models.SceneVolumeOverrideRecord>? volumeOverrides)
        {
            var fieldStart = _fields.Count;
            foreach (var kvp in fields)
            {
                _fields.Add(EncodeField(kvp.Key, kvp.Value));
            }
            var fieldCount = _fields.Count - fieldStart;

            var volStart = _volumeOverrides.Count;
            if (volumeOverrides != null)
            {
                for (var v = 0; v < volumeOverrides.Count; v++)
                {
                    var vol = volumeOverrides[v];
                    var volFieldStart = _fields.Count;
                    foreach (var kvp in vol.Fields)
                    {
                        _fields.Add(EncodeField(kvp.Key, kvp.Value));
                    }

                    _volumeOverrides.Add(new SceneVolumeOverrideHeader
                    {
                        typeNameIndex = InternString(vol.Type),
                        fieldStartIndex = volFieldStart,
                        fieldCount = _fields.Count - volFieldStart
                    });
                }
            }
            var volCount = _volumeOverrides.Count - volStart;

            return new SceneComponentHeader
            {
                typeNameIndex = InternString(typeName),
                fieldStartIndex = fieldStart,
                fieldCount = fieldCount,
                volumeOverrideStartIndex = volStart,
                volumeOverrideCount = volCount
            };
        }

        private SceneFieldRecord EncodeField(string fieldName, JsonElement value)
        {
            var hash = CacheFileHeader.Fnv1a(fieldName);

            if (SceneJsonTokens.TryGetAssetRef(value, out var assetGuid))
            {
                return new SceneFieldRecord
                {
                    fieldNameHash = hash,
                    kind = SceneFieldValueKind.AssetHandle,
                    payloadOffset = RegisterAssetDependency(assetGuid),
                    entityTarget = SceneEntityTarget.Invalid
                };
            }

            if (SceneJsonTokens.TryGetEntityRef(value, out var entityGuid))
            {
                return new SceneFieldRecord
                {
                    fieldNameHash = hash,
                    kind = SceneFieldValueKind.EntityRef,
                    payloadOffset = -1,
                    entityTarget = ResolveTarget(entityGuid)
                };
            }

            if (SceneJsonTokens.TryGetPaletteRef(value, out var paletteGuids))
            {
                var start = _materialDependencyIndices.Count;
                for (var i = 0; i < paletteGuids.Length; i++)
                {
                    _materialDependencyIndices.Add(RegisterAssetDependency(paletteGuids[i]));
                }

                return new SceneFieldRecord
                {
                    fieldNameHash = hash,
                    kind = SceneFieldValueKind.MaterialPalette,
                    payloadOffset = start,
                    payloadCount = paletteGuids.Length,
                    entityTarget = SceneEntityTarget.Invalid
                };
            }

            switch (value.ValueKind)
            {
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return new SceneFieldRecord
                    {
                        fieldNameHash = hash,
                        kind = SceneFieldValueKind.Bool,
                        scalarValue = value.ValueKind == JsonValueKind.True ? 1L : 0L,
                        payloadOffset = -1,
                        entityTarget = SceneEntityTarget.Invalid
                    };

                case JsonValueKind.Number:
                    if (value.TryGetInt64(out var i64))
                    {
                        return new SceneFieldRecord
                        {
                            fieldNameHash = hash,
                            kind = SceneFieldValueKind.Int64,
                            scalarValue = i64,
                            payloadOffset = -1,
                            entityTarget = SceneEntityTarget.Invalid
                        };
                    }

                    if (value.TryGetUInt64(out var u64))
                    {
                        return new SceneFieldRecord
                        {
                            fieldNameHash = hash,
                            kind = SceneFieldValueKind.UInt64,
                            scalarValue = unchecked((long)u64),
                            payloadOffset = -1,
                            entityTarget = SceneEntityTarget.Invalid
                        };
                    }

                    return new SceneFieldRecord
                    {
                        fieldNameHash = hash,
                        kind = SceneFieldValueKind.Double,
                        scalarValue = BitConverter.DoubleToInt64Bits(value.GetDouble()),
                        payloadOffset = -1,
                        entityTarget = SceneEntityTarget.Invalid
                    };

                case JsonValueKind.String:
                {
                    var (offset, len) = WriteUtf8String(value.GetString() ?? string.Empty);
                    return new SceneFieldRecord
                    {
                        fieldNameHash = hash,
                        kind = SceneFieldValueKind.String,
                        payloadOffset = offset,
                        payloadCount = len,
                        entityTarget = SceneEntityTarget.Invalid
                    };
                }

                case JsonValueKind.Array:
                    return EncodeNumericArray(hash, value);

                default:
                    return new SceneFieldRecord
                    {
                        fieldNameHash = hash,
                        kind = SceneFieldValueKind.None,
                        payloadOffset = -1,
                        entityTarget = SceneEntityTarget.Invalid
                    };
            }
        }

        private SceneFieldRecord EncodeNumericArray(uint hash, JsonElement array)
        {
            var len = array.GetArrayLength();
            if (len == 0)
            {
                return new SceneFieldRecord
                {
                    fieldNameHash = hash,
                    kind = SceneFieldValueKind.FloatArray,
                    payloadOffset = _dataBlob.Count,
                    payloadCount = 0,
                    entityTarget = SceneEntityTarget.Invalid
                };
            }

            var allInts = true;
            for (var i = 0; i < len; i++)
            {
                var elem = array[i];
                if (elem.ValueKind != JsonValueKind.Number)
                {
                    return new SceneFieldRecord
                    {
                        fieldNameHash = hash,
                        kind = SceneFieldValueKind.None,
                        payloadOffset = -1,
                        entityTarget = SceneEntityTarget.Invalid
                    };
                }

                if (!elem.TryGetInt64(out _))
                {
                    allInts = false;
                }
            }

            if (allInts)
            {
                AlignDataBlob(sizeof(long));
                var offset = _dataBlob.Count;
                Span<long> values = len <= 32 ? stackalloc long[len] : new long[len];
                for (var i = 0; i < len; i++)
                {
                    values[i] = array[i].GetInt64();
                }

                var bytes = MemoryMarshal.AsBytes(values);
                for (var i = 0; i < bytes.Length; i++)
                {
                    _dataBlob.Add(bytes[i]);
                }

                return new SceneFieldRecord
                {
                    fieldNameHash = hash,
                    kind = SceneFieldValueKind.IntArray,
                    payloadOffset = offset,
                    payloadCount = len,
                    entityTarget = SceneEntityTarget.Invalid
                };
            }

            AlignDataBlob(sizeof(float));
            var floatOffset = _dataBlob.Count;
            Span<float> floatValues = len <= 32 ? stackalloc float[len] : new float[len];
            for (var i = 0; i < len; i++)
            {
                floatValues[i] = array[i].GetSingle();
            }

            var floatBytes = MemoryMarshal.AsBytes(floatValues);
            for (var i = 0; i < floatBytes.Length; i++)
            {
                _dataBlob.Add(floatBytes[i]);
            }

            return new SceneFieldRecord
            {
                fieldNameHash = hash,
                kind = SceneFieldValueKind.FloatArray,
                payloadOffset = floatOffset,
                payloadCount = len,
                entityTarget = SceneEntityTarget.Invalid
            };
        }

        private int InternString(string value)
        {
            if (_stringToIndex.TryGetValue(value, out var existing))
            {
                return existing;
            }

            var index = _strings.Count;
            _strings.Add(value);
            _stringToIndex[value] = index;
            return index;
        }

        private int RegisterAssetDependency(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return -1;
            }

            if (_depToIndex.TryGetValue(guid, out var existing))
            {
                return existing;
            }

            var index = _ctx.AssetDependencies.Count;
            _ctx.AddAssetDependency(guid);
            _depToIndex[guid] = index;
            return index;
        }

        private (int Offset, int ByteLength) WriteUtf8String(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return (0, 0);
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            var offset = _dataBlob.Count;
            _dataBlob.AddRange(bytes);
            return (offset, bytes.Length);
        }

        private void AlignDataBlob(int alignment)
        {
            var rem = _dataBlob.Count % alignment;
            if (rem != 0)
            {
                var pad = alignment - rem;
                for (var i = 0; i < pad; i++)
                {
                    _dataBlob.Add(0);
                }
            }
        }

        private byte[] BuildStringTableBytes()
        {
            using var ms = new MemoryStream();
            for (var i = 0; i < _strings.Count; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(_strings[i]);
                if (bytes.Length > ushort.MaxValue)
                {
                    throw new InvalidDataException($"Type name exceeds max length: '{_strings[i]}'.");
                }

                var len = (ushort)bytes.Length;
                ms.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref len, 1)));
                ms.Write(bytes);
            }

            return ms.ToArray();
        }

        private static void WriteList<T>(Stream dst, List<T> list)
            where T : unmanaged
        {
            if (list.Count > 0)
            {
                dst.Write(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(list)));
            }
        }
    }
}
