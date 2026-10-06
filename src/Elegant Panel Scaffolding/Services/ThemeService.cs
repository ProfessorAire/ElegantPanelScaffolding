using System;

namespace EPS.Services
{
    public interface IThemeService
    {
        /// <summary>Current theme: "dark" or "light".</summary>
        string CurrentTheme { get; }
        event Action ThemeChanged;
        void SetTheme(string theme);
        void Toggle();
    }

    public class ThemeService : IThemeService
    {
        private string _theme = "dark";

        public string CurrentTheme => _theme;

        public event Action? ThemeChanged;

        public void SetTheme(string theme)
        {
            if (theme != "dark" && theme != "light")
                theme = "dark";

            if (_theme == theme) return;
            _theme = theme;
            ThemeChanged?.Invoke();
        }

        public void Toggle() => SetTheme(_theme == "dark" ? "light" : "dark");
    }
}
