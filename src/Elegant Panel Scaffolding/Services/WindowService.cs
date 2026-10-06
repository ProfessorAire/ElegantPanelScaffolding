using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EPS.Services
{
    public interface IWindowService
    {
        bool IsMaximized { get; }
        void Minimize();
        void MaximizeRestore();
        void Close();
        void BeginDrag();
        void SetWindow(Window window);
    }

    public class WindowService : IWindowService
    {
        private Window? _window;

        // WM_NCLBUTTONDOWN with HTCAPTION hands the move gesture to the OS,
        // which gives full Aero Snap / snap-layout / window-menu behavior.
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 2;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        public bool IsMaximized => _window?.WindowState == WindowState.Maximized;

        public void SetWindow(Window window) => _window = window;

        public void Minimize()
        {
            if (_window is not null)
                _window.Dispatcher.Invoke(() => _window.WindowState = WindowState.Minimized);
        }

        public void MaximizeRestore()
        {
            if (_window is not null)
                _window.Dispatcher.Invoke(() =>
                    _window.WindowState = _window.WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized);
        }

        public void Close()
        {
            if (_window is not null)
                _window.Dispatcher.Invoke(() => _window.Close());
        }

        public void BeginDrag()
        {
            if (_window is null) return;
            _window.Dispatcher.Invoke(() =>
            {
                var hwnd = new WindowInteropHelper(_window).Handle;
                ReleaseCapture();
                SendMessage(hwnd, WM_NCLBUTTONDOWN, new IntPtr(HTCAPTION), IntPtr.Zero);
            });
        }
    }
}
