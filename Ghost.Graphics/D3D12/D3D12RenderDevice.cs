using Ghost.Graphics.RHI;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of the render device interface
/// </summary>
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

        _graphicsQueue = new D3D12CommandQueue(_device, CommandQueueType.Graphics);
        _computeQueue = new D3D12CommandQueue(_device, CommandQueueType.Compute);
        _copyQueue = new D3D12CommandQueue(_device, CommandQueueType.Copy);
    }

    ~D3D12RenderDevice()
    {
        Dispose();
    }

    private void InitializeDevice()
    {
#if DEBUG
        CreateDXGIFactory2(true, __uuidof<IDXGIFactory7>(), _dxgiFactory.GetVoidAddressOf());
#else
        CreateDXGIFactory2(false, __uuidof<IDXGIFactory7>(), _dxgiFactory.GetVoidAddressOf());
#endif

        using ComPtr<IDXGIAdapter1> adapter = default;

        for (uint adapterIndex = 0;
            _dxgiFactory.Get()->EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, __uuidof<IDXGIAdapter1>(), adapter.ReleaseAndGetVoidAddressOf()).Success;
            adapterIndex++)
        {
            AdapterDescription1 desc = default;
            adapter.Get()->GetDesc1(&desc);

            // Don't select the Basic Render Driver adapter.
            if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
            {
                continue;
            }

            if (D3D12CreateDevice((IUnknown*)adapter.Get(), FeatureLevel.Level_12_0, __uuidof<ID3D12Device14>(), _device.GetVoidAddressOf()).Success)
            {
                _adapter = adapter.Move();
                break;
            }
        }

        if (_device.Get() == null)
        {
            throw new PlatformNotSupportedException("Cannot create ID3D12Device with feature level 12.0");
        }
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
