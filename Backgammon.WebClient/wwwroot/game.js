// ==== DEBUG LOGGING ====
let debugEnabled = false;

function debug(message, data = null, level = 'info') {
    const timestamp = new Date().toISOString().split('T')[1].substring(0, 12);
    const colors = {
        info: '#3b82f6',
        success: '#22c55e',
        warning: '#f59e0b',
        error: '#ef4444',
        trace: '#94a3b8'
    };

    const color = colors[level] || colors.info;
    const icon = {
        info: 'ℹ️',
        success: '✅',
        warning: '⚠️',
        error: '❌',
        trace: '🔍'
    }[level] || 'ℹ️';

    // Always log to console
    if (data !== null) {
        console.log(`[${timestamp}] ${icon} ${message}`, data);
    } else {
        console.log(`[${timestamp}] ${icon} ${message}`);
    }

    // Log to debug panel if enabled
    if (debugEnabled) {
        const debugLog = document.getElementById('debugLog');
        if (debugLog) {
            const entry = document.createElement('div');
            entry.style.cssText = `margin: 2px 0; padding: 4px; border-left: 3px solid ${color}; background: rgba(255,255,255,0.03);`;

            let html = `<span style="color: #64748b;">[${timestamp}]</span> ${icon} <span style="color: ${color};">${message}</span>`;
            if (data !== null) {
                const dataStr = typeof data === 'object' ? JSON.stringify(data, null, 2) : String(data);
                html += `<pre style="margin: 4px 0 0 20px; padding: 4px; background: rgba(0,0,0,0.3); border-radius: 3px; font-size: 10px; overflow-x: auto;">${dataStr}</pre>`;
            }
            entry.innerHTML = html;
            debugLog.appendChild(entry);

            // Auto-scroll if enabled
            const autoScroll = document.getElementById('debugAutoScroll');
            if (autoScroll && autoScroll.checked) {
                debugLog.scrollTop = debugLog.scrollHeight;
            }
        }
    }
}

function toggleDebugPanel() {
    const panel = document.getElementById('debugPanel');
    if (panel) {
        debugEnabled = panel.style.display === 'none';
        panel.style.display = debugEnabled ? 'block' : 'none';
        debug(`Debug panel ${debugEnabled ? 'enabled' : 'disabled'}`, null, 'info');
    }
}

function clearDebugLog() {
    const debugLog = document.getElementById('debugLog');
    if (debugLog) {
        debugLog.innerHTML = '<div style="color: #94a3b8; font-size: 10px; padding: 5px; text-align: center;">Debug log cleared.</div>';
        debug('Debug log cleared', null, 'info');
    }
}

// ==== STATE ====
let connection = null;
let myColor = null;
let currentGameId = null;
let gameRefreshInterval = null;
let currentGameState = null;
let selectedChecker = null; // { point: number, x: number, y: number }
let validDestinations = [];
let myPlayerId = null;  // Persistent player ID
let apiBaseUrl = 'http://localhost:5000';  // Default fallback, will be overridden from /api/config
let isAiGame = false;  // Track if playing against AI

// ==== URL ROUTING ====
function getGameIdFromUrl() {
    const pathParts = window.location.pathname.split('/');
    // Match /game/{gameId}
    if (pathParts.length >= 3 && pathParts[1] === 'game') {
        return pathParts[2];
    }
    return null;
}

function setGameUrl(gameId) {
    const newUrl = `/game/${gameId}`;
    if (window.location.pathname !== newUrl) {
        window.history.pushState({ gameId }, '', newUrl);
    }
}

function setHomeUrl() {
    if (window.location.pathname !== '/') {
        window.history.pushState({}, '', '/');
    }
}

// ==== CONNECTION HELPERS ====
async function waitForConnection(timeoutMs = 10000) {
    const startTime = Date.now();
    while (Date.now() - startTime < timeoutMs) {
        if (connection && connection.state === 'Connected') {
            return true;
        }
        await new Promise(resolve => setTimeout(resolve, 100));
    }
    return false;
}

// ==== PLAYER ID MANAGEMENT ====
function getOrCreatePlayerId() {
    let playerId = localStorage.getItem('backgammon_player_id');
    if (!playerId) {
        playerId = 'player_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
        localStorage.setItem('backgammon_player_id', playerId);
        log(`🆔 Created new player ID: ${playerId}`, 'info');
    }
    return playerId;
}

// ==== INITIALIZATION ====
window.addEventListener('load', async () => {
    // Check authentication first (from auth.js)
    if (typeof checkAuth === 'function') {
        await checkAuth();
        updateAuthUI();
    }

    // Use effective player ID (authenticated user or anonymous)
    myPlayerId = typeof getEffectivePlayerId === 'function'
        ? getEffectivePlayerId()
        : getOrCreatePlayerId();

    // Check if URL contains a game ID
    const urlGameId = getGameIdFromUrl();
    if (urlGameId) {
        // Show game page immediately
        showGamePage();

        // Update placeholder with loading state
        const boardPlaceholder = document.getElementById('boardPlaceholder');
        if (boardPlaceholder) {
            boardPlaceholder.innerHTML = '<div class="loading loading-spinner loading-lg text-primary"></div><p class="mt-4">Connecting to game...</p>';
        }

        // Connect to server
        await autoConnect();

        // Wait for connection to be ready
        const isConnected = await waitForConnection();

        if (isConnected) {
            await joinSpecificGame(urlGameId);
        } else {
            // Connection failed - show error UI
            if (boardPlaceholder) {
                boardPlaceholder.innerHTML = `
                    <div class="alert alert-error max-w-md">
                        <span>Failed to connect to server</span>
                    </div>
                    <div class="flex gap-4 mt-4">
                        <button class="btn btn-primary" onclick="location.reload()">Retry</button>
                        <button class="btn btn-ghost" onclick="showLandingPage()">Back to Home</button>
                    </div>
                `;
            }
        }
    } else {
        showLandingPage();
        await autoConnect();
    }

    // Load friends if authenticated
    if (typeof isAuthenticated === 'function' && isAuthenticated()) {
        if (typeof loadFriends === 'function') loadFriends();
        if (typeof loadFriendRequests === 'function') loadFriendRequests();
    }

    // Add Roll Dice button handler
    const rollBtn = document.getElementById('rollBtn');
    if (rollBtn) {
        rollBtn.addEventListener('click', async () => {
            debug('Roll Dice button clicked', { disabled: rollBtn.disabled, currentGameId }, 'trace');
            rollBtn.disabled = true;
            try {
                await rollDice();
            } catch (err) {
                debug('Roll dice failed', err, 'error');
                log(`Failed to roll dice: ${err}`, 'error');
            }
        });
    }
    // Add Undo button handler
    const undoBtn = document.getElementById('undoBtn');
    if (undoBtn) {
        undoBtn.addEventListener('click', async () => {
            undoBtn.disabled = true;
            try {
                await undoMove();
            } catch (err) {
                log(`Failed to undo move: ${err}`, 'error');
            }
        });
    }
    // Add End Turn button handler
    const endTurnBtn = document.getElementById('endTurnBtn');
    if (endTurnBtn) {
        endTurnBtn.addEventListener('click', async () => {
            endTurnBtn.disabled = true;
            try {
                await endTurn();
            } catch (err) {
                log(`Failed to end turn: ${err}`, 'error');
            }
        });
    }
    // Add Double button handler
    const doubleBtn = document.getElementById('doubleBtn');
    if (doubleBtn) {
        doubleBtn.addEventListener('click', () => {
            // Show confirmation modal
            if (currentGameState) {
                const currentStakes = currentGameState.doublingCubeValue || 1;
                const newStakes = currentStakes * 2;
                document.getElementById('doubleConfirmCurrentStakes').textContent = `${currentStakes}x`;
                document.getElementById('doubleConfirmNewStakes').textContent = `${newStakes}x`;
            }
            document.getElementById('doubleConfirmModal').showModal();
        });
    }
});

