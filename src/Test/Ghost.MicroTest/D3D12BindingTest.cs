using Ghost.D3D12;
using Ghost.D3D12MA;
using Ghost.DXGI;
using Ghost.MicroTest.Core;
using Ghost.Win32;
using System.Runtime.InteropServices;
using System.Text;
using static Ghost.D3D12.D3D_FEATURE_LEVEL;
using static Ghost.D3D12.D3D12;
using static Ghost.D3D12.D3D12_BARRIER_LAYOUT;
using static Ghost.D3D12.D3D12_COMMAND_LIST_TYPE;
using static Ghost.D3D12.D3D12_HEAP_TYPE;
using static Ghost.D3D12.D3D12_RESOURCE_DIMENSION;
using static Ghost.D3D12.D3D12_TEXTURE_LAYOUT;
using static Ghost.D3D12MA.D3D12MA_ALLOCATION_FLAGS;
using static Ghost.D3D12MA.D3D12MA_ALLOCATOR_FLAGS;
using static Ghost.DXGI.DXGI;
using static Ghost.DXGI.DXGI_FORMAT;
using static Ghost.DXGI.DXGI_GPU_PREFERENCE;
using static Ghost.Win32.UUID;

namespace Ghost.MicroTest;

internal unsafe class D3D12BindingTest : ITest
{
    private const uint AGILITY_SDK_VERSION = 620;

    private IDXGIFactory7* _dxgiFactory;
    private IDXGIAdapter1* _adapter;
    private ID3D12SDKConfiguration1* _sdkConfig;
    private ID3D12DeviceFactory* _deviceFactory;
    private ID3D12Device14* _device;
    private D3D12MA_Allocator* _allocator;

    public void Setup()
    {
        IDXGIFactory7* pDxgiFactory = default;
        var hr = CreateDXGIFactory2(0, __uuidof(pDxgiFactory), (void**)&pDxgiFactory);
        Assert.IsTrue(hr >= 0, $"CreateDXGIFactory2 failed. HRESULT: 0x{hr:X8}");
        _dxgiFactory = pDxgiFactory;

        IDXGIAdapter1* pAdapter = default;
        hr = _dxgiFactory->EnumAdapterByGpuPreference(0, DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE, __uuidof(pAdapter), (void**)&pAdapter);
        Assert.IsTrue(hr >= 0, $"IDXGIFactory7.EnumAdapterByGpuPreference failed. HRESULT: 0x{hr:X8}");
        _adapter = pAdapter;

        ID3D12SDKConfiguration1* pSdkConfig = default;
        var clsidSdkConfig = CLSID_D3D12SDKConfiguration;
        hr = D3D12GetInterface(&clsidSdkConfig, __uuidof(pSdkConfig), (void**)&pSdkConfig);
        Assert.IsTrue(hr >= 0, $"D3D12GetInterface(CLSID_D3D12SDKConfiguration) failed. HRESULT: 0x{hr:X8}");
        _sdkConfig = pSdkConfig;

        var sdkPath = Path.Combine(AppContext.BaseDirectory, "D3D12");
        var sdkPathUtf8 = Encoding.UTF8.GetBytes(sdkPath + '\0');

        ID3D12DeviceFactory* pFactory = default;
        fixed (byte* pSdkPath = sdkPathUtf8)
        {
            hr = _sdkConfig->CreateDeviceFactory(AGILITY_SDK_VERSION, (sbyte*)pSdkPath, __uuidof(pFactory), (void**)&pFactory);
        }

        Assert.IsTrue(hr >= 0, $"ID3D12SDKConfiguration1.CreateDeviceFactory(version {AGILITY_SDK_VERSION}, path '{sdkPath}') failed. HRESULT: 0x{hr:X8}");
        _deviceFactory = pFactory;

        ID3D12Device14* pDevice = default;
        hr = _deviceFactory->CreateDevice((IUnknown*)_adapter, D3D_FEATURE_LEVEL_12_0, __uuidof(pDevice), (void**)&pDevice);
        Assert.IsTrue(hr >= 0, $"ID3D12DeviceFactory.CreateDevice(ID3D12Device14) failed. HRESULT: 0x{hr:X8}");
        Assert.IsTrue(pDevice != null, "Created ID3D12Device14 pointer is null.");
        _device = pDevice;

        var allocatorDesc = new D3D12MA_ALLOCATOR_DESC
        {
            Flags = D3D12MA_ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED | D3D12MA_ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED,
            pDevice = (ID3D12Device*)_device,
            pAdapter = (IDXGIAdapter*)_adapter
        };

        D3D12MA_Allocator* pAllocator = default;
        hr = D3D12MAApi.CreateAllocator(&allocatorDesc, &pAllocator);
        Assert.IsTrue(hr >= 0, $"D3D12MAApi.CreateAllocator failed. HRESULT: 0x{hr:X8}");
        Assert.IsTrue(pAllocator != null, "Created D3D12MA_Allocator pointer is null.");
        _allocator = pAllocator;
    }

