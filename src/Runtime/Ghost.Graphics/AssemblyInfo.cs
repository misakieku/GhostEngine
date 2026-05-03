using Ghost.Core.Attributes;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ghost.Engine")]
[assembly: InternalsVisibleTo("Ghost.Editor")]
[assembly: InternalsVisibleTo("Ghost.Editor.Core")]
[assembly: InternalsVisibleTo("Ghost.Graphics.Test")]
[assembly: InternalsVisibleTo("Ghost.UnitTest")]

[assembly: EngineAssembly]