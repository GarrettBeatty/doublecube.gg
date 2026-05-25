import React, { useCallback, useState } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import { useToast } from '@/hooks/use-toast'
import { CreateMatchModal } from '@/components/modals/CreateMatchModal'
import { CreateCorrespondenceMatchModal } from '@/components/modals/CreateCorrespondenceMatchModal'
import { FriendsDialog } from '@/components/friends/FriendsDialog'
import { PlayActionsContext } from '@/contexts/PlayActionsContext'
import type { PlayChoice } from '@/components/PlayMenuItems'

export function PlayActionsProvider({ children }: { children: React.ReactNode }) {
  const { isConnected } = useSignalR()
  const { toast } = useToast()

  const [showCreateMatchModal, setShowCreateMatchModal] = useState(false)
  const [matchModalType, setMatchModalType] = useState<'AI' | 'OpenLobby'>('OpenLobby')
  const [showCorrespondenceModal, setShowCorrespondenceModal] = useState(false)
  const [showFriendsDialog, setShowFriendsDialog] = useState(false)

  const requireConnection = useCallback(() => {
    if (!isConnected) {
      toast({
        title: 'Not connected',
        description: 'Please wait for connection to server...',
        variant: 'destructive',
      })
      return false
    }
    return true
  }, [isConnected, toast])

  const onPlayChoice = useCallback(
    (choice: PlayChoice) => {
      switch (choice) {
        case 'quick':
          if (!requireConnection()) return
          setMatchModalType('OpenLobby')
          setShowCreateMatchModal(true)
          break
        case 'computer':
          if (!requireConnection()) return
          setMatchModalType('AI')
          setShowCreateMatchModal(true)
          break
        case 'friend':
          setShowFriendsDialog(true)
          break
        case 'correspondence':
          if (!requireConnection()) return
          setShowCorrespondenceModal(true)
          break
      }
    },
    [requireConnection]
  )

  return (
    <PlayActionsContext.Provider value={{ onPlayChoice }}>
      {children}
      <CreateMatchModal
        isOpen={showCreateMatchModal}
        onClose={() => setShowCreateMatchModal(false)}
        defaultOpponentType={matchModalType}
      />
      <CreateCorrespondenceMatchModal
        isOpen={showCorrespondenceModal}
        onClose={() => setShowCorrespondenceModal(false)}
      />
      <FriendsDialog
        isOpen={showFriendsDialog}
        onClose={() => setShowFriendsDialog(false)}
      />
    </PlayActionsContext.Provider>
  )
}
