using Misaki.HighPerformance.Mathematics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.RenderPipeline;

/// <summary>
/// Power-of-two quadtree buddy allocator for persistent shadow atlas regions.
/// Supports O(log N) allocation, automatic sibling coalescing on free, and block-level LRU eviction to prevent fragmentation.
/// </summary>
internal sealed class ShadowAtlasBuddyAllocator
{
    public const uint MIN_TILE_SIZE = 64u;

    private enum NodeState : byte
    {
        Free = 0,
        Split = 1,
        Allocated = 2
    }

    private struct BuddyNode
    {
        public NodeState state;
        public uint x;
        public uint y;
        public uint size;
        public ulong lastUsedFrame;
    }

    private readonly uint _atlasSize;
    private readonly int _maxDepth;
    private readonly BuddyNode[] _nodes;
    private readonly int[] _levelStartOffsets;

    public uint AtlasSize => _atlasSize;

    public ShadowAtlasBuddyAllocator(uint atlasSize)
    {
        var clamped = Math.Max(MIN_TILE_SIZE, BitOperations.RoundUpToPowerOf2(atlasSize));
        _atlasSize = clamped;
        _maxDepth = BitOperations.Log2(_atlasSize / MIN_TILE_SIZE);

        var levelCount = _maxDepth + 1;
        _levelStartOffsets = new int[levelCount + 1];
        var totalNodes = 0;
        var nodesAtLevel = 1;
        for (var lvl = 0; lvl < levelCount; lvl++)
        {
            _levelStartOffsets[lvl] = totalNodes;
            totalNodes += nodesAtLevel;
            nodesAtLevel <<= 2;
        }
        _levelStartOffsets[levelCount] = totalNodes;

        _nodes = new BuddyNode[totalNodes];
        InitNode(0, 0, 0, _atlasSize, 0);
    }

    private void InitNode(uint nodeIndex, uint x, uint y, uint size, int depth)
    {
        _nodes[nodeIndex] = new BuddyNode
        {
            state = NodeState.Free,
            x = x,
            y = y,
            size = size,
            lastUsedFrame = 0
        };

        if (depth >= _maxDepth)
        {
            return;
        }

        var half = size >> 1;
        var firstChild = (nodeIndex << 2) + 1u;
        InitNode(firstChild + 0u, x, y, half, depth + 1);
        InitNode(firstChild + 1u, x + half, y, half, depth + 1);
        InitNode(firstChild + 2u, x, y + half, half, depth + 1);
        InitNode(firstChild + 3u, x + half, y + half, half, depth + 1);
    }

