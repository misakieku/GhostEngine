using Ghost.Graphics.D3D12.Utilities;
using Misaki.HighPerformance.LowLevel;
using TerraFX.Interop.DirectX;

using static TerraFX.Aliases.DXGI_Alias;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12DebugLayer
{
    private UniquePtr<ID3D12Debug6> _d3d12Debug;
    private UniquePtr<IDXGIDebug1> _dxgiDebug;
    private UniquePtr<IDXGIInfoQueue> _dxgiInfoQueue;

    public D3D12DebugLayer()
    {
        ID3D12Debug6* pDebug = default;
        ThrowIfFailed(D3D12GetDebugInterface(__uuidof(pDebug), (void**)&pDebug));
        pDebug->EnableDebugLayer();

        IDXGIDebug1* pDxgiDebug = default;
        ThrowIfFailed(DXGIGetDebugInterface1(0u, __uuidof(pDxgiDebug), (void**)&pDxgiDebug));
        pDxgiDebug->EnableLeakTrackingForThread();

        IDXGIInfoQueue* pDxgiInfoQueue = default;
        ThrowIfFailed(DXGIGetDebugInterface1(0u, __uuidof(pDxgiInfoQueue), (void**)&pDxgiInfoQueue));
        ThrowIfFailed(pDxgiInfoQueue->SetBreakOnSeverity(DXGI_DEBUG_ALL, DXGI_INFO_QUEUE_MESSAGE_SEVERITY_ERROR, true));
        ThrowIfFailed(pDxgiInfoQueue->SetBreakOnSeverity(DXGI_DEBUG_ALL, DXGI_INFO_QUEUE_MESSAGE_SEVERITY_CORRUPTION, true));

        _d3d12Debug.Attach(pDebug);
        _dxgiDebug.Attach(pDxgiDebug);
        _dxgiInfoQueue.Attach(pDxgiInfoQueue);
    }

    public void Dispose()
    {
        ThrowIfFailed(_dxgiDebug.Get()->ReportLiveObjects(DXGI_DEBUG_ALL, DXGI_DEBUG_RLO_ALL | DXGI_DEBUG_RLO_IGNORE_INTERNAL));

        _d3d12Debug.Dispose();
        _dxgiDebug.Dispose();
        _dxgiInfoQueue.Dispose();
    }
}