using Ghost.Engine.Editor;
using Ghost.SparseEntities;
using Ghost.SparseEntities.Components;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Components;

[SkipLocalsInit]
[HideEditor]
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