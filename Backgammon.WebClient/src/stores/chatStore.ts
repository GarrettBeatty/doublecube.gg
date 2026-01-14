import { create } from 'zustand'
import { ChatMessage } from '@/types/game.types'

interface ChatStore {
  // Chat state
  chatMessages: ChatMessage[]
  showChat: boolean

  // Actions
  addChatMessage: (message: ChatMessage) => void
  clearChatMessages: () => void
  toggleChat: () => void
  setShowChat: (show: boolean) => void
  resetChat: () => void
}

export const useChatStore = create<ChatStore>((set) => ({
  // Initial state
  chatMessages: [],
  showChat: false,

  // Actions
  addChatMessage: (message) =>
    set((state) => ({
      chatMessages: [...state.chatMessages, message],
    })),

  clearChatMessages: () => set({ chatMessages: [] }),

  toggleChat: () => set((state) => ({ showChat: !state.showChat })),

  setShowChat: (show) => set({ showChat: show }),

  resetChat: () =>
    set({
      chatMessages: [],
      showChat: false,
    }),
}))
