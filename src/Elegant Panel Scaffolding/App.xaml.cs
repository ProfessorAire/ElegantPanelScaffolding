using System;
using System.Windows;

namespace EPS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StartupUri = new Uri("UI/MainWindow.xaml", UriKind.RelativeOrAbsolute);

        }
    }
}
