import { memo } from 'react'
import { Heart } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import type { ThemeColors } from '@/types/theme.types'
import { ThemePreviewBoard } from './ThemePreviewBoard'

interface ThemeForCard {
  themeId: string
  name: string
  authorUsername: string
  usageCount: number
  likeCount: number
  isDefault: boolean
  colors?: ThemeColors
}

interface ThemeCardProps {
  theme: ThemeForCard
  isSelected?: boolean
  isLiked?: boolean
  onSelect?: () => void
  onLike?: () => void
}

export const ThemeCard = memo(function ThemeCard({
  theme,
  isSelected = false,
  isLiked = false,
  onSelect,
  onLike,
}: ThemeCardProps) {
  return (
    <Card
      className={`cursor-pointer transition-all hover:ring-2 hover:ring-primary/50 ${
        isSelected ? 'ring-2 ring-primary' : ''
      }`}
      onClick={onSelect}
    >
      <CardContent className="p-3">
        {/* Preview board */}
        <div className="mb-2 rounded overflow-hidden">
          {theme.colors ? (
            <ThemePreviewBoard colors={theme.colors} />
          ) : (
            <div className="h-24 bg-muted flex items-center justify-center text-muted-foreground text-xs">
              Preview
            </div>
          )}
        </div>

        {/* Theme info */}
        <div className="space-y-1">
          <div className="flex items-center justify-between">
            <h4 className="font-medium text-sm truncate">{theme.name}</h4>
            {theme.isDefault && (
              <span className="text-xs bg-primary/10 text-primary px-1.5 py-0.5 rounded">
                Default
              </span>
            )}
          </div>

          <p className="text-xs text-muted-foreground truncate">
            by {theme.authorUsername}
          </p>

          <div className="flex items-center justify-between pt-1">
            <span className="text-xs text-muted-foreground">
              {theme.usageCount} {theme.usageCount === 1 ? 'user' : 'users'}
            </span>

            {onLike && (
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={(e) => {
                  e.stopPropagation()
                  onLike()
                }}
              >
                <Heart
                  className={`h-4 w-4 ${
                    isLiked ? 'fill-red-500 text-red-500' : ''
                  }`}
                />
              </Button>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  )
})
