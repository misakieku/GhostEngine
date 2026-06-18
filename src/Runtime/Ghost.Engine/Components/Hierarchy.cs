using Ghost.Core;
using Ghost.Engine.Editor;
using Ghost.Entities;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Components;

[HideEditor]
public struct Hierarchy : IComponentData
{
    [ReadOnlyInInspector]
    public Entity parent;
    [ReadOnlyInInspector]
    public Entity firstChild;
    [ReadOnlyInInspector]
    public Entity nextSibling;

    public static Hierarchy Root
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new()
        {
            parent = Entity.Invalid,
            firstChild = Entity.Invalid,
            nextSibling = Entity.Invalid
        };
    }
}