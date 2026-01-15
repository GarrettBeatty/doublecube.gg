import { memo, useMemo, useCallback } from 'react'
import type { GameState } from '@/types/generated/Backgammon.Server.Models'
import { GameStatus } from '@/types/generated/Backgammon.Server.Models'
import { CheckerColor } from '@/types/generated/Backgammon.Core'
import { useGameStore } from '@/stores/gameStore'
import { useSignalR } from '@/contexts/SignalRContext'
import {
  UnifiedBoard,
  BoardPosition,
  PointHighlight,
  DiceState,
  ButtonConfig,
  DoublingCubeState,
} from '../index'

interface GameBoardAdapterProps {
  gameState: GameState
  isSpectator?: boolean
  onOfferDouble?: () => void
  suppressButtons?: boolean
}

export const GameBoardAdapter = memo(function GameBoardAdapter({
  gameState,
  isSpectator = false,
  onOfferDouble,
  suppressButtons = false,
}: GameBoardAdapterProps) {
  const { hub } = useSignalR()
  const {
    selectedChecker,
    selectChecker,
    validSources,
    isBoardFlipped,
    isFreeMoveEnabled,
    highlightedMoves,
    doublingCube,
  } = useGameStore()

  // Transform game state to BoardPosition
  const position = useMemo<BoardPosition>(() => {
    return {
      points: gameState.board.map((p) => ({
        position: p.position,
        color: p.color === CheckerColor.White ? 'white' : p.color === CheckerColor.Red ? 'red' : null,
        count: p.count,
      })),
      whiteOnBar: gameState.whiteCheckersOnBar,
      redOnBar: gameState.redCheckersOnBar,
      whiteBornOff: gameState.whiteBornOff,
      redBornOff: gameState.redBornOff,
    }
  }, [gameState])

  // Build highlights from game state
  const highlights = useMemo<PointHighlight[]>(() => {
    const result: PointHighlight[] = []

    // Don't show move highlights for completed games
    if (gameState.status === GameStatus.Completed) {
      return result
    }

    // Don't show highlights in free move mode
    if (gameState.isAnalysisMode && isFreeMoveEnabled) {
      return result
    }

    // Highlight moveable sources (yellow)
    for (const point of validSources) {
      if (selectedChecker?.point !== point) {
        result.push({ point, type: 'source' })
      }
    }

    // Highlight selected point (green)
    if (selectedChecker?.point !== undefined) {
      result.push({ point: selectedChecker.point, type: 'selected' })
    }

    // Highlight valid destinations from selected piece
    if (selectedChecker?.point !== undefined && gameState.validMoves) {
      const movesFromSelected = gameState.validMoves.filter(
        (m) => m.from === selectedChecker.point
      )

      for (const move of movesFromSelected) {
        const destPoint = gameState.board.find((p) => p.position === move.to)
        const isCapture =
          destPoint?.color !== null &&
          destPoint?.color !== gameState.yourColor &&
          destPoint?.count === 1

        if (move.isCombinedMove) {
          result.push({ point: move.to, type: 'combined' })
        } else if (isCapture) {
          result.push({ point: move.to, type: 'capture' })
        } else {
          result.push({ point: move.to, type: 'destination' })
        }
      }
    }

    // Highlight analysis suggested moves
    if (highlightedMoves && highlightedMoves.length > 0) {
      for (const move of highlightedMoves) {
        if (!result.some((h) => h.point === move.from && h.type === 'analysis')) {
          result.push({ point: move.from, type: 'analysis' })
        }
        if (!result.some((h) => h.point === move.to && h.type === 'analysis')) {
          result.push({ point: move.to, type: 'analysis' })
        }
      }
    }

    return result
  }, [
    gameState,
    validSources,
    selectedChecker,
    isFreeMoveEnabled,
    highlightedMoves,
  ])

  // Build dice state
  const diceState = useMemo<DiceState | undefined>(() => {
    if (!gameState.dice || gameState.dice.length === 0 || !gameState.dice.some((d) => d > 0)) {
      return undefined
    }
    return {
      values: gameState.dice,
      remainingMoves: gameState.remainingMoves ?? [],
    }
  }, [gameState.dice, gameState.remainingMoves])

  // Build doubling cube state
  const cubeState = useMemo<DoublingCubeState | undefined>(() => {
    if (isSpectator) return undefined
    return {
      value: doublingCube.value,
      owner:
        doublingCube.owner === CheckerColor.White
          ? 'white'
          : doublingCube.owner === CheckerColor.Red
          ? 'red'
          : 'center',
      isCrawford: gameState.isCrawfordGame,
    }
  }, [doublingCube, gameState.isCrawfordGame, isSpectator])

  // Build buttons based on game state
  const buttons = useMemo<ButtonConfig[]>(() => {
    // No action buttons for spectators, completed games, or during turn navigation
    if (isSpectator || gameState.status === GameStatus.Completed || suppressButtons) return []

    const result: ButtonConfig[] = []
    const isGameInProgress = gameState.status === GameStatus.InProgress
    const isYourTurn = gameState.isYourTurn
    const hasDiceRolled =
      gameState.dice && gameState.dice.length > 0 && gameState.dice.some((d) => d > 0)

    // Opening roll logic
    const isOpeningRoll = gameState.isOpeningRoll || false
    const yourColor = gameState.yourColor
    const youHaveRolled =
      isOpeningRoll &&
      ((yourColor === CheckerColor.White && gameState.whiteOpeningRoll != null) ||
        (yourColor === CheckerColor.Red && gameState.redOpeningRoll != null))

    // Roll button - always hidden in analysis mode (use Set Dice instead)
    const hideRollForCustomDice = gameState.isAnalysisMode
    const canRoll =
      !hideRollForCustomDice &&
      isGameInProgress &&
      (isOpeningRoll
        ? !youHaveRolled || gameState.isOpeningRollTie
        : (gameState.isAnalysisMode || isYourTurn) && !hasDiceRolled)

    // Debug logging for roll button conditions
    console.log('[GameBoardAdapter] Roll button conditions:', {
      isGameInProgress,
      isAnalysisMode: gameState.isAnalysisMode,
      isOpeningRoll,
      hasDiceRolled,
      isYourTurn,
      hideRollForCustomDice,
      status: gameState.status,
      canRoll,
    })

    if (canRoll) {
      result.push({
        type: 'roll',
        label: 'Roll',
        onClick: () => hub?.rollDice(),
        variant: 'default',
      })
    }

    // Undo button
    const isDoubles =
      hasDiceRolled && gameState.dice.length === 2 && gameState.dice[0] === gameState.dice[1]
    const totalMoves = hasDiceRolled ? (isDoubles ? 4 : gameState.dice.length) : 0
    const remainingMovesCount = gameState.remainingMoves?.length ?? 0
    const movesMade = totalMoves > 0 && remainingMovesCount < totalMoves
    const canUndo =
      isGameInProgress &&
      !isOpeningRoll &&
      hasDiceRolled &&
      movesMade &&
      (gameState.isAnalysisMode || isYourTurn)

    if (canUndo) {
      result.push({
        type: 'undo',
        label: 'Undo',
        onClick: () => hub?.undoLastMove(),
        variant: 'default',
      })
    }

    // End turn button
    const hasUsedAllMoves = gameState.remainingMoves && gameState.remainingMoves.length === 0
    const canEndTurn =
      isGameInProgress &&
      !isOpeningRoll &&
      hasDiceRolled &&
      (gameState.isAnalysisMode
        ? hasUsedAllMoves || !gameState.hasValidMoves
        : !gameState.hasValidMoves && isYourTurn)

    if (canEndTurn) {
      result.push({
        type: 'end',
        label: 'End',
        onClick: () => hub?.endTurn(),
        variant: 'primary',
      })
    }

    // Double button
    const canDouble = doublingCube.canDouble && !hasDiceRolled && !isOpeningRoll
    if (canDouble && onOfferDouble) {
      result.push({
        type: 'double',
        label: '2×',
        onClick: onOfferDouble,
        variant: 'warning',
      })
    }

    return result
  }, [
    gameState,
    isSpectator,
    doublingCube.canDouble,
    hub,
    onOfferDouble,
    suppressButtons,
  ])

  // Handle checker selection (for highlighting during drag)
  const handleCheckerSelect = useCallback(
    (point: number) => {
      selectChecker({ point })
    },
    [selectChecker]
  )

  // Handle move attempt
  const handleMoveAttempt = useCallback(
    async (from: number, to: number) => {
      // In free move mode, allow any move
      if (gameState.isAnalysisMode && isFreeMoveEnabled) {
        await hub?.makeMove(from, to)
        selectChecker(null) // Clear selection after move
        return
      }

      // Validate the move is in validMoves
      const validMove = gameState.validMoves?.find(
        (m) => m.from === from && m.to === to
      )

      if (validMove) {
        await hub?.makeMove(from, to)
      }
      selectChecker(null) // Clear selection after move attempt
    },
    [gameState, isFreeMoveEnabled, hub, selectChecker]
  )

  // Get valid destinations for a point
  const getValidDestinations = useCallback(
    (from: number): number[] => {
      if (gameState.isAnalysisMode && isFreeMoveEnabled) {
        // In free move mode, all points are valid destinations
        return Array.from({ length: 26 }, (_, i) => i) // 0-25
      }

      if (!gameState.validMoves) return []

      return gameState.validMoves
        .filter((m) => m.from === from)
        .map((m) => m.to)
    },
    [gameState.validMoves, gameState.isAnalysisMode, isFreeMoveEnabled]
  )

  // Check if a point is draggable
  const isDraggable = useCallback(
    (point: number): boolean => {
      if (isSpectator) return false
      // Never allow moves on completed games
      if (gameState.status === GameStatus.Completed) return false
      if (gameState.isAnalysisMode && isFreeMoveEnabled) {
        // In free move, any piece can be dragged
        const pointData = position.points.find((p) => p.position === point)
        return pointData !== undefined && pointData.count > 0
      }
      return validSources.includes(point)
    },
    [isSpectator, gameState.status, gameState.isAnalysisMode, isFreeMoveEnabled, validSources, position.points]
  )

  // Disable interactions for spectators and completed games
  const isInteractionDisabled = isSpectator || gameState.status === GameStatus.Completed

  return (
    <UnifiedBoard
      position={position}
      display={{
        isFlipped: isBoardFlipped,
        showPointNumbers: true,
        interactionMode: isInteractionDisabled ? 'none' : 'drag',
      }}
      highlights={highlights}
      interaction={{
        onMoveAttempt: handleMoveAttempt,
        getValidDestinations,
        isDraggable,
        onCheckerSelect: handleCheckerSelect,
      }}
      dice={diceState}
      buttons={buttons}
      doublingCube={cubeState}
    />
  )
})
