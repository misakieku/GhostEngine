using Ghost.Core;
using Ghost.Graphics.Data;
using System.Collections.Immutable;
using Win32;
using Win32.Graphics.Direct3D;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;

namespace Ghost.Graphics.D3D12;

internal unsafe class GraphicsDevice
{
    private ComPtr<IDXGIFactory7> _dxgiFactory;
    private ComPtr<ID3D12Device14> _device;
    private ComPtr<IDXGIAdapter1> _adapter;
    private ComPtr<ID3D12CommandQueue> _commandQueue;

    private ImmutableArray<Renderer> _initializeQueue;
    private ImmutableArray<Renderer> _renderers;

    private bool _disposed;

    public ReadOnlySpan<Renderer> InitializeQueue => _initializeQueue.AsSpan();
    public ReadOnlySpan<Renderer> Renderers => _renderers.AsSpan();

    public ConstPtr<ID3D12Device14> NativeDevice => new(_device.Get());
    public ConstPtr<IDXGIFactory7> DXGIFactory => new(_dxgiFactory.Get());
    public ConstPtr<IDXGIAdapter1> Adapter => new(_adapter.Get());
    public ConstPtr<ID3D12CommandQueue> CommandQueue => new(_commandQueue.Get());

    public GraphicsDevice()
    {
        InitializeDevice();
        InitializeCommandQueue();

        _initializeQueue = ImmutableArray<Renderer>.Empty;
        _renderers = ImmutableArray<Renderer>.Empty;
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
            _device.Get()->CreateCommandQueue(&queueDesc, __uuidof<ID3D12CommandQueue>(), (void**)queuePtr);
        }
    }

    public Renderer CreateRenderer(in SwapChainPresenter presenter)
    {
        var renderView = new Renderer(this, in presenter);
        ImmutableInterlocked.Update(ref _initializeQueue, old => old.Add(renderView));

        return renderView;
    }

    public void RemoveRenderer(Renderer renderer)
    {
        if (renderer is Renderer dx12RenderView)
        {
            dx12RenderView.Dispose();

            var index = _initializeQueue.IndexOf(dx12RenderView);
            if (index > -1)
            {
                ImmutableInterlocked.Update(ref _initializeQueue, old => old.RemoveAt(index));
            }
            else
            {
                ImmutableInterlocked.Update(ref _renderers, old => old.Remove(dx12RenderView));
            }
        }
    }

    public void InitializePendingRenderers()
    {
        if (_initializeQueue.IsEmpty)
        {
            return;
        }

        foreach (var renderer in _initializeQueue.AsSpan())
        {
            renderer.Initialize();
        }

        ImmutableInterlocked.Update(ref _renderers, old => old.AddRange(_initializeQueue));
        _initializeQueue = _initializeQueue.Clear();
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

        _commandQueue.Dispose();
        _device.Reset();
        _dxgiFactory.Dispose();

        _disposed = true;
    }
}