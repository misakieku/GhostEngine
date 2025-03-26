using Ghost.Data.Services;
using Ghost.Editor.View.Pages;
using Ghost.Editor.View.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Ghost.Editor
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        public Window? CurrentWindow
        {
            get => _window;
            set => _window = value;
        }

        public IHost Host
        {
            get;
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();

            Host = Microsoft.Extensions.Hosting.Host.
                CreateDefaultBuilder().
                UseContentRoot(AppContext.BaseDirectory).
                ConfigureServices((context, services) =>
                {
                    services.AddTransient<ProjectService>();

                    HostHelper.SetupPageService(context, services);
                })
                .Build();
        }

        public static Window? GetWindow()
        {
            return (Current as App)?.CurrentWindow;
        }

        public static T GetService<T>() where T : class
        {
            if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
            {
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }

            return service;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            ActivationHandler.Handle(args);

            Host.Start();

            _window = GetService<LandingWindow>();
            _window.Activate();
        }
    }
}