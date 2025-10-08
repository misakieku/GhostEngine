global using static TerraFX.Interop.Windows.Windows;
global using static TerraFX.Interop.DirectX.DirectX;
global using static TerraFX.Interop.DirectX.D3D12;
global using static TerraFX.Interop.DirectX.DXGI;

using Ghost.Core.Attributes;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

[assembly: InternalsVisibleTo("Ghost.Engine")]
[assembly: InternalsVisibleTo("Ghost.Editor")]
[assembly: InternalsVisibleTo("Ghost.Editor.Core")]
[assembly: InternalsVisibleTo("Ghost.UnitTest")]

[assembly: SupportedOSPlatform("windows10.0.22621.0")]
[assembly: EngineAssembly]