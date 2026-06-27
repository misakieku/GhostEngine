using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.AssetForge.Core.Models;

namespace Ghost.AssetForge.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    public partial Project? CurrentProject { get; set; }

    [ObservableProperty]
    public partial string CurrentPageName { get; set; } = "Dashboard";

    public bool IsProjectLoaded => CurrentProject != null;

    partial void OnCurrentProjectChanged(Project? value)
    {
        OnPropertyChanged(nameof(IsProjectLoaded));
    }
}
