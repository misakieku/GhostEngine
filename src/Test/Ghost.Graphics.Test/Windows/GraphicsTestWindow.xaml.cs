using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Systems;
using Ghost.Engine.Utilities;
using Ghost.Entities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.Test.Windows;

public sealed partial class GraphicsTestWindow : Window
{
    private RenderSystem? _renderSystem;
    private ISwapChain? _swapChain;
    private World? _world;

    private bool _isFirstActivationHandled;

    public unsafe GraphicsTestWindow()
    {
        InitializeComponent();

        Activated += GraphicsTestWindow_Activated;
        Closed += GraphicsTestWindow_Closed;

        Panel.SizeChanged += SwapChainPanel_SizeChanged;
        Panel.CompositionScaleChanged += SwapChainPanel_CompositionScaleChanged;
    }

    private void GraphicsTestWindow_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (_isFirstActivationHandled)
        {
            return;
        }

        _renderSystem = new RenderSystem(new RenderSystemDesc()
        {
            FrameBufferCount = 2,
            GraphicsAPI = GraphicsAPI.Direct3D12
        });

        _swapChain = _renderSystem.GraphicsEngine.CreateSwapChain(new SwapChainDesc
        {
            Width = (uint)AppWindow.Size.Width,
            Height = (uint)AppWindow.Size.Height,
            ScaleX = Panel.CompositionScaleX,
            ScaleY = Panel.CompositionScaleY,
            Format = TextureFormat.B8G8R8A8_UNorm,
            Target = SwapChainTarget.FromCompositionSurface(Panel)
        });

        _renderSystem.RenderPipelineSettings = new RenderPasses.TestRenderPipelineSettings();
        _renderSystem.Start();

        // ECS Setup
        _world = World.Create();
        _world.AddService(_renderSystem);

        // Add Systems
        _world.SystemManager.GetSystem<DefaultSystemGroup>().AddSystem<RenderExtractionSystem>();

        _world.SystemManager.InitializeAll(default);

        // Create Camera Entity

        using var scope = AllocationManager.CreateStackScope();
        var camSet = new ComponentSet(scope.AllocationHandle, ComponentTypeID<Camera>.Value, ComponentTypeID<LocalToWorld>.Value);
        var cameraEntity = _world.EntityManager.CreateEntity(camSet);

        _world.EntityManager.SetComponent(cameraEntity, new Camera
        {
            colorTarget = _swapChain.GetCurrentBackBuffer(), // TODO: This should be updated every frame to the current back buffer.
            depthTarget = Handle<Texture>.Invalid,
            nearClipPlane = 0.1f,
            farClipPlane = 1000.0f,
            focalLength = 50.0f,
            sensorSize = new float2(36.0f, 24.0f),
            gateFit = GateFit.Vertical,
            renderingLayerMask = RenderingLayerMask.All,
        });

        _world.EntityManager.SetComponent(cameraEntity, new LocalToWorld
        {
            matrix = float4x4.TRS(new float3(0.0f, 0.0f, -5.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f))
        });

        // var cameraEntity = _world.EntityManager.CreateEntity();
        // _world.EntityManager.AddComponent(cameraEntity, new Camera
        // {
        //     colorTarget = _swapChain.GetCurrentBackBuffer(),
        //     depthTarget = Handle<Texture>.Invalid,
        //     nearClipPlane = 0.1f,
        //     farClipPlane = 1000.0f,
        //     focalLength = 50.0f,
        //     sensorSize = new float2(36.0f, 24.0f),
        //     gateFit = GateFit.Fill,
        //     renderingLayerMask = new RenderingLayerMask(uint.MaxValue),
        // });
        //
        // _world.EntityManager.AddComponent(cameraEntity, new LocalToWorld
        // {
        //     matrix = float4x4.TRS(new float3(0.0f, 0.0f, -5.0f), quaternion.identity, new float3(1.0f, 1.0f, 1.0f))
        // });

        // Create Mesh Entity
        var meshEntity = _world.EntityManager.CreateEntity();

        MeshBuilder.CreateCube(0.75f, default, Allocator.Persistent, out var vertices, out var indices);

        var directCmd = _renderSystem.GraphicsEngine.CreateCommandBuffer(CommandBufferType.Graphics);
        var ctx = new RenderingContext(_renderSystem.GraphicsEngine, _renderSystem.ResourceManager, directCmd);

        directCmd.Begin(_renderSystem.GraphicsEngine.CreateCommandAllocator(CommandBufferType.Graphics));

        var meshHandle = ctx.CreateMesh(vertices, indices, true);

        var meshRefResult = _renderSystem.ResourceManager.GetMeshReference(meshHandle);
        if (meshRefResult.IsSuccess)
        {
            meshRefResult.Value.CookMeshlets();
        }

        ctx.UploadMeshlets(meshHandle);
        ctx.UpdateObjectData(meshHandle, float4x4.identity);

        directCmd.End().ThrowIfFailed();
        _renderSystem.GraphicsEngine.Device.GraphicsQueue.Submit(directCmd);
        _renderSystem.GraphicsEngine.Device.GraphicsQueue.WaitIdle();

        _world.EntityManager.AddComponent(meshEntity, new MeshInstance
        {
            mesh = meshHandle,
            renderingLayerMask = new RenderingLayerMask(uint.MaxValue),
            shadowCastingMode = Engine.ShadowCastingMode.On
        });

        _world.EntityManager.AddComponent(meshEntity, new LocalToWorld
        {
            matrix = float4x4.identity
        });

        CompositionTarget.Rendering += OnRendering;

        e.Handled = true;
        _isFirstActivationHandled = true;
    }

    private void GraphicsTestWindow_Closed(object sender, WindowEventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        _renderSystem?.Stop();

        if (_world != null)
        {
            World.Destroy(_world.ID);
        }

        _swapChain?.Dispose();
        _renderSystem?.Dispose();

        AllocationManager.Dispose();
    }

    private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_renderSystem == null || _swapChain == null)
        {
            return;
        }

        var newWidth = (uint)(Panel.ActualWidth * Panel.CompositionScaleX);
        var newHeight = (uint)(Panel.ActualHeight * Panel.CompositionScaleY);

        if (newWidth < 8 || newHeight < 8)
        {
            return;
        }

        _renderSystem.RequestSwapChainResize(_swapChain, new uint2(newWidth, newHeight));
    }

    private void SwapChainPanel_CompositionScaleChanged(SwapChainPanel sender, object args)
    {
        _swapChain?.SetScale(sender.CompositionScaleX, sender.CompositionScaleY);
    }

    private void OnRendering(object? sender, object e)
    {
        if (_renderSystem == null || _world == null || _swapChain == null)
        {
            return;
        }

        if (_renderSystem.CPUFenceValue < _renderSystem.GPUFenceValue + _renderSystem.MaxFrameLatency)
        {
            // TODO: In a real system, the camera target would be updated correctly.
            // For now, let's just make sure it renders to the correct back buffer.

            _world.SystemManager.UpdateAll(default); // This runs RenderExtractionSystem, extracting data and queueing RenderRequests
            _renderSystem.SignalCPUReady();
        }
    }
}
