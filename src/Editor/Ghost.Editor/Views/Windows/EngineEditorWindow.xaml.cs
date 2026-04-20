using WinUIEx;

namespace Ghost.Editor.Views.Windows;

internal sealed partial class EngineEditorWindow : WindowEx
{
    public EngineEditorWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/icon.ico"));
        ExtendsContentIntoTitleBar = true;
        Title = "Ghost Engine";

        SetTitleBar(TitleBar);
    }
}
