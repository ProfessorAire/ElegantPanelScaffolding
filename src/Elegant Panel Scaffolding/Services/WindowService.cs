using System.Windows;

namespace EPS.Services
{
    public interface IWindowService
    {
        bool IsMaximized { get; }
        void Minimize();
        void MaximizeRestore();
        void Close();
        void SetWindow(Window window);
    }

    public class WindowService : IWindowService
    {
        private Window? _window;

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
    }
}
