import { useEffect } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { HubEvents } from '@/types/signalr.types'
import { useGameStore } from '@/stores/gameStore'
import { useGameAudio } from './useGameAudio'

/**
 * Hook to handle chat events: ReceiveChatMessage
 */
export function useChatEvents(connection: HubConnection | null) {
  const { addChatMessage } = useGameStore()
  const { playChatMessageSound } = useGameAudio()

  useEffect(() => {
    if (!connection) return

    // ReceiveChatMessage - Chat message from opponent
    const handleReceiveChatMessage = (
      senderName: string,
      message: string,
      senderConnectionId: string
    ) => {
      const isOwn = senderConnectionId === connection.connectionId
      const displayName = isOwn ? 'You' : senderName

      // Play sound for incoming messages (not our own)
      if (!isOwn) {
        playChatMessageSound()
      }

      addChatMessage({
        senderName: displayName,
        message,
        timestamp: new Date(),
        isOwn,
      })
    }

    // Register handler
    connection.on(HubEvents.ReceiveChatMessage, handleReceiveChatMessage)

    return () => {
      connection.off(HubEvents.ReceiveChatMessage)
    }
  }, [connection, addChatMessage, playChatMessageSound])
}
