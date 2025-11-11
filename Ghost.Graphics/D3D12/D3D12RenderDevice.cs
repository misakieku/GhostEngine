using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Aliases.D3D_Alias;
using static TerraFX.Aliases.DXGI_Alias;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of the render device interface
/// </summary>
[SupportedOSPlatform(Win32Utility.OS_SUPPORTED_VERSION)]
internal unsafe class D3D12RenderDevice : IRenderDevice
{
    private ComPtr<IDXGIFactory7> _dxgiFactory;
    private ComPtr<ID3D12Device14> _device;
    private ComPtr<IDXGIAdapter1> _adapter;

    private readonly D3D12CommandQueue _graphicsQueue;
    private readonly D3D12CommandQueue _computeQueue;
    private readonly D3D12CommandQueue _copyQueue;

    private bool _disposed;

    public ICommandQueue GraphicsQueue => _graphicsQueue;
    public ICommandQueue ComputeQueue => _computeQueue;
    public ICommandQueue CopyQueue => _copyQueue;

    public ID3D12Device14* NativeDevice => _device.Get();
    public IDXGIFactory7* DXGIFactory => _dxgiFactory.Get();
    public IDXGIAdapter1* Adapter => _adapter.Get();

    public D3D12RenderDevice()
    {
        InitializeDevice();

        _graphicsQueue = new D3D12CommandQueue(_device.Get(), CommandQueueType.Graphics);
        _computeQueue = new D3D12CommandQueue(_device.Get(), CommandQueueType.Compute);
        _copyQueue = new D3D12CommandQueue(_device.Get(), CommandQueueType.Copy);
    }

    ~D3D12RenderDevice()
    {
        Dispose();
    }

    private void InitializeDevice()
    {
        IDXGIFactory7* pFactory = default;
#if DEBUG
        CreateDXGIFactory2(TRUE, __uuidof(pFactory), (void**)&pFactory);
#else
        CreateDXGIFactory2(FALSE, __uuidof(pFactory), (void**)&pFactory);
#endif

        _dxgiFactory.Attach(pFactory);

        ID3D12Device14* pDevice = default;
        IDXGIAdapter1* pAdapter = default;

        for (uint adapterIndex = 0;
            _dxgiFactory.Get()->EnumAdapterByGpuPreference(adapterIndex, DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE, __uuidof<IDXGIAdapter1>(), (void**)&pAdapter).SUCCEEDED;
            adapterIndex++)
        {
            DXGI_ADAPTER_DESC1 desc = default;
            pAdapter->GetDesc1(&desc);

            // Don't select the Basic Render Driver adapter.
            if (desc.Flags.HasFlag(DXGI_ADAPTER_FLAG_SOFTWARE))
            {
                goto NEXT_ITERATION;
            }

            if (D3D12CreateDevice((IUnknown*)pAdapter, D3D_FEATURE_LEVEL_12_0, __uuidof(pDevice), (void**)&pDevice).SUCCEEDED)
            {
                _adapter.Attach(pAdapter);
                break;
            }

        NEXT_ITERATION:
            pAdapter->Release();
        }

        if (pDevice == null)
        {
            pAdapter->Release(); // Dispose the last adapter we tried. If the operation succeeded, we would have moved it.
            throw new PlatformNotSupportedException("Cannot create ID3D12Device with feature level 12.0");
        }

        _device.Attach(pDevice);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _graphicsQueue?.Dispose();
        _computeQueue?.Dispose();
        _copyQueue?.Dispose();

        _device.Reset();
        _dxgiFactory.Dispose();
        _adapter.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
