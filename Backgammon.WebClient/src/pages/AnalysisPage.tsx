import { useEffect, useState, useRef } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useSignalR } from '@/contexts/SignalRContext'
import { HubMethods } from '@/types/signalr.types'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { BarChart3 } from 'lucide-react'
import { useToast } from '@/hooks/use-toast'
import { useGameStore } from '@/stores/gameStore'
import { BoardSVG } from '@/components/game/BoardSVG'
import { PlayerCard } from '@/components/game/PlayerCard'
import { GameControls } from '@/components/game/GameControls'
import { BoardOverlayControls } from '@/components/game/BoardOverlayControls'
import { DiceSelector } from '@/components/game/DiceSelector'
import { PlayerSwitcher } from '@/components/game/PlayerSwitcher'
import { PositionControls } from '@/components/game/PositionControls'
import { AnalysisModeToggles } from '@/components/game/AnalysisModeToggles'
import { PositionEvaluation as PositionEvaluationComponent } from '@/components/game/PositionEvaluation'
import { BestMovesPanel } from '@/components/game/BestMovesPanel'
import { EvaluatorSelector } from '@/components/game/EvaluatorSelector'
import { CheckerColor, Move } from '@/types/game.types'
import {
  PositionEvaluation,
  BestMovesAnalysis,
} from '@/types/analysis.types'

