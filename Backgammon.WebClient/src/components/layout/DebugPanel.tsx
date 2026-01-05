import React, { useState } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useAuth } from '@/contexts/AuthContext'
import { useGameStore } from '@/stores/gameStore'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

interface DebugPanelProps {
  isVisible: boolean
}

export const DebugPanel: React.FC<DebugPanelProps> = ({ isVisible }) => {
  const { connectionState, connection } = useSignalR()
  const { user, isAuthenticated, getEffectivePlayerId } = useAuth()
  const { currentGameState, myColor, currentGameId } = useGameStore()
  const [autoScroll, setAutoScroll] = useState(true)

  if (!isVisible) return null

  return (
    <div className="fixed bottom-4 right-4 w-96 max-h-96 overflow-hidden z-50">
      <Card className="bg-background/95 backdrop-blur">
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="text-sm">Debug Panel</CardTitle>
            <Badge variant="secondary" className="text-xs">
              {connectionState}
            </Badge>
          </div>
        </CardHeader>
        <CardContent className="space-y-3 text-xs">
          {/* Connection Info */}
          <div>
            <div className="font-semibold text-muted-foreground mb-1">Connection</div>
            <div className="space-y-1 font-mono text-xs">
              <div>State: {connectionState}</div>
              <div>ID: {connection?.connectionId?.substring(0, 8) || 'N/A'}</div>
            </div>
          </div>

          {/* Auth Info */}
          <div>
            <div className="font-semibold text-muted-foreground mb-1">Auth</div>
            <div className="space-y-1 font-mono text-xs">
              <div>Status: {isAuthenticated ? 'Authenticated' : 'Anonymous'}</div>
              <div>User: {user?.username || 'Anonymous'}</div>
              <div>Player ID: {getEffectivePlayerId().substring(0, 12)}...</div>
            </div>
          </div>

          {/* Game Info */}
          <div>
            <div className="font-semibold text-muted-foreground mb-1">Game</div>
            <div className="space-y-1 font-mono text-xs">
              <div>Game ID: {currentGameId?.substring(0, 8) || 'None'}</div>
              <div>My Color: {myColor !== null ? (myColor === 0 ? 'White' : 'Red') : 'None'}</div>
              <div>Turn: {currentGameState?.isYourTurn ? 'Yours' : 'Opponent'}</div>
              <div>Dice: {currentGameState?.dice?.join(', ') || 'None'}</div>
              <div>
                Moves Left: {currentGameState?.remainingMoves?.length || 0}
                {currentGameState?.hasValidMoves === false && currentGameState?.remainingMoves?.length > 0 && ' (No valid moves)'}
              </div>
            </div>
          </div>

          {/* Controls */}
          <div className="pt-2 border-t border-border">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={autoScroll}
                onChange={(e) => setAutoScroll(e.target.checked)}
                className="w-4 h-4"
              />
              <span className="text-xs">Auto-scroll</span>
            </label>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
