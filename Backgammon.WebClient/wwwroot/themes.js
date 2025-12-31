// themes.js - Theme configuration and management system

const ThemeManager = (function() {
    // Default theme configurations - serializable as JSON
    const DEFAULT_THEMES = {
        classic: {
            id: 'classic',
            name: 'Classic Wood',
            description: 'Traditional backgammon board with wood finish',
            colors: {
                // Board
                boardBackground: '#5d4e37',
                boardBorder: '#4a7c4e',
                
                // Points
                pointLight: '#d4b896',
                pointDark: '#6b5a47',
                
                // Bar and bearoff
                bar: '#3d3024',
                bearoff: '#3d3024',
                
                // Checkers
                checkerWhite: '#F5F5F5',
                checkerWhiteStroke: '#BDBDBD',
                checkerRed: '#D32F2F',
                checkerRedStroke: '#B71C1C',
                
                // Highlights
                highlightSource: 'rgba(255, 213, 79, 0.6)',
                highlightSelected: 'rgba(76, 175, 80, 0.7)',
                highlightDest: 'rgba(33, 150, 243, 0.6)',
                highlightCapture: 'rgba(244, 67, 54, 0.6)',
                
                // Text
                textLight: 'rgba(255, 255, 255, 0.5)',
                textDark: 'rgba(0, 0, 0, 0.7)',
                
                // Dice
                diceBackground: '#FFFFFF',
                diceBorder: '#5C6BC0',
                diceText: '#5C6BC0',
                diceUsedBackground: '#E0E0E0',
                diceUsedBorder: '#9E9E9E',
                diceUsedText: '#757575',
                
                // Doubling cube
                cubeBackground: '#3D3D3D',
                cubeBorder: '#FFD700',
                cubeText: '#FFD700',
                
                // Buttons on board
                buttonRollBg: 'rgba(76, 175, 80, 0.9)',
                buttonConfirmBg: 'rgba(33, 150, 243, 0.9)',
                buttonUndoBg: 'rgba(245, 158, 11, 0.9)',
                buttonDoubleBg: 'rgba(59, 130, 246, 0.9)'
            }
        },
        
        modern: {
            id: 'modern',
            name: 'Modern Dark',
            description: 'Sleek dark theme with high contrast',
            colors: {
                boardBackground: '#1a1f2e',
                boardBorder: '#2d3748',
                pointLight: '#4a5568',
                pointDark: '#2d3748',
                bar: '#171923',
                bearoff: '#171923',
                checkerWhite: '#E2E8F0',
                checkerWhiteStroke: '#A0AEC0',
                checkerRed: '#E53E3E',
                checkerRedStroke: '#C53030',
                highlightSource: 'rgba(237, 137, 54, 0.6)',
                highlightSelected: 'rgba(72, 187, 120, 0.7)',
                highlightDest: 'rgba(56, 178, 172, 0.6)',
                highlightCapture: 'rgba(229, 62, 62, 0.6)',
                textLight: 'rgba(255, 255, 255, 0.7)',
                textDark: 'rgba(255, 255, 255, 0.3)',
                diceBackground: '#2D3748',
                diceBorder: '#4A5568',
                diceText: '#E2E8F0',
                diceUsedBackground: '#1A202C',
                diceUsedBorder: '#2D3748',
                diceUsedText: '#4A5568',
                cubeBackground: '#2D3748',
                cubeBorder: '#ED8936',
                cubeText: '#ED8936',
                buttonRollBg: 'rgba(72, 187, 120, 0.9)',
                buttonConfirmBg: 'rgba(56, 178, 172, 0.9)',
                buttonUndoBg: 'rgba(237, 137, 54, 0.9)',
                buttonDoubleBg: 'rgba(66, 153, 225, 0.9)'
            }
        },
        
        ocean: {
            id: 'ocean',
            name: 'Ocean Blue',
            description: 'Calming blue and teal theme',
            colors: {
                boardBackground: '#0f3460',
                boardBorder: '#16537e',
                pointLight: '#2196f3',
                pointDark: '#1565c0',
                bar: '#0a2342',
                bearoff: '#0a2342',
                checkerWhite: '#B3E5FC',
                checkerWhiteStroke: '#81D4FA',
                checkerRed: '#FF5252',
                checkerRedStroke: '#F44336',
                highlightSource: 'rgba(255, 235, 59, 0.6)',
                highlightSelected: 'rgba(129, 212, 250, 0.7)',
                highlightDest: 'rgba(100, 255, 218, 0.6)',
                highlightCapture: 'rgba(255, 82, 82, 0.6)',
                textLight: 'rgba(255, 255, 255, 0.7)',
                textDark: 'rgba(255, 255, 255, 0.4)',
                diceBackground: '#1E88E5',
                diceBorder: '#1565C0',
                diceText: '#FFFFFF',
                diceUsedBackground: '#1565C0',
                diceUsedBorder: '#0D47A1',
                diceUsedText: '#64B5F6',
                cubeBackground: '#1565C0',
                cubeBorder: '#FFD54F',
                cubeText: '#FFD54F',
                buttonRollBg: 'rgba(129, 212, 250, 0.9)',
                buttonConfirmBg: 'rgba(100, 255, 218, 0.9)',
                buttonUndoBg: 'rgba(255, 235, 59, 0.9)',
                buttonDoubleBg: 'rgba(124, 179, 255, 0.9)'
            }
        },
        
        forest: {
            id: 'forest',
            name: 'Forest Green',
            description: 'Natural green theme inspired by nature',
            colors: {
                boardBackground: '#1b4332',
                boardBorder: '#2d6a4f',
                pointLight: '#52b788',
                pointDark: '#40916c',
                bar: '#081c15',
                bearoff: '#081c15',
                checkerWhite: '#d8f3dc',
                checkerWhiteStroke: '#b7e4c7',
                checkerRed: '#e76f51',
                checkerRedStroke: '#e63946',
                highlightSource: 'rgba(255, 193, 7, 0.6)',
                highlightSelected: 'rgba(139, 195, 74, 0.7)',
                highlightDest: 'rgba(129, 199, 132, 0.6)',
                highlightCapture: 'rgba(244, 67, 54, 0.6)',
                textLight: 'rgba(255, 255, 255, 0.7)',
                textDark: 'rgba(255, 255, 255, 0.4)',
                diceBackground: '#52b788',
                diceBorder: '#40916c',
                diceText: '#081c15',
                diceUsedBackground: '#40916c',
                diceUsedBorder: '#2d6a4f',
                diceUsedText: '#1b4332',
                cubeBackground: '#40916c',
                cubeBorder: '#ffc107',
                cubeText: '#ffc107',
                buttonRollBg: 'rgba(139, 195, 74, 0.9)',
                buttonConfirmBg: 'rgba(129, 199, 132, 0.9)',
                buttonUndoBg: 'rgba(255, 193, 7, 0.9)',
                buttonDoubleBg: 'rgba(102, 187, 106, 0.9)'
            }
        }
    };

    // Current theme
    let currentTheme = null;

    // Storage key
    const STORAGE_KEY = 'backgammon-theme';
    const CUSTOM_THEMES_KEY = 'backgammon-custom-themes';

    // Initialize theme system
    function init() {
        // Create style element for CSS variables
        const styleEl = document.createElement('style');
        styleEl.id = 'theme-variables';
        document.head.appendChild(styleEl);

        // Load saved theme or default
        const savedThemeId = localStorage.getItem(STORAGE_KEY) || 'classic';
        applyTheme(savedThemeId);
    }

    // Apply a theme by ID
    function applyTheme(themeId) {
        let theme = DEFAULT_THEMES[themeId];
        
        // Check custom themes if not found in defaults
        if (!theme) {
            const customThemes = getCustomThemes();
            theme = customThemes[themeId];
        }
        
        if (!theme) {
            console.warn(`Theme '${themeId}' not found, using classic`);
            theme = DEFAULT_THEMES.classic;
        }

        currentTheme = theme;
        updateCSSVariables(theme);
        localStorage.setItem(STORAGE_KEY, theme.id);
        
        // Dispatch event for any listeners
        window.dispatchEvent(new CustomEvent('themeChanged', { detail: theme }));
    }

    // Update CSS variables with theme colors
    function updateCSSVariables(theme) {
        const styleEl = document.getElementById('theme-variables');
        if (!styleEl) return;

        const cssVars = Object.entries(theme.colors)
            .map(([key, value]) => {
                // Convert camelCase to kebab-case
                const varName = key.replace(/([A-Z])/g, '-$1').toLowerCase();
                return `    --theme-${varName}: ${value};`;
            })
            .join('\n');

        styleEl.textContent = `:root {\n${cssVars}\n}`;

        // Update board immediately if it exists
        if (window.BoardSVG && window.BoardSVG.applyTheme) {
            window.BoardSVG.applyTheme(theme);
        }
    }

    // Get all available themes (default + custom)
    function getAllThemes() {
        const customThemes = getCustomThemes();
        return { ...DEFAULT_THEMES, ...customThemes };
    }

    // Get custom themes from storage
    function getCustomThemes() {
        try {
            const stored = localStorage.getItem(CUSTOM_THEMES_KEY);
            return stored ? JSON.parse(stored) : {};
        } catch (e) {
            console.error('Failed to load custom themes:', e);
            return {};
        }
    }

    // Save a custom theme
    function saveCustomTheme(theme) {
        if (!theme.id || !theme.name || !theme.colors) {
            throw new Error('Invalid theme structure');
        }

        const customThemes = getCustomThemes();
        customThemes[theme.id] = theme;
        
        try {
            localStorage.setItem(CUSTOM_THEMES_KEY, JSON.stringify(customThemes));
            return true;
        } catch (e) {
            console.error('Failed to save custom theme:', e);
            return false;
        }
    }

    // Delete a custom theme
    function deleteCustomTheme(themeId) {
        // Can't delete default themes
        if (DEFAULT_THEMES[themeId]) {
            return false;
        }

        const customThemes = getCustomThemes();
        delete customThemes[themeId];
        
        try {
            localStorage.setItem(CUSTOM_THEMES_KEY, JSON.stringify(customThemes));
            
            // If deleted theme was active, switch to classic
            if (currentTheme && currentTheme.id === themeId) {
                applyTheme('classic');
            }
            
            return true;
        } catch (e) {
            console.error('Failed to delete custom theme:', e);
            return false;
        }
    }

    // Get current theme
    function getCurrentTheme() {
        return currentTheme;
    }

    // Create a theme from current colors (for theme editor)
    function createThemeFromColors(name, colors) {
        const id = name.toLowerCase().replace(/\s+/g, '-') + '-' + Date.now();
        return {
            id,
            name,
            description: 'Custom theme',
            colors: { ...DEFAULT_THEMES.classic.colors, ...colors }
        };
    }

    // Export theme as JSON
    function exportTheme(themeId) {
        const theme = getAllThemes()[themeId];
        if (!theme) return null;
        
        return JSON.stringify(theme, null, 2);
    }

    // Import theme from JSON
    function importTheme(jsonString) {
        try {
            const theme = JSON.parse(jsonString);
            if (!theme.id || !theme.name || !theme.colors) {
                throw new Error('Invalid theme format');
            }
            
            // Generate new ID to avoid conflicts
            theme.id = theme.id + '-imported-' + Date.now();
            theme.name = theme.name + ' (Imported)';
            
            return saveCustomTheme(theme) ? theme : null;
        } catch (e) {
            console.error('Failed to import theme:', e);
            return null;
        }
    }

    // Public API
    return {
        init,
        applyTheme,
        getAllThemes,
        getCurrentTheme,
        saveCustomTheme,
        deleteCustomTheme,
        createThemeFromColors,
        exportTheme,
        importTheme
    };
})();