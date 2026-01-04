import { useEffect, useState, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { BarChart3, Home } from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import { useGameStore } from '@/stores/gameStore'
import { BoardSVG } from '@/components/game/BoardSVG'
import { PlayerCard } from '@/components/game/PlayerCard'
import { GameControls } from '@/components/game/GameControls'
import { BoardOverlayControls } from '@/components/game/BoardOverlayControls'
import { DiceSelector } from '@/components/game/DiceSelector'
import { CheckerColor } from '@/types/game.types'

export const AnalysisPage: React.FC = () => {
  const navigate = useNavigate()
  const { invoke, isConnected } = useSignalR()
  const { toast } = useToast()
  const { currentGameState, setCurrentGameId, resetGame } = useGameStore()
  const [isCreating, setIsCreating] = useState(false)
  const hasCreatedGame = useRef(false)

  useEffect(() => {
    // Auto-create an analysis game when the page loads
    const createAnalysisGame = async () => {
      if (!isConnected) {
        return
      }

      if (isCreating || hasCreatedGame.current) {
        return
      }

      setIsCreating(true)
      hasCreatedGame.current = true
      try {
        console.log('[AnalysisPage] Creating analysis game...')
        await invoke(HubMethods.CreateAnalysisGame)
        console.log('[AnalysisPage] Analysis game created')
      } catch (error) {
        console.error('[AnalysisPage] Failed to create analysis game:', error)
        toast({
          title: 'Error',
          description: 'Failed to create analysis game',
          variant: 'destructive',
        })
        setIsCreating(false)
        hasCreatedGame.current = false
      }
    }

    createAnalysisGame()
  }, [isConnected, invoke, toast, isCreating])

  // Update the current game ID when game state arrives
  useEffect(() => {
    if (currentGameState?.gameId) {
      setCurrentGameId(currentGameState.gameId)
    }
  }, [currentGameState, setCurrentGameId])

  // Cleanup when leaving the page
  useEffect(() => {
    return () => {
      console.log('[AnalysisPage] Component unmounting, leaving game and resetting state')
      invoke(HubMethods.LeaveGame).catch((err) => {
        console.error('[AnalysisPage] Failed to leave game on unmount:', err)
      })
      resetGame()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  if (!isConnected || !currentGameState) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Card className="w-96">
          <CardContent className="p-8">
            <div className="text-center space-y-4">
              {!isConnected ? (
                <p className="text-muted-foreground">Connecting to server...</p>
              ) : (
                <>
                  <BarChart3 className="h-12 w-12 mx-auto text-muted-foreground" />
                  <p className="text-muted-foreground">Loading analysis board...</p>
                </>
              )}
              <div className="flex justify-center">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
              </div>
              <Button variant="outline" onClick={() => navigate('/')}>
                Cancel
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  const whitePlayer = {
    playerName: currentGameState.whitePlayerName || 'White',
    username: currentGameState.whiteUsername,
    color: CheckerColor.White,
    isYourTurn: currentGameState.currentPlayer === CheckerColor.White,
    isYou: currentGameState.yourColor === CheckerColor.White,
    pipCount: currentGameState.whitePipCount,
    checkersOnBar: currentGameState.whiteCheckersOnBar,
    bornOff: currentGameState.whiteBornOff,
  }

  const redPlayer = {
    playerName: currentGameState.redPlayerName || 'Red',
    username: currentGameState.redUsername,
    color: CheckerColor.Red,
    isYourTurn: currentGameState.currentPlayer === CheckerColor.Red,
    isYou: currentGameState.yourColor === CheckerColor.Red,
    pipCount: currentGameState.redPipCount,
    checkersOnBar: currentGameState.redCheckersOnBar,
    bornOff: currentGameState.redBornOff,
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-[1920px] mx-auto px-2 py-4">
        {/* Header */}
        <div className="mb-4 flex items-center justify-between">
          <Badge variant="secondary" className="text-lg py-2 px-4">
            <BarChart3 className="h-5 w-5 mr-2" />
            Analysis Mode - Control Both Sides
          </Badge>
          <Button variant="outline" onClick={() => navigate('/')}>
            <Home className="h-4 w-4 mr-2" />
            Back to Home
          </Button>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-[320px_1fr] gap-4">
          {/* Left Sidebar - Player Info & Controls */}
          <div className="space-y-4">
            <PlayerCard {...whitePlayer} />

            <PlayerCard {...redPlayer} />

            {/* Dice Selector - Analysis Mode Only */}
            <DiceSelector />

            {/* Doubling Cube */}
            {currentGameState.doublingCubeValue > 1 && (
              <Card>
                <CardContent className="p-4 text-center">
                  <div className="text-sm text-muted-foreground mb-2">Stakes</div>
                  <div className="text-3xl font-bold text-yellow-500">
                    {currentGameState.doublingCubeValue}x
                  </div>
                </CardContent>
              </Card>
            )}

            <GameControls gameState={currentGameState} isSpectator={false} />
          </div>

          {/* Main Board Area */}
          <div>
            <Card>
              <CardContent className="p-2 relative">
                {/* Board with overlay controls */}
                <div className="relative">
                  <BoardSVG gameState={currentGameState} />
                  <BoardOverlayControls
                    gameState={currentGameState}
                    isSpectator={false}
                    isAnalysisMode={true}
                  />
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </div>
  )
}
