using Ghost.Entities;
using Ghost.Entities.Components;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Components;

[SkipLocalsInit]
public struct Hierarchy : IComponentData
{
    public Entity parent;
    public Entity firstChild;
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