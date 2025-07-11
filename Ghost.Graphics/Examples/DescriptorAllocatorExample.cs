using Ghost.Graphics.D3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Examples;

/// <summary>
/// Example demonstrating how to use the IDescriptorAllocator interface.
/// </summary>
public static class DescriptorAllocatorExample
{
    /// <summary>
    /// Example showing basic descriptor allocation and release.
    /// </summary>
    public static void BasicDescriptorUsage()
    {
        // Note: In a real application, you would get the descriptor allocator from the graphics device
        // var device = GraphicsPipeline.GetGraphicsDevice<D3D12GraphicsDevice>();
        // var descriptorAllocator = device.DescriptorAllocator;

        // For demonstration purposes, we'll show the interface usage
        DescriptorAllocator descriptorAllocator = null!; // Would be obtained from graphics device

        // Allocate a single RTV descriptor
        var rtvDescriptor = descriptorAllocator.AllocateRTV();

        // Allocate multiple SRV descriptors
        var srvDescriptors = descriptorAllocator.AllocateSRVs(4);

        // Allocate a DSV descriptor
        var dsvDescriptor = descriptorAllocator.AllocateDSV();

        // Allocate sampler descriptors
        var samplerDescriptors = descriptorAllocator.AllocateSamplers(2);

        // Use the descriptors...
        ProcessDescriptors(rtvDescriptor, srvDescriptors, dsvDescriptor, samplerDescriptors);

        // Release descriptors when done
        descriptorAllocator.ReleaseRTV(rtvDescriptor);
        descriptorAllocator.ReleaseSRVs(srvDescriptors);
        descriptorAllocator.ReleaseDSV(dsvDescriptor);
        descriptorAllocator.ReleaseSamplers(samplerDescriptors);
    }

    /// <summary>
    /// Example showing how to work with D3D12-specific descriptor handles.
    /// </summary>
    public static void D3D12SpecificUsage()
    {
        // In D3D12-specific code, you can cast to get the underlying handles
        DescriptorAllocator descriptorAllocator = null!; // Would be obtained from graphics device

        var srvDescriptor = descriptorAllocator.AllocateSRV();

        // For D3D12, you can cast to get the specific handles
        if (srvDescriptor is ShaderResourceDescriptor d3d12Srv)
        {
            var cpuHandle = d3d12Srv.CpuHandle;
            var gpuHandle = d3d12Srv.GpuHandle;

            // Use the handles for D3D12 API calls
            // device.CreateShaderResourceView(resource, &srvDesc, cpuHandle);
            // commandList.SetGraphicsRootDescriptorTable(0, gpuHandle);
        }

        descriptorAllocator.ReleaseSRV(srvDescriptor);
    }

    /// <summary>
    /// Example showing descriptor management in a texture class.
    /// </summary>
    public static void TextureDescriptorUsage()
    {
        // This shows how you might integrate descriptor allocation in a texture class
        DescriptorAllocator descriptorAllocator = null!; // Would be obtained from graphics device

        // Create a texture with SRV
        var texture = new ExampleTexture2D(512, 512, Format.R8G8B8A8Unorm, descriptorAllocator);

        // The texture automatically manages its SRV descriptor
        var srvDescriptor = texture.GetShaderResourceView();

        // Use the texture...

        // Dispose will automatically release the descriptor
        texture.Dispose();
    }

    private static void ProcessDescriptors(
        RenderTargetDescriptor rtv,
        ShaderResourceDescriptor[] srvs,
        DepthStencilDescriptor dsv,
        SamplerDescriptor[] samplers)
    {
        // Process the descriptors - check validity, shader visibility, etc.
        if (rtv.IsValid)
        {
            // Use RTV descriptor
        }

        foreach (var srv in srvs)
        {
            if (srv.IsValid && srv.IsShaderVisible)
            {
                // Use SRV descriptor in shaders
            }
        }

        if (dsv.IsValid)
        {
            // Use DSV descriptor
        }

        foreach (var sampler in samplers)
        {
            if (sampler.IsValid && sampler.IsShaderVisible)
            {
                // Use sampler descriptor in shaders
            }
        }
    }
}

/// <summary>
/// Example texture class showing descriptor integration.
/// </summary>
internal class ExampleTexture2D : IDisposable
{
    private readonly DescriptorAllocator _descriptorAllocator;
    private readonly ShaderResourceDescriptor _srvDescriptor;
    private readonly uint _width;
    private readonly uint _height;
    private readonly Format _format;
    private bool _disposed;

    public ExampleTexture2D(uint width, uint height, Format format, DescriptorAllocator descriptorAllocator)
    {
        _width = width;
        _height = height;
        _format = format;
        _descriptorAllocator = descriptorAllocator;

        // Allocate SRV descriptor for this texture
        _srvDescriptor = _descriptorAllocator.AllocateSRV();

        // Create the actual texture resource and SRV...
        CreateTextureResource();
    }

    public ShaderResourceDescriptor GetShaderResourceView() => _srvDescriptor;

    private void CreateTextureResource()
    {
        // Create D3D12 texture resource
        // Create SRV using the allocated descriptor

        // For D3D12:
        if (_srvDescriptor is ShaderResourceDescriptor d3d12Srv)
        {
            var cpuHandle = d3d12Srv.CpuHandle;
            // device.CreateShaderResourceView(textureResource, &srvDesc, cpuHandle);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Release the descriptor
        _descriptorAllocator.ReleaseSRV(_srvDescriptor);

        // Dispose texture resource...

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}