export const AnalysisPage: React.FC = () => {
  const navigate = useNavigate()
  const { sgf } = useParams<{ sgf?: string }>()
  const { invoke, isConnected } = useSignalR()
  const { toast } = useToast()
  const {
    currentGameState,
    setCurrentGameId,
    resetGame,
    isCustomDiceEnabled,
    currentEvaluation,
    bestMoves,
    isAnalyzing,
    highlightedMoves,
    setCurrentEvaluation,
    setBestMoves,
    setIsAnalyzing,
    setHighlightedMoves,
  } = useGameStore()
  const [isCreating, setIsCreating] = useState(false)
  const [evaluatorType, setEvaluatorType] = useState<'Heuristic' | 'Gnubg'>('Gnubg')
  const hasCreatedGame = useRef(false)
  const lastImportedSgf = useRef<string | null>(null)
  const lastExportedSgf = useRef<string | null>(null)
  const isAnalyzingRef = useRef(false)
  const lastAnalyzedState = useRef<string | null>(null)
  const isExecutingMoves = useRef(false)
  const analysisRequestCounter = useRef(0)

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

  // Import position from URL if present (only when SGF parameter changes and is different)
  useEffect(() => {
    const importFromUrl = async () => {
      if (!currentGameState || !sgf) {
        return
      }

      // Skip import if we're currently executing moves (prevents flashing during move sequences)
      if (isExecutingMoves.current) {
        console.log('[AnalysisPage] Skipping import - executing moves')
        return
      }

      // Decode the SGF from URL
      const decodedSgf = decodeURIComponent(sgf)

      // Only import if this is a different SGF than what we last imported
      if (decodedSgf === lastImportedSgf.current) {
        console.log('[AnalysisPage] Skipping import - already imported this SGF')
        return
      }

      try {
        console.log('[AnalysisPage] Importing position from URL:', decodedSgf)
        lastImportedSgf.current = decodedSgf
        lastExportedSgf.current = decodedSgf // Also track as exported to prevent immediate re-export
        await invoke(HubMethods.ImportPosition, decodedSgf)
      } catch (error) {
        console.error('[AnalysisPage] Failed to import position from URL:', error)
        toast({
          title: 'Error',
          description: 'Failed to load position from URL',
          variant: 'destructive',
        })
        lastImportedSgf.current = null
        lastExportedSgf.current = null
      }
    }

    importFromUrl()
  }, [sgf, currentGameState, invoke, toast])

  // Update URL when position changes (but don't trigger if we just imported)
  useEffect(() => {
    const updateUrl = async () => {
      if (!currentGameState) {
        return
      }

      try {
        // Export current position to SGF
        const exportedSgf = (await invoke(HubMethods.ExportPosition)) as string
        if (exportedSgf) {
          // Skip if we already exported this exact SGF (prevents duplicate exports on GameUpdate)
          if (exportedSgf === lastExportedSgf.current) {
            return
          }

          // Only update URL if the exported SGF is different from current URL
          const currentSgfFromUrl = sgf ? decodeURIComponent(sgf) : null

          if (currentSgfFromUrl !== exportedSgf) {
            console.log('[AnalysisPage] Updating URL with new position')
            const encodedSgf = encodeURIComponent(exportedSgf)
            // Mark this as imported so we don't re-import it
            lastImportedSgf.current = exportedSgf
            lastExportedSgf.current = exportedSgf
            navigate(`/analysis/${encodedSgf}`, { replace: true })
          } else {
            // URL already matches, just update our tracking
            lastExportedSgf.current = exportedSgf
          }
        }
      } catch (error) {
        console.error('[AnalysisPage] Failed to update URL:', error)
      }
    }

    updateUrl()
  }, [currentGameState, invoke, navigate, sgf])

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

  // Execute a sequence of moves
  const handleExecuteMoves = async (moves: Move[]) => {
    console.log('[AnalysisPage] handleExecuteMoves called', {
      movesCount: moves.length,
      moves: moves.map((m) => `${m.from}/${m.to}`).join(' '),
      hasGameState: !!currentGameState,
      gameId: currentGameState?.gameId,
    })

    if (!currentGameState?.gameId) {
      console.error('[AnalysisPage] Cannot execute moves: no game ID', {
        currentGameState,
      })
      toast({
        title: 'Error',
        description: 'Cannot execute moves: game not properly initialized',
        variant: 'destructive',
      })
      return
    }

    // Set flag to prevent URL imports during move execution
    isExecutingMoves.current = true

    try {
      // Execute each move in sequence
      for (let i = 0; i < moves.length; i++) {
        const move = moves[i]
        console.log(`[AnalysisPage] Executing move ${i + 1}/${moves.length}: ${move.from}/${move.to}`)
        await invoke(HubMethods.MakeMove, move.from, move.to)
      }
      console.log('[AnalysisPage] All moves executed successfully')
    } catch (error) {
      console.error('[AnalysisPage] Failed to execute moves:', error)
      toast({
        title: 'Error',
        description: 'Failed to execute moves',
        variant: 'destructive',
      })
    } finally {
      // Clear flag after all moves complete (or error)
      isExecutingMoves.current = false
    }
  }

  // Auto-analyze position whenever game state changes
  useEffect(() => {
    const analyzePosition = async () => {
      if (!currentGameState?.gameId) return
      if (isAnalyzingRef.current) return

      // Create a hash of the current state to detect actual changes
      const stateHash = JSON.stringify({
        board: currentGameState.board,
        whiteCheckersOnBar: currentGameState.whiteCheckersOnBar,
        redCheckersOnBar: currentGameState.redCheckersOnBar,
        whiteBornOff: currentGameState.whiteBornOff,
        redBornOff: currentGameState.redBornOff,
        currentPlayer: currentGameState.currentPlayer,
        dice: currentGameState.dice,
        evaluatorType: evaluatorType,
      })

      // Only analyze if state actually changed
      if (stateHash === lastAnalyzedState.current) return
      lastAnalyzedState.current = stateHash

      // Increment request counter to invalidate previous requests
      analysisRequestCounter.current += 1
      const currentRequestId = analysisRequestCounter.current

      isAnalyzingRef.current = true
      setIsAnalyzing(true)
      try {
        const evaluation = (await invoke(
          HubMethods.AnalyzePosition,
          currentGameState.gameId,
          evaluatorType
        )) as PositionEvaluation | null

        // Only update if this is still the latest request
        if (currentRequestId === analysisRequestCounter.current) {
          setCurrentEvaluation(evaluation)
        }

        // Auto-find best moves if dice are rolled
        // In analysis mode, you control both sides, so we only check for dice
        // Check if dice have actual values (not [0, 0] which is set after ending turn)
        if (
          currentGameState.dice &&
          currentGameState.dice.length > 0 &&
          currentGameState.dice.some((die) => die > 0)
        ) {
          const analysis = (await invoke(
            HubMethods.FindBestMoves,
            currentGameState.gameId,
            evaluatorType
          )) as BestMovesAnalysis | null

          // Only update if this is still the latest request
          if (analysis && currentRequestId === analysisRequestCounter.current) {
            setBestMoves(analysis)
            setCurrentEvaluation(analysis.initialEvaluation)
          }
        } else {
          // Clear best moves when dice are not rolled (e.g., after ending turn)
          // Only update if this is still the latest request
          if (currentRequestId === analysisRequestCounter.current) {
            setBestMoves(null)
          }
        }
      } catch (error) {
        console.error('[AnalysisPage] Failed to analyze position:', error)
      } finally {
        setIsAnalyzing(false)
        isAnalyzingRef.current = false
      }
    }

    analyzePosition()
    // Zustand setters (setCurrentEvaluation, setBestMoves, setIsAnalyzing) are stable and don't need to be in deps
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentGameState, invoke, evaluatorType])

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
        <div className="mb-4">
          <Badge variant="secondary" className="text-lg py-2 px-4">
            <BarChart3 className="h-5 w-5 mr-2" />
            Analysis Mode - Control Both Sides
          </Badge>
        </div>

        <div className="grid grid-cols-1 xl:grid-cols-[280px_1fr_320px] gap-3">
          {/* Left Sidebar - Player Info & Controls */}
          <div className="space-y-3">
            <PlayerCard {...whitePlayer} />

            <PlayerCard {...redPlayer} />

            {/* Analysis Mode Toggles */}
            <AnalysisModeToggles />

            {/* Dice Selector - Only shown when custom dice is enabled */}
            {isCustomDiceEnabled && <DiceSelector />}

            {/* Player Switcher - Always shown in analysis */}
            <PlayerSwitcher currentPlayer={currentGameState.currentPlayer} />

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
          <div className="space-y-3">
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

            {/* Position SGF */}
            <PositionControls />
          </div>

          {/* Right Sidebar - Analysis */}
          <div className="space-y-3">
            {/* Evaluator Selector */}
            <EvaluatorSelector
              value={evaluatorType}
              onChange={setEvaluatorType}
              disabled={isAnalyzing}
            />

            {/* Position Evaluation */}
            <PositionEvaluationComponent
              evaluation={currentEvaluation}
              isAnalyzing={isAnalyzing}
            />

            {/* Best Moves Panel */}
            <BestMovesPanel
              analysis={bestMoves}
              isAnalyzing={isAnalyzing}
              onHighlightMoves={setHighlightedMoves}
              highlightedMoves={highlightedMoves}
              onExecuteMoves={handleExecuteMoves}
            />
          </div>
        </div>
      </div>
    </div>
  )
}
