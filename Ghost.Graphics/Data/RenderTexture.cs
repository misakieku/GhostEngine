using Ghost.Graphics.D3D12;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.Data;

/// <summary>
/// Defines the type of render texture.
/// </summary>
public enum RenderTextureType
{
    /// <summary>
    /// Render target view - used for color output.
    /// </summary>
    ColorTarget,

    /// <summary>
    /// Depth stencil view - used for depth/stencil testing.
    /// </summary>
    DepthStencil
}

/// <summary>
/// Render texture class that encapsulates GPU resources for rendering.
/// </summary>
public unsafe class RenderTexture : Texture
{
    private readonly RenderTextureType _renderTextureType;
    private readonly RenderTargetDescriptor? _rtvDescriptor;
    private readonly DepthStencilDescriptor? _dsvDescriptor;

    private RenderTexture(uint width, uint height, Format format, RenderTextureType renderTextureType, in TextureHandle handle, BindlessDescriptor bindlessDescriptor, RenderTargetDescriptor? rtvDescriptor, DepthStencilDescriptor? dsvDescriptor)
        : base(width, height, format, in handle, bindlessDescriptor)
    {
        _renderTextureType = renderTextureType;
        _rtvDescriptor = rtvDescriptor;
        _dsvDescriptor = dsvDescriptor;
    }

    /// <summary>
    /// Gets the type of this render texture.
    /// </summary>
    public RenderTextureType RenderTextureType => _renderTextureType;

    /// <summary>
    /// Gets the render target view descriptor. Only valid for color render textures.
    /// </summary>
    internal RenderTargetDescriptor? RenderTargetView => _rtvDescriptor;

    /// <summary>
    /// Gets the depth stencil view descriptor. Only valid for depth render textures.
    /// </summary>
    internal DepthStencilDescriptor? DepthStencilView => _dsvDescriptor;

    /// <summary>
    /// Creates a color render texture.
    /// </summary>
    /// <param name="width">Width of the render texture</param>
    /// <param name="height">Height of the render texture</param>
    /// <param name="format">Color format (e.g., Format.R8G8B8A8Unorm)</param>
    /// <returns>A new color render texture</returns>
    public static RenderTexture CreateColorTarget(uint width, uint height, Format format = Format.R8G8B8A8Unorm, bool tempResource = false)
    {
        ValidateColorFormat(format);

        var handle = GraphicsPipeline.ResourceAllocator.CreateTexture2D(width, height, 1, format, resFlags: ResourceFlags.AllowRenderTarget | ResourceFlags.AllowUnorderedAccess, tempResource: tempResource);

        var resource = handle.ResourceHandle.GetAllocation().Resource;
        var bindlessDescriptor = CreateBindlessDescriptorForRenderTexture(resource, format);
        var rtvDescriptor = CreateRenderTargetView(resource, format);

        return new RenderTexture(width, height, format, RenderTextureType.ColorTarget, in handle, bindlessDescriptor, rtvDescriptor, null);
    }

    /// <summary>
    /// Creates a depth stencil render texture.
    /// </summary>
    /// <param name="width">Width of the render texture</param>
    /// <param name="height">Height of the render texture</param>
    /// <param name="format">Depth format (e.g., Format.D24UnormS8Uint, Format.D32Float)</param>
    /// <returns>A new depth stencil render texture</returns>
    public static RenderTexture CreateDepthStencil(uint width, uint height, Format format = Format.D24UnormS8Uint, bool tempResource = false)
    {
        ValidateDepthFormat(format);

        var handle = GraphicsPipeline.ResourceAllocator.CreateTexture2D(width, height, 1, format, resFlags: ResourceFlags.AllowDepthStencil, tempResource: tempResource);

        var resource = handle.ResourceHandle.GetAllocation().Resource;
        var bindlessDescriptor = CreateBindlessDescriptorForRenderTexture(resource, GetShaderResourceFormat(format));
        var dsvDescriptor = CreateDepthStencilView(resource, format);

        return new RenderTexture(width, height, format, RenderTextureType.DepthStencil, in handle, bindlessDescriptor, null, dsvDescriptor);
    }

    /// <summary>
    /// Validates that the format is suitable for color render targets.
    /// </summary>
    private static void ValidateColorFormat(Format format)
    {
        switch (format)
        {
            case Format.R8G8B8A8Unorm:
            case Format.R8G8B8A8UnormSrgb:
            case Format.B8G8R8A8Unorm:
            case Format.B8G8R8A8UnormSrgb:
            case Format.R16G16B16A16Float:
            case Format.R32G32B32A32Float:
            case Format.R16G16Float:
            case Format.R32Float:
                break;
            default:
                throw new ArgumentException($"Format {format} is not supported for color render targets.");
        }
    }

    /// <summary>
    /// Validates that the format is suitable for depth stencil targets.
    /// </summary>
    private static void ValidateDepthFormat(Format format)
    {
        switch (format)
        {
            case Format.D32Float:
            case Format.D24UnormS8Uint:
            case Format.D16Unorm:
                break;
            default:
                throw new ArgumentException($"Format {format} is not supported for depth stencil targets.");
        }
    }

    /// <summary>
    /// Gets the shader resource format for depth textures (for sampling depth in shaders).
    /// </summary>
    private static Format GetShaderResourceFormat(Format depthFormat)
    {
        return depthFormat switch
        {
            Format.D32Float => Format.R32Float,
            Format.D24UnormS8Uint => Format.R24UnormX8Typeless,
            Format.D16Unorm => Format.R16Unorm,
            _ => throw new ArgumentException($"Cannot determine shader resource format for depth format {depthFormat}")
        };
    }

