/**
 * Audio Manager Module
 * Manages sound effects for the Backgammon game
 * Uses IIFE pattern for encapsulation
 */
const AudioManager = (function() {
    // Private state
    let initialized = false;
    let audioEnabled = true;
    let masterVolume = 0.7;
    let soundCache = {};
    let audioContext = null;

    // Sound file definitions
    const SOUNDS = {
        'dice-roll': '/sounds/dice-roll.mp3',
        'checker-move': '/sounds/checker-move.mp3',
        'checker-hit': '/sounds/checker-hit.mp3',
        'bear-off': '/sounds/bear-off.mp3',
        'double-offer': '/sounds/double-offer.mp3',
        'game-won': '/sounds/game-won.mp3',
        'game-lost': '/sounds/game-lost.mp3',
        'chat-message': '/sounds/chat-message.mp3',
        'turn-change': '/sounds/turn-change.mp3'
    };

    /**
     * Load settings from localStorage
     */
    function loadSettings() {
        try {
            const enabled = localStorage.getItem('backgammon_audio_enabled');
            const volume = localStorage.getItem('backgammon_audio_volume');

            // Default to enabled if not set
            audioEnabled = enabled === null ? true : enabled === 'true';

            // Default to 0.7 if not set
            masterVolume = volume ? parseFloat(volume) : 0.7;

            console.log('[AudioManager] Settings loaded:', { audioEnabled, masterVolume });
        } catch (err) {
            console.error('[AudioManager] Failed to load settings:', err);
        }
    }

    /**
     * Save settings to localStorage
     */
    function saveSettings() {
        try {
            localStorage.setItem('backgammon_audio_enabled', audioEnabled.toString());
            localStorage.setItem('backgammon_audio_volume', masterVolume.toString());
            console.log('[AudioManager] Settings saved:', { audioEnabled, masterVolume });
        } catch (err) {
            console.error('[AudioManager] Failed to save settings:', err);
        }
    }

    /**
     * Update UI elements to reflect current settings
     */
    function updateSettingsUI() {
        const audioEnabledToggle = document.getElementById('audioEnabled');
        if (audioEnabledToggle) {
            audioEnabledToggle.checked = audioEnabled;
        }

        const volumeSlider = document.getElementById('volumeSlider');
        if (volumeSlider) {
            volumeSlider.value = Math.round(masterVolume * 100);
        }

        const volumeDisplay = document.getElementById('volumeDisplay');
        if (volumeDisplay) {
            volumeDisplay.textContent = `${Math.round(masterVolume * 100)}%`;
        }
    }

    /**
     * Preload a sound file
     * @param {string} name - Sound name
     * @param {string} url - Sound file URL
     */
    function preloadSound(name, url) {
        try {
            const audio = new Audio(url);
            audio.preload = 'auto';
            audio.volume = masterVolume;
            soundCache[name] = audio;
            console.log(`[AudioManager] Preloaded sound: ${name}`);
        } catch (err) {
            console.error(`[AudioManager] Failed to preload sound ${name}:`, err);
        }
    }

    /**
     * Initialize the audio manager
     * Loads settings and preloads sounds
     */
    function init() {
        if (initialized) {
            console.log('[AudioManager] Already initialized');
            return;
        }

        console.log('[AudioManager] Initializing...');

        // Load settings from localStorage
        loadSettings();

        // Update UI to reflect loaded settings
        updateSettingsUI();

        // Preload all sounds
        for (const [name, url] of Object.entries(SOUNDS)) {
            preloadSound(name, url);
        }

        // Handle browser autoplay policy
        // Create audio context on first user interaction
        document.addEventListener('click', function initAudioContext() {
            if (!audioContext) {
                try {
                    audioContext = new (window.AudioContext || window.webkitAudioContext)();
                    console.log('[AudioManager] Audio context created');
                } catch (err) {
                    console.error('[AudioManager] Failed to create audio context:', err);
                }
            }
            document.removeEventListener('click', initAudioContext);
        }, { once: true });

        initialized = true;
        console.log('[AudioManager] Initialization complete');
    }

    /**
     * Play a sound effect
     * @param {string} soundName - Name of the sound to play
     */
    function playSound(soundName) {
        // Check if audio is enabled
        if (!audioEnabled) {
            return;
        }

        // Check if sound exists
        if (!SOUNDS[soundName]) {
            console.warn(`[AudioManager] Unknown sound: ${soundName}`);
            return;
        }

        try {
            let audio = soundCache[soundName];

            // If sound not in cache, create it
            if (!audio) {
                audio = new Audio(SOUNDS[soundName]);
                soundCache[soundName] = audio;
            }

            // Stop previous instance if still playing (prevents overlap)
            if (audio.currentTime > 0 && !audio.paused) {
                audio.pause();
                audio.currentTime = 0;
            }

            // Set volume and play
            audio.volume = masterVolume;
            const playPromise = audio.play();

            // Handle autoplay errors gracefully
            if (playPromise !== undefined) {
                playPromise.catch(err => {
                    // Only log if it's not an autoplay error
                    if (err.name !== 'NotAllowedError') {
                        console.error(`[AudioManager] Failed to play sound ${soundName}:`, err);
                    }
                });
            }
        } catch (err) {
            console.error(`[AudioManager] Error playing sound ${soundName}:`, err);
        }
    }

    /**
     * Enable or disable audio
     * @param {boolean} enabled - Whether to enable audio
     */
    function setEnabled(enabled) {
        audioEnabled = !!enabled;
        saveSettings();
        console.log(`[AudioManager] Audio ${audioEnabled ? 'enabled' : 'disabled'}`);
    }

    /**
     * Set master volume
     * @param {number} volume - Volume level (0.0 to 1.0)
     */
    function setVolume(volume) {
        // Clamp volume between 0 and 1
        masterVolume = Math.max(0, Math.min(1, volume));

        // Update volume for all cached sounds
        for (const audio of Object.values(soundCache)) {
            if (audio instanceof Audio) {
                audio.volume = masterVolume;
            }
        }

        saveSettings();
        console.log(`[AudioManager] Volume set to ${Math.round(masterVolume * 100)}%`);
    }

    /**
     * Get current enabled state
     * @returns {boolean}
     */
    function isEnabled() {
        return audioEnabled;
    }

    /**
     * Get current volume level
     * @returns {number} Volume (0.0 to 1.0)
     */
    function getVolume() {
        return masterVolume;
    }

    // Public API
    return {
        init,
        playSound,
        setEnabled,
        setVolume,
        isEnabled,
        getVolume
    };
})();
