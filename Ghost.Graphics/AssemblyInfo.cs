global using static TerraFX.Interop.DirectX.D3D12;
global using static TerraFX.Interop.DirectX.DirectX;
global using static TerraFX.Interop.DirectX.DXGI;
global using static TerraFX.Interop.Windows.Windows;

using Ghost.Core.Attributes;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

[assembly: InternalsVisibleTo("Ghost.Engine")]
[assembly: InternalsVisibleTo("Ghost.Editor")]
[assembly: InternalsVisibleTo("Ghost.Editor.Core")]
[assembly: InternalsVisibleTo("Ghost.Graphics.Test")]
[assembly: InternalsVisibleTo("Ghost.Graphics.Test-Winui")]
[assembly: SupportedOSPlatform("windows10.0.19041.0")]


[assembly: EngineAssembly]