    /// <summary>
    /// Creates a bindless descriptor for render texture shader resource access.
    /// </summary>
    private static BindlessDescriptor CreateBindlessDescriptorForRenderTexture(ID3D12Resource* resource, Format srvFormat)
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;
        var bindlessDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateBindless();

        var srvDesc = new ShaderResourceViewDescription
        {
            Format = srvFormat,
            ViewDimension = SrvDimension.Texture2D,
            Texture2D = new Texture2DSrv { MipLevels = 1 },
            Shader4ComponentMapping = 0x1688 // D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
        };

        device->CreateShaderResourceView(resource, &srvDesc, bindlessDescriptor.CpuHandle);
        return bindlessDescriptor;
    }

    /// <summary>
    /// Creates a render target view for color render textures.
    /// </summary>
    private static RenderTargetDescriptor CreateRenderTargetView(ID3D12Resource* resource, Format format)
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;
        var rtvDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateRTV();

        var rtvDesc = new RenderTargetViewDescription
        {
            Format = format,
            ViewDimension = RtvDimension.Texture2D,
            Texture2D = new Texture2DRtv { MipSlice = 0 }
        };

        device->CreateRenderTargetView(resource, &rtvDesc, rtvDescriptor.CpuHandle);
        return rtvDescriptor;
    }

    /// <summary>
    /// Creates a depth stencil view for depth render textures.
    /// </summary>
    private static DepthStencilDescriptor CreateDepthStencilView(ID3D12Resource* resource, Format format)
    {
        var device = GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr;
        var dsvDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateDSV();

        var dsvDesc = new DepthStencilViewDescription
        {
            Format = format,
            ViewDimension = DsvDimension.Texture2D,
            Texture2D = new Texture2DDsv { MipSlice = 0 }
        };

        device->CreateDepthStencilView(resource, &dsvDesc, dsvDescriptor.CpuHandle);
        return dsvDescriptor;
    }

    /// <summary>
    /// Clears the render texture with the specified color (for color targets only).
    /// </summary>
    /// <param name="commandList">Command list to record clear commands</param>
    /// <param name="clearColor">Color to clear to</param>
    public void ClearColor(CommandList commandList, Color16 clearColor)
    {
        ThrowIfDisposed();

        if (_renderTextureType != RenderTextureType.ColorTarget || _rtvDescriptor == null)
        {
            throw new InvalidOperationException("ClearColor can only be called on color render textures.");
        }

        commandList.NativeCommandList.Ptr->ClearRenderTargetView(_rtvDescriptor.CpuHandle, (float*)&clearColor, 0, null);
    }

    /// <summary>
    /// Clears the depth stencil render texture.
    /// </summary>
    /// <param name="commandList">Command list to record clear commands</param>
    /// <param name="clearDepth">Depth value to clear to (0.0 to 1.0)</param>
    /// <param name="clearStencil">Stencil value to clear to</param>
    public void ClearDepthStencil(CommandList commandList, ClearFlags flags, float clearDepth = 1.0f, byte clearStencil = 0)
    {
        ThrowIfDisposed();

        if (_renderTextureType != RenderTextureType.DepthStencil || _dsvDescriptor == null)
        {
            throw new InvalidOperationException("ClearDepthStencil can only be called on depth stencil render textures.");
        }

        commandList.NativeCommandList.Ptr->ClearDepthStencilView(_dsvDescriptor.CpuHandle, flags, clearDepth, clearStencil, 0, null);
    }

    /// <summary>
    /// Transitions the render texture to the specified resource state.
    /// </summary>
    /// <param name="commandList">Command list to record transition</param>
    /// <param name="newState">New resource state</param>
    internal void TransitionTo(CommandList commandList, ResourceStates newState)
    {
        ThrowIfDisposed();

        commandList.BarrierTransition(this, ResourceStates.Common, newState);
    }

    /// <summary>
    /// Convenience method to transition to render target state (for color targets).
    /// </summary>
    /// <param name="commandList">Command list to record transition</param>
    internal void TransitionToRenderTarget(CommandList commandList)
    {
        if (_renderTextureType != RenderTextureType.ColorTarget)
        {
            throw new InvalidOperationException("TransitionToRenderTarget can only be called on color render textures.");
        }

        TransitionTo(commandList, ResourceStates.RenderTarget);
    }

    /// <summary>
    /// Convenience method to transition to depth write state (for depth targets).
    /// </summary>
    /// <param name="commandList">Command list to record transition</param>
    internal void TransitionToDepthWrite(CommandList commandList)
    {
        if (_renderTextureType != RenderTextureType.DepthStencil)
        {
            throw new InvalidOperationException("TransitionToDepthWrite can only be called on depth stencil render textures.");
        }

        TransitionTo(commandList, ResourceStates.DepthWrite);
    }

    /// <summary>
    /// Convenience method to transition to shader resource state (for reading in shaders).
    /// </summary>
    /// <param name="commandList">Command list to record transition</param>
    public void TransitionToShaderResource(CommandList commandList)
    {
        TransitionTo(commandList, ResourceStates.PixelShaderResource);
    }

    /// <summary>
    /// Disposes the render texture and releases associated descriptors.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();

        if (_rtvDescriptor != null)
        {
            GraphicsPipeline.DescriptorAllocator.ReleaseRTV(_rtvDescriptor);
        }

        if (_dsvDescriptor != null)
        {
            GraphicsPipeline.DescriptorAllocator.ReleaseDSV(_dsvDescriptor);
        }
    }
}