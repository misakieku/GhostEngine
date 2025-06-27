using Ghost.Graphics.Contracts;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.DXGI;
using Vortice.DXGI.Debug;

namespace Ghost.Graphics.DX12;

internal class DX12DebugLayer : IDebugLayer
{
#if DEBUG
    private readonly ID3D12Debug6 _d3d12Debug;
    private readonly IDXGIDebug1 _dxgiDebug;
#endif

    public DX12DebugLayer()
    {
#if DEBUG
        _d3d12Debug = D3D12.D3D12GetDebugInterface<ID3D12Debug6>();
        _d3d12Debug.EnableDebugLayer();

        _dxgiDebug = DXGI.DXGIGetDebugInterface1<IDXGIDebug1>();
        _dxgiDebug.EnableLeakTrackingForThread();
#endif
    }

    public void Dispose()
    {
#if DEBUG
        _dxgiDebug.ReportLiveObjects(DXGI.DebugAll, ReportLiveObjectFlags.Detail | ReportLiveObjectFlags.IgnoreInternal);

        _d3d12Debug?.Dispose();
        _dxgiDebug?.Dispose();
#endif
    }
}