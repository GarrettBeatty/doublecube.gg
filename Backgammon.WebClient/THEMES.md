# Backgammon Theme System Documentation

## Overview

The Backgammon theme system provides a flexible, JSON-based approach to customizing the board appearance. It's designed to be generic and portable across different frontend implementations.

## Architecture

### 1. Theme Manager (`themes.js`)
- Handles theme storage, loading, and application
- Provides API for theme management
- Stores themes in localStorage (web) but can be adapted for other storage mechanisms
- Updates CSS variables dynamically

### 2. Theme Configuration Structure

```json
{
    "id": "unique-theme-id",
    "name": "Display Name",
    "description": "Theme description",
    "colors": {
        // Board colors
        "boardBackground": "#5d4e37",
        "boardBorder": "#4a7c4e",
        
        // Point colors
        "pointLight": "#d4b896",
        "pointDark": "#6b5a47",
        
        // Bar and bearoff
        "bar": "#3d3024",
        "bearoff": "#3d3024",
        
        // Checker colors
        "checkerWhite": "#F5F5F5",
        "checkerWhiteStroke": "#BDBDBD",
        "checkerRed": "#D32F2F",
        "checkerRedStroke": "#B71C1C",
        
        // Highlights
        "highlightSource": "rgba(255, 213, 79, 0.6)",
        "highlightSelected": "rgba(76, 175, 80, 0.7)",
        "highlightDest": "rgba(33, 150, 243, 0.6)",
        "highlightCapture": "rgba(244, 67, 54, 0.6)",
        
        // Text
        "textLight": "rgba(255, 255, 255, 0.5)",
        "textDark": "rgba(0, 0, 0, 0.7)",
        
        // Dice
        "diceBackground": "#FFFFFF",
        "diceBorder": "#5C6BC0",
        "diceText": "#5C6BC0",
        "diceUsedBackground": "#E0E0E0",
        "diceUsedBorder": "#9E9E9E",
        "diceUsedText": "#757575",
        
        // Doubling cube
        "cubeBackground": "#3D3D3D",
        "cubeBorder": "#FFD700",
        "cubeText": "#FFD700",
        
        // Board buttons
        "buttonRollBg": "rgba(76, 175, 80, 0.9)",
        "buttonConfirmBg": "rgba(33, 150, 243, 0.9)",
        "buttonUndoBg": "rgba(245, 158, 11, 0.9)",
        "buttonDoubleBg": "rgba(59, 130, 246, 0.9)"
    }
}
```

## Implementation Guide

### Web Implementation (Current)

1. **CSS Variables**: Theme colors are applied as CSS custom properties:
   ```css
   --theme-board-background: #5d4e37;
   --theme-board-border: #4a7c4e;
   /* etc... */
   ```

2. **SVG Board**: The board renderer reads CSS variables dynamically:
   ```javascript
   const COLORS = {
       get boardBackground() { 
           return getComputedStyle(document.documentElement)
               .getPropertyValue('--theme-board-background') || '#5d4e37'; 
       }
       // ... other colors
   };
   ```

3. **Storage**: Themes stored in localStorage with keys:
   - `backgammon-theme`: Current theme ID
   - `backgammon-custom-themes`: User-created themes

### Adapting for Other Frontends

#### React/Vue/Angular
```javascript
// Import theme configuration
import themes from './themes.json';

// Apply theme to component state
const [currentTheme, setCurrentTheme] = useState(themes.classic);

// Use in render
<Board 
    backgroundColor={currentTheme.colors.boardBackground}
    pointLightColor={currentTheme.colors.pointLight}
    // ... other colors
/>
```

#### Native Mobile (React Native, Flutter)
```javascript
// Store theme in app state/preferences
const theme = await AsyncStorage.getItem('backgammon-theme');

// Apply to components
<BoardView
    style={{
        backgroundColor: theme.colors.boardBackground,
        borderColor: theme.colors.boardBorder
    }}
/>
```

#### Unity/Game Engines
```csharp
// Parse theme JSON
ThemeConfig theme = JsonUtility.FromJson<ThemeConfig>(themeJson);

// Apply to materials/sprites
boardMaterial.color = HexToColor(theme.colors.boardBackground);
checkerWhiteMaterial.color = HexToColor(theme.colors.checkerWhite);
```

## API Reference

### ThemeManager Methods

```javascript
// Initialize theme system
ThemeManager.init();

// Apply a theme by ID
ThemeManager.applyTheme('ocean');

// Get all available themes
const themes = ThemeManager.getAllThemes();

// Get current active theme
const current = ThemeManager.getCurrentTheme();

// Save a custom theme
ThemeManager.saveCustomTheme({
    id: 'my-theme',
    name: 'My Custom Theme',
    colors: { /* ... */ }
});

// Export theme as JSON
const json = ThemeManager.exportTheme('my-theme');

// Import theme from JSON
const imported = ThemeManager.importTheme(jsonString);
```

## Default Themes

1. **Classic Wood**: Traditional backgammon appearance
2. **Modern Dark**: Sleek dark theme with high contrast
3. **Ocean Blue**: Calming blue and teal theme
4. **Forest Green**: Natural green theme

## Creating Custom Themes

1. Use the in-app theme editor (floating button in bottom-left)
2. Manually create JSON following the structure above
3. Import existing themes and modify them

## Best Practices

1. **Color Contrast**: Ensure sufficient contrast between:
   - Light and dark points
   - Checkers and board background
   - Highlighted states and normal states

2. **Consistency**: Keep related colors harmonious:
   - Checker stroke colors should complement fill colors
   - Highlight colors should work well together

3. **Accessibility**: Consider colorblind users:
   - Don't rely solely on color to distinguish game elements
   - Maintain good contrast ratios

4. **Performance**: 
   - Cache theme colors in your renderer
   - Avoid re-parsing theme on every frame
   - Use CSS variables for web implementations

## Migration Guide

If migrating from hardcoded colors:

1. Extract all color values to theme configuration
2. Replace hardcoded values with theme lookups
3. Add theme switching UI
4. Test with multiple themes to ensure flexibility

## Future Enhancements

Potential additions to the theme system:
- Board textures/patterns
- Checker shapes/styles
- Animation preferences
- Sound theme associations
- Board layout options (orientation, numbering)