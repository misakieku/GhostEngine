using Ghost.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;
using System.Collections.Immutable;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;
using static Win32.Apis;
using static Win32.Graphics.Direct3D12.Apis;
using static Win32.Graphics.Dxgi.Apis;

namespace Ghost.Graphics.DX12;

internal unsafe class DX12GraphicsDevice : IGraphicsDevice
{
#if DEBUG
    private readonly DX12DebugLayer _debugLayer;
#endif
    private readonly ComPtr<IDXGIFactory7> _dxgiFactory;
    private readonly ComPtr<ID3D12Device14> _device;
    private readonly ComPtr<ID3D12CommandQueue> _commandQueue;

    private ImmutableArray<IRenderer> _renderers;

    private bool _disposed;

    public static GraphicsAPI TargetAPI => GraphicsAPI.DX12;
    public ReadOnlySpan<IRenderer> Renderers => _renderers.AsSpan();

    public ConstPtr<ID3D12Device14> NativeDevice => new(_device.Get());
    public ConstPtr<IDXGIFactory7> DXGIFactory => new(_dxgiFactory.Get());
    public ConstPtr<ID3D12CommandQueue> CommandQueue => new(_commandQueue.Get());

    public DX12GraphicsDevice()
    {
#if DEBUG
        _debugLayer = new DX12DebugLayer();
#endif

        InitializeDevice();
        InitializeCommandQueue();

        _renderers = ImmutableArray<IRenderer>.Empty;
    }

    private void InitializeDevice()
    {
        fixed (void* factoryPtr = &_dxgiFactory)
        {
#if DEBUG
            CreateDXGIFactory2(true, __uuidof<IDXGIFactory2>(), &factoryPtr);
            //factory = DXGI.CreateDXGIFactory2<IDXGIFactory7>(true);
#else
            //factory = DXGI.CreateDXGIFactory2<IDXGIFactory7>(false);
            CreateDXGIFactory2(false, __uuidof<IDXGIFactory2>(), &factoryPtr);
#endif
        }

        using ComPtr<IDXGIAdapter1> adapter = default;

        for (uint adapterIndex = 0;
            _dxgiFactory.Get()->EnumAdapterByGpuPreference(adapterIndex, GpuPreference.HighPerformance, __uuidof<IDXGIAdapter1>(), (void**)adapter.ReleaseAndGetAddressOf()).Success;
            adapterIndex++)
        {
            AdapterDescription1 desc = default;
            adapter.Get()->GetDesc1(&desc);

            // Don't select the Basic Render Driver adapter.
            if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
            {
                continue;
            }

            fixed (void* devicePtr = &_device)
            {
                if (D3D12CreateDevice((IUnknown*)adapter.Get(), FeatureLevel.Level_11_0, __uuidof<ID3D12Device>(), (void**)devicePtr).Success)
                {
                    break;
                }
            }
        }

        if (_device.Get() == null)
        {
            throw new PlatformNotSupportedException("Cannot create ID3D12Device");
        }
    }

    private void InitializeCommandQueue()
    {
        var queueDesc = new CommandQueueDescription
        {
            Type = CommandListType.Direct,
            Priority = (int)CommandQueuePriority.High,
            Flags = CommandQueueFlags.None,
        };

        fixed (void* queuePtr = &_commandQueue)
        {
            _device.Get()->CreateCommandQueue(&queueDesc, __uuidof<ID3D12CommandQueue>(), &queuePtr);
        }
    }

    public IRenderer CreateRenderer(in SwapChainPresenter presenter)
    {
        var renderView = new DX12Renderer(this, in presenter);
        ImmutableInterlocked.Update(ref _renderers, old => old.Add(renderView));

        return renderView;
    }

    public void RemoveRenderer(IRenderer renderer)
    {
        if (renderer is DX12Renderer dx12RenderView)
        {
            dx12RenderView.Dispose();
            ImmutableInterlocked.Update(ref _renderers, old => old.Remove(dx12RenderView));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var renderer in _renderers)
        {
            renderer.Dispose();
        }

        foreach (var renderView in _renderers)
        {
            renderView.Dispose();
        }

        _commandQueue.Dispose();
        _device.Dispose();
        _dxgiFactory.Dispose();

#if DEBUG
        _debugLayer.Dispose();
#endif
        _disposed = true;
    }
}