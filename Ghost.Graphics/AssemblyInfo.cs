global using static TerraFX.Interop.DirectX.D3D12;
global using static TerraFX.Interop.DirectX.DirectX;
global using static TerraFX.Interop.DirectX.DXGI;
global using static TerraFX.Interop.Windows.Windows;

using Ghost.Core.Attributes;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ghost.Engine")]
[assembly: InternalsVisibleTo("Ghost.Editor")]
[assembly: InternalsVisibleTo("Ghost.Editor.Core")]
[assembly: InternalsVisibleTo("Ghost.Graphics.Test")]

[assembly: EngineAssembly]