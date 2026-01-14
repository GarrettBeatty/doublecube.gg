import { useState, useEffect, useRef } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Textarea } from '@/components/ui/textarea'
import { useSignalR } from '@/contexts/SignalRContext'
import { useToast } from '@/hooks/use-toast'
import { useGameStore } from '@/stores/gameStore'

export const PositionControls: React.FC = () => {
  const { hub } = useSignalR()
  const { toast } = useToast()
  const { currentGameState } = useGameStore()
  const [sgfText, setSgfText] = useState('')
  const [isImporting, setIsImporting] = useState(false)
  const currentSgfRef = useRef('')

  // Auto-fetch SGF whenever game state changes
  useEffect(() => {
    const fetchSgf = async () => {
      if (!currentGameState || isImporting) return

      try {
        const sgf = (await hub?.exportPosition()) as string

        // Only update if SGF actually changed
        if (sgf !== currentSgfRef.current) {
          currentSgfRef.current = sgf
          setSgfText(sgf)
        }
      } catch (error) {
        console.error('Failed to export position:', error)
      }
    }

    fetchSgf()
  }, [currentGameState, hub, isImporting])

  const handleBlur = async () => {
    // If text hasn't changed, nothing to do
    if (sgfText.trim() === currentSgfRef.current.trim()) return

    // If empty, reset to current
    if (!sgfText.trim()) {
      setSgfText(currentSgfRef.current)
      return
    }

    // Import the new position
    setIsImporting(true)
    try {
      await hub?.importPosition(sgfText.trim())
      toast({
        title: 'Success',
        description: 'Position imported',
        duration: 2000
      })
    } catch (error) {
      console.error('Import failed:', error)
      toast({
        title: 'Error',
        description: 'Failed to import position. Check SGF format.',
        variant: 'destructive',
      })
      // Reset to current SGF on error
      setSgfText(currentSgfRef.current)
    } finally {
      setIsImporting(false)
    }
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-sm font-medium">
          Position (SGF)
        </CardTitle>
      </CardHeader>
      <CardContent>
        <Textarea
          value={sgfText}
          onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setSgfText(e.target.value)}
          onBlur={handleBlur}
          placeholder="Loading position..."
          rows={6}
          className="font-mono text-xs resize-none"
        />
        <p className="text-xs text-muted-foreground mt-2">
          Copy to export • Paste to import
        </p>
      </CardContent>
    </Card>
  )
}
