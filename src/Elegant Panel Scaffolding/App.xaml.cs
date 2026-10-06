using EPS.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace EPS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            services.AddWpfBlazorWebView();
#if DEBUG
            services.AddBlazorWebViewDeveloperTools();
#endif
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IOptionsService, OptionsService>();
            services.AddSingleton<ICompilerService, CompilerService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IToastService, ToastService>();

            Services = services.BuildServiceProvider();

            var window = new UI.MainWindow();
            window.Show();
        }
    }
}
