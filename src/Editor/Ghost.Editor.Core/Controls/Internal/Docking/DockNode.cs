using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

public abstract partial class DockNode : ObservableObject
{
    [ObservableProperty]
    private DockGroupNode? _parent;
}
