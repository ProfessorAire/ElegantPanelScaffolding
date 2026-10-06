using System;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using EPS.Services;
using Microsoft.AspNetCore.Components.WebView.Wpf;

namespace EPS.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Expose the DI service provider to the BlazorWebView via an Application resource.
            this.Resources.Add("ServiceProvider", App.Services);

            // Register the root Blazor component programmatically.
            // The Razor-generated type is resolved at runtime to avoid C# compile-time
            // dependency on the Razor build output.
            var blazorAppType = Type.GetType("EPS.Blazor.BlazorApp, Elegant Panel Scaffolding")
                ?? typeof(App).Assembly
                    .GetTypes()
                    .First(t => t.FullName == "EPS.Blazor.BlazorApp");
            this.BlazorView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = blazorAppType
            });
        }

        protected override void OnSourceInitialized(System.EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Hook WndProc so the IWindowService can call back into WPF for
            // drag, min/max/close via JS interop messages.
            var source = (HwndSource)PresentationSource.FromVisual(this);
            source?.AddHook(WndProc);

            // Register the window with the WindowService so Blazor can control it.
            if (App.Services.GetService(typeof(IWindowService)) is WindowService ws)
            {
                ws.SetWindow(this);
            }
        }

        private static System.IntPtr WndProc(System.IntPtr hwnd, int msg, System.IntPtr wParam,
            System.IntPtr lParam, ref bool handled)
        {
            return System.IntPtr.Zero;
        }
    }
}
