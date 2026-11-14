using Microsoft.UI.Reactor.Core;

namespace Ghost.Studio.Workspace;

/// <summary>
/// Descriptor for a single tab inside a <see cref="StudioTabPane"/>.
/// </summary>
public sealed record StudioTabItem(string Key, string Title, string IconGlyph, Func<Element> ContentFactory);
