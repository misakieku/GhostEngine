using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Engine.Systems;

public class RenderExtractionSystem : ISystem
{
    private Identifier<EntityQuery> _queryID;

    public void Initialize(ref readonly SystemAPI systemAPI)
    {
        _queryID = new QueryBuilder()
            // TODO: We also need to filter by MeshPalette.
            .WithAll<MeshInstance, LocalToWorld>()
            .Build(systemAPI.World);
    }

    public void Update(ref readonly SystemAPI systemAPI)
    {
        if (_queryID.IsInvalid)
        {
            return;
        }

        ref var query = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_queryID);
        var renderList = new RenderList(1, 64, Allocator.Temp);

        // TODO: We should extract the render record for each camera because different cameras may have different culling results.
        foreach (var chunk in query.GetChunkIterator())
        {
            var meshInstances = chunk.GetComponentData<MeshInstance>();
            var localToWorlds = chunk.GetComponentData<LocalToWorld>();

            for (int i = 0; i < chunk.Count; i++)
            {
                ref readonly var meshInstance = ref meshInstances[i];
                ref readonly var localToWorld = ref localToWorlds[i];

                renderList.Add(new RenderRecord
                {
                    localToWorld = localToWorld.matrix,
                    // TODO: Get mesh and material from palette. This requires some changes to ISharedComponent since it's now fully functional right now.
                    // mesh = meshInstance.meshIndex,
                    // material = meshInstance.materialIndex,
                    renderingLayerMask = meshInstance.renderingLayerMask,
                    subMeshIndex = meshInstance.subMeshIndex,

                }, 0);
            }
        }

        // TODO: Send render list to render pipeline.
    }

    public void Cleanup(ref readonly SystemAPI systemAPI)
    {
    }
}
