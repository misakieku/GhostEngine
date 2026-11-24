using System.Runtime.CompilerServices;

namespace Ghost.D3D12MA;

public partial struct D3D12MA_TotalStatistics
{
    [NativeTypeName("D3D12MA_DetailedStatistics[5]")]
    public _HeapType_e__FixedBuffer HeapType;

    [NativeTypeName("D3D12MA_DetailedStatistics[2]")]
    public _MemorySegmentGroup_e__FixedBuffer MemorySegmentGroup;

    public D3D12MA_DetailedStatistics Total;

    [InlineArray(5)]
    public partial struct _HeapType_e__FixedBuffer
    {
        public D3D12MA_DetailedStatistics e0;
    }

    [InlineArray(2)]
    public partial struct _MemorySegmentGroup_e__FixedBuffer
    {
        public D3D12MA_DetailedStatistics e0;
    }
}
