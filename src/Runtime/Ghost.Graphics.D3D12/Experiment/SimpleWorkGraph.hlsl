#ifndef SIMPLE_WORKGRAPH_HLSL
#define SIMPLE_WORKGRAPH_HLSL

#include "F:/csharp/GhostEngine/src/Runtime/Ghost.Graphics/Shaders/Includes/Common.hlsl"
#include "F:/csharp/GhostEngine/src/Runtime/Ghost.Graphics/Shaders/Includes/Random.hlsl"

// The record types
struct InitialRecord
{
    uint seed;
};

struct IncrementRecord
{
    uint incrementValue;
};

// Bindless root constants
cbuffer RootConstants : register(b0)
{
    uint counterBufferId;
    uint threshold; 
};

// Node: MainNode
// Entry point for the Work Graph. Spawns threads matching the record count.
[Shader("node")]
[NodeIsProgramEntry]
[NodeLaunch("thread")]
void MainNode(
    uint dispatchThreadID : SV_DispatchThreadID,
    in InitialRecord record,
    [MaxRecords(1)] NodeOutput<IncrementRecord> IncrementNode) // Target the IncrementNode
{
    // Generate a random number from JenkinsHash
    uint randValue = JenkinsHash(record.seed ^ dispatchThreadID);

    // If the random number exceeds our threshold, launch the next node
    if (randValue > threshold)
    {
        ThreadNodeOutputRecords<IncrementRecord> outRecord = IncrementNode.GetThreadNodeOutputRecords(1);
        outRecord.Get().incrementValue = 1;
        outRecord.OutputComplete();
    }
}

// Node: IncrementNode
// Triggered dynamically from MainNode
[Shader("node")]
[NodeLaunch("thread")]
void IncrementNode(
    uint dispatchThreadID : SV_DispatchThreadID,
    in IncrementRecord record)
{
    // Retrieve our RWByteAddressBuffer generically through SM6.6 bindless descriptor heap
    RWByteAddressBuffer counterBuffer = ResourceDescriptorHeap[counterBufferId];

    // Thread-safe atomic increment of our counter across all dispatched records
    uint originalValue;
    counterBuffer.InterlockedAdd(0, record.incrementValue, originalValue);
}

#endif
