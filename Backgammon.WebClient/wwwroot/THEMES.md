# Backgammon Theme System Documentation

## Overview

The backgammon theme system provides a generic, portable way to customize the visual appearance of the game board. It's designed to work across different frontend implementations (web, mobile, desktop) by using a standardized JSON structure.

## How It Works

### 1. Theme Structure

Each theme is a JSON object with the following structure:

```json
{
  "name": "Theme Name",
  "description": "Brief description of the theme",
  "colors": {
    // Board colors
    "boardBackground": "#5d4e37",
    "boardBorder": "#4a7c4e", 
    "pointLight": "#D4C4B0",
    "pointDark": "#8B7355",
    "bar": "#3D2E22",
    "bearoff": "#3D2E22",
    
    // Checker colors
    "checkerWhite": "#F5F5F5",
    "checkerWhiteStroke": "#BDBDBD",
    "checkerRed": "#D32F2F",
    "checkerRedStroke": "#B71C1C",
    
    // Highlight colors
    "highlightSource": "rgba(255, 213, 79, 0.5)",
    "highlightSelected": "rgba(76, 175, 80, 0.6)",
    "highlightDest": "rgba(33, 150, 243, 0.5)", 
    "highlightCapture": "rgba(244, 67, 54, 0.5)",
    
    // Text colors
    "textLight": "#FFFFFF",
    "textDark": "#37474F",
    
    // Dice colors
    "diceBackground": "#FFFFFF",
    "diceBorder": "#5C6BC0",
    "diceValue": "#5C6BC0",
    "diceUsedBackground": "#E0E0E0",
    "diceUsedBorder": "#9E9E9E",
    "diceUsedValue": "#757575",
    
    // Doubling cube colors
    "cubeBackground": "#3D3D3D",
    "cubeBorder": "#FFD700",
    "cubeText": "#FFD700",
    
    // UI colors
    "playerCardBackground": "rgba(0, 0, 0, 0.4)",
    "playerCardActive": "rgba(76, 175, 80, 0.2)",
    "playerCardActiveBorder": "rgba(76, 175, 80, 0.8)"
  }
}
```

### 2. CSS Variables (Web Implementation)

In the web implementation, theme colors are mapped to CSS variables:

- `boardBackground` → `--theme-board-background`
- `checkerWhite` → `--theme-checker-white`
- etc.

The naming convention converts camelCase to kebab-case with a `--theme-` prefix.

### 3. Default Themes

The system includes 4 pre-built themes:

1. **Classic Wood** - Traditional backgammon appearance
2. **Modern Dark** - Sleek dark theme with high contrast
3. **Ocean Blue** - Calming blue and teal color scheme
4. **Forest Green** - Natural green palette

## Using the Theme System

### Web Implementation

```javascript
// Apply a theme
ThemeManager.applyTheme(theme);

// Get all available themes
const themes = ThemeManager.getAllThemes();

// Export current theme
const json = ThemeManager.exportTheme(currentTheme);

// Import theme from JSON
const imported = ThemeManager.importTheme(jsonString);
```

### Theme Settings UI

The web implementation includes a built-in settings UI that provides:

1. **Theme Selection** - Browse and apply pre-built themes
2. **Customization** - Create custom themes with color pickers
3. **Import/Export** - Share themes via JSON files

Access the settings by clicking the 🎨 button in the bottom-left corner.

## Porting to Other Frontends

### React/Vue Example

```javascript
// theme-context.js
export function applyTheme(theme) {
  Object.entries(theme.colors).forEach(([key, value]) => {
    // Apply to your styling system
    setColor(key, value);
  });
}
```

### Mobile (React Native) Example

```javascript
// themes.js
export function getThemeStyles(theme) {
  return {
    board: {
      backgroundColor: theme.colors.boardBackground,
      borderColor: theme.colors.boardBorder,
    },
    checkerWhite: {
      backgroundColor: theme.colors.checkerWhite,
      borderColor: theme.colors.checkerWhiteStroke,
    },
    // ... etc
  };
}
```

### Game Engine (Unity/Godot) Example

```csharp
// ThemeManager.cs
public void ApplyTheme(ThemeData theme) {
    boardRenderer.color = HexToColor(theme.colors.boardBackground);
    // ... etc
}
```

## Color Properties Reference

### Board Elements
- `boardBackground` - Main board surface color
- `boardBorder` - Board edge/frame color
- `pointLight` - Light-colored triangular points
- `pointDark` - Dark-colored triangular points
- `bar` - Center bar dividing the board
- `bearoff` - Bear-off area background

### Game Pieces
- `checkerWhite` - White checker fill color
- `checkerWhiteStroke` - White checker border
- `checkerRed` - Red checker fill color  
- `checkerRedStroke` - Red checker border

### Interactive Highlights
- `highlightSource` - Valid checkers that can be moved
- `highlightSelected` - Currently selected checker
- `highlightDest` - Valid destination points
- `highlightCapture` - Destinations where capture occurs

### Dice
- `diceBackground` - Dice face color
- `diceBorder` - Dice edge color
- `diceValue` - Number/dot color
- `diceUsed*` - Used dice appearance

### Doubling Cube
- `cubeBackground` - Cube face color
- `cubeBorder` - Cube edge color
- `cubeText` - Number color

### UI Elements
- `playerCardBackground` - Player info card background
- `playerCardActive` - Active player highlight
- `playerCardActiveBorder` - Active player border

## Best Practices

1. **Color Contrast** - Ensure sufficient contrast between:
   - Checkers and board points
   - Light and dark points
   - Text and backgrounds

2. **Highlight Visibility** - Make interactive highlights clearly visible but not overwhelming

3. **Consistency** - Keep related colors harmonious (e.g., all highlights in similar hue family)

4. **Accessibility** - Consider colorblind users when choosing checker colors

## Theme File Format

Themes can be exported/imported as `.json` files with this structure:

```json
{
  "name": "My Custom Theme",
  "description": "A unique theme design",
  "colors": { ... },
  "version": "1.0",
  "exportDate": "2024-01-01T00:00:00.000Z"
}
```

## Storage

- Web: Themes stored in localStorage
- Mobile: Store in app preferences
- Desktop: Store in user config directory

The storage keys used:
- `backgammon-theme` - Currently selected theme ID
- `backgammon-custom-themes` - User-created themes (JSON)