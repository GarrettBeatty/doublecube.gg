// themes.js - Generic theme management system for backgammon board

const ThemeManager = (function() {
    // Default themes with generic properties that can work across different frontends
    const DEFAULT_THEMES = {
        classic: {
            name: 'Classic Wood',
            description: 'Traditional backgammon board appearance',
            colors: {
                // Board colors
                boardBackground: '#5d4e37',
                boardBorder: '#4a7c4e',
                pointLight: '#D4C4B0',
                pointDark: '#8B7355',
                bar: '#3D2E22',
                bearoff: '#3D2E22',
                
                // Checker colors
                checkerWhite: '#F5F5F5',
                checkerWhiteStroke: '#BDBDBD',
                checkerRed: '#D32F2F',
                checkerRedStroke: '#B71C1C',
                
                // Highlight colors
                highlightSource: 'rgba(255, 213, 79, 0.5)',
                highlightSelected: 'rgba(76, 175, 80, 0.6)',
                highlightDest: 'rgba(33, 150, 243, 0.5)',
                highlightCapture: 'rgba(244, 67, 54, 0.5)',
                
                // Text colors
                textLight: '#FFFFFF',
                textDark: '#37474F',
                
                // Dice colors
                diceBackground: '#FFFFFF',
                diceBorder: '#5C6BC0',
                diceValue: '#5C6BC0',
                diceUsedBackground: '#E0E0E0',
                diceUsedBorder: '#9E9E9E',
                diceUsedValue: '#757575',
                
                // Doubling cube colors
                cubeBackground: '#3D3D3D',
                cubeBorder: '#FFD700',
                cubeText: '#FFD700',
                
                // UI colors
                playerCardBackground: 'rgba(0, 0, 0, 0.4)',
                playerCardActive: 'rgba(76, 175, 80, 0.2)',
                playerCardActiveBorder: 'rgba(76, 175, 80, 0.8)'
            }
        },
        modernDark: {
            name: 'Modern Dark',
            description: 'Sleek dark theme with high contrast',
            colors: {
                boardBackground: '#1a1a1a',
                boardBorder: '#333333',
                pointLight: '#4a4a4a',
                pointDark: '#2d2d2d',
                bar: '#0f0f0f',
                bearoff: '#0f0f0f',
                
                checkerWhite: '#ffffff',
                checkerWhiteStroke: '#cccccc',
                checkerRed: '#ff4444',
                checkerRedStroke: '#cc0000',
                
                highlightSource: 'rgba(255, 235, 59, 0.6)',
                highlightSelected: 'rgba(139, 195, 74, 0.7)',
                highlightDest: 'rgba(66, 165, 245, 0.6)',
                highlightCapture: 'rgba(255, 82, 82, 0.6)',
                
                textLight: '#ffffff',
                textDark: '#000000',
                
                diceBackground: '#2d2d2d',
                diceBorder: '#666666',
                diceValue: '#ffffff',
                diceUsedBackground: '#1a1a1a',
                diceUsedBorder: '#333333',
                diceUsedValue: '#666666',
                
                cubeBackground: '#2d2d2d',
                cubeBorder: '#ffd700',
                cubeText: '#ffd700',
                
                playerCardBackground: 'rgba(255, 255, 255, 0.05)',
                playerCardActive: 'rgba(139, 195, 74, 0.2)',
                playerCardActiveBorder: 'rgba(139, 195, 74, 0.8)'
            }
        },
        ocean: {
            name: 'Ocean Blue',
            description: 'Calming blue and teal color scheme',
            colors: {
                boardBackground: '#1e3a5f',
                boardBorder: '#2e5a8f',
                pointLight: '#5a8fb8',
                pointDark: '#3a6f98',
                bar: '#0e2a4f',
                bearoff: '#0e2a4f',
                
                checkerWhite: '#e8f4f8',
                checkerWhiteStroke: '#b8d4e8',
                checkerRed: '#ff6b6b',
                checkerRedStroke: '#ee5a5a',
                
                highlightSource: 'rgba(255, 193, 7, 0.6)',
                highlightSelected: 'rgba(0, 230, 118, 0.7)',
                highlightDest: 'rgba(0, 176, 255, 0.6)',
                highlightCapture: 'rgba(255, 107, 107, 0.6)',
                
                textLight: '#ffffff',
                textDark: '#1e3a5f',
                
                diceBackground: '#ffffff',
                diceBorder: '#2e5a8f',
                diceValue: '#2e5a8f',
                diceUsedBackground: '#d0e8f8',
                diceUsedBorder: '#5a8fb8',
                diceUsedValue: '#5a8fb8',
                
                cubeBackground: '#2e5a8f',
                cubeBorder: '#ffd700',
                cubeText: '#ffd700',
                
                playerCardBackground: 'rgba(255, 255, 255, 0.1)',
                playerCardActive: 'rgba(0, 230, 118, 0.3)',
                playerCardActiveBorder: 'rgba(0, 230, 118, 0.8)'
            }
        },
        forest: {
            name: 'Forest Green',
            description: 'Natural green palette inspired by nature',
            colors: {
                boardBackground: '#2d4a2b',
                boardBorder: '#4a7c4e',
                pointLight: '#7fb069',
                pointDark: '#5d8a4e',
                bar: '#1d3a1b',
                bearoff: '#1d3a1b',
                
                checkerWhite: '#f4f7f0',
                checkerWhiteStroke: '#c4d7b0',
                checkerRed: '#d64545',
                checkerRedStroke: '#b63535',
                
                highlightSource: 'rgba(255, 213, 79, 0.6)',
                highlightSelected: 'rgba(139, 195, 74, 0.7)',
                highlightDest: 'rgba(102, 187, 106, 0.6)',
                highlightCapture: 'rgba(214, 69, 69, 0.6)',
                
                textLight: '#ffffff',
                textDark: '#2d4a2b',
                
                diceBackground: '#ffffff',
                diceBorder: '#4a7c4e',
                diceValue: '#4a7c4e',
                diceUsedBackground: '#e0f0d0',
                diceUsedBorder: '#7fb069',
                diceUsedValue: '#7fb069',
                
                cubeBackground: '#3d5a3b',
                cubeBorder: '#ffd700',
                cubeText: '#ffd700',
                
                playerCardBackground: 'rgba(255, 255, 255, 0.08)',
                playerCardActive: 'rgba(139, 195, 74, 0.25)',
                playerCardActiveBorder: 'rgba(139, 195, 74, 0.8)'
            }
        }
    };
    
    // Current theme
    let currentTheme = null;
    
    // Storage key
    const STORAGE_KEY = 'backgammon-theme';
    const CUSTOM_THEMES_KEY = 'backgammon-custom-themes';
    
    // CSS variable mapping
    function applyTheme(theme) {
        const root = document.documentElement;
        
        // Apply each color as a CSS variable
        Object.entries(theme.colors).forEach(([key, value]) => {
            const cssVarName = `--theme-${key.replace(/([A-Z])/g, '-$1').toLowerCase()}`;
            root.style.setProperty(cssVarName, value);
        });
        
        // Store theme preference
        currentTheme = theme;
        localStorage.setItem(STORAGE_KEY, theme.id || 'custom');
        
        // Dispatch event for other components
        window.dispatchEvent(new CustomEvent('themeChanged', { detail: theme }));
    }
    
    // Get all available themes
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
            console.error('Error loading custom themes:', e);
            return {};
        }
    }
    
    // Save custom theme
    function saveCustomTheme(id, theme) {
        const customThemes = getCustomThemes();
        customThemes[id] = theme;
        localStorage.setItem(CUSTOM_THEMES_KEY, JSON.stringify(customThemes));
    }
    
    // Delete custom theme
    function deleteCustomTheme(id) {
        const customThemes = getCustomThemes();
        delete customThemes[id];
        localStorage.setItem(CUSTOM_THEMES_KEY, JSON.stringify(customThemes));
    }
    
    // Load theme on startup
    function loadSavedTheme() {
        const savedThemeId = localStorage.getItem(STORAGE_KEY) || 'classic';
        const allThemes = getAllThemes();
        const theme = allThemes[savedThemeId] || DEFAULT_THEMES.classic;
        
        // Add ID if not present
        if (!theme.id) {
            theme.id = savedThemeId;
        }
        
        applyTheme(theme);
    }
    
    // Export theme as JSON
    function exportTheme(theme) {
        const exportData = {
            ...theme,
            version: '1.0',
            exportDate: new Date().toISOString()
        };
        return JSON.stringify(exportData, null, 2);
    }
    
    // Import theme from JSON
    function importTheme(jsonString) {
        try {
            const imported = JSON.parse(jsonString);
            
            // Validate theme structure
            if (!imported.name || !imported.colors || typeof imported.colors !== 'object') {
                throw new Error('Invalid theme format');
            }
            
            // Generate unique ID
            const id = 'custom_' + Date.now();
            const theme = {
                name: imported.name,
                description: imported.description || 'Imported theme',
                colors: imported.colors,
                id: id
            };
            
            saveCustomTheme(id, theme);
            return theme;
        } catch (e) {
            console.error('Error importing theme:', e);
            throw new Error('Invalid theme file');
        }
    }
    
    // Create theme from current colors
    function createThemeFromCurrent(name, description) {
        const colors = {};
        const root = document.documentElement;
        
        // Read all theme CSS variables
        Object.keys(DEFAULT_THEMES.classic.colors).forEach(key => {
            const cssVarName = `--theme-${key.replace(/([A-Z])/g, '-$1').toLowerCase()}`;
            const value = getComputedStyle(root).getPropertyValue(cssVarName).trim();
            if (value) {
                colors[key] = value;
            }
        });
        
        const id = 'custom_' + Date.now();
        const theme = {
            name,
            description,
            colors,
            id
        };
        
        saveCustomTheme(id, theme);
        return theme;
    }
    
    // Public API
    return {
        init: loadSavedTheme,
        applyTheme,
        getAllThemes,
        getDefaultThemes: () => DEFAULT_THEMES,
        getCustomThemes,
        saveCustomTheme,
        deleteCustomTheme,
        getCurrentTheme: () => currentTheme,
        exportTheme,
        importTheme,
        createThemeFromCurrent
    };
})();

// Initialize theme system when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', ThemeManager.init);
} else {
    ThemeManager.init();
}