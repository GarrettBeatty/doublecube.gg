/* global AudioManager, BoardSVG, checkAuth, updateAuthUI, getEffectivePlayerId, isAuthenticated, loadFriends, loadFriendRequests, getProfileUsernameFromUrl, showProfilePage, loadProfile, initializeMatchEvents, continueMatch, authToken, showError */
/* exported toggleDebugPanel, clearDebugLog, goHome, createGame, createAnalysisGame, createAiGame, joinGameById, showAbandonConfirm, cancelAbandon, confirmAbandon, confirmOfferDouble, cancelOfferDouble, acceptDouble, declineDouble, toggleChatSidebar, handleChatKeyPress, clearLog, toggleBoardFlip, drawBarCheckers, drawBornOff, setupBoardClickHandler, getPointAtPosition, exportPosition, showImportModal, copyPosition, applyPosition, closePositionModal, spectateGame */

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
let isBoardFlipped = false;  // Track board flip state

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

function detectRoute() {
    const path = window.location.pathname;

    // Match routes: /match/{matchId}, /match/{matchId}/lobby, /match/{matchId}/results, /match/{matchId}/game/{gameId}
    const matchLobbyPattern = /^\/match\/([^/]+)\/lobby$/;
    const matchResultsPattern = /^\/match\/([^/]+)\/results$/;
    const matchGamePattern = /^\/match\/([^/]+)\/game\/([^/]+)$/;
    const matchPattern = /^\/match\/([^/]+)$/;
    const gamePattern = /^\/game\/([^/]+)$/;

    if (matchLobbyPattern.test(path)) {
        const matchId = path.match(matchLobbyPattern)[1];
        return { type: 'match-lobby', matchId };
    } else if (matchResultsPattern.test(path)) {
        const matchId = path.match(matchResultsPattern)[1];
        return { type: 'match-results', matchId };
    } else if (matchGamePattern.test(path)) {
        const matches = path.match(matchGamePattern);
        return { type: 'match-game', matchId: matches[1], gameId: matches[2] };
    } else if (matchPattern.test(path)) {
        const matchId = path.match(matchPattern)[1];
        return { type: 'match', matchId };
    } else if (gamePattern.test(path)) {
        const gameId = path.match(gamePattern)[1];
        return { type: 'game', gameId };
    }

    return { type: 'home' };
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

    // Expose myPlayerId globally for other modules
    window.myPlayerId = myPlayerId;

    // Check if URL contains a profile username
    const profileUsername = getProfileUsernameFromUrl();
    if (profileUsername) {
        // Show profile page immediately
        showProfilePage();

        // Connect to server first
        await autoConnect();

        // Wait for connection to be ready
        const isConnected = await waitForConnection();

        if (isConnected) {
            await loadProfile(profileUsername);
        } else {
            // Connection failed - show error UI
            showLandingPage();
            log('Failed to connect to server', 'error');
        }
    } else {
        // Detect route and handle accordingly
        const route = detectRoute();
        debug('Route detected', route, 'info');

        switch (route.type) {
            case 'game':
            case 'match-game':
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
                await joinSpecificGame(route.gameId);
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
            break;

        case 'match-lobby':
            // Show match lobby
            if (typeof window.matchLobbyView !== 'undefined') {
                await autoConnect();
                // Wait for connection to be ready
                const isConnected = await waitForConnection();
                if (isConnected) {
                    await window.matchLobbyView.show(route.matchId);
                } else {
                    console.error('Failed to connect to server for match lobby');
                    showLandingPage();
                }
            } else {
                console.error('MatchLobbyView not loaded');
                showLandingPage();
            }
            break;

        case 'match-results':
            // Show match results
            if (typeof window.matchResultsView !== 'undefined') {
                await autoConnect();
                await window.matchResultsView.show(route.matchId);
            } else {
                console.error('MatchResultsView not loaded');
                showLandingPage();
            }
            break;

        case 'match':
            // Generic match route - redirect to lobby
            if (typeof window.matchController !== 'undefined') {
                window.matchController.navigateToLobby(route.matchId);
                location.reload();
            } else {
                showLandingPage();
            }
            break;

        default:
            // Home route
            showLandingPage();
            await autoConnect();
            break;
        }
    }

    // Load friends if authenticated
    if (typeof isAuthenticated === 'function' && isAuthenticated()) {
        if (typeof loadFriends === 'function') loadFriends();
        if (typeof loadFriendRequests === 'function') loadFriendRequests();
    }

    // Initialize audio system
    if (typeof AudioManager !== 'undefined') {
        AudioManager.init();
    }

    // Add Roll Dice button handler
    const rollBtn = document.getElementById('rollBtn');
    if (rollBtn) {
        rollBtn.addEventListener('click', async () => {
            debug('Roll Dice button clicked', { disabled: rollBtn.disabled, currentGameId }, 'trace');
            rollBtn.disabled = true;
            try {
                await rollDice();
                if (typeof AudioManager !== 'undefined') {
                    AudioManager.playSound('dice-roll');
                }
            } catch (err) {
                debug('Roll dice failed', err, 'error');
                log(`Failed to roll dice: ${err}`, 'error');
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
    // Add Undo button handler
    const undoBtn = document.getElementById('undoBtn');
    if (undoBtn) {
        undoBtn.addEventListener('click', async () => {
            debug('Undo button clicked', { disabled: undoBtn.disabled }, 'trace');
            undoBtn.disabled = true;
            try {
                await undoLastMove();
            } finally {
                undoBtn.disabled = false;
            }
        });
    }
    // Add Double button handler
    const doubleBtn = document.getElementById('doubleBtn');
    if (doubleBtn) {
        doubleBtn.addEventListener('click', () => {
            debug('Double button clicked', { disabled: doubleBtn.disabled, currentGameState: !!currentGameState }, 'trace');

            // Show confirmation modal
            if (currentGameState) {
                const currentStakes = currentGameState.doublingCubeValue || 1;
                const newStakes = currentStakes * 2;
                document.getElementById('doubleConfirmCurrentStakes').textContent = `${currentStakes}x`;
                document.getElementById('doubleConfirmNewStakes').textContent = `${newStakes}x`;
            }

            const modal = document.getElementById('doubleConfirmModal');
            debug('Opening double confirm modal', { modalExists: !!modal }, 'trace');
            if (modal) {
                modal.showModal();
            } else {
                console.error('doubleConfirmModal not found in DOM');
            }
        });
    } else {
        console.error('doubleBtn not found in DOM during setup');
    }

    // Add audio settings event handlers
    const audioEnabledToggle = document.getElementById('audioEnabled');
    if (audioEnabledToggle) {
        audioEnabledToggle.addEventListener('change', (e) => {
            if (typeof AudioManager !== 'undefined') {
                AudioManager.setEnabled(e.target.checked);
            }
        });
    }

    const volumeSlider = document.getElementById('volumeSlider');
    if (volumeSlider) {
        volumeSlider.addEventListener('input', (e) => {
            if (typeof AudioManager !== 'undefined') {
                const volume = parseInt(e.target.value) / 100;
                AudioManager.setVolume(volume);
                const volumeDisplay = document.getElementById('volumeDisplay');
                if (volumeDisplay) {
                    volumeDisplay.textContent = `${e.target.value}%`;
                }
            }
        });
    }

    // Load board flip preference from session
    loadBoardFlipPreference();
});

// Handle browser back/forward buttons
window.addEventListener('popstate', (_event) => {
    // Check for profile URL first
    const profileUsername = getProfileUsernameFromUrl();
    if (profileUsername) {
        // User navigated to a profile URL
        showProfilePage();
        loadProfile(profileUsername);
        return;
    }
    
    // Check for game URL
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

        // Update matchController with API base URL
        if (typeof window.matchController !== 'undefined') {
            window.matchController.setApiBaseUrl(apiBaseUrl);
        }

        log(`Using SignalR URL: ${serverUrl}`, 'info');
        log(`Using API Base URL: ${apiBaseUrl}`, 'info');
    } catch (error) {
        // Fallback to hardcoded URL if config endpoint fails
        serverUrl = document.getElementById('serverUrl').value;
        apiBaseUrl = serverUrl.replace('/gamehub', '');

        // Update matchController with fallback API base URL
        if (typeof window.matchController !== 'undefined') {
            window.matchController.setApiBaseUrl(apiBaseUrl);
        }

        log(`Using fallback URL: ${serverUrl}`, 'warning');
    }

    // Build connection options - include auth token if authenticated
    const connectionOptions = {};
    if (typeof authToken !== 'undefined' && authToken) {
        connectionOptions.accessTokenFactory = () => authToken;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl(serverUrl, connectionOptions)
        .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 30000])  // Custom retry delays
        .withServerTimeout(60 * 1000)          // 60s - matches server ClientTimeoutInterval
        .withKeepAliveInterval(15 * 1000)      // 15s - faster than server's 20s KeepAliveInterval
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Expose connection globally for other modules
    window.connection = connection;

    setupEventHandlers();
    
    // Initialize match events
    initializeMatchEvents();

    try {
        await connection.start();
        updateConnectionStatus(true);
        log('Connected to server', 'success');

        // Load active match from server (if any)
        if (typeof window.matchController !== 'undefined' && myPlayerId) {
            await window.matchController.loadActiveMatch(myPlayerId);
            const activeMatch = window.matchController.getCurrentMatch();
            if (activeMatch) {
                debug('Loaded active match from server', activeMatch, 'info');
            }
        }

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

        // Detect and play sounds BEFORE updating state
        if (typeof AudioManager !== 'undefined' && currentGameState) {
            // Turn change detection
            if (currentGameState.currentPlayer !== gameState.currentPlayer) {
                AudioManager.playSound('turn-change');
            }

            // Move detection
            const moveType = detectMoveType(currentGameState, gameState);
            if (moveType === 'hit') {
                AudioManager.playSound('checker-hit');
            } else if (moveType === 'bearoff') {
                AudioManager.playSound('bear-off');
            } else if (moveType === 'move') {
                AudioManager.playSound('checker-move');
            }
        }

        updateGameState(gameState);
        // Update URL to reflect current game (only if we're on the game page)
        if (gameState.gameId && document.getElementById('gamePage').style.display !== 'none') {
            setGameUrl(gameState.gameId);
        }
    });

    connection.on("GameStart", (gameState) => {
        debug('SignalR: GameStart received', { gameId: gameState.gameId }, 'success');
        log('🎮 Game started! Both players connected.', 'success');
        updateGameState(gameState);
        // Update URL to reflect current game (only if we're on the game page)
        if (gameState.gameId && document.getElementById('gamePage').style.display !== 'none') {
            setGameUrl(gameState.gameId);
        }
    });

    connection.on("WaitingForOpponent", (gameId) => {
        log(`⏳ Waiting for opponent... Game ID: ${gameId}`, 'info');
        currentGameId = gameId;
        setGameUrl(gameId);
        showGamePage(); // Show game page so player can see board while waiting
    });

    connection.on("OpponentJoined", (_opponentId) => {
        log(`👋 Opponent joined`, 'success');
    });

    connection.on("OpponentLeft", () => {
        log('👋 Opponent left the game', 'warning');
    });

    connection.on("DoubleOffered", (currentStakes, newStakes) => {
        log(`🎲 Opponent offers to double! Stakes would be ${newStakes}x`, 'warning');
        if (typeof AudioManager !== 'undefined') {
            AudioManager.playSound('double-offer');
        }
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

    connection.on("Info", (infoMessage) => {
        debug('SignalR: Info received', { infoMessage }, 'info');
        log(`ℹ️ ${infoMessage}`, 'info');
    });

    connection.on("ReceiveChatMessage", (senderName, message, senderConnectionId) => {
        // Determine if this message is from us
        const isOwn = senderConnectionId === connection.connectionId;
        const displayName = isOwn ? 'You' : senderName;
        // Play sound for incoming messages (not our own)
        if (!isOwn && typeof AudioManager !== 'undefined') {
            AudioManager.playSound('chat-message');
        }
        addChatMessage(displayName, message, isOwn);
    });

    connection.on("GameOver", (gameState) => {
        debug('SignalR: GameOver received', {
            gameId: gameState.gameId,
            winner: gameState.winner
        }, 'success');

        // Determine winner and show appropriate message
        const winner = gameState.winner ?? gameState.Winner;
        if (winner) {
            // Play win/loss sound
            if (typeof AudioManager !== 'undefined') {
                const didWeWin = (myColor === 'White' && winner === 'White') ||
                                 (myColor === 'Red' && winner === 'Red');
                AudioManager.playSound(didWeWin ? 'game-won' : 'game-lost');
            }
            log(`🏆 Game Over! ${winner} wins!`, 'success');
        } else {
            log('Game Over!', 'info');
        }

        // Check if this is a match game
        const match = typeof window.matchController !== 'undefined'
            ? window.matchController.getCurrentMatch()
            : null;

        if (match && match.status === 'InProgress') {
            // This is part of an ongoing match
            setTimeout(() => {
                const matchComplete = window.matchController.isMatchComplete();

                if (matchComplete) {
                    // Match is complete - navigate to results
                    debug('Match complete, navigating to results', { matchId: match.matchId }, 'success');
                    window.matchController.navigateToResults(match.matchId);
                    window.location.href = `/match/${match.matchId}/results`;
                } else {
                    // Match continues - show continue dialog
                    if (confirm(`Game complete! Score: ${match.player1Score} - ${match.player2Score}. Continue to next game?`)) {
                        // Continue the match
                        continueMatch(match.matchId);
                    } else if (confirm('Do you want to abandon this match? Your opponent will win.')) {
                        // Abandon the match
                        currentGameId = null;
                        myColor = null;
                        window.matchController.clearMatch();
                        leaveGameAndReturn();
                    }
                }
            }, 1500);
        } else {
            // Single game - use existing behavior
            setTimeout(() => {
                if (confirm(`Game Over! ${winner} wins! Return to lobby?`)) {
                    // Clear game state
                    currentGameId = null;
                    myColor = null;
                    leaveGameAndReturn();
                }
            }, 1500);
        }
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
    document.getElementById('profilePage').style.display = 'none';
    setHomeUrl();
    if (gameRefreshInterval) {
        clearInterval(gameRefreshInterval);
        gameRefreshInterval = setInterval(refreshGamesList, 3000);
    }
}

function showGamePage() {
    document.getElementById('landingPage').style.display = 'none';
    document.getElementById('gamePage').style.display = 'block';
    document.getElementById('profilePage').style.display = 'none';

    if (gameRefreshInterval) {
        clearInterval(gameRefreshInterval);
    }
}

/**
 * Navigate to home - properly leaves game if currently in one
 */
async function goHome() {
    const isOnGamePage = document.getElementById('gamePage').style.display !== 'none';
    if (isOnGamePage && currentGameId) {
        // In a game - leave it first
        await leaveGameAndReturn();
    } else {
        // Already on landing page or not in a game - just show landing page
        showLandingPage();
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
            let html = '';

            // Show waiting games (available to join)
            if (data.waitingGames.length > 0) {
                html += '<div class="mb-6"><h3 class="font-semibold mb-3 text-base-content/80">Waiting for Opponent</h3><div class="space-y-2">';
                data.waitingGames.forEach(game => {
                    const waitTime = game.minutesWaiting < 1 ? 'just now' :
                                    game.minutesWaiting === 1 ? '1 min ago' :
                                    `${game.minutesWaiting} mins ago`;
                    // Make username clickable if we have one
                    const playerLink = game.playerUsername && game.playerUsername !== 'Computer' 
                        ? `<a href="/profile/${encodeURIComponent(game.playerUsername)}" class="link link-hover" onclick="event.preventDefault(); navigateToProfile('${game.playerUsername}')">${escapeHtml(game.playerName)}</a>`
                        : escapeHtml(game.playerName);
                    
                    html += `
                        <div class="card bg-base-200 shadow-sm hover:shadow-md transition-all border-l-4 border-warning">
                            <div class="card-body p-4 flex-row justify-between items-center">
                                <div>
                                    <p class="font-semibold">👤 ${playerLink}</p>
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
                    // Make usernames clickable if we have them
                    const whiteLink = game.whiteUsername && game.whiteUsername !== 'Computer'
                        ? `<a href="/profile/${encodeURIComponent(game.whiteUsername)}" class="link link-hover" onclick="event.preventDefault(); navigateToProfile('${game.whiteUsername}')">${escapeHtml(game.whitePlayer)}</a>`
                        : escapeHtml(game.whitePlayer);
                    
                    const redLink = game.redUsername && game.redUsername !== 'Computer'
                        ? `<a href="/profile/${encodeURIComponent(game.redUsername)}" class="link link-hover" onclick="event.preventDefault(); navigateToProfile('${game.redUsername}')">${escapeHtml(game.redPlayer)}</a>`
                        : escapeHtml(game.redPlayer);
                    
                    html += `
                        <div class="card bg-base-200 shadow-sm border-l-4 border-success">
                            <div class="card-body p-4">
                                <p class="font-semibold">⚪ ${whiteLink} vs 🔴 ${redLink}</p>
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
            const opponentIcon = isAiOpponent ? '🤖' : '';
            const timeAgo = getTimeAgo(new Date(game.lastActivity));
            const borderColor = game.isMyTurn ? 'border-success' : 'border-info';
            const bgColor = game.isMyTurn ? 'bg-success/10' : 'bg-base-200';
            
            // Make opponent name clickable if we have username and it's not AI
            let opponentStatus;
            if (!game.isFull) {
                opponentStatus = 'Waiting for opponent';
            } else if (game.opponentUsername && !isAiOpponent) {
                opponentStatus = `<a href="/profile/${encodeURIComponent(game.opponentUsername)}" class="link link-hover" onclick="event.preventDefault(); navigateToProfile('${game.opponentUsername}')">${escapeHtml(game.opponent)}</a>`;
            } else {
                opponentStatus = escapeHtml(game.opponent);
            }

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
        // Leave any existing game first (important for spectated games)
        if (currentGameId && connection.state === 'Connected') {
            await connection.invoke("LeaveGame");
        }

        currentGameId = null;
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
        // Leave any existing game first (important for spectated games)
        if (currentGameId && connection.state === 'Connected') {
            await connection.invoke("LeaveGame");
        }

        currentGameId = null;
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
        // Leave any existing game first (important for spectated games)
        if (currentGameId && connection.state === 'Connected') {
            await connection.invoke("LeaveGame");
        }

        currentGameId = null;
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
        resetGameUI();
        resetBoardFlipPreference();
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

async function undoLastMove() {
    try {
        await connection.invoke("UndoLastMove");
        debug('Undo move invoked successfully', null, 'success');
    } catch (err) {
        debug('Failed to undo move', err, 'error');
        showError(err.toString());
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
        debug('offerDouble called', { isAiGame, currentGameId }, 'info');
        await connection.invoke("OfferDouble");
        log('Double offered to opponent...', 'info');
        debug('OfferDouble invoked successfully', null, 'success');
        document.getElementById('doubleConfirmModal').close();
    } catch (err) {
        debug('OfferDouble failed', err, 'error');
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

/**
 * Detect type of move by comparing board states
 * @param {Object} prevState - Previous game state
 * @param {Object} newState - New game state
 * @returns {string|null} 'hit' | 'bearoff' | 'move' | null
 */
function detectMoveType(prevState, newState) {
    if (!prevState || !newState) return null;

    // Check for bear-off (bornOff count increased)
    if (newState.whiteBornOff > prevState.whiteBornOff ||
        newState.redBornOff > prevState.redBornOff) {
        return 'bearoff';
    }

    // Check for hit (bar count increased)
    if (newState.whiteCheckersOnBar > prevState.whiteCheckersOnBar ||
        newState.redCheckersOnBar > prevState.redCheckersOnBar) {
        return 'hit';
    }

    // Regular move (board changed)
    if (JSON.stringify(prevState.board) !== JSON.stringify(newState.board)) {
        return 'move';
    }

    return null;
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

// ==== HELPER FUNCTIONS ====
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

        // Add spectator badge if not present
        if (!document.getElementById('spectatorBadge')) {
            const badge = document.createElement('div');
            badge.id = 'spectatorBadge';
            badge.className = 'alert alert-info py-2 mb-4';
            badge.innerHTML = `
                <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
                    <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" />
                </svg>
                <span>You are spectating this game</span>
            `;
            // Insert badge at top of game board area
            const gameBoard = document.querySelector('.game-board-section');
            if (gameBoard) {
                gameBoard.insertBefore(badge, gameBoard.firstChild);
            }
        }

        // Disable all control buttons except Leave (spectators should be able to leave)
        const controlButtons = [
            'rollBtn', 'doubleBtn', 'endTurnBtn',
            'abandonBtn', 'exportBtn', 'importBtn'
        ];
        controlButtons.forEach(btnId => {
            const btn = document.getElementById(btnId);
            if (btn) {
                btn.disabled = true;
                btn.style.pointerEvents = 'none';
            }
        });
    } else {
        document.body.classList.remove('spectator-mode');
        // Remove spectator badge if present
        const badge = document.getElementById('spectatorBadge');
        if (badge) badge.remove();

        // Re-enable control buttons (they'll be managed by normal game logic)
        const controlButtons = [
            'rollBtn', 'doubleBtn', 'endTurnBtn',
            'abandonBtn', 'exportBtn', 'importBtn'
        ];
        controlButtons.forEach(btnId => {
            const btn = document.getElementById(btnId);
            if (btn) {
                btn.style.pointerEvents = '';
                // Don't set disabled=false here, let normal game logic handle it
            }
        });
    }

    // Show analysis mode indicator if applicable
    const analysisBadge = document.getElementById('analysisBadge');
    if (analysisBadge) {
        analysisBadge.style.display = state.isAnalysisMode ? '' : 'none';
    }

    // Show import button only in analysis mode
    const importBtn = document.getElementById('importBtn');
    if (importBtn) {
        importBtn.style.display = state.isAnalysisMode ? '' : 'none';
    }

    // Show export button only in analysis mode
    const exportBtn = document.getElementById('exportBtn');
    if (exportBtn) {
        exportBtn.style.display = state.isAnalysisMode ? '' : 'none';
    }

    // Update Abandon button text to Forfeit if game has started
    const abandonBtn = document.getElementById('abandonBtn');
    if (abandonBtn) {
        // Game has started if both players have made at least one move
        // Status: 0 = WaitingForPlayer, 1 = InProgress, 2 = Completed
        const gameHasStarted = state.status !== 0 && state.status !== undefined;
        if (gameHasStarted) {
            abandonBtn.innerHTML = '⚠️ Forfeit';
        } else {
            abandonBtn.innerHTML = '⚠️ Abandon';
        }
    }

    // Update current game ID (no need to display it, it's in the URL)
    currentGameId = state.gameId;

    // Store player color
    if (state.yourColor !== null && state.yourColor !== undefined) {
        myColor = state.yourColor === 0 ? 'White' : 'Red';
        console.log('My color:', myColor);

        // Auto-flip board when player color is assigned
        autoFlipForColor();
    }

    // Update player cards (active state)
    const whiteCard = document.getElementById('whitePlayerCard');
    const redCard = document.getElementById('redPlayerCard');
    if (whiteCard && redCard) {
        // currentPlayer is 0 for White, 1 for Red
        whiteCard.classList.toggle('active', state.currentPlayer === 0);
        redCard.classList.toggle('active', state.currentPlayer === 1);
    }

    // Update player names and IDs next to board
    const whiteNameEl = document.getElementById('whitePlayerName');
    const redNameEl = document.getElementById('redPlayerName');
    const whiteIdEl = document.getElementById('whitePlayerId');
    const redIdEl = document.getElementById('redPlayerId');

    if (whiteNameEl) {
        const isYou = state.yourColor === 0; // White = 0
        const name = state.whitePlayerName || state.WhitePlayerName || 'White Player';
        const username = state.whiteUsername || state.WhiteUsername || '';
        
        // Make username clickable if it's not "Computer" and we have a username
        if (username && username !== 'Computer') {
            whiteNameEl.innerHTML = `<a href="/profile/${encodeURIComponent(username)}" class="link link-hover" onclick="event.preventDefault(); navigateToProfile('${username}')">${escapeHtml(name)}</a>${isYou ? ' (You)' : ''}`;
        } else {
            whiteNameEl.textContent = name + (isYou ? ' (You)' : '');
        }
    }
    if (whiteIdEl) {
        const whiteId = state.whitePlayerId || state.WhitePlayerId || '-';
        whiteIdEl.textContent = whiteId;
    }

    if (redNameEl) {
        const isYou = state.yourColor === 1; // Red = 1
        const name = state.redPlayerName || state.RedPlayerName || 'Red Player';
        const username = state.redUsername || state.RedUsername || '';
        
        // Make username clickable if it's not "Computer" and we have a username
        if (username && username !== 'Computer') {
            redNameEl.innerHTML = `<a href="/profile/${encodeURIComponent(username)}" class="link link-hover" onclick="event.preventDefault(); navigateToProfile('${username}')">${escapeHtml(name)}</a>${isYou ? ' (You)' : ''}`;
        } else {
            redNameEl.textContent = name + (isYou ? ' (You)' : '');
        }
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
        // setupBoardClickHandler(); // Disabled - using drag-and-drop only
    }

    // Update controls - centralized button state management
    updateAllButtonStates(state);

    // Update match-related UI if this is a match game
    updateMatchUI();

    // Note: Winner detection and game-over handling is done by the GameOver event handler
    // (see setupEventHandlers), not here. updateGameState() only updates UI with current state.
}

/**
 * Centralized button state management - ensures all buttons
 * are in sync with current game state after every update
 */
function updateAllButtonStates(state) {
    if (!state) return;

    const isMyTurn = state.isYourTurn ?? state.IsYourTurn ?? false;
    const hasRemainingMoves = (state.remainingMoves ?? state.RemainingMoves ?? []).length > 0;
    const dice = state.dice ?? state.Dice ?? [];
    const hasDice = dice.length > 0 && (dice[0] > 0 || dice[1] > 0);
    const movesMade = state.movesMadeThisTurn ?? state.MovesMadeThisTurn ?? 0;
    const isWaitingForPlayer = (state.status ?? state.Status ?? 1) === 0;
    const validMoves = state.validMoves ?? state.ValidMoves ?? [];

    // Roll button: can only roll at start of turn (no dice yet)
    const rollBtn = document.getElementById('rollBtn');
    if (rollBtn) {
        rollBtn.disabled = isWaitingForPlayer || !isMyTurn || hasDice;
    }

    // End Turn button: can end if dice rolled AND (no moves left OR no valid moves)
    const endTurnBtn = document.getElementById('endTurnBtn');
    if (endTurnBtn) {
        const allMovesMade = !hasRemainingMoves;
        const noValidMoves = validMoves.length === 0;
        const canEndTurn = isMyTurn && hasDice && (allMovesMade || noValidMoves);

        debug('End Turn button state', {
            isMyTurn, hasDice, hasRemainingMoves,
            validMovesCount: validMoves.length,
            allMovesMade, noValidMoves, canEndTurn
        }, 'trace');

        endTurnBtn.disabled = !canEndTurn;
    }

    // Undo button: can undo if moves made this turn
    const undoBtn = document.getElementById('undoBtn');
    if (undoBtn) {
        undoBtn.disabled = !(isMyTurn && movesMade > 0);
    }

    // Double button: can double before rolling, if you own cube or it's centered
    const doubleBtn = document.getElementById('doubleBtn');
    if (doubleBtn) {
        const doublingCubeOwner = state.doublingCubeOwner ?? state.DoublingCubeOwner;
        const canDouble = isMyTurn && !hasDice && !isWaitingForPlayer &&
                         (doublingCubeOwner == null || doublingCubeOwner === myColor);
        doubleBtn.disabled = !canDouble;
    }
}

function updateMatchUI() {
    // Get match data from MatchController
    const match = typeof window.matchController !== 'undefined'
        ? window.matchController.getCurrentMatch()
        : null;

    const matchScoreContainer = document.querySelector('.match-score');

    if (match && match.targetScore) {
        // Show match score container
        if (matchScoreContainer) {
            matchScoreContainer.style.display = '';
        }

        // Update match length display
        const matchLengthEl = document.getElementById('matchLength');
        if (matchLengthEl) {
            matchLengthEl.textContent = match.targetScore;
        }

        // Update match stake display (doubling cube value or game stakes)
        const matchStakeEl = document.getElementById('matchStake');
        if (matchStakeEl && currentGameState) {
            const cubeValue = currentGameState.doublingCubeValue ?? currentGameState.DoublingCubeValue ?? 1;
            matchStakeEl.textContent = cubeValue;
        }
    } else {
        // Hide match score container for regular games
        if (matchScoreContainer) {
            matchScoreContainer.style.display = 'none';
        }
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
    const endTurnBtn = document.getElementById('endTurnBtn');

    if (rollBtn) rollBtn.disabled = true;
    if (endTurnBtn) endTurnBtn.disabled = true;

    // Reset board flip when resetting UI
    isBoardFlipped = false;
    applyBoardFlip();
}

// ==== AI GAME UI ====
function updateAiGameUI() {
    const chatSidebar = document.getElementById('chatSidebar');

    if (isAiGame) {
        // Hide chat sidebar for AI games
        if (chatSidebar) {
            chatSidebar.style.display = 'none';
        }
    } else {
        // Show chat for human games
        if (chatSidebar) {
            chatSidebar.style.display = '';
        }
    }
}

// ==== BOARD FLIP FEATURE ====

/**
 * Toggle board flip manually
 */
function toggleBoardFlip() {
    isBoardFlipped = !isBoardFlipped;
    applyBoardFlip();
    saveBoardFlipPreference();

    const flipBtn = document.getElementById('flipBoardBtn');
    if (flipBtn) {
        flipBtn.classList.toggle('active', isBoardFlipped);
    }

    log(`Board ${isBoardFlipped ? 'flipped' : 'unflipped'}`, 'info');
}

/**
 * Apply the flip transformation to the board
 */
function applyBoardFlip() {
    const boardSvg = document.getElementById('boardSvg');
    if (boardSvg) {
        if (isBoardFlipped) {
            boardSvg.classList.add('flipped');
        } else {
            boardSvg.classList.remove('flipped');
        }
    }
}

/**
 * Auto-flip board when player is Red (called when color is assigned)
 */
function autoFlipForColor() {
    // Only auto-flip if user hasn't manually toggled
    const manualOverride = sessionStorage.getItem('backgammon_flip_manual_override');

    if (!manualOverride && myColor === 'Red') {
        isBoardFlipped = true;
        applyBoardFlip();

        const flipBtn = document.getElementById('flipBoardBtn');
        if (flipBtn) {
            flipBtn.classList.toggle('active', true);
        }
    }
}

/**
 * Save flip preference to session storage
 */
function saveBoardFlipPreference() {
    // Mark that user has manually toggled (prevents auto-flip from overriding)
    sessionStorage.setItem('backgammon_flip_manual_override', 'true');
    sessionStorage.setItem('backgammon_board_flipped', isBoardFlipped ? '1' : '0');
}

/**
 * Load flip preference from session storage
 */
function loadBoardFlipPreference() {
    const saved = sessionStorage.getItem('backgammon_board_flipped');
    if (saved !== null) {
        isBoardFlipped = saved === '1';
        applyBoardFlip();

        const flipBtn = document.getElementById('flipBoardBtn');
        if (flipBtn) {
            flipBtn.classList.toggle('active', isBoardFlipped);
        }
    }
}

/**
 * Reset flip preference (call when leaving game)
 */
function resetBoardFlipPreference() {
    sessionStorage.removeItem('backgammon_flip_manual_override');
    sessionStorage.removeItem('backgammon_board_flipped');
    isBoardFlipped = false;
    applyBoardFlip();
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
        console.log('[game.js] Initializing BoardSVG');
        BoardSVG.init('boardSvg');

        // Register drag and drop handlers
        console.log('[game.js] Registering drag and drop handlers');
        BoardSVG.setMoveHandlers({
            onSelectChecker: async (point) => {
                console.log(`[game.js] onSelectChecker called for point ${point}`);
                await selectChecker(point);
            },
            onExecuteMove: async (from, to) => {
                console.log(`[game.js] onExecuteMove called: ${from} -> ${to}`);
                await executeMove(from, to);
            },
            getRenderCallback: () => {
                console.log('[game.js] getRenderCallback called');
                return () => {
                    console.log('[game.js] Render callback executing');
                    selectedChecker = null;
                    validDestinations = [];
                    if (currentGameState) {
                        renderBoard(currentGameState);
                    }
                };
            },
            getValidDestinations: (fromPoint) => {
                console.log(`[game.js] getValidDestinations called for point ${fromPoint}`);

                if (!currentGameState || !currentGameState.validMoves) {
                    console.log('[game.js] No validMoves in currentGameState');
                    return [];
                }
                // Filter validMoves to get only moves from this point
                const destinations = currentGameState.validMoves
                    .filter(m => m.from === fromPoint)
                    .map(m => ({ to: m.to, isHit: m.isHit }));
                console.log(`[game.js] Found ${destinations.length} valid destinations:`, destinations);
                return destinations;
            }
        });
        console.log('[game.js] Handlers registered');
    }

    // Get valid source points for highlighting
    let validSources = [];
    console.log('[game.js] Calculating validSources:');
    console.log('  - state.isYourTurn:', state.isYourTurn);
    console.log('  - state.remainingMoves:', state.remainingMoves);
    console.log('  - remainingMoves.length:', state.remainingMoves?.length);
    console.log('  - selectedChecker:', selectedChecker);
    console.log('  - state.validMoves:', state.validMoves);

    if (state.isYourTurn && state.remainingMoves && state.remainingMoves.length > 0 && !selectedChecker) {
        console.log('[game.js] Conditions met for calculating validSources');
        if (state.validMoves && state.validMoves.length > 0) {
            validSources = [...new Set(state.validMoves.map(m => m.from))];
            console.log('[game.js] validSources calculated:', validSources);
        } else {
            console.log('[game.js] No validMoves in state');
        }
    } else {
        console.log('[game.js] Conditions NOT met:', {
            isYourTurn: state.isYourTurn,
            hasRemainingMoves: state.remainingMoves?.length > 0,
            noSelection: !selectedChecker
        });
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

async function handleBoardClick(_event) {
    // DISABLED: Click-to-move functionality removed in favor of drag-and-drop only
    console.log('=== BOARD CLICK (DISABLED) ===');
    return;

    /* OLD CLICK-TO-MOVE CODE - KEEPING FOR REFERENCE
    console.log('=== BOARD CLICK START ===');
    console.log('  - event.target:', event.target);
    console.log('  - event.target.tagName:', event.target?.tagName);
    console.log('  - event.target.classList:', event.target?.classList);
    console.log('  - has draggable class?:', event.target?.classList?.contains('draggable'));

    // Check if we clicked on a draggable checker - if so, let drag handler take over
    if (event.target && event.target.classList && event.target.classList.contains('draggable')) {
        console.log('Clicked on draggable checker, skipping click handler');
        return;
    }

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
    */
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

// ==================== BOT GAMES & SPECTATOR MODE ====================

/**
 * Load and display active bot games
 */
async function loadBotGames() {
    try {
        const response = await fetch(`${apiBaseUrl}/api/bot-games`);
        const botGames = await response.json();

        const botGamesList = document.getElementById('botGamesList');
        if (!botGamesList) return;

        if (botGames.length === 0) {
            botGamesList.innerHTML = '<p class="text-base-content/60 text-center py-4 italic">No bot games running</p>';
            return;
        }

        botGamesList.innerHTML = botGames.map(game => `
            <div class="card bg-base-200 shadow-sm hover:shadow-md transition-shadow">
                <div class="card-body p-4">
                    <div class="flex items-center justify-between">
                        <div class="flex-1">
                            <div class="font-semibold text-base mb-1">
                                ${game.whitePlayer} vs ${game.redPlayer}
                            </div>
                            <div class="text-sm text-base-content/60">
                                ${game.currentPlayer} to move • Pips: ${game.whitePipCount} - ${game.redPipCount}
                                ${game.spectatorCount > 0 ? ` • 👁 ${game.spectatorCount} watching` : ''}
                            </div>
                        </div>
                        <button onclick="spectateGame('${game.gameId}')"
                                class="btn btn-primary btn-sm gap-1">
                            👀 Watch
                        </button>
                    </div>
                </div>
            </div>
        `).join('');

        debug('Loaded bot games', { count: botGames.length }, 'info');
    } catch (err) {
        debug('Failed to load bot games', err, 'error');
        // Hide section on error
        const section = document.getElementById('botGamesSection');
        if (section) section.style.display = 'none';
    }
}

/**
 * Spectate a game by ID
 */
async function spectateGame(gameId) {
    debug('Spectating game', { gameId }, 'info');

    // Navigate to game URL
    setGameUrl(gameId);
    showGamePage();

    // Wait for connection
    const isConnected = await waitForConnection();
    if (!isConnected) {
        log('Failed to connect to server', 'error');
        showLandingPage();
        return;
    }

    // Join game (will be added as spectator if full)
    try {
        await joinSpecificGame(gameId);
    } catch (err) {
        log('Failed to join game as spectator', 'error');
        showLandingPage();
    }
}

// Load bot games on page load and refresh every 5 seconds
if (typeof window !== 'undefined') {
    window.addEventListener('DOMContentLoaded', async () => {
        await autoConnect();  // Wait for config to load first
        loadBotGames();       // Now apiBaseUrl is set correctly
        setInterval(loadBotGames, 5000);
    });
}
