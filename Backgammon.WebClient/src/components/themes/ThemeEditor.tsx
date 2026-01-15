import { useState, useEffect, useCallback } from 'react'
import { RotateCcw, Save, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { ColorPicker } from '@/components/ui/color-picker'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion'
import { ScrollArea } from '@/components/ui/scroll-area'
import { ThemePreviewBoard } from './ThemePreviewBoard'
import { useThemeStore, DEFAULT_THEME_COLORS } from '@/stores/themeStore'
import { themeService } from '@/services/theme.service'
import { hslToHex, hexToHsl, getAlpha, hexToHsla } from '@/lib/colorUtils'
import type { ThemeColors, BoardTheme } from '@/types/theme.types'

interface ThemeEditorProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  editingTheme?: BoardTheme | null
}

interface ColorGroup {
  name: string
  keys: (keyof ThemeColors)[]
  labels: Record<string, string>
}

const COLOR_GROUPS: ColorGroup[] = [
  {
    name: 'Board',
    keys: ['boardBackground', 'boardBorder', 'bar', 'bearoff'],
    labels: {
      boardBackground: 'Background',
      boardBorder: 'Border',
      bar: 'Bar',
      bearoff: 'Bear-off',
    },
  },
  {
    name: 'Points',
    keys: ['pointLight', 'pointDark'],
    labels: {
      pointLight: 'Light Points',
      pointDark: 'Dark Points',
    },
  },
  {
    name: 'Checkers',
    keys: ['checkerWhite', 'checkerWhiteStroke', 'checkerRed', 'checkerRedStroke'],
    labels: {
      checkerWhite: 'White Fill',
      checkerWhiteStroke: 'White Stroke',
      checkerRed: 'Red Fill',
      checkerRedStroke: 'Red Stroke',
    },
  },
  {
    name: 'Dice',
    keys: ['diceBackground', 'diceDots'],
    labels: {
      diceBackground: 'Background',
      diceDots: 'Dots',
    },
  },
  {
    name: 'Doubling Cube',
    keys: ['doublingCubeBackground', 'doublingCubeStroke', 'doublingCubeText'],
    labels: {
      doublingCubeBackground: 'Background',
      doublingCubeStroke: 'Stroke',
      doublingCubeText: 'Text',
    },
  },
  {
    name: 'Highlights',
    keys: ['highlightSource', 'highlightSelected', 'highlightDest', 'highlightCapture', 'highlightAnalysis'],
    labels: {
      highlightSource: 'Source',
      highlightSelected: 'Selected',
      highlightDest: 'Destination',
      highlightCapture: 'Capture',
      highlightAnalysis: 'Analysis',
    },
  },
  {
    name: 'Text',
    keys: ['textLight', 'textDark'],
    labels: {
      textLight: 'Light Text',
      textDark: 'Dark Text',
    },
  },
]

