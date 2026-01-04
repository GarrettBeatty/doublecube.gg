/**
 * Audio Service
 * Manages sound effects for the Backgammon game
 */

type SoundName =
  | 'dice-roll'
  | 'checker-move'
  | 'checker-hit'
  | 'bear-off'
  | 'double-offer'
  | 'game-won'
  | 'game-lost'
  | 'chat-message'
  | 'turn-change'

interface AudioSettings {
  enabled: boolean
  volume: number
}

class AudioService {
  private initialized = false
  private audioEnabled = true
  private masterVolume = 0.7
  private soundCache: Map<SoundName, HTMLAudioElement> = new Map()
  private audioContext: AudioContext | null = null

  // Sound file definitions
  private readonly SOUNDS: Record<SoundName, string> = {
    'dice-roll': '/sounds/dice-roll.mp3',
    'checker-move': '/sounds/checker-move.mp3',
    'checker-hit': '/sounds/checker-hit.mp3',
    'bear-off': '/sounds/bear-off.mp3',
    'double-offer': '/sounds/double-offer.mp3',
    'game-won': '/sounds/game-won.mp3',
    'game-lost': '/sounds/game-lost.mp3',
    'chat-message': '/sounds/chat-message.mp3',
    'turn-change': '/sounds/turn-change.mp3',
  }

  private readonly STORAGE_KEYS = {
    enabled: 'backgammon_audio_enabled',
    volume: 'backgammon_audio_volume',
  }

  /**
   * Load settings from localStorage
   */
  private loadSettings(): void {
    try {
      const enabledStr = localStorage.getItem(this.STORAGE_KEYS.enabled)
      const volumeStr = localStorage.getItem(this.STORAGE_KEYS.volume)

      // Default to enabled if not set
      this.audioEnabled = enabledStr === null ? true : enabledStr === 'true'

      // Default to 0.7 if not set
      this.masterVolume = volumeStr ? parseFloat(volumeStr) : 0.7

      console.log('[AudioService] Settings loaded:', {
        audioEnabled: this.audioEnabled,
        masterVolume: this.masterVolume,
      })
    } catch (err) {
      console.error('[AudioService] Failed to load settings:', err)
    }
  }

  /**
   * Save settings to localStorage
   */
  private saveSettings(): void {
    try {
      localStorage.setItem(this.STORAGE_KEYS.enabled, String(this.audioEnabled))
      localStorage.setItem(this.STORAGE_KEYS.volume, String(this.masterVolume))
      console.log('[AudioService] Settings saved:', {
        audioEnabled: this.audioEnabled,
        masterVolume: this.masterVolume,
      })
    } catch (err) {
      console.error('[AudioService] Failed to save settings:', err)
    }
  }

  /**
   * Preload a sound file
   */
  private preloadSound(name: SoundName, url: string): void {
    try {
      const audio = new Audio(url)
      audio.preload = 'auto'
      audio.volume = this.masterVolume
      this.soundCache.set(name, audio)
      console.log(`[AudioService] Preloaded sound: ${name}`)
    } catch (err) {
      console.error(`[AudioService] Failed to preload sound ${name}:`, err)
    }
  }

  /**
   * Initialize the audio service
   * Loads settings and preloads sounds
   */
  init(): void {
    if (this.initialized) {
      console.log('[AudioService] Already initialized')
      return
    }

    console.log('[AudioService] Initializing...')

    // Load settings from localStorage
    this.loadSettings()

    // Preload all sounds
    Object.entries(this.SOUNDS).forEach(([name, url]) => {
      this.preloadSound(name as SoundName, url)
    })

    // Handle browser autoplay policy
    // Create audio context on first user interaction
    const initAudioContext = () => {
      if (!this.audioContext) {
        try {
          this.audioContext = new (window.AudioContext ||
            (window as any).webkitAudioContext)()
          console.log('[AudioService] Audio context created')
        } catch (err) {
          console.error('[AudioService] Failed to create audio context:', err)
        }
      }
      document.removeEventListener('click', initAudioContext)
    }

    document.addEventListener('click', initAudioContext, { once: true })

    this.initialized = true
    console.log('[AudioService] Initialization complete')
  }

  /**
   * Play a sound effect
   */
  playSound(soundName: SoundName): void {
    // Check if audio is enabled
    if (!this.audioEnabled) {
      return
    }

    // Check if sound exists
    if (!this.SOUNDS[soundName]) {
      console.warn(`[AudioService] Unknown sound: ${soundName}`)
      return
    }

    try {
      let audio = this.soundCache.get(soundName)

      // If sound not in cache, create it
      if (!audio) {
        audio = new Audio(this.SOUNDS[soundName])
        this.soundCache.set(soundName, audio)
      }

      // Stop previous instance if still playing (prevents overlap)
      if (audio.currentTime > 0 && !audio.paused) {
        audio.pause()
        audio.currentTime = 0
      }

      // Set volume and play
      audio.volume = this.masterVolume
      const playPromise = audio.play()

      // Handle autoplay errors gracefully
      if (playPromise !== undefined) {
        playPromise.catch((err) => {
          // Only log if it's not an autoplay error
          if (err.name !== 'NotAllowedError') {
            console.error(`[AudioService] Failed to play sound ${soundName}:`, err)
          }
        })
      }
    } catch (err) {
      console.error(`[AudioService] Error playing sound ${soundName}:`, err)
    }
  }

  /**
   * Enable or disable audio
   */
  setEnabled(enabled: boolean): void {
    this.audioEnabled = enabled
    this.saveSettings()
    console.log(`[AudioService] Audio ${this.audioEnabled ? 'enabled' : 'disabled'}`)
  }

  /**
   * Set master volume
   */
  setVolume(volume: number): void {
    // Clamp volume between 0 and 1
    this.masterVolume = Math.max(0, Math.min(1, volume))

    // Update volume for all cached sounds
    this.soundCache.forEach((audio) => {
      audio.volume = this.masterVolume
    })

    this.saveSettings()
    console.log(`[AudioService] Volume set to ${Math.round(this.masterVolume * 100)}%`)
  }

  /**
   * Get current enabled state
   */
  isEnabled(): boolean {
    return this.audioEnabled
  }

  /**
   * Get current volume level
   */
  getVolume(): number {
    return this.masterVolume
  }

  /**
   * Get current settings
   */
  getSettings(): AudioSettings {
    return {
      enabled: this.audioEnabled,
      volume: this.masterVolume,
    }
  }
}

export const audioService = new AudioService()
