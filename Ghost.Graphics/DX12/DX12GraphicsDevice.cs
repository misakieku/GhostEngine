using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Ghost.Graphics.DX12;

internal class DX12GraphicsDevice : IGraphicsDevice
{
    private readonly IDXGIFactory7 _dxgiFactory;
    private readonly ID3D12Device14 _device;
    private readonly ID3D12CommandQueue _commandQueue;

    private readonly List<IRenderView> _renderViews = new();
    private readonly Lock _lock = new();

#if DEBUG
    private readonly DX12DebugLayer _debugLayer;
#endif

    private bool _disposed;

    public ID3D12Device14 Device => _device;
    public IDXGIFactory7 DXGIFactory => _dxgiFactory;
    public ID3D12CommandQueue CommandQueue => _commandQueue;

    public static IGraphicsDevice Create() => new DX12GraphicsDevice();

    private DX12GraphicsDevice()
    {
#if DEBUG
        _debugLayer = new DX12DebugLayer();
#endif

        InitializeDevice(out _dxgiFactory, out _device);
        InitializeCommandQueue(out _commandQueue);
    }

    private void InitializeDevice(out IDXGIFactory7 factory, out ID3D12Device14 device)
    {
#if DEBUG
        factory = DXGI.CreateDXGIFactory2<IDXGIFactory7>(true);
#else
        factory = DXGI.CreateDXGIFactory2<IDXGIFactory7>(false);
#endif

        ID3D12Device14? d3d12Device = default;
        for (uint adapterIndex = 0;
            factory.EnumAdapters1(adapterIndex, out var adapter).Success;
            adapterIndex++)
        {
            var desc = adapter.Description1;

            // Don't select the Basic Render Driver adapter.
            if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
            {
                adapter.Dispose();
                continue;
            }

            if (D3D12.D3D12CreateDevice(adapter, FeatureLevel.Level_11_0, out d3d12Device).Success)
            {
                adapter.Dispose();
                break;
            }

            adapter.Dispose();
        }

        if (d3d12Device == null)
        {
            throw new PlatformNotSupportedException("Cannot create ID3D12Device");
        }

        device = d3d12Device;
    }

    private void InitializeCommandQueue(out ID3D12CommandQueue queue)
    {
        var queueDesc = new CommandQueueDescription
        {
            Type = CommandListType.Direct,
            Priority = (int)CommandQueuePriority.High,
            Flags = CommandQueueFlags.None,
        };

        queue = _device.CreateCommandQueue(queueDesc);
    }

    public IRenderView CreateRenderView(in SwapChainPresenter swapChainSurface)
    {
        var renderView = new DX12RenderView(this, swapChainSurface);
        lock (_lock)
        {
            _renderViews.Add(renderView);
        }

        return renderView;
    }

    public void OnRender()
    {
        lock (_lock)
        {
            foreach (var renderView in _renderViews)
            {
                renderView.ExecutePendingResize();

                renderView.BeginRender();
                renderView.Render();
                renderView.EndRender();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var renderView in _renderViews)
        {
            renderView.Dispose();
        }
        _renderViews.Clear();

        _commandQueue.Release();
        _device.Release();
        _dxgiFactory.Release();

#if DEBUG
        _debugLayer.Dispose();
#endif
        _disposed = true;
    }
}