using Win32;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12DebugLayer
{
    private readonly ComPtr<ID3D12Debug6> _d3d12Debug;
    private readonly ComPtr<IDXGIDebug1> _dxgiDebug;
    private readonly ComPtr<IDXGIInfoQueue> _dxgiInfoQueue;

    public D3D12DebugLayer()
    {
        D3D12GetDebugInterface(__uuidof<ID3D12Debug6>(), _d3d12Debug.GetVoidAddressOf());
        _d3d12Debug.Get()->EnableDebugLayer();

        DXGIGetDebugInterface1(0u, __uuidof<IDXGIDebug1>(), _dxgiDebug.GetVoidAddressOf());
        _dxgiDebug.Get()->EnableLeakTrackingForThread();

        DXGIGetDebugInterface1(0u, __uuidof<IDXGIInfoQueue>(), _dxgiInfoQueue.GetVoidAddressOf());
        _dxgiInfoQueue.Get()->SetBreakOnSeverity(DXGI_DEBUG_ALL, InfoQueueMessageSeverity.Error, true);
        _dxgiInfoQueue.Get()->SetBreakOnSeverity(DXGI_DEBUG_ALL, InfoQueueMessageSeverity.Corruption, true);
    }

    public void Dispose()
    {
        _dxgiDebug.Get()->ReportLiveObjects(DXGI_DEBUG_ALL, ReportLiveObjectFlags.All);

        _d3d12Debug.Dispose();
        _dxgiDebug.Dispose();
        _dxgiInfoQueue.Dispose();
    }
}