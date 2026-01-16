import { useEffect } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { HubEvents } from '@/types/signalr.types'
import { useGameStore } from '@/stores/gameStore'
import { useGameAudio } from './useGameAudio'
import type { ChatHistoryDto } from '@/types/generated/Backgammon.Server.Models.SignalR'

/**
 * Hook to handle chat events: ReceiveChatMessage, ReceiveChatHistory
 */
export function useChatEvents(connection: HubConnection | null) {
  const { addChatMessage, clearChatMessages } = useGameStore()
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

    // ReceiveChatHistory - Chat history from previous games in the match
    const handleReceiveChatHistory = (history: ChatHistoryDto) => {
      // Clear existing messages for a clean transition
      clearChatMessages()

      // Add all history messages (no sound for history)
      history.messages.forEach((msg) => {
        addChatMessage({
          senderName: msg.senderName,
          message: msg.message,
          timestamp: new Date(msg.timestamp),
          isOwn: msg.isOwn,
        })
      })
    }

    // Register handlers
    connection.on(HubEvents.ReceiveChatMessage, handleReceiveChatMessage)
    connection.on(HubEvents.ReceiveChatHistory, handleReceiveChatHistory)

    return () => {
      connection.off(HubEvents.ReceiveChatMessage)
      connection.off(HubEvents.ReceiveChatHistory)
    }
  }, [connection, addChatMessage, clearChatMessages, playChatMessageSound])
}
