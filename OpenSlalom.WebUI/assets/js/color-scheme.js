
(() => {
    'use strict';

    const STORAGE_KEY = 'theme';
    const root = document.documentElement;
    const toggle = document.getElementById('theme-toggle');
    const icon = document.getElementById('theme-toggle-icon');
    const colorScheme = window.matchMedia('(prefers-color-scheme: dark)');

    const getPreferredTheme = () => {
        return colorScheme.matches ? 'dark' : 'light';
    };

    const getStoredTheme = () => {
        return localStorage.getItem(STORAGE_KEY);
    };

    const setTheme = (theme) => {
        root.dataset.bsTheme = theme;

        if (icon) {
            icon.className = theme === 'dark'
                ? 'bi bi-sun fs-5'
                : 'bi bi-moon-stars fs-5';
        }

        if (toggle) {
            toggle.setAttribute(
                'aria-label',
                theme === 'dark'
                    ? 'Helles Design aktivieren'
                    : 'Dunkles Design aktivieren'
            );

            toggle.setAttribute('aria-pressed', String(theme === 'dark'));
            toggle.title = theme === 'dark'
                ? 'Helles Design aktivieren'
                : 'Dunkles Design aktivieren';

            toggle.className = `btn btn-sm btn-link ${theme === 'dark' ? 'text-bg-warning' : 'text-bg-dark'
                }`;
        }
    };

    // Apply the saved theme or follow the system preference.
    const storedTheme = getStoredTheme();
    setTheme(storedTheme || getPreferredTheme());

    // Toggle between light and dark mode.
    toggle?.addEventListener('click', () => {
        const currentTheme = root.dataset.bsTheme;
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

        localStorage.setItem(STORAGE_KEY, newTheme);
        setTheme(newTheme);
    });

    // Follow system changes only if the user has not selected a theme.
    colorScheme.addEventListener('change', () => {
        if (!getStoredTheme()) {
            setTheme(getPreferredTheme());
        }
    });
})();
