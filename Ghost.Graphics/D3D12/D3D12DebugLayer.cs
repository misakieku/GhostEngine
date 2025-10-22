using Ghost.Core.Utilities;
using Ghost.Graphics.D3D12.Utilities;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.DXGI_Alias;

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
        _dxgiInfoQueue.Get()->SetBreakOnSeverity(DXGI_DEBUG_ALL, DXGI_INFO_QUEUE_MESSAGE_SEVERITY_ERROR, true);
        _dxgiInfoQueue.Get()->SetBreakOnSeverity(DXGI_DEBUG_ALL, DXGI_INFO_QUEUE_MESSAGE_SEVERITY_CORRUPTION, true);
    }

    public void Dispose()
    {
        _dxgiDebug.Get()->ReportLiveObjects(DXGI_DEBUG_ALL, DXGI_DEBUG_RLO_ALL | DXGI_DEBUG_RLO_IGNORE_INTERNAL);

        _d3d12Debug.Dispose();
        _dxgiDebug.Dispose();
        _dxgiInfoQueue.Dispose();
    }
}