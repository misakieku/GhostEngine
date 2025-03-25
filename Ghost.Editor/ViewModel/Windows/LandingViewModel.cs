using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.Editor.ViewModel.Windows;

internal partial class LandingViewModel : ObservableRecipient
{
    [ObservableProperty]
    public partial int TabIndex
    {
        get;
        set;
    }
}