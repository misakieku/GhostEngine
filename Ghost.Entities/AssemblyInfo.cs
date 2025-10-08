global using EntityID = System.Int32;
global using GenerationID = System.UInt16;
global using WorldID = System.UInt16;

using Ghost.Core.Attributes;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ghost.Engine")]
[assembly: InternalsVisibleTo("Ghost.Editor.Core")]
[assembly: InternalsVisibleTo("Ghost.Entities.Test")]

[assembly: EngineAssembly]