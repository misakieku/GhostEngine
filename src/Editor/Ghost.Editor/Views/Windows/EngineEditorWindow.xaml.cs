using Ghost.Editor.ViewModels.Windows;
using Ghost.Editor.Views.Pages;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using WinUIEx;

namespace Ghost.Editor.Views.Windows;

internal sealed partial class EngineEditorWindow : WindowEx
{
    private int _previousSelectedIndex = 0;

    public EngineEditorViewModel ViewModel
    {
        get;
    }

    public EngineEditorWindow()
    {
        ViewModel = App.GetService<EngineEditorViewModel>();

        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/icon.ico"));
        ExtendsContentIntoTitleBar = true;
        Title = "Ghost Engine";

        SetTitleBar(TitleBar);

        ContentFrame.Navigate(typeof(EditPage));
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        if (currentSelectedIndex == _previousSelectedIndex)
        {
            return;
        }

        var pageType = currentSelectedIndex switch
        {
            0 => typeof(EditPage),
            _ => typeof(EditPage),
        };

        var slideNavigationTransitionEffect = currentSelectedIndex - _previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });
        _previousSelectedIndex = currentSelectedIndex;
    }

    private void Undo_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        var undoService = Ghost.Editor.Core.EditorApplication.GetService<Ghost.Editor.Core.Services.IUndoService>();
        undoService.PerformUndo();
        args.Handled = true;
    }

    private void Redo_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        var undoService = Ghost.Editor.Core.EditorApplication.GetService<Ghost.Editor.Core.Services.IUndoService>();
        undoService.PerformRedo();
        args.Handled = true;
    }
}
