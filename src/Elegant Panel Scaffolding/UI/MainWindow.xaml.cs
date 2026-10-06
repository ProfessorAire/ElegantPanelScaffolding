using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using EPS.Services;
using Microsoft.AspNetCore.Components.WebView.Wpf;

namespace EPS.UI
{
    public partial class MainWindow : Window
    {
        // DWM window attributes.
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY = 19;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        private IThemeService? _themeService;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref uint attrValue, int attrSize);

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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            if (App.Services.GetService(typeof(IWindowService)) is WindowService ws)
            {
                ws.SetWindow(this);
            }

            _themeService = App.Services.GetService(typeof(IThemeService)) as IThemeService;
            if (_themeService is not null)
            {
                _themeService.ThemeChanged += OnThemeChanged;
                ApplyNativeTitleBarTheme(_themeService.CurrentTheme);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_themeService is not null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            }

            base.OnClosed(e);
        }

        private void OnThemeChanged()
        {
            var theme = _themeService?.CurrentTheme ?? "dark";
            Dispatcher.Invoke(() => ApplyNativeTitleBarTheme(theme));
        }

        private void ApplyNativeTitleBarTheme(string theme)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var dark = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase);

            // Keep native title bar in dark/light mode where supported.
            var immersive = dark ? 1 : 0;
            TrySetDwmInt(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, immersive);
            TrySetDwmInt(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY, immersive);

            // Nord-matching caption/text/border colors.
            var caption = dark ? Color.FromRgb(0x2E, 0x34, 0x40) : Color.FromRgb(0xE5, 0xE9, 0xF0);
            var text = dark ? Color.FromRgb(0xD8, 0xDE, 0xE9) : Color.FromRgb(0x3B, 0x42, 0x52);
            var border = dark ? Color.FromRgb(0x4C, 0x56, 0x6A) : Color.FromRgb(0xC0, 0xC8, 0xD8);

            TrySetDwmColor(hwnd, DWMWA_CAPTION_COLOR, caption);
            TrySetDwmColor(hwnd, DWMWA_TEXT_COLOR, text);
            TrySetDwmColor(hwnd, DWMWA_BORDER_COLOR, border);
        }

        private static void TrySetDwmInt(IntPtr hwnd, int attr, int value)
        {
            _ = DwmSetWindowAttribute(hwnd, attr, ref value, Marshal.SizeOf<int>());
        }

        private static void TrySetDwmColor(IntPtr hwnd, int attr, Color color)
        {
            var colorRef = ToColorRef(color);
            _ = DwmSetWindowAttribute(hwnd, attr, ref colorRef, Marshal.SizeOf<uint>());
        }

        private static uint ToColorRef(Color color)
        {
            // COLORREF format: 0x00BBGGRR
            return (uint)(color.R | (color.G << 8) | (color.B << 16));
        }
    }
}
