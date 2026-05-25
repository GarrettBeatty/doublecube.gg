import { createContext } from 'react'
import type { PlayChoice } from '@/components/PlayMenuItems'

export interface PlayActionsContextValue {
  onPlayChoice: (choice: PlayChoice) => void
}

export const PlayActionsContext = createContext<PlayActionsContextValue | undefined>(undefined)
