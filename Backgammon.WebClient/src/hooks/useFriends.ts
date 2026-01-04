import { useState, useEffect } from 'react'
import { useSignalR } from '@/contexts/SignalRContext'
import type { Friend } from '../types/home.types'

export const useFriends = () => {
  const { invoke, isConnected, connection } = useSignalR()
  const [friends, setFriends] = useState<Friend[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchFriends = async () => {
      if (!isConnected) {
        setIsLoading(false)
        return
      }

      try {
        setIsLoading(true)
        setError(null)

        // Try to fetch friends from backend
        const friendsList = await invoke<Friend[]>('GetFriends')
        setFriends(friendsList || [])
      } catch (err) {
        console.warn('GetFriends not fully implemented yet:', err)
        setError('Friends list temporarily unavailable')
        // Empty array as fallback
        setFriends([])
      } finally {
        setIsLoading(false)
      }
    }

    fetchFriends()

    // Listen for friend status updates (if backend supports it)
    const handleFriendStatusChange = (userId: string, status: string) => {
      setFriends((prev) =>
        prev.map((friend) =>
          friend.userId === userId
            ? { ...friend, status: status as Friend['status'] }
            : friend
        )
      )
    }

    connection?.on('FriendStatusChanged', handleFriendStatusChange)

    return () => {
      connection?.off('FriendStatusChanged', handleFriendStatusChange)
    }
  }, [isConnected, invoke, connection])

  return { friends, isLoading, error }
}
