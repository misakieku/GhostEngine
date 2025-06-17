using Ghost.Editor.ViewModels.Pages.Landing;
using Ghost.Data.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace Ghost.Editor.View.Pages.Landing;

internal sealed partial class OpenProjectPage : Page
{
    public OpenProjectViewModel ViewModel
    {
        get;
    }

    public OpenProjectPage()
    {
        ViewModel = EditorApplication.GetService<OpenProjectViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.OnNavigatedTo(e.Parameter);
    }

    override protected void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.OnNavigatedFrom();
    }

    private void ProjectContainer_DragEnter(object sender, DragEventArgs e)
    {
        ViewModel.DragVisibility = Visibility.Visible;
        ViewModel.EmptyVisibility = Visibility.Collapsed;
    }

    private void ProjectContainer_DragLeave(object sender, DragEventArgs e)
    {
        ViewModel.DragVisibility = Visibility.Collapsed;
        ViewModel.UpdateEmptyPlaceHolderVisibility();
    }

    private void ProjectContainer_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Link;
        }
        else
        {
            e.AcceptedOperation = DataPackageOperation.None;
        }
    }

    private async void ProjectContainer_Drop(object sender, DragEventArgs e)
    {
        await ViewModel.ContentDrop(e.DataView);
    }

    private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ProjectMetadataInfo project)
        {
            await ViewModel.LoadProject(project);
        }
    }
}