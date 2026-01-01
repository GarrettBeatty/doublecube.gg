// ==== TIME CONTROL UI ====

/**
 * Show time control modal for game creation
 * @param {string} gameType - 'human' or 'ai'
 */
function showTimeControlModal(gameType) {
    const modal = document.getElementById('timeControlModal');
    if (modal) {
        // Store game type for later use
        modal.dataset.gameType = gameType;
        modal.showModal();
    }
}

/**
 * Select a time control mode and create the game
 * @param {number} mode - 0=Untimed, 1=Blitz, 2=Rapid, 3=Classical
 */
async function selectTimeControl(mode) {
    const modal = document.getElementById('timeControlModal');
    const gameType = modal?.dataset.gameType || 'human';

    // Close modal
    if (modal) {
        modal.close();
    }

    // Create game with selected time control
    if (gameType === 'ai') {
        await createAiGameWithTimeControl(mode);
    } else {
        await createGameWithTimeControl(mode);
    }
}

/**
 * Create a human vs human game with time control
 * @param {number} timeControlMode - 0=Untimed, 1=Blitz, 2=Rapid, 3=Classical
 */
async function createGameWithTimeControl(timeControlMode) {
    if (!connection || connection.state !== 'Connected') {
        alert('Not connected to server. Please wait...');
        return;
    }

    try {
        // Leave any existing game first
        if (currentGameId && connection.state === 'Connected') {
            await connection.invoke("LeaveGame");
        }

        currentGameId = null;
        localStorage.removeItem('currentGameId');

        // Pass time control mode to JoinGame (3rd parameter)
        await connection.invoke("JoinGame", myPlayerId, null, timeControlMode);
        log(`🎮 Creating new game with time control: ${getTimeControlName(timeControlMode)}`, 'info');
        showGamePage();
    } catch (err) {
        log(`❌ Failed to create game: ${err}`, 'error');
    }
}

/**
 * Create an AI game with time control
 * @param {number} timeControlMode - 0=Untimed, 1=Blitz, 2=Rapid, 3=Classical
 */
async function createAiGameWithTimeControl(timeControlMode) {
    if (!connection || connection.state !== 'Connected') {
        alert('Not connected to server. Please wait...');
        return;
    }

    try {
        // Leave any existing game first
        if (currentGameId && connection.state === 'Connected') {
            await connection.invoke("LeaveGame");
        }

        currentGameId = null;
        localStorage.removeItem('currentGameId');

        // Pass time control mode to CreateAiGame (2nd parameter)
        await connection.invoke("CreateAiGame", myPlayerId, timeControlMode);
        log(`🤖 Creating AI game with time control: ${getTimeControlName(timeControlMode)}`, 'info');
        showGamePage();
    } catch (err) {
        log(`❌ Failed to create AI game: ${err}`, 'error');
    }
}

/**
 * Get human-readable name for time control mode
 * @param {number} mode - 0=Untimed, 1=Blitz, 2=Rapid, 3=Classical
 * @returns {string}
 */
function getTimeControlName(mode) {
    switch (mode) {
        case 0: return 'Untimed';
        case 1: return 'Blitz (30s)';
        case 2: return 'Rapid (2min)';
        case 3: return 'Classical (5min)';
        default: return 'Unknown';
    }
}

/**
 * Update clock displays based on game state
 * @param {Object} gameState - Current game state from server
 */
function updateClocks(gameState) {
    const whiteClockEl = document.getElementById('whitePlayerClock');
    const redClockEl = document.getElementById('redPlayerClock');

    if (!whiteClockEl || !redClockEl) return;

    // Check if time control is enabled
    if (!gameState.timeControlMode || gameState.timeControlMode === 0) {
        // Untimed - hide clocks
        whiteClockEl.style.display = 'none';
        redClockEl.style.display = 'none';
        return;
    }

    // Show clocks
    whiteClockEl.style.display = 'block';
    redClockEl.style.display = 'block';

    // Update clock values
    const whiteTime = formatTime(gameState.whiteRemainingMs);
    const redTime = formatTime(gameState.redRemainingMs);

    whiteClockEl.textContent = whiteTime;
    redClockEl.textContent = redTime;

    // Apply warning styles if time is low (< 10 seconds)
    if (gameState.whiteRemainingMs < 10000) {
        whiteClockEl.classList.add('time-warning');
    } else {
        whiteClockEl.classList.remove('time-warning');
    }

    if (gameState.redRemainingMs < 10000) {
        redClockEl.classList.add('time-warning');
    } else {
        redClockEl.classList.remove('time-warning');
    }

    // Highlight active clock
    if (gameState.currentPlayer === 0) {
        // White's turn
        whiteClockEl.classList.add('active-clock');
        redClockEl.classList.remove('active-clock');
    } else {
        // Red's turn
        redClockEl.classList.add('active-clock');
        whiteClockEl.classList.remove('active-clock');
    }
}

/**
 * Format milliseconds to MM:SS
 * @param {number} ms - Time in milliseconds
 * @returns {string} Formatted time string
 */
function formatTime(ms) {
    if (ms <= 0) return '0:00';

    const totalSeconds = Math.ceil(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

/**
 * Start real-time clock countdown updates (called when game state updates)
 * Updates every 100ms for smooth countdown
 */
let clockUpdateInterval = null;

function startClockUpdates(gameState) {
    // Clear any existing interval
    if (clockUpdateInterval) {
        clearInterval(clockUpdateInterval);
        clockUpdateInterval = null;
    }

    // Only start updates if time control is enabled and not paused
    if (!gameState.timeControlMode || gameState.timeControlMode === 0 || gameState.isClockPaused) {
        return;
    }

    // Store initial state for client-side countdown
    let whiteMs = gameState.whiteRemainingMs;
    let redMs = gameState.redRemainingMs;
    const currentPlayer = gameState.currentPlayer;
    const startTime = Date.now();

    // Update clocks immediately
    updateClocks(gameState);

    // Update every 100ms for smooth countdown
    clockUpdateInterval = setInterval(() => {
        const elapsed = Date.now() - startTime;

        // Calculate new times
        let newWhiteMs = whiteMs;
        let newRedMs = redMs;

        if (currentPlayer === 0) {
            // White's clock is running
            newWhiteMs = Math.max(0, whiteMs - elapsed);
        } else {
            // Red's clock is running
            newRedMs = Math.max(0, redMs - elapsed);
        }

        // Create temporary state for display
        const displayState = {
            ...gameState,
            whiteRemainingMs: newWhiteMs,
            redRemainingMs: newRedMs
        };

        updateClocks(displayState);

        // Stop countdown if time runs out (server will handle game over)
        if (newWhiteMs <= 0 || newRedMs <= 0) {
            clearInterval(clockUpdateInterval);
            clockUpdateInterval = null;
        }
    }, 100);
}

/**
 * Stop clock updates (called when leaving game)
 */
function stopClockUpdates() {
    if (clockUpdateInterval) {
        clearInterval(clockUpdateInterval);
        clockUpdateInterval = null;
    }
}