// Handle browser back/forward buttons
window.addEventListener('popstate', (event) => {
    const gameId = getGameIdFromUrl();
    if (gameId) {
        // User navigated to a game URL
        if (currentGameId !== gameId) {
            joinSpecificGame(gameId);
        }
    } else {
        // User navigated back to home
        if (currentGameId) {
            leaveGameAndReturn();
        }
    }
});

async function autoConnect() {
    // Fetch SignalR URL from Aspire service discovery
    let serverUrl;
    try {
        const response = await fetch('/api/config');
        const config = await response.json();
        serverUrl = config.signalrUrl;

        // Extract base API URL by removing /gamehub suffix
        apiBaseUrl = serverUrl.replace('/gamehub', '');

        log(`Using SignalR URL: ${serverUrl}`, 'info');
        log(`Using API Base URL: ${apiBaseUrl}`, 'info');
    } catch (error) {
        // Fallback to hardcoded URL if config endpoint fails
        serverUrl = document.getElementById('serverUrl').value;
        apiBaseUrl = serverUrl.replace('/gamehub', '');
        log(`Using fallback URL: ${serverUrl}`, 'warning');
    }

    // Build connection options - include auth token if authenticated
    const connectionOptions = {};
    if (typeof authToken !== 'undefined' && authToken) {
        connectionOptions.accessTokenFactory = () => authToken;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl(serverUrl, connectionOptions)
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    setupEventHandlers();

    try {
        await connection.start();
        updateConnectionStatus(true);
        log('Connected to server', 'success');

        // Always start on landing page and refresh games list
        // User can manually rejoin games from "My Games" section
        refreshGamesList();
        gameRefreshInterval = setInterval(refreshGamesList, 3000);
    } catch (err) {
        updateConnectionStatus(false);
        log(`Connection failed: ${err}`, 'error');
        setTimeout(autoConnect, 5000);
    }
}

function setupEventHandlers() {
        connection.on("SpectatorJoined", (gameState) => {
            debug('SignalR: SpectatorJoined received', { gameId: gameState.gameId }, 'info');
            log('👀 You are spectating this game.', 'info');
            updateGameState(gameState, true); // pass spectator flag
            showGamePage();
            window.isSpectator = true;
        });
    connection.on("GameUpdate", (gameState) => {
        debug('SignalR: GameUpdate received', {
            gameId: gameState.gameId,
            currentPlayer: gameState.currentPlayer,
            isYourTurn: gameState.isYourTurn,
            dice: gameState.dice
        }, 'info');
        updateGameState(gameState);
        // Update URL to reflect current game
        if (gameState.gameId) {
            setGameUrl(gameState.gameId);
        }
    });

    connection.on("GameStart", (gameState) => {
        debug('SignalR: GameStart received', { gameId: gameState.gameId }, 'success');
        log('🎮 Game started! Both players connected.', 'success');
        updateGameState(gameState);
        // Update URL to reflect current game
        if (gameState.gameId) {
            setGameUrl(gameState.gameId);
        }
    });

    connection.on("WaitingForOpponent", (gameId) => {
        log(`⏳ Waiting for opponent... Game ID: ${gameId}`, 'info');
        currentGameId = gameId;
        localStorage.setItem('currentGameId', gameId);
        setGameUrl(gameId);
        showGamePage(); // Show game page so player can see board while waiting
    });

    connection.on("OpponentJoined", (opponentId) => {
        log(`👋 Opponent joined`, 'success');
    });

    connection.on("OpponentLeft", () => {
        log('👋 Opponent left the game', 'warning');
    });

    connection.on("DoubleOffered", (currentStakes, newStakes) => {
        log(`🎲 Opponent offers to double! Stakes would be ${newStakes}x`, 'warning');
        document.getElementById('doubleCurrentStakes').textContent = `${currentStakes}x`;
        document.getElementById('doubleNewStakes').textContent = `${newStakes}x`;
        document.getElementById('doubleOfferModal').showModal();
    });

    connection.on("DoubleAccepted", (gameState) => {
        log(`✓ Double accepted! New stakes: ${gameState.doublingCubeValue}x`, 'success');
        updateGameState(gameState);
    });

    connection.on("Error", (errorMessage) => {
        debug('SignalR: Error received', { errorMessage }, 'error');
        log(`❌ Error: ${errorMessage}`, 'error');
    });

    connection.on("ReceiveChatMessage", (senderName, message, senderConnectionId) => {
        // Determine if this message is from us
        const isOwn = senderConnectionId === connection.connectionId;
        const displayName = isOwn ? 'You' : senderName;
        addChatMessage(displayName, message, isOwn);
    });

    connection.onreconnecting(() => {
        updateConnectionStatus(false);
    });

    connection.onreconnected(() => {
        updateConnectionStatus(true);
    });

    connection.onclose(() => {
        updateConnectionStatus(false);
        setTimeout(autoConnect, 2000);
    });
}

// ==== PAGE NAVIGATION ====
function showLandingPage() {
    document.getElementById('landingPage').style.display = 'block';
    document.getElementById('gamePage').style.display = 'none';
    setHomeUrl();
    if (gameRefreshInterval) {
        clearInterval(gameRefreshInterval);
        gameRefreshInterval = setInterval(refreshGamesList, 3000);
    }
}

function showGamePage() {
    document.getElementById('landingPage').style.display = 'none';
    document.getElementById('gamePage').style.display = 'block';

    if (gameRefreshInterval) {
        clearInterval(gameRefreshInterval);
    }
}

function updateConnectionStatus(isConnected) {
    const indicator = document.getElementById('connectionIndicator');
    if (isConnected) {
        indicator.innerHTML = '<div class="badge badge-success gap-1"><div class="w-2 h-2 rounded-full bg-success animate-pulse"></div>Online</div>';
    } else {
        indicator.innerHTML = '<span class="loading loading-ring loading-sm text-error"></span>';
    }
}

// ==== GAME LIST ====
async function refreshGamesList() {
    if (!connection || connection.state !== 'Connected') return;

    try {
        // Fetch all games list
        const response = await fetch(`${apiBaseUrl}/api/games`);
        const data = await response.json();

        const gamesListEl = document.getElementById('gamesList');

        if (data.waitingGames.length === 0 && data.activeGames.length === 0) {
            gamesListEl.innerHTML = '<p class="text-base-content/60 text-center py-8 italic">No games available. Create one to get started!</p>';
        } else {
            let html = '<div class="text-center py-3 px-4 bg-base-200 rounded-lg mb-4 font-medium">' +
                `${data.activeGames.length} active game(s) • ${data.waitingGames.length} waiting for players` +
                '</div>';

            // Show waiting games (available to join)
            if (data.waitingGames.length > 0) {
                html += '<div class="mb-6"><h3 class="font-semibold mb-3 text-base-content/80">Waiting for Opponent</h3><div class="space-y-2">';
                data.waitingGames.forEach(game => {
                    const waitTime = game.minutesWaiting < 1 ? 'just now' :
                                    game.minutesWaiting === 1 ? '1 min ago' :
                                    `${game.minutesWaiting} mins ago`;
                    html += `
                        <div class="card bg-base-200 shadow-sm hover:shadow-md transition-all border-l-4 border-warning">
                            <div class="card-body p-4 flex-row justify-between items-center">
                                <div>
                                    <p class="font-semibold">👤 ${escapeHtml(game.playerName)}</p>
                                    <p class="text-sm text-base-content/60">Created ${waitTime}</p>
                                </div>
                                <button class="btn btn-primary btn-sm" onclick="joinSpecificGame('${game.gameId}')">Join Game</button>
                            </div>
                        </div>
                    `;
                });
                html += '</div></div>';
            }

            // Show active games (spectate only - future feature)
            if (data.activeGames.length > 0) {
                html += '<div><h3 class="font-semibold mb-3 text-base-content/80">Games in Progress</h3><div class="space-y-2">';
                data.activeGames.forEach(game => {
                    html += `
                        <div class="card bg-base-200 shadow-sm border-l-4 border-success">
                            <div class="card-body p-4">
                                <p class="font-semibold">⚪ ${escapeHtml(game.whitePlayer)} vs 🔴 ${escapeHtml(game.redPlayer)}</p>
                            </div>
                        </div>
                    `;
                });
                html += '</div></div>';
            }

            gamesListEl.innerHTML = html;
        }

        // Fetch player's active games
        await refreshMyGames();
    } catch (err) {
        console.error('Failed to fetch games list:', err);
    }
}

async function refreshMyGames() {
    try {
        const response = await fetch(`${apiBaseUrl}/api/player/${myPlayerId}/active-games`);
        const myGames = await response.json();

        const myGamesListEl = document.getElementById('myGamesList');

        if (myGames.length === 0) {
            myGamesListEl.innerHTML = '<p class="text-base-content/60 text-center py-8 italic">You have no active games</p>';
            return;
        }

        let html = '';
        myGames.forEach(game => {
            const isAiOpponent = game.opponent === 'Computer';
            const opponentStatus = game.isFull ? escapeHtml(game.opponent) : 'Waiting for opponent';
            const opponentIcon = isAiOpponent ? '🤖' : '';
            const timeAgo = getTimeAgo(new Date(game.lastActivity));
            const borderColor = game.isMyTurn ? 'border-success' : 'border-info';
            const bgColor = game.isMyTurn ? 'bg-success/10' : 'bg-base-200';

            html += `
                <div class="card ${bgColor} shadow-sm hover:shadow-md transition-all border-l-4 ${borderColor} ${game.isMyTurn ? 'animate-pulse' : ''}">
                    <div class="card-body p-4 flex-row justify-between items-center">
                        <div>
                            <p class="font-semibold">
                                ${game.myColor === 'White' ? '⚪' : '🔴'} You vs ${opponentIcon} ${opponentStatus}
                            </p>
                            <div class="flex items-center gap-2 mt-1">
                                ${isAiOpponent ? '<span class="badge badge-secondary badge-sm">AI</span>' : ''}
                                <span class="badge ${game.isMyTurn ? 'badge-success' : 'badge-ghost'} badge-sm">${game.isMyTurn ? 'Your Turn' : 'Waiting'}</span>
                                <span class="text-sm text-base-content/60">Active ${timeAgo}</span>
                            </div>
                        </div>
                        <button class="btn ${game.isMyTurn ? 'btn-success' : 'btn-ghost'} btn-sm" onclick="joinSpecificGame('${game.gameId}')">
                            ${game.isMyTurn ? '▶ Play' : '👁 View'}
                        </button>
                    </div>
                </div>
            `;
        });

        myGamesListEl.innerHTML = html;
    } catch (err) {
        console.error('Failed to fetch my games:', err);
        document.getElementById('myGamesList').innerHTML = '<p class="text-error text-center py-8">Error loading your games</p>';
    }
}

function getTimeAgo(date) {
    const seconds = Math.floor((new Date() - date) / 1000);
    
    if (seconds < 60) return 'just now';
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    return `${days}d ago`;
}

async function joinSpecificGame(gameId) {
    // Wait for connection if not ready yet
    if (!connection || connection.state !== 'Connected') {
        log('Connection not ready, waiting...', 'warning');
        const isReady = await waitForConnection(5000);
        if (!isReady) {
            const boardPlaceholder = document.getElementById('boardPlaceholder');
            if (boardPlaceholder) {
                boardPlaceholder.innerHTML = `
                    <div class="alert alert-error max-w-md">
                        <span>Not connected to server</span>
                    </div>
                    <div class="flex gap-4 mt-4">
                        <button class="btn btn-primary" onclick="location.reload()">Retry</button>
                        <button class="btn btn-ghost" onclick="showLandingPage()">Back to Home</button>
                    </div>
                `;
            }
            return;
        }
    }

    try {
        currentGameId = gameId;
        localStorage.setItem('currentGameId', gameId);
        setGameUrl(gameId);
        await connection.invoke("JoinGame", myPlayerId, gameId);
        log(`🎮 Joining game ${gameId}...`, 'info');
        showGamePage();
    } catch (err) {
        log(`❌ Failed to join game: ${err}`, 'error');
        const boardPlaceholder = document.getElementById('boardPlaceholder');
        if (boardPlaceholder) {
            boardPlaceholder.innerHTML = `
                <div class="alert alert-error max-w-md">
                    <span>Failed to join game: ${err.message || err}</span>
                </div>
                <div class="flex gap-4 mt-4">
                    <button class="btn btn-primary" onclick="location.reload()">Retry</button>
                    <button class="btn btn-ghost" onclick="showLandingPage()">Back to Home</button>
                </div>
            `;
        }
    }
}

// ==== GAME ACTIONS ====
async function createGame() {
    if (!connection || connection.state !== 'Connected') {
        alert('Not connected to server. Please wait...');
        return;
    }

    try {
        currentGameId = null;
        localStorage.removeItem('currentGameId'); // Clear any old game
        await connection.invoke("JoinGame", myPlayerId, null);
        log('🎮 Creating new game...', 'info');
        showGamePage();
    } catch (err) {
        log(`❌ Failed to create game: ${err}`, 'error');
    }
}

/**
 * Create an analysis/practice game (solo mode)
 */
async function createAnalysisGame() {
    if (!connection || connection.state !== 'Connected') {
        alert('Not connected to server. Please wait...');
        return;
    }

    try {
        currentGameId = null;
        localStorage.removeItem('currentGameId');
        await connection.invoke("CreateAnalysisGame");
        log('📊 Creating analysis game...', 'info');
        showGamePage();
    } catch (err) {
        log(`❌ Failed to create analysis game: ${err}`, 'error');
    }
}

async function createAiGame() {
    debug('createAiGame called', { connectionState: connection?.state, myPlayerId }, 'trace');

    if (!connection || connection.state !== 'Connected') {
        debug('Cannot create AI game - not connected', null, 'warning');
        alert('Not connected to server. Please wait...');
        return;
    }

    try {
        currentGameId = null;
        localStorage.removeItem('currentGameId'); // Clear any old game
        debug('Invoking CreateAiGame on server', { myPlayerId }, 'info');
        await connection.invoke("CreateAiGame", myPlayerId);
        log('🤖 Creating AI game...', 'info');
        debug('AI game created, showing game page', null, 'success');
        showGamePage();
    } catch (err) {
        debug('Failed to create AI game', err, 'error');
        log(`❌ Failed to create AI game: ${err}`, 'error');
    }
}

async function joinGameById(gameId) {
    if (!connection || connection.state !== 'Connected') {
        alert('Not connected to server. Please wait...');
        return;
    }

    try {
        currentGameId = gameId;
        await connection.invoke("JoinGame", myPlayerId, gameId);
        log(`🎮 Joining game: ${gameId}`, 'info');
        showGamePage();
    } catch (err) {
        log(`❌ Failed to join game: ${err}`, 'error');
    }
}

async function leaveGameAndReturn() {
    try {
        if (connection && connection.state === 'Connected') {
            await connection.invoke("LeaveGame");
        }
        log('👋 Left game', 'info');
        currentGameId = null;
        myColor = null;
        localStorage.removeItem('currentGameId'); // Clear saved game
        resetGameUI();
        showLandingPage();
    } catch (err) {
        log(`❌ Error leaving game: ${err}`, 'error');
    }
}

// ==== GAME CONTROLS ====
async function rollDice() {
    debug('rollDice called', {
        connectionState: connection?.state,
        currentGameId,
        myColor,
        hasCurrentGameState: !!currentGameState
    }, 'trace');

    try {
        debug('Invoking RollDice on server', null, 'info');
        await connection.invoke("RollDice");
        log('🎲 Rolling dice...', 'info');
        debug('RollDice invoked successfully', null, 'success');
    } catch (err) {
        debug('RollDice invoke failed', err, 'error');
        log(`❌ Failed to roll dice: ${err}`, 'error');
    }
}

async function undoMove() {
    try {
        await connection.invoke("UndoMove");
        log('↶ Undoing last move...', 'info');
        // Clear selection state
        selectedChecker = null;
        validDestinations = [];
    } catch (err) {
        log(`❌ Failed to undo: ${err}`, 'error');
    }
}

async function endTurn() {
    try {
        await connection.invoke("EndTurn");
        log('✓ Ending turn...', 'info');
        // Clear selection state
        selectedChecker = null;
        validDestinations = [];
    } catch (err) {
        log(`❌ Failed to end turn: ${err}`, 'error');
    }
}

// ==== ABANDON GAME ====
function showAbandonConfirm() {
    const isWaitingForPlayer = currentGameState && currentGameState.status === 0; // GameStatus.WaitingForPlayer = 0
    const abandonMessage = document.getElementById('abandonMessage');
    const abandonStakesMessage = document.getElementById('abandonStakesMessage');

    if (isWaitingForPlayer) {
        // No opponent yet - show different message
        abandonMessage.textContent = 'Cancel this game?';
        abandonStakesMessage.textContent = 'This game will not count.';
    } else {
        // Has opponent - show forfeit message
        abandonMessage.textContent = 'Your opponent will win if you abandon this game.';
        if (currentGameState && currentGameState.doublingCubeValue) {
            document.getElementById('abandonStakes').textContent =
                `${currentGameState.doublingCubeValue}x`;
        }
        abandonStakesMessage.innerHTML = 'This will count as a loss with stakes: <span id="abandonStakes" class="font-bold">' +
            (currentGameState && currentGameState.doublingCubeValue ? currentGameState.doublingCubeValue : 1) + 'x</span>';
    }

    document.getElementById('abandonConfirmModal').showModal();
}

function cancelAbandon() {
    document.getElementById('abandonConfirmModal').close();
}

async function confirmAbandon() {
    try {
        const wasWaitingForPlayer = currentGameState && currentGameState.status === 0; // GameStatus.WaitingForPlayer = 0

        await connection.invoke("AbandonGame");
        document.getElementById('abandonConfirmModal').close();

        if (wasWaitingForPlayer) {
            // No opponent yet - just return to lobby
            log('Game cancelled.', 'info');
            leaveGameAndReturn();
        } else {
            // Had an opponent - they win
            log('Game abandoned. Opponent wins.', 'info');
        }
    } catch (err) {
        log(`Failed to abandon: ${err}`, 'error');
        document.getElementById('abandonConfirmModal').close();
    }
}

// ==== DOUBLING CUBE ====
function confirmOfferDouble() {
    offerDouble();
}

function cancelOfferDouble() {
    document.getElementById('doubleConfirmModal').close();
}

async function offerDouble() {
    try {
        await connection.invoke("OfferDouble");
        log('Double offered to opponent...', 'info');
        document.getElementById('doubleConfirmModal').close();
    } catch (err) {
        log(`Failed to offer double: ${err}`, 'error');
        document.getElementById('doubleConfirmModal').close();
    }
}

async function acceptDouble() {
    try {
        await connection.invoke("AcceptDouble");
        log('Double accepted!', 'success');
        document.getElementById('doubleOfferModal').close();
    } catch (err) {
        log(`Failed to accept double: ${err}`, 'error');
        document.getElementById('doubleOfferModal').close();
    }
}

async function declineDouble() {
    try {
        await connection.invoke("DeclineDouble");
        log('Double declined. You lose at current stakes.', 'warning');
        document.getElementById('doubleOfferModal').close();
    } catch (err) {
        log(`Failed to decline double: ${err}`, 'error');
        document.getElementById('doubleOfferModal').close();
    }
}

// ==== CHAT ====
function toggleChatSidebar() {
    const chatSidebar = document.getElementById('chatSidebar');
    const chatToggle = document.getElementById('chatToggle');

    if (chatSidebar.classList.contains('collapsed')) {
        chatSidebar.classList.remove('collapsed');
        chatToggle.textContent = '◀';
    } else {
        chatSidebar.classList.add('collapsed');
        chatToggle.textContent = '▶';
    }
}

function handleChatKeyPress(event) {
    if (event.key === 'Enter') {
        sendChat();
    }
}

async function sendChat() {
    const input = document.getElementById('chatInput');
    const message = input.value.trim();

    if (!message) return;

    // Clear input immediately for better UX
    input.value = '';

    try {
        // Send via SignalR
        await connection.invoke("SendChatMessage", message);
    } catch (err) {
        log(`Failed to send chat message: ${err}`, 'error');
        // Optionally restore message to input on failure
        input.value = message;
    }
}

function addChatMessage(sender, text, isOwn) {
    const messagesEl = document.getElementById('chatMessages');
    const welcome = messagesEl.querySelector('.chat-welcome');
    if (welcome) welcome.remove();
    
    const msgEl = document.createElement('div');
    msgEl.className = `chat-message ${isOwn ? 'own' : 'opponent'}`;
    msgEl.innerHTML = `
        <div class="chat-message-sender">${sender}</div>
        <div class="chat-message-text">${escapeHtml(text)}</div>
    `;
    messagesEl.appendChild(msgEl);
    messagesEl.scrollTop = messagesEl.scrollHeight;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ==== LOGGING ====
function log(message, type = 'info') {
    const logEl = document.getElementById('log');
    if (!logEl) return;
    
    const timestamp = new Date().toLocaleTimeString();
    const entry = document.createElement('div');
    entry.className = `log-entry log-${type}`;
    entry.innerHTML = `<span class="timestamp">[${timestamp}]</span>${message}`;
    logEl.appendChild(entry);
    logEl.scrollTop = logEl.scrollHeight;
}

function clearLog() {
    const logEl = document.getElementById('log');
    if (logEl) logEl.innerHTML = '';
}

// ==== PIP COUNT CALCULATION ====
function calculatePipCount(state, color) {
    if (!state || !state.board) return 0;

    let pips = 0;

    // Count pips from checkers on board
    state.board.forEach((point, index) => {
        if (point.color === color && point.count > 0) {
            const pointNum = index + 1; // board array is 0-indexed, points are 1-indexed

            if (color === 0) {
                // White moves 24→1, so distance is just the point number
                pips += point.count * pointNum;
            } else {
                // Red moves 1→24, so distance is (25 - point number)
                pips += point.count * (25 - pointNum);
            }
        }
    });

    // Add pips for checkers on bar (25 pips each)
    if (color === 0) {
        pips += (state.whiteCheckersOnBar || 0) * 25;
    } else {
        pips += (state.redCheckersOnBar || 0) * 25;
    }

    return pips;
}

// ==== GAME STATE ====
function updateGameState(state, isSpectator = false) {
    debug('updateGameState called', {
        gameId: state.gameId,
        currentPlayer: state.currentPlayer,
        isYourTurn: state.isYourTurn,
        IsYourTurn: state.IsYourTurn,
        yourColor: state.yourColor,
        YourColor: state.YourColor,
        hasDice: state.dice,
        remainingMoves: state.remainingMoves?.length,
        status: state.status
    }, 'trace');

    console.log('Game State:', state);
    console.log('Player Names - White:', state.whitePlayerName, 'Red:', state.redPlayerName);

    currentGameState = state; // Store for click handling

    // Detect AI game (opponent named "Computer")
    const whiteName = state.whitePlayerName || state.WhitePlayerName || '';
    const redName = state.redPlayerName || state.RedPlayerName || '';
    isAiGame = whiteName === 'Computer' || redName === 'Computer';

    // Update AI game UI (hide chat and double button for AI games)
    updateAiGameUI();

    // Spectator mode UI
    if (isSpectator || window.isSpectator) {
        document.body.classList.add('spectator-mode');
        // Optionally disable move controls here
    } else {
        document.body.classList.remove('spectator-mode');
    }

    // Show analysis mode indicator if applicable
    const analysisBadge = document.getElementById('analysisBadge');
    if (analysisBadge) {
        analysisBadge.style.display = state.isAnalysisMode ? '' : 'none';
    }

    // Update current game ID (no need to display it, it's in the URL)
    currentGameId = state.gameId;
    // Save to localStorage for reconnection
    if (state.gameId) {
        localStorage.setItem('currentGameId', state.gameId);
    }

    // Store player color
    if (state.yourColor !== null && state.yourColor !== undefined) {
        myColor = state.yourColor === 0 ? 'White' : 'Red';
        console.log('My color:', myColor);
    }

    // Update player badges
    const whiteBadge = document.getElementById('whitePlayerBadge');
    const redBadge = document.getElementById('redPlayerBadge');
    if (whiteBadge && redBadge) {
        // currentPlayer is 0 for White, 1 for Red
        whiteBadge.classList.toggle('active', state.currentPlayer === 0);
        redBadge.classList.toggle('active', state.currentPlayer === 1);
    }

    // Update player names and IDs next to board
    const whiteNameEl = document.getElementById('whitePlayerName');
    const redNameEl = document.getElementById('redPlayerName');
    const whiteIdEl = document.getElementById('whitePlayerId');
    const redIdEl = document.getElementById('redPlayerId');

    if (whiteNameEl) {
        const isYou = state.yourColor === 0; // White = 0
        const name = state.whitePlayerName || state.WhitePlayerName || 'White Player';
        whiteNameEl.textContent = name + (isYou ? ' (You)' : '');
    }
    if (whiteIdEl) {
        const whiteId = state.whitePlayerId || state.WhitePlayerId || '-';
        whiteIdEl.textContent = whiteId;
    }

    if (redNameEl) {
        const isYou = state.yourColor === 1; // Red = 1
        const name = state.redPlayerName || state.RedPlayerName || 'Red Player';
        redNameEl.textContent = name + (isYou ? ' (You)' : '');
    }
    if (redIdEl) {
        const redId = state.redPlayerId || state.RedPlayerId || '-';
        redIdEl.textContent = redId;
    }

    // Update pip counts (prefer server values, fall back to client calculation)
    const whitePipEl = document.getElementById('whitePipCount');
    const redPipEl = document.getElementById('redPipCount');
    if (whitePipEl) {
        const pipCount = state.whitePipCount ?? state.WhitePipCount ?? calculatePipCount(state, 0);
        whitePipEl.textContent = `Pips: ${pipCount}`;
    }
    if (redPipEl) {
        const pipCount = state.redPipCount ?? state.RedPipCount ?? calculatePipCount(state, 1);
        redPipEl.textContent = `Pips: ${pipCount}`;
    }

    // Dice are now rendered on the board via BoardSVG.renderDice()
    // (called from renderBoard)

    // Render board
    if (state.board) {
        renderBoard(state);
        setupBoardClickHandler(); // Enable click interactions
    }

    // Update controls
    // Handle both PascalCase and camelCase (SignalR may use either)
    const isMyTurn = state.isYourTurn ?? state.IsYourTurn ?? false;
    const hasRemainingMoves = (state.remainingMoves ?? state.RemainingMoves ?? []).length > 0;
    const dice = state.dice ?? state.Dice ?? [];
    const hasDice = dice.length > 0 && (dice[0] > 0 || dice[1] > 0);
    const movesMade = state.movesMadeThisTurn ?? state.MovesMadeThisTurn ?? 0;
    const isWaitingForPlayer = (state.status ?? state.Status ?? 1) === 0; // GameStatus.WaitingForPlayer = 0

    debug('Button state calculation', {
        isMyTurn,
        hasRemainingMoves,
        hasDice,
        movesMade,
        isWaitingForPlayer,
        dice
    }, 'trace');

    const rollBtn = document.getElementById('rollBtn');
    if (rollBtn) {
        // Can only roll at the START of your turn (no dice rolled yet)
        // Once you roll, you must end turn before rolling again
        // Also disable if waiting for another player to join
        const shouldDisable = isWaitingForPlayer || !isMyTurn || hasDice;
        debug('Roll button state update', {
            wasDisabled: rollBtn.disabled,
            willBeDisabled: shouldDisable,
            reason: shouldDisable ? (isWaitingForPlayer ? 'waiting for player' : (!isMyTurn ? 'not your turn' : 'has dice')) : 'enabled'
        }, 'trace');
        rollBtn.disabled = shouldDisable;
    }
    
    const undoBtn = document.getElementById('undoBtn');
    if (undoBtn) {
        // Can undo only if you've made at least one move this turn
        // Check movesMadeThisTurn or fallback to comparing dice vs remaining
        const movesMadeFromState = movesMade;
        const totalDice = dice.length >= 2 && dice[0] === dice[1] ? 4 : 2;
        const remainingCount = (state.remainingMoves ?? state.RemainingMoves ?? []).length;
        const movesMadeFromDice = hasDice ? (totalDice - remainingCount) : 0;
        const actualMovesMade = Math.max(movesMadeFromState, movesMadeFromDice);

        const canUndo = isMyTurn && (actualMovesMade > 0);
        undoBtn.disabled = !canUndo;
    }
    
    const endTurnBtn = document.getElementById('endTurnBtn');
    if (endTurnBtn) {
        // Can end turn only if:
        // 1. You've rolled dice (hasDice is true), AND
        // 2. All moves are made (no remaining moves), OR no valid moves are possible
        const allMovesMade = !hasRemainingMoves;
        const validMoves = state.validMoves ?? state.ValidMoves ?? [];
        const noValidMoves = validMoves.length === 0;
        const canEndTurn = isMyTurn && hasDice && (allMovesMade || noValidMoves);
        endTurnBtn.disabled = !canEndTurn;
    }

    const doubleBtn = document.getElementById('doubleBtn');
    if (doubleBtn) {
        // Can offer double only if:
        // 1. It's your turn
        // 2. You haven't rolled dice yet (before rolling)
        // 3. Game is in progress (not waiting for player)
        // 4. You own the cube OR it's centered (null)
        const myColorString = myColor === 0 ? "White" : "Red";
        const doublingCubeOwner = state.doublingCubeOwner ?? state.DoublingCubeOwner;
        const canDouble = isMyTurn &&
                          !hasDice &&
                          !isWaitingForPlayer &&
                          (doublingCubeOwner === null ||
                           doublingCubeOwner === myColorString);
        doubleBtn.disabled = !canDouble;
    }

    // Check for winner
    const winner = state.winner ?? state.Winner;
    if (winner) {
        log(`🏆 Game Over! ${winner} wins!`, 'success');
        localStorage.removeItem('currentGameId'); // Clear completed game
        setTimeout(() => {
            if (confirm(`Game Over! ${winner} wins! Return to lobby?`)) {
                leaveGameAndReturn();
            }
        }, 2000);
    }
}

function resetGameUI() {
    const boardSvg = document.getElementById('boardSvg');
    const placeholder = document.getElementById('boardPlaceholder');

    if (boardSvg) boardSvg.style.display = 'none';
    if (placeholder) {
        placeholder.style.display = 'block';
        placeholder.textContent = 'Waiting for game to start...';
    }

    const rollBtn = document.getElementById('rollBtn');
    const undoBtn = document.getElementById('undoBtn');
    const endTurnBtn = document.getElementById('endTurnBtn');

    if (rollBtn) rollBtn.disabled = true;
    if (undoBtn) undoBtn.disabled = true;
    if (endTurnBtn) endTurnBtn.disabled = true;
}

// ==== AI GAME UI ====
function updateAiGameUI() {
    const chatSidebar = document.getElementById('chatSidebar');
    const doubleBtn = document.getElementById('doubleBtn');

    if (isAiGame) {
        // Hide chat sidebar for AI games
        if (chatSidebar) {
            chatSidebar.style.display = 'none';
        }
        // Hide double button for AI games (Phase 1 limitation)
        if (doubleBtn) {
            doubleBtn.style.display = 'none';
        }
    } else {
        // Show chat and double button for human games
        if (chatSidebar) {
            chatSidebar.style.display = '';
        }
        if (doubleBtn) {
            doubleBtn.style.display = '';
        }
    }
}

// ==== BOARD RENDERING ====
function renderBoard(state) {
    const boardSvg = document.getElementById('boardSvg');
    const placeholder = document.getElementById('boardPlaceholder');

    if (!boardSvg || !placeholder) {
        console.error('SVG board or placeholder element not found');
        return;
    }

    if (!state.board || state.board.length === 0) {
        placeholder.style.display = 'block';
        boardSvg.style.display = 'none';
        return;
    }

    placeholder.style.display = 'none';
    boardSvg.style.display = 'block';

    // Initialize SVG board if not already done
    if (!BoardSVG.isInitialized()) {
        BoardSVG.init('boardSvg');
    }

    // Get valid source points for highlighting
    let validSources = [];
    if (state.isYourTurn && state.remainingMoves && state.remainingMoves.length > 0 && !selectedChecker) {
        if (state.validMoves && state.validMoves.length > 0) {
            validSources = [...new Set(state.validMoves.map(m => m.from))];
        }
    }

    // Build dice state for rendering on board
    const diceState = {
        dice: state.currentDice || state.dice || [],
        remainingMoves: state.remainingMoves || []
    };

    // Render the board with current state
    BoardSVG.render(state, selectedChecker, validDestinations, validSources, diceState);
}

function drawChecker(ctx, x, y, radius, color) {
    // Validate inputs to prevent NaN errors
    if (!Number.isFinite(x) || !Number.isFinite(y) || !Number.isFinite(radius) || radius <= 0) {
        console.error('Invalid checker parameters:', { x, y, radius });
        return;
    }
    
    // Outer circle (shadow)
    ctx.fillStyle = 'rgba(0, 0, 0, 0.4)';
    ctx.beginPath();
    ctx.arc(x + 2, y + 2, radius, 0, 2 * Math.PI);
    ctx.fill();
    
    // Main circle
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.arc(x, y, radius, 0, 2 * Math.PI);
    ctx.fill();
    
    // Highlight
    const gradient = ctx.createRadialGradient(
        x - radius * 0.3, y - radius * 0.3, 0,
        x, y, radius
    );
    
    if (color === '#ffffff') {
        // White checker: subtle gray gradient
        gradient.addColorStop(0, 'rgba(255, 255, 255, 1)');
        gradient.addColorStop(0.7, 'rgba(230, 230, 230, 1)');
        gradient.addColorStop(1, 'rgba(200, 200, 200, 1)');
    } else {
        // Red checker: bright to dark gradient
        gradient.addColorStop(0, 'rgba(255, 100, 100, 0.8)');
        gradient.addColorStop(0.5, color);
        gradient.addColorStop(1, 'rgba(0, 0, 0, 0.3)');
    }
    ctx.fillStyle = gradient;
    ctx.fill();
    
    // Strong border for contrast
    ctx.strokeStyle = color === '#ffffff' ? '#333' : '#000';
    ctx.lineWidth = 3;
    ctx.stroke();
}

function drawBarCheckers(ctx, barX, barWidth, height, radius, color, count, isBottom) {
    const centerX = barX + barWidth / 2;
    const baseY = isBottom ? height * 0.8 : height * 0.2;
    const displayCount = Math.min(count, 4);
    
    for (let i = 0; i < displayCount; i++) {
        const y = baseY + (isBottom ? -1 : 1) * (i * radius * 2.2);
        drawChecker(ctx, centerX, y, radius * 0.8, color);
    }
    
    if (count > 4) {
        ctx.fillStyle = color === '#ffffff' ? '#000' : '#fff';
        ctx.font = `bold ${radius}px Arial`;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(count, centerX, baseY + (isBottom ? -1 : 1) * (3 * radius * 2.2));
    }
}

function drawBornOff(ctx, x, y, radius, color, count) {
    if (count > 0) {
        drawChecker(ctx, x, y, radius * 0.8, color);
        
        ctx.fillStyle = color === '#ffffff' ? '#000' : '#fff';
        ctx.font = `bold ${radius}px Arial`;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(count, x, y);
    }
}

// ==== INTERACTIVE BOARD ====
let boardClickHandlerSetup = false;

function setupBoardClickHandler() {
    const boardSvg = document.getElementById('boardSvg');
    if (!boardSvg || boardClickHandlerSetup) return;

    boardSvg.addEventListener('click', handleBoardClick);
    boardClickHandlerSetup = true;
}

async function handleBoardClick(event) {
    console.log('=== BOARD CLICK START ===');
    if (!currentGameState || !currentGameState.isYourTurn) {
        console.log('Not your turn or no game state');
        return;
    }
    if (currentGameState.remainingMoves.length === 0) {
        console.log('No remaining moves');
        return;
    }

    // Get clicked point from SVG coordinates
    const clickedPoint = BoardSVG.getPointAtPosition(event.clientX, event.clientY);
    console.log('Clicked point:', clickedPoint);
    console.log('Current selection:', selectedChecker);
    console.log('Valid destinations:', JSON.stringify(validDestinations));

    if (clickedPoint === null) {
        console.log('Clicked outside - deselecting');
        // Clicked outside any point - deselect
        selectedChecker = null;
        validDestinations = [];
        renderBoard(currentGameState);
        return;
    }

    // If we have a selection and clicked a valid destination
    const matchingDest = validDestinations.find(m => m.to === clickedPoint);
    console.log('Matching destination:', matchingDest);

    if (selectedChecker !== null && matchingDest) {
        console.log('*** EXECUTING MOVE from', selectedChecker.point, 'to', clickedPoint);
        await executeMove(selectedChecker.point, clickedPoint);
        selectedChecker = null;
        validDestinations = [];
        renderBoard(currentGameState);
        return;
    }

    // Try to select this checker
    console.log('Trying to select checker at point', clickedPoint);
    await selectChecker(clickedPoint);
}

async function selectChecker(point) {
    try {
        console.log('=== SELECT CHECKER at point', point, '===');
        // Check if this point has our checkers
        const pointData = currentGameState.board.find(p => p.position === point);
        const myColorValue = myColor === 'White' ? 0 : 1;
        console.log('Point data:', pointData, 'My color value:', myColorValue);

        // Special case: check bar
        if (point === 0) {
            // In analysis mode, check which side has checkers on bar for current player
            let onBar;
            if (currentGameState.isAnalysisMode) {
                onBar = currentGameState.currentPlayer === 0 ? currentGameState.whiteCheckersOnBar : currentGameState.redCheckersOnBar;
            } else {
                onBar = myColor === 'White' ? currentGameState.whiteCheckersOnBar : currentGameState.redCheckersOnBar;
            }
            console.log('Bar selection - checkers on bar:', onBar);
            if (onBar === 0) {
                console.log('No checkers on bar - cannot select');
                selectedChecker = null;
                validDestinations = [];
                renderBoard(currentGameState);
                return;
            }
        } else if (!pointData || pointData.count === 0 || (!currentGameState.isAnalysisMode && pointData.color !== myColorValue)) {
            console.log('Point not selectable - wrong color or empty');
            selectedChecker = null;
            validDestinations = [];
            renderBoard(currentGameState);
            return;
        }

        // Get valid destinations from this point
        console.log('Invoking GetValidDestinations for point', point);
        const destinations = await connection.invoke("GetValidDestinations", point);
        console.log('Received destinations:', JSON.stringify(destinations));

        if (destinations.length === 0) {
            log(`No valid moves from point ${point}`, 'info');
            selectedChecker = null;
            validDestinations = [];
            renderBoard(currentGameState);
            return;
        }

        selectedChecker = { point };
        validDestinations = destinations;
        console.log('Selected! validDestinations now:', JSON.stringify(validDestinations));
        log(`Selected checker at point ${point}, ${destinations.length} valid move(s)`, 'info');
        renderBoard(currentGameState);
    } catch (err) {
        console.error('Error selecting checker:', err);
    }
}

async function executeMove(from, to) {
    try {
        console.log('=== EXECUTE MOVE from', from, 'to', to, '===');
        await connection.invoke("MakeMove", from, to);
        console.log('Move successful!');
        log(`Moved checker from ${from} to ${to}`, 'success');
    } catch (err) {
        console.error('Move failed:', err);
        log(`Failed to move: ${err}`, 'error');
    }
}

function getPointAtPosition(x, y, canvasWidth, canvasHeight) {
    const barWidth = canvasWidth * 0.08;
    const sideMargin = canvasWidth * 0.03;
    const playableWidth = canvasWidth - (2 * sideMargin) - barWidth;
    const pointWidth = playableWidth / 12;
    const pointHeight = canvasHeight * 0.42;
    const padding = canvasHeight * 0.03;
    const barX = sideMargin + (6 * pointWidth);
    
    // Check if in bar area
    if (x >= barX && x <= barX + barWidth) {
        const myColorValue = myColor === 'White' ? 0 : 1;
        const onBar = myColor === 'White' ? currentGameState.whiteCheckersOnBar : currentGameState.redCheckersOnBar;
        if (onBar > 0) {
            return 0; // Bar point
        }
        return null;
    }
    
    // Check top points (13-24)
    if (y >= padding && y <= padding + pointHeight) {
        let posIndex = -1;
        if (x >= sideMargin && x < sideMargin + 6 * pointWidth) {
            posIndex = Math.floor((x - sideMargin) / pointWidth);
        } else if (x >= barX + barWidth && x < canvasWidth - sideMargin) {
            posIndex = Math.floor((x - barX - barWidth) / pointWidth) + 6;
        }
        
        if (posIndex >= 0 && posIndex < 6) {
            return 13 + posIndex; // Points 13-18
        } else if (posIndex >= 6 && posIndex < 12) {
            return 19 + (posIndex - 6); // Points 19-24
        }
    }
    
    // Check bottom points (1-12)
    if (y >= canvasHeight - padding - pointHeight && y <= canvasHeight - padding) {
        let posIndex = -1;
        if (x >= sideMargin && x < sideMargin + 6 * pointWidth) {
            posIndex = Math.floor((x - sideMargin) / pointWidth);
        } else if (x >= barX + barWidth && x < canvasWidth - sideMargin) {
            posIndex = Math.floor((x - barX - barWidth) / pointWidth) + 6;
        }
        
        if (posIndex >= 0 && posIndex < 6) {
            return 12 - posIndex; // Points 12-7
        } else if (posIndex >= 6 && posIndex < 12) {
            return 6 - (posIndex - 6); // Points 6-1
        }
    }

    return null;
}

// ============================================================================
// Position Import/Export Functions (SGF Format)
// ============================================================================

/**
 * Export the current position to SGF format
 */
async function exportPosition() {
    if (!connection) {
        log('Not connected to server', 'error');
        return;
    }

    try {
        const sgf = await connection.invoke("ExportPosition");
        if (sgf) {
            // Show modal with exported position
            document.getElementById('positionText').value = sgf;
            document.getElementById('positionModalTitle').textContent = 'Export Position (SGF Format)';
            document.getElementById('copyPositionBtn').style.display = '';
            document.getElementById('importPositionBtn').style.display = 'none';
            document.getElementById('positionModal').showModal();
            log('Position exported successfully', 'success');
        }
    } catch (error) {
        log(`Failed to export position: ${error}`, 'error');
    }
}

/**
 * Show the import modal
 */
function showImportModal() {
    document.getElementById('positionText').value = '';
    document.getElementById('positionModalTitle').textContent = 'Import Position (SGF Format)';
    document.getElementById('copyPositionBtn').style.display = 'none';
    document.getElementById('importPositionBtn').style.display = '';
    document.getElementById('positionModal').showModal();
}

/**
 * Copy the exported position to clipboard
 */
async function copyPosition() {
    const text = document.getElementById('positionText').value;
    if (!text) {
        log('No position to copy', 'error');
        return;
    }

    try {
        await navigator.clipboard.writeText(text);
        log('Position copied to clipboard!', 'success');
    } catch (error) {
        log('Failed to copy to clipboard', 'error');
    }
}

/**
 * Import a position from SGF format
 */
async function applyPosition() {
    const sgf = document.getElementById('positionText').value.trim();
    if (!sgf) {
        log('Please enter an SGF position string', 'error');
        return;
    }

    if (!connection) {
        log('Not connected to server', 'error');
        return;
    }

    try {
        await connection.invoke("ImportPosition", sgf);
        document.getElementById('positionModal').close();
        log('Position imported successfully', 'success');
    } catch (error) {
        log(`Failed to import position: ${error}`, 'error');
    }
}

/**
 * Close the position modal
 */
function closePositionModal() {
    document.getElementById('positionModal').close();
}
