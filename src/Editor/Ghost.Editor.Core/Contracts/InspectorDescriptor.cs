using Ghost.Entities;
using Microsoft.UI.Xaml;
using System;

namespace Ghost.Editor.Core.Contracts;

/// <summary>
/// Discriminated descriptor: tells the inspector panel *what* to inspect.
/// </summary>
public abstract record InspectorDescriptor;

/// <summary>
/// Inspect an entity — auto-generate UI from archetype components.
/// </summary>
public sealed record EntityInspectorDescriptor(World World, Entity Entity) : InspectorDescriptor;

/// <summary>
/// Custom inspector — for assets, settings, or other non-entity inspectables.
/// </summary>
public sealed record CustomInspectorDescriptor(Func<UIElement> Factory) : InspectorDescriptor;