    public bool TryAllocate(uint requestedSize, ulong currentFrame, out uint nodeIndex, out ShadowAtlasRegion region)
    {
        var allocSize = Math.Max(MIN_TILE_SIZE, BitOperations.RoundUpToPowerOf2(requestedSize));
        if (allocSize > _atlasSize)
        {
            nodeIndex = uint.MaxValue;
            region = default;
            return false;
        }

        var targetDepth = BitOperations.Log2(_atlasSize / allocSize);
        if (TryAllocateRecursive(0u, 0, targetDepth, currentFrame, out nodeIndex))
        {
            ref readonly var node = ref _nodes[nodeIndex];
            region = CreateRegion(node.x, node.y, requestedSize, requestedSize);
            return true;
        }

        nodeIndex = uint.MaxValue;
        region = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Touch(uint nodeIndex, ulong frameNumber)
    {
        if (nodeIndex < (uint)_nodes.Length)
        {
            _nodes[nodeIndex].lastUsedFrame = frameNumber;
        }
    }

    public void Free(uint nodeIndex)
    {
        if (nodeIndex >= (uint)_nodes.Length || _nodes[nodeIndex].state != NodeState.Allocated)
        {
            return;
        }

        _nodes[nodeIndex].state = NodeState.Free;
        _nodes[nodeIndex].lastUsedFrame = 0;

        while (nodeIndex > 0u)
        {
            var parent = (nodeIndex - 1u) >> 2;
            var firstChild = (parent << 2) + 1u;
            if (_nodes[firstChild + 0u].state == NodeState.Free &&
                _nodes[firstChild + 1u].state == NodeState.Free &&
                _nodes[firstChild + 2u].state == NodeState.Free &&
                _nodes[firstChild + 3u].state == NodeState.Free)
            {
                _nodes[parent].state = NodeState.Free;
                _nodes[parent].lastUsedFrame = 0;
                nodeIndex = parent;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Finds the least-recently-used block at the requested allocation size whose allocated descendants can be evicted to coalesce a contiguous slot.
    /// Excludes any block containing tiles already touched on <paramref name="currentFrame"/>.
    /// </summary>
    public bool TryFindEvictionBlock(uint requestedSize, ulong currentFrame, out uint victimBlockNode, List<uint> outAllocatedLeaves)
    {
        outAllocatedLeaves.Clear();
        var allocSize = Math.Max(MIN_TILE_SIZE, BitOperations.RoundUpToPowerOf2(requestedSize));
        if (allocSize > _atlasSize)
        {
            victimBlockNode = uint.MaxValue;
            return false;
        }

        var targetDepth = BitOperations.Log2(_atlasSize / allocSize);
        var startIdx = _levelStartOffsets[targetDepth];
        var endIdx = _levelStartOffsets[targetDepth + 1];

        var bestBlock = uint.MaxValue;
        var bestMaxFrame = ulong.MaxValue;
        var bestLeafCount = int.MaxValue;

        for (var idx = startIdx; idx < endIdx; idx++)
        {
            var nodeState = _nodes[idx].state;
            if (nodeState == NodeState.Free)
            {
                continue;
            }

            if (EvaluateSubtreeEvictionCost((uint)idx, currentFrame, out var maxFrame, out var leafCount) && leafCount > 0)
            {
                if (maxFrame < bestMaxFrame || (maxFrame == bestMaxFrame && leafCount < bestLeafCount))
                {
                    bestMaxFrame = maxFrame;
                    bestLeafCount = leafCount;
                    bestBlock = (uint)idx;
                }
            }
        }

        if (bestBlock == uint.MaxValue)
        {
            victimBlockNode = uint.MaxValue;
            return false;
        }

        CollectAllocatedLeaves(bestBlock, outAllocatedLeaves);
        victimBlockNode = bestBlock;
        return outAllocatedLeaves.Count > 0;
    }

    private bool EvaluateSubtreeEvictionCost(uint nodeIndex, ulong currentFrame, out ulong maxLastUsedFrame, out int leafCount)
    {
        ref readonly var node = ref _nodes[nodeIndex];
        if (node.state == NodeState.Free)
        {
            maxLastUsedFrame = 0;
            leafCount = 0;
            return true;
        }

        if (node.state == NodeState.Allocated)
        {
            if (node.lastUsedFrame >= currentFrame)
            {
                maxLastUsedFrame = ulong.MaxValue;
                leafCount = 1;
                return false;
            }

            maxLastUsedFrame = node.lastUsedFrame;
            leafCount = 1;
            return true;
        }

        // Split node: inspect all 4 children
        maxLastUsedFrame = 0;
        leafCount = 0;
        var firstChild = (nodeIndex << 2) + 1u;
        for (var c = 0u; c < 4u; c++)
        {
            if (!EvaluateSubtreeEvictionCost(firstChild + c, currentFrame, out var childMaxFrame, out var childCount))
            {
                return false;
            }

            if (childCount > 0)
            {
                if (childMaxFrame > maxLastUsedFrame)
                {
                    maxLastUsedFrame = childMaxFrame;
                }
                leafCount += childCount;
            }
        }

        return true;
    }

    private void CollectAllocatedLeaves(uint nodeIndex, List<uint> outLeaves)
    {
        var state = _nodes[nodeIndex].state;
        if (state == NodeState.Free)
        {
            return;
        }

        if (state == NodeState.Allocated)
        {
            outLeaves.Add(nodeIndex);
            return;
        }

        var firstChild = (nodeIndex << 2) + 1u;
        for (var c = 0u; c < 4u; c++)
        {
            CollectAllocatedLeaves(firstChild + c, outLeaves);
        }
    }

    private bool TryAllocateRecursive(uint nodeIndex, int depth, int targetDepth, ulong currentFrame, out uint allocatedNode)
    {
        ref var node = ref _nodes[nodeIndex];
        if (node.state == NodeState.Allocated)
        {
            allocatedNode = uint.MaxValue;
            return false;
        }

        if (depth == targetDepth)
        {
            if (node.state == NodeState.Free)
            {
                node.state = NodeState.Allocated;
                node.lastUsedFrame = currentFrame;
                allocatedNode = nodeIndex;
                return true;
            }

            allocatedNode = uint.MaxValue;
            return false;
        }

        if (node.state == NodeState.Split)
        {
            var firstChild = (nodeIndex << 2) + 1u;
            for (var c = 0u; c < 4u; c++)
            {
                if (TryAllocateRecursive(firstChild + c, depth + 1, targetDepth, currentFrame, out allocatedNode))
                {
                    return true;
                }
            }

            allocatedNode = uint.MaxValue;
            return false;
        }

        // Split Free node and allocate first child branch
        node.state = NodeState.Split;
        var child0 = (nodeIndex << 2) + 1u;
        return TryAllocateRecursive(child0, depth + 1, targetDepth, currentFrame, out allocatedNode);
    }

    private ShadowAtlasRegion CreateRegion(uint x, uint y, uint w, uint h)
    {
        var invAtlas = 1.0f / _atlasSize;
        return new ShadowAtlasRegion
        {
            x = x,
            y = y,
            width = w,
            height = h,
            tileOffsetScale = new float4(x * invAtlas, y * invAtlas, w * invAtlas, h * invAtlas)
        };
    }

    public void Reset()
    {
        InitNode(0u, 0u, 0u, _atlasSize, 0);
    }
}
