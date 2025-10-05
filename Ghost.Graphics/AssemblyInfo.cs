using Ghost.Core.Attributes;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

using static TerraFX.Interop.Windows.Windows;

[assembly: InternalsVisibleTo("Ghost.Engine")]
[assembly: InternalsVisibleTo("Ghost.Editor")]
[assembly: InternalsVisibleTo("Ghost.Editor.Core")]
[assembly: InternalsVisibleTo("Ghost.UnitTest")]

[assembly: SupportedOSPlatform("windows10.0.22621.0")]
[assembly: EngineAssembly]