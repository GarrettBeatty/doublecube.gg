import { useState, useEffect, useCallback } from 'react'
import { Loader2, Search, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ThemeCard } from './ThemeCard'
import { themeService } from '@/services/theme.service'
import { useThemeStore } from '@/stores/themeStore'
import type { ThemeColors, BoardTheme } from '@/types/theme.types'

interface ThemeBrowserProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

interface ThemeWithColors {
  themeId: string
  name: string
  description: string
  authorId: string
  authorUsername: string
  usageCount: number
  likeCount: number
  isDefault: boolean
  colors?: ThemeColors
}

export function ThemeBrowser({ open, onOpenChange }: ThemeBrowserProps) {
  const [activeTab, setActiveTab] = useState<'defaults' | 'community' | 'my'>(
    'defaults'
  )
  const [themes, setThemes] = useState<ThemeWithColors[]>([])
  const [loading, setLoading] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const [likedThemes, setLikedThemes] = useState<Set<string>>(new Set())

  const { selectedThemeId, setSelectedTheme, setPreviewColors, clearPreview } =
    useThemeStore()

  const loadThemes = useCallback(async () => {
    setLoading(true)
    try {
      let result: BoardTheme[] = []

      switch (activeTab) {
        case 'defaults':
          result = await themeService.getDefaultThemes()
          break
        case 'community':
          if (searchQuery) {
            result = await themeService.searchThemes(searchQuery)
          } else {
            const response = await themeService.getPublicThemes()
            result = response.themes
          }
          break
        case 'my':
          result = await themeService.getMyThemes()
          break
      }

      // Convert BoardTheme to ThemeWithColors
      const themesWithColors: ThemeWithColors[] = result.map((theme) => ({
        themeId: theme.themeId,
        name: theme.name,
        description: theme.description,
        authorId: theme.authorId,
        authorUsername: theme.authorUsername,
        usageCount: theme.usageCount,
        likeCount: theme.likeCount,
        isDefault: theme.isDefault,
        colors: theme.colors,
      }))

      setThemes(themesWithColors)
    } catch (error) {
      console.error('Failed to load themes:', error)
    } finally {
      setLoading(false)
    }
  }, [activeTab, searchQuery])

  useEffect(() => {
    if (open) {
      loadThemes()
    }
  }, [open, loadThemes])

  const handleSelectTheme = async (themeId: string) => {
    await setSelectedTheme(themeId)
    onOpenChange(false)
  }

  const handleLikeTheme = async (themeId: string) => {
    try {
      if (likedThemes.has(themeId)) {
        await themeService.unlikeTheme(themeId)
        setLikedThemes((prev) => {
          const next = new Set(prev)
          next.delete(themeId)
          return next
        })
      } else {
        await themeService.likeTheme(themeId)
        setLikedThemes((prev) => new Set(prev).add(themeId))
      }
    } catch (error) {
      console.error('Failed to like/unlike theme:', error)
    }
  }

  const handlePreviewTheme = (theme: ThemeWithColors) => {
    if (theme.colors) {
      setPreviewColors(theme.colors)
    }
  }

  const handleCancelPreview = () => {
    clearPreview()
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-w-4xl max-h-[80vh] overflow-hidden flex flex-col"
        onPointerDownOutside={handleCancelPreview}
        onEscapeKeyDown={handleCancelPreview}
      >
        <DialogHeader>
          <DialogTitle>Theme Browser</DialogTitle>
          <DialogDescription>
            Browse and select a theme for your board
          </DialogDescription>
        </DialogHeader>

        <Tabs
          value={activeTab}
          onValueChange={(v) => setActiveTab(v as typeof activeTab)}
          className="flex-1 flex flex-col overflow-hidden"
        >
          <div className="flex items-center justify-between gap-4 mb-4">
            <TabsList>
              <TabsTrigger value="defaults">Default</TabsTrigger>
              <TabsTrigger value="community">Community</TabsTrigger>
              <TabsTrigger value="my">My Themes</TabsTrigger>
            </TabsList>

            {activeTab === 'community' && (
              <div className="relative flex-1 max-w-xs">
                <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search themes..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-8"
                />
              </div>
            )}

            <Button
              variant="outline"
              size="icon"
              onClick={loadThemes}
              disabled={loading}
            >
              <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            </Button>
          </div>

          <div className="flex-1 overflow-y-auto">
            {loading ? (
              <div className="flex items-center justify-center h-48">
                <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
              </div>
            ) : themes.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-48 text-muted-foreground">
                <p>No themes found</p>
                {activeTab === 'my' && (
                  <p className="text-sm mt-2">
                    Create your first theme in the Theme Editor
                  </p>
                )}
              </div>
            ) : (
              <TabsContent value={activeTab} className="mt-0">
                <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                  {themes.map((theme) => (
                    <div
                      key={theme.themeId}
                      onMouseEnter={() => handlePreviewTheme(theme)}
                      onMouseLeave={handleCancelPreview}
                    >
                      <ThemeCard
                        theme={theme}
                        isSelected={selectedThemeId === theme.themeId}
                        isLiked={likedThemes.has(theme.themeId)}
                        onSelect={() => handleSelectTheme(theme.themeId)}
                        onLike={
                          activeTab !== 'defaults'
                            ? () => handleLikeTheme(theme.themeId)
                            : undefined
                        }
                      />
                    </div>
                  ))}
                </div>
              </TabsContent>
            )}
          </div>
        </Tabs>
      </DialogContent>
    </Dialog>
  )
}
