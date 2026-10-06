/**
 * Elegant Panel Scaffolding — Theme helper
 * Manages data-theme attribute on <html> and persists preference to localStorage.
 */

(function () {
    const STORAGE_KEY = 'eps-theme';
    const VALID = ['dark', 'light'];

    /** Returns the OS-preferred theme. */
    function getSystemTheme() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches
            ? 'light'
            : 'dark';
    }

    /** Applies the theme to the document root. */
    function applyTheme(theme) {
        if (!VALID.includes(theme)) theme = 'dark';
        document.documentElement.setAttribute('data-theme', theme);
    }

    /** Reads stored theme (or falls back to system preference) and applies it. */
    function initTheme() {
        const stored = localStorage.getItem(STORAGE_KEY);
        const theme = stored && VALID.includes(stored) ? stored : getSystemTheme();
        applyTheme(theme);
        return theme;
    }

    /** Sets and persists a theme. Called from Blazor TitleBar. */
    function setTheme(theme) {
        if (!VALID.includes(theme)) theme = 'dark';
        localStorage.setItem(STORAGE_KEY, theme);
        applyTheme(theme);
    }

    /** Returns the currently active theme from the DOM. */
    function getCurrentTheme() {
        return document.documentElement.getAttribute('data-theme') || 'dark';
    }

    // Expose as window globals so Blazor JS interop can call them.
    window.__initTheme   = initTheme;
    window.__setTheme    = setTheme;
    window.__getTheme    = getCurrentTheme;
    window.__systemTheme = getSystemTheme;
})();
