// Time Control Management

let selectedTimeControl = null;
let pendingGameType = null; // 'human' or 'ai'
let clockUpdateInterval = null;

/**
 * Show time control selector modal
 */
function showTimeControlModal(gameType) {
    pendingGameType = gameType;
    const modal = document.getElementById('timeControlModal');
    if (modal) {
        modal.showModal();
    }
}

/**
 * Select a time control mode
 */
function selectTimeControl(mode) {
    selectedTimeControl = mode;
    const modal = document.getElementById('timeControlModal');
    if (modal) {
        modal.close();
    }

    // Create the appropriate game type
    if (pendingGameType === 'human') {
        createGameWithTimeControl(selectedTimeControl);
    } else if (pendingGameType === 'ai') {
        createAiGameWithTimeControl(selectedTimeControl);
    }

    // Reset
    selectedTimeControl = null;
    pendingGameType = null;
}

/**
 * Create a game with time control
 */
async function createGameWithTimeControl(timeControlMode) {
    try {
        debug(`Creating game with time control: ${timeControlMode}`, null, 'info');

        const gameId = generateGameId();
        await connection.invoke('JoinGame', myPlayerId, gameId, timeControlMode);

        currentGameId = gameId;
        setGameUrl(gameId);
        showPage('gamePage');
    } catch (error) {
        console.error('Error creating game:', error);
        alert('Failed to create game: ' + error.message);
    }
}

/**
 * Create an AI game with time control
 */
async function createAiGameWithTimeControl(timeControlMode) {
    try {
        debug(`Creating AI game with time control: ${timeControlMode}`, null, 'info');

        isAiGame = true;
        await connection.invoke('CreateAiGame', myPlayerId, timeControlMode);
        showPage('gamePage');
    } catch (error) {
        console.error('Error creating AI game:', error);
        alert('Failed to create AI game: ' + error.message);
        isAiGame = false;
    }
}

/**
 * Update clock displays based on game state
 */
function updateClocks(gameState) {
    const whiteClockEl = document.getElementById('whitePlayerClock');
    const redClockEl = document.getElementById('redPlayerClock');

    if (!whiteClockEl || !redClockEl) return;

    // Check if this is a timed game
    if (gameState.timeControlMode === 'Untimed' || gameState.timeControlMode === 0) {
        whiteClockEl.style.display = 'none';
        redClockEl.style.display = 'none';
        stopClockUpdates();
        return;
    }

    // Show clocks
    whiteClockEl.style.display = 'block';
    redClockEl.style.display = 'block';

    // Update white clock
    const whiteTime = formatTime(gameState.whiteRemainingMs);
    whiteClockEl.textContent = `⏱️ ${whiteTime}`;
    whiteClockEl.style.color = gameState.whiteRemainingMs < 10000 ? '#ef4444' : ''; // Red if < 10s

    // Update red clock
    const redTime = formatTime(gameState.redRemainingMs);
    redClockEl.textContent = `⏱️ ${redTime}`;
    redClockEl.style.color = gameState.redRemainingMs < 10000 ? '#ef4444' : ''; // Red if < 10s

    // Highlight active player's clock
    if (gameState.currentPlayer === 0) { // White
        whiteClockEl.style.fontWeight = 'bold';
        redClockEl.style.fontWeight = 'normal';
    } else { // Red
        whiteClockEl.style.fontWeight = 'normal';
        redClockEl.style.fontWeight = 'bold';
    }

    // Start real-time clock updates if not paused and game in progress
    if (!gameState.isClockPaused && gameState.status === 1) { // InProgress = 1
        startClockUpdates(gameState);
    } else {
        stopClockUpdates();
    }
}

/**
 * Format milliseconds to MM:SS
 */
function formatTime(ms) {
    if (ms <= 0) return '0:00';

    const totalSeconds = Math.ceil(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

/**
 * Start periodic clock updates for smooth countdown
 */
function startClockUpdates(initialGameState) {
    stopClockUpdates(); // Clear any existing interval

    let gameState = initialGameState;
    const startTime = Date.now();

    clockUpdateInterval = setInterval(() => {
        if (!gameState || gameState.isClockPaused || gameState.status !== 1) {
            stopClockUpdates();
            return;
        }

        // Calculate elapsed time since last server update
        const elapsed = Date.now() - startTime;

        // Update the active player's clock
        const whiteClockEl = document.getElementById('whitePlayerClock');
        const redClockEl = document.getElementById('redPlayerClock');

        if (gameState.currentPlayer === 0) { // White's turn
            const whiteRemaining = Math.max(0, gameState.whiteRemainingMs - elapsed);
            whiteClockEl.textContent = `⏱️ ${formatTime(whiteRemaining)}`;
            whiteClockEl.style.color = whiteRemaining < 10000 ? '#ef4444' : '';
        } else { // Red's turn
            const redRemaining = Math.max(0, gameState.redRemainingMs - elapsed);
            redClockEl.textContent = `⏱️ ${formatTime(redRemaining)}`;
            redClockEl.style.color = redRemaining < 10000 ? '#ef4444' : '';
        }
    }, 100); // Update every 100ms for smooth countdown
}

/**
 * Stop clock updates
 */
function stopClockUpdates() {
    if (clockUpdateInterval) {
        clearInterval(clockUpdateInterval);
        clockUpdateInterval = null;
    }
}
