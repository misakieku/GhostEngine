using Ghost.AssetBaker.Models;
using Ghost.AssetBaker.Views;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Navigation;
using System;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.AssetBaker;

internal class App : Component
{
    static App()
    {
        AppTheme.Register(theme => theme
            .Add("ConsoleBackgroundBrush",
                light: "#0F0F0F",
                dark: "#0F0F0F",
                highContrast: "SystemColorWindowColorBrush")
            .Add("ConsoleForegroundBrush",
                light: "#D4D4D4",
                dark: "#D4D4D4",
                highContrast: "SystemColorWindowTextColorBrush")
            .Add("CodeBlockBackgroundBrush",
                light: "#1A1A1A",
                dark: "#1A1A1A",
                highContrast: "SystemColorWindowColorBrush")
            .Add("CodeBlockForegroundBrush",
                light: "#E6E6E6",
                dark: "#E6E6E6",
                highContrast: "SystemColorWindowTextColorBrush")
        );
    }

    private static string ToTag(Route r) => r.ToString().ToLowerInvariant();
    private static Route ToRoute(string t) => Enum.Parse<Route>(t, ignoreCase: true);

    public override Element Render()
    {
        var window = UseWindow();
        if (window?.NativeWindow == null)
        {
            return FlexColumn(TextBlock("Initializing window...")).Backdrop(BackdropKind.Mica);
        }

        var nav = UseNavigation(Route.Workspace);
        var (globalSettings, setGlobalSettings) = UseState(new BakeSettings());

        var titleBar = TitleBar("Ghost.AssetBaker") with
        {
            Subtitle = "AOT Asset Processing Utility"
        };

        var body = NavigationHost(nav, route => route switch
        {
            Route.Workspace => Component<WorkspaceView, WorkspaceViewProps>(new WorkspaceViewProps(
                WindowContext: window.NativeWindow,
                GlobalSettings: globalSettings
            )),
            Route.Settings => Component<GlobalSettingsView, GlobalSettingsViewProps>(new GlobalSettingsViewProps(
                Settings: globalSettings,
                OnSettingsChanged: setGlobalSettings,
                WindowContext: window.NativeWindow
            )),
            Route.Help => Component<HelpView>(),
            _ => TextBlock("View not found")
        }).Flex(grow: 1, basis: 0)
        with
        {
            Transition = NavigationTransition.Slide(SlideDirection.FromBottom),
        };

        var navigation = NavigationView(
            [
                NavItem("Workspace", icon: "\uE80F", tag: ToTag(Route.Workspace)),
                NavItem("Global Settings", icon: "\uE713", tag: ToTag(Route.Settings)),
                NavItem("Help & Integration", icon: "\uE897", tag: ToTag(Route.Help))
            ],
            body
        ).WithNavigation(nav, ToTag, ToRoute)
        with
        {
            IsSettingsVisible = false,
        };

        return FlexColumn(titleBar, navigation.Flex(grow: 1, basis: 0)
            .PaneDisplayMode(Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.LeftCompact))
            .Backdrop(BackdropKind.Mica);
    }
}
