global using EntityID = System.Int32;
global using GenerationID = System.Int32;

using Ghost.Core;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ghost.Engine")]
[assembly: InternalsVisibleTo("Ghost.Editor.Core")]
[assembly: InternalsVisibleTo("Ghost.Entities.Test")]
[assembly: InternalsVisibleTo("Ghost.Graphics.Test")]
[assembly: InternalsVisibleTo("Ghost.UnitTest")]

[assembly: EngineAssembly]

