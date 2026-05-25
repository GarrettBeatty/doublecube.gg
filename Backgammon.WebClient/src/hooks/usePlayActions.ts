import { useContext } from 'react'
import { PlayActionsContext } from '@/contexts/PlayActionsContext'

export function usePlayActions() {
  const ctx = useContext(PlayActionsContext)
  if (!ctx) throw new Error('usePlayActions must be used within PlayActionsProvider')
  return ctx
}
