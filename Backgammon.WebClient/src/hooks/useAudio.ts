import { useState, useEffect } from 'react'
import { audioService } from '@/services/audio.service'

export const useAudio = () => {
  const [isEnabled, setIsEnabled] = useState(audioService.isEnabled())
  const [volume, setVolume] = useState(audioService.getVolume())

  useEffect(() => {
    // Initialize audio service
    audioService.init()
  }, [])

  const toggleEnabled = () => {
    const newEnabled = !isEnabled
    audioService.setEnabled(newEnabled)
    setIsEnabled(newEnabled)
  }

  const updateVolume = (newVolume: number) => {
    audioService.setVolume(newVolume)
    setVolume(newVolume)
  }

  const playSound = (soundName: Parameters<typeof audioService.playSound>[0]) => {
    audioService.playSound(soundName)
  }

  return {
    isEnabled,
    volume,
    toggleEnabled,
    updateVolume,
    playSound,
  }
}