export function ThemeEditor({ open, onOpenChange, editingTheme }: ThemeEditorProps) {
  const [themeName, setThemeName] = useState('')
  const [themeDescription, setThemeDescription] = useState('')
  const [editedColors, setEditedColors] = useState<Partial<ThemeColors>>({})
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const { setPreviewColors, clearPreview, resolvedColors, refreshTheme } = useThemeStore()

  // Initialize state when dialog opens
  useEffect(() => {
    if (open) {
      if (editingTheme) {
        setThemeName(editingTheme.name)
        setThemeDescription(editingTheme.description)
        setEditedColors(editingTheme.colors || {})
        setPreviewColors(editingTheme.colors || {})
      } else {
        setThemeName('')
        setThemeDescription('')
        setEditedColors({})
        clearPreview()
      }
      setError(null)
    }
  }, [open, editingTheme, setPreviewColors, clearPreview])

  // Cleanup preview when dialog closes
  const handleClose = useCallback(() => {
    clearPreview()
    onOpenChange(false)
  }, [clearPreview, onOpenChange])

  // Get base colors (either from editing theme or defaults)
  const getBaseColors = useCallback((): ThemeColors => {
    if (editingTheme?.colors) {
      return { ...DEFAULT_THEME_COLORS, ...editingTheme.colors }
    }
    return DEFAULT_THEME_COLORS
  }, [editingTheme])

  // Handle color change
  const handleColorChange = useCallback(
    (key: keyof ThemeColors, hexValue: string) => {
      const baseColors = getBaseColors()
      const originalValue = baseColors[key]
      const alpha = getAlpha(originalValue)

      // Convert hex to HSL/HSLA preserving original alpha
      const newValue = alpha < 1 ? hexToHsla(hexValue, alpha) : hexToHsl(hexValue)

      const newColors = { ...editedColors, [key]: newValue }
      setEditedColors(newColors)
      setPreviewColors(newColors)
    },
    [editedColors, getBaseColors, setPreviewColors]
  )

  // Get current color value for a key
  const getColorValue = useCallback(
    (key: keyof ThemeColors): string => {
      const baseColors = getBaseColors()
      const value = editedColors[key] || baseColors[key]
      return hslToHex(value)
    },
    [editedColors, getBaseColors]
  )

  // Reset to defaults
  const handleReset = useCallback(() => {
    setEditedColors({})
    clearPreview()
  }, [clearPreview])

  // Save theme
  const handleSave = async () => {
    if (!themeName.trim()) {
      setError('Please enter a theme name')
      return
    }

    setIsSaving(true)
    setError(null)

    try {
      // Merge edited colors with base colors to get full theme
      const baseColors = getBaseColors()
      const fullColors: ThemeColors = { ...baseColors, ...editedColors }

      if (editingTheme) {
        // Update existing theme
        await themeService.updateTheme(editingTheme.themeId, {
          name: themeName.trim(),
          description: themeDescription.trim(),
          colors: fullColors,
        })
      } else {
        // Create new theme
        await themeService.createTheme({
          name: themeName.trim(),
          description: themeDescription.trim(),
          visibility: 'private',
          colors: fullColors,
        })
      }

      await refreshTheme()
      handleClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save theme')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-hidden flex flex-col">
        <DialogHeader>
          <DialogTitle>
            {editingTheme ? 'Edit Theme' : 'Create New Theme'}
          </DialogTitle>
          <DialogDescription>
            Customize your board colors. Changes preview in real-time.
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 grid grid-cols-1 md:grid-cols-2 gap-6 overflow-hidden">
          {/* Left panel - Color pickers */}
          <div className="flex flex-col gap-4 overflow-hidden">
            {/* Theme name and description */}
            <div className="space-y-3">
              <div>
                <Label htmlFor="theme-name">Theme Name</Label>
                <Input
                  id="theme-name"
                  value={themeName}
                  onChange={(e) => setThemeName(e.target.value)}
                  placeholder="My Custom Theme"
                  className="mt-1"
                />
              </div>
              <div>
                <Label htmlFor="theme-description">Description (optional)</Label>
                <Textarea
                  id="theme-description"
                  value={themeDescription}
                  onChange={(e) => setThemeDescription(e.target.value)}
                  placeholder="A brief description of your theme..."
                  className="mt-1 h-16 resize-none"
                />
              </div>
            </div>

            {/* Color groups */}
            <ScrollArea className="flex-1">
              <Accordion type="multiple" defaultValue={['Board', 'Checkers']} className="pr-4">
                {COLOR_GROUPS.map((group) => (
                  <AccordionItem key={group.name} value={group.name}>
                    <AccordionTrigger className="text-sm font-medium">
                      {group.name}
                    </AccordionTrigger>
                    <AccordionContent>
                      <div className="space-y-2 py-2">
                        {group.keys.map((key) => (
                          <ColorPicker
                            key={key}
                            label={group.labels[key]}
                            value={getColorValue(key)}
                            onChange={(hex) => handleColorChange(key, hex)}
                          />
                        ))}
                      </div>
                    </AccordionContent>
                  </AccordionItem>
                ))}
              </Accordion>
            </ScrollArea>
          </div>

          {/* Right panel - Preview */}
          <div className="flex flex-col gap-4">
            <div className="flex-1 flex flex-col items-center justify-center bg-muted/30 rounded-lg p-4">
              <Label className="mb-4 text-muted-foreground">Live Preview</Label>
              <ThemePreviewBoard
                colors={resolvedColors}
                width={320}
                height={160}
              />
            </div>

            {error && (
              <p className="text-sm text-destructive text-center">{error}</p>
            )}
          </div>
        </div>

        {/* Bottom actions */}
        <div className="flex items-center justify-between pt-4 border-t">
          <Button
            variant="outline"
            onClick={handleReset}
            disabled={isSaving}
          >
            <RotateCcw className="h-4 w-4 mr-2" />
            Reset to Default
          </Button>

          <div className="flex gap-2">
            <Button
              variant="outline"
              onClick={handleClose}
              disabled={isSaving}
            >
              <X className="h-4 w-4 mr-2" />
              Cancel
            </Button>
            <Button onClick={handleSave} disabled={isSaving}>
              <Save className="h-4 w-4 mr-2" />
              {isSaving ? 'Saving...' : 'Save Theme'}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
