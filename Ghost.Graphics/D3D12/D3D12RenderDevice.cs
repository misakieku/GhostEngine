using Ghost.Core.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Aliases.D3D_Alias;
using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.DXGI_Alias;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of the render device interface
/// </summary>
internal unsafe class D3D12RenderDevice : IRenderDevice
{
    private UniquePtr<ID3D12Device14> _device;
    private UniquePtr<IDXGIFactory7> _dxgiFactory;
    private UniquePtr<IDXGIAdapter1> _adapter;

    private readonly D3D12CommandQueue _graphicsQueue;
    private readonly D3D12CommandQueue _computeQueue;
    private readonly D3D12CommandQueue _copyQueue;

    private bool _disposed;

    public ICommandQueue GraphicsQueue => _graphicsQueue;
    public ICommandQueue ComputeQueue => _computeQueue;
    public ICommandQueue CopyQueue => _copyQueue;

    public FeatureSupport FeatureSupport
    {
        get;
    }

    public SharedPtr<IDXGIFactory7> DXGIFactory => _dxgiFactory.Share();
    public SharedPtr<ID3D12Device14> NativeDevice => _device.Share();
    public SharedPtr<IDXGIAdapter1> Adapter => _adapter.Share();
    public SharedPtr<ID3D12CommandQueue> NativeGraphicsQueue => _graphicsQueue.NativeQueue;
    public SharedPtr<ID3D12CommandQueue> NativeComputeQueue => _computeQueue.NativeQueue;
    public SharedPtr<ID3D12CommandQueue> NativeCopyQueue => _copyQueue.NativeQueue;

    public D3D12RenderDevice()
    {
        IDXGIFactory7* pFactory = default;
#if DEBUG
        ThrowIfFailed(CreateDXGIFactory2(TRUE, __uuidof(pFactory), (void**)&pFactory));
#else
        ThrowIfFailed(CreateDXGIFactory2(FALSE, __uuidof(pFactory), (void**)&pFactory));
#endif

        _dxgiFactory.Attach(pFactory);

        InitializeDevice();

        _graphicsQueue = new D3D12CommandQueue(_device.Get(), CommandQueueType.Graphics);
        _computeQueue = new D3D12CommandQueue(_device.Get(), CommandQueueType.Compute);
        _copyQueue = new D3D12CommandQueue(_device.Get(), CommandQueueType.Copy);

        FeatureSupport = GetFeatureSupport();
    }

    ~D3D12RenderDevice()
    {
        Dispose();
    }

    private void InitializeDevice()
    {
        ID3D12Device14* pDevice = default;
        IDXGIAdapter1* pAdapter = default;

        for (uint adapterIndex = 0;
            _dxgiFactory.Get()->EnumAdapterByGpuPreference(adapterIndex, DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE, __uuidof(pAdapter), (void**)&pAdapter).SUCCEEDED;
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
            pAdapter->Release(); // Dispose the last adapter we tried.
            throw new PlatformNotSupportedException("Cannot create ID3D12Device with feature level 12.0");
        }

        _device.Attach(pDevice);
    }

    private FeatureSupport GetFeatureSupport()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        FeatureSupport support = FeatureSupport.None;

        D3D12_FEATURE_DATA_D3D12_OPTIONS options = default;
        if (_device.Get()->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS, &options, (uint)sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS)).SUCCEEDED)
        {
            if (options.ResourceBindingTier == D3D12_RESOURCE_BINDING_TIER_3)
            {
                support |= FeatureSupport.BindlessResources;
            }
        }

        D3D12_FEATURE_DATA_D3D12_OPTIONS5 options5 = default;
        if (_device.Get()->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS5, &options5, (uint)sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS5)).SUCCEEDED)
        {
            if (options5.RaytracingTier != D3D12_RAYTRACING_TIER_NOT_SUPPORTED)
            {
                support |= FeatureSupport.RayTracing;
            }
        }

        D3D12_FEATURE_DATA_D3D12_OPTIONS6 options6 = default;
        if (_device.Get()->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS6, &options6, (uint)sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS6)).SUCCEEDED)
        {
            if (options6.VariableShadingRateTier != D3D12_VARIABLE_SHADING_RATE_TIER_NOT_SUPPORTED)
            {
                support |= FeatureSupport.VariableRateShading;
            }
        }

        D3D12_FEATURE_DATA_D3D12_OPTIONS7 options7 = default;
        if (_device.Get()->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS7, &options7, (uint)sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS7)).SUCCEEDED)
        {
            if (options7.MeshShaderTier != D3D12_MESH_SHADER_TIER_NOT_SUPPORTED)
            {
                support |= FeatureSupport.MeshShaders;
            }

            if (options7.SamplerFeedbackTier != D3D12_SAMPLER_FEEDBACK_TIER_NOT_SUPPORTED)
            {
                support |= FeatureSupport.SamplerFeedback;
            }
        }

        D3D12_FEATURE_DATA_D3D12_OPTIONS21 options9 = default;
        if (_device.Get()->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS21, &options9, (uint)sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS8)).SUCCEEDED)
        {
            if (options9.WorkGraphsTier != D3D12_WORK_GRAPHS_TIER.D3D12_WORK_GRAPHS_TIER_NOT_SUPPORTED)
            {
                support |= FeatureSupport.WorkGraphs;
            }
        }

        return support;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _graphicsQueue.Dispose();
        _computeQueue.Dispose();
        _copyQueue.Dispose();

        _device.Dispose();
        _dxgiFactory.Dispose();
        _adapter.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