    public void Run()
    {
        DXGI_ADAPTER_DESC1 adapterDesc;
        var hr = _adapter->GetDesc1(&adapterDesc);
        Assert.IsTrue(hr >= 0, $"IDXGIAdapter1.GetDesc1 failed. HRESULT: 0x{hr:X8}");

        var gpuName = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(&adapterDesc.Description.e0).ToString();
        var nodeCount = _device->GetNodeCount();
        Assert.IsTrue(nodeCount >= 1, $"Expected at least 1 GPU node, got {nodeCount}.");

        var luid = _device->GetAdapterLuid();
        Assert.AreEqual(adapterDesc.AdapterLuid.LowPart, luid.LowPart, "Device LUID LowPart mismatch.");
        Assert.AreEqual(adapterDesc.AdapterLuid.HighPart, luid.HighPart, "Device LUID HighPart mismatch.");

        var queueDesc = new D3D12_COMMAND_QUEUE_DESC
        {
            Type = D3D12_COMMAND_LIST_TYPE_DIRECT
        };

        ID3D12CommandQueue* pQueue = default;
        hr = _device->CreateCommandQueue(&queueDesc, __uuidof(pQueue), (void**)&pQueue);
        Assert.IsTrue(hr >= 0, $"ID3D12Device14.CreateCommandQueue failed. HRESULT: 0x{hr:X8}");
        Assert.IsTrue(pQueue != null, "Created ID3D12CommandQueue pointer is null.");
        pQueue->Release();

        var allocDesc = new D3D12MA_ALLOCATION_DESC
        {
            Flags = D3D12MA_ALLOCATION_FLAG_NONE,
            HeapType = D3D12_HEAP_TYPE_DEFAULT
        };

        var resourceDesc = new D3D12_RESOURCE_DESC1
        {
            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
            Width = 64 * 1024,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = DXGI_FORMAT_UNKNOWN,
            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR
        };

        D3D12MA_Allocation* pAllocation = default;
        var nullGuid = Guid.Empty;
        hr = _allocator->CreateResource3(&allocDesc, &resourceDesc, D3D12_BARRIER_LAYOUT_UNDEFINED, null, 0, null, &pAllocation, &nullGuid, null);
        Assert.IsTrue(hr >= 0, $"D3D12MA_Allocator.CreateResource3 failed. HRESULT: 0x{hr:X8}");
        Assert.IsTrue(pAllocation != null, "Created D3D12MA_Allocation pointer is null.");
        Assert.IsTrue(pAllocation->GetResource() != null, "Allocation resource pointer is null.");
        Assert.IsTrue(pAllocation->GetSize() >= 64 * 1024, "Allocation size is smaller than requested.");

        fixed (char* pName = "TestBuffer")
        {
            pAllocation->SetName(pName);
        }

        D3D12MA_Budget localBudget;
        _allocator->GetBudget(&localBudget, null);
        Assert.IsTrue(localBudget.BudgetBytes > 0, "Expected non-zero local VRAM budget.");

        pAllocation->Release();

        Console.WriteLine($"D3D12 + D3D12MA initialized on '{gpuName}' (Agility SDK {AGILITY_SDK_VERSION}, Nodes: {nodeCount}, VRAM Budget: {localBudget.BudgetBytes / (1024 * 1024)} MB).");
    }

    public void Cleanup()
    {
        if (_allocator != null)
        {
            _allocator->Release();
            _allocator = null;
        }

        if (_device != null)
        {
            _device->Release();
            _device = null;
        }

        if (_deviceFactory != null)
        {
            _deviceFactory->Release();
            _deviceFactory = null;
        }

        if (_sdkConfig != null)
        {
            _sdkConfig->Release();
            _sdkConfig = null;
        }

        if (_adapter != null)
        {
            _adapter->Release();
            _adapter = null;
        }

        if (_dxgiFactory != null)
        {
            _dxgiFactory->Release();
            _dxgiFactory = null;
        }
    }
}
