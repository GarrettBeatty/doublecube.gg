// ==== STATE ====
let connection = null;
let myColor = null;
let currentGameId = null;
let gameRefreshInterval = null;
let currentGameState = null;
let selectedChecker = null; // { point: number, x: number, y: number }
let validDestinations = [];
let myPlayerId = null;  // Persistent player ID

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
    myPlayerId = getOrCreatePlayerId();
    
    // Check if URL contains a game ID
    const urlGameId = getGameIdFromUrl();
    if (urlGameId) {
        // Wait for connection before joining
        await autoConnect();
        await joinSpecificGame(urlGameId);
    } else {
        showLandingPage();
        await autoConnect();
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
        log(`📡 Using SignalR URL: ${serverUrl}`, 'info');
    } catch (error) {
        // Fallback to hardcoded URL if config endpoint fails
        serverUrl = document.getElementById('serverUrl').value;
        log(`⚠️ Using fallback URL: ${serverUrl}`, 'warning');
    }
    
    connection = new signalR.HubConnectionBuilder()
        .withUrl(serverUrl)
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    setupEventHandlers();

    try {
        await connection.start();
        updateConnectionStatus(true);
        log('✅ Connected to server', 'success');
        
        // Always start on landing page and refresh games list
        // User can manually rejoin games from "My Games" section
        refreshGamesList();
        gameRefreshInterval = setInterval(refreshGamesList, 3000);
    } catch (err) {
        updateConnectionStatus(false);
        log(`❌ Connection failed: ${err}`, 'error');
        setTimeout(autoConnect, 5000);
    }
}

function setupEventHandlers() {
        connection.on("SpectatorJoined", (gameState) => {
            log('👀 You are spectating this game.', 'info');
            updateGameState(gameState, true); // pass spectator flag
            showGamePage();
            window.isSpectator = true;
        });
    connection.on("GameUpdate", (gameState) => {
        updateGameState(gameState);
    });

    connection.on("GameStart", (gameState) => {
        log('🎮 Game started! Both players connected.', 'success');
        updateGameState(gameState);
    });

    connection.on("WaitingForOpponent", (gameId) => {
        log(`⏳ Waiting for opponent... Game ID: ${gameId}`, 'info');
        currentGameId = gameId;
        localStorage.setItem('currentGameId', gameId);
        setGameUrl(gameId);
    });

    connection.on("OpponentJoined", (opponentId) => {
        log(`👋 Opponent joined`, 'success');
    });

    connection.on("OpponentLeft", () => {
        log('👋 Opponent left the game', 'warning');
    });

    connection.on("Error", (errorMessage) => {
        log(`❌ Error: ${errorMessage}`, 'error');
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
    const dot = document.getElementById('connectionIndicator');
    dot.className = `connection-dot ${isConnected ? 'online' : 'offline'}`;
}

// ==== GAME LIST ====
async function refreshGamesList() {
    if (!connection || connection.state !== 'Connected') return;
    
    try {
        // Fetch all games list
        const response = await fetch('http://localhost:5000/api/games');
        const data = await response.json();
        
        const gamesListEl = document.getElementById('gamesList');
        
        if (data.waitingGames.length === 0 && data.activeGames.length === 0) {
            gamesListEl.innerHTML = '<div class="loading-games">No games available. Create one to get started!</div>';
        } else {
            let html = '<div class="games-stats">' +
                `${data.activeGames.length} active game(s) • ${data.waitingGames.length} waiting for players` +
                '</div>';
            
            // Show waiting games (available to join)
            if (data.waitingGames.length > 0) {
                html += '<div class="game-section"><h3>⏳ Waiting for Opponent</h3>';
                data.waitingGames.forEach(game => {
                    const waitTime = game.minutesWaiting < 1 ? 'just now' : 
                                    game.minutesWaiting === 1 ? '1 min ago' : 
                                    `${game.minutesWaiting} mins ago`;
                    html += `
                        <div class="game-item waiting">
                            <div class="game-item-info">
                                <div class="game-item-player">👤 ${game.playerName}</div>
                                <div class="game-item-time">Created ${waitTime}</div>
                            </div>
                            <button class="btn-join" onclick="joinSpecificGame('${game.gameId}')">Join Game</button>
                        </div>
                    `;
                });
                html += '</div>';
            }
            
            // Show active games (spectate only - future feature)
            if (data.activeGames.length > 0) {
                html += '<div class="game-section"><h3>🎮 Games in Progress</h3>';
                data.activeGames.forEach(game => {
                    html += `
                        <div class="game-item active">
                            <div class="game-item-info">
                                <div class="game-item-players">⚪ ${game.whitePlayer} vs 🔴 ${game.redPlayer}</div>
                            </div>
                        </div>
                    `;
                });
                html += '</div>';
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
        const response = await fetch(`http://localhost:5000/api/player/${myPlayerId}/active-games`);
        const myGames = await response.json();
        
        const myGamesListEl = document.getElementById('myGamesList');
        
        if (myGames.length === 0) {
            myGamesListEl.innerHTML = '<div class="loading-games">You have no active games</div>';
            return;
        }
        
        let html = '';
        myGames.forEach(game => {
            const turnIndicator = game.isMyTurn ? '🟢 Your Turn' : '⏳ Waiting';
            const opponentStatus = game.isFull ? game.opponent : 'Waiting for opponent';
            const timeAgo = getTimeAgo(new Date(game.lastActivity));
            
            html += `
                <div class="game-item my-game ${game.isMyTurn ? 'my-turn' : ''}">
                    <div class="game-item-info">
                        <div class="game-item-player">
                            ${game.myColor === 'White' ? '⚪' : '🔴'} You vs ${opponentStatus}
                        </div>
                        <div class="game-item-status">
                            <span class="turn-badge">${turnIndicator}</span>
                            <span class="game-item-time">Active ${timeAgo}</span>
                        </div>
                    </div>
                    <button class="btn-join ${game.isMyTurn ? 'btn-join-urgent' : ''}" onclick="joinSpecificGame('${game.gameId}')">
                        ${game.isMyTurn ? '▶️ Play' : '👁️ View'}
                    </button>
                </div>
            `;
        });
        
        myGamesListEl.innerHTML = html;
    } catch (err) {
        console.error('Failed to fetch my games:', err);
        document.getElementById('myGamesList').innerHTML = '<div class="loading-games">Error loading your games</div>';
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
    if (!connection || connection.state !== 'Connected') {
        alert('Not connected to server. Please wait...');
        return;
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

async function quickMatch() {
    if (!connection || connection.state !== 'Connected') {
        alert('Not connected to server. Please wait...');
        return;
    }

    try {
        currentGameId = null;
        localStorage.removeItem('currentGameId'); // Clear any old game
        await connection.invoke("JoinGame", myPlayerId, null);
        log('🎯 Finding match...', 'info');
        showGamePage();
    } catch (err) {
        log(`❌ Failed to find match: ${err}`, 'error');
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
    try {
        await connection.invoke("RollDice");
        log('🎲 Rolling dice...', 'info');
    } catch (err) {
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

// ==== CHAT ====
function handleChatKeyPress(event) {
    if (event.key === 'Enter') {
        sendChat();
    }
}

function sendChat() {
    const input = document.getElementById('chatInput');
    const message = input.value.trim();
    
    if (!message) return;
    
    // For now, just add to UI - in future would send via SignalR
    addChatMessage('You', message, true);
    input.value = '';
    
    // TODO: Implement SignalR chat message sending
    // await connection.invoke("SendChatMessage", message);
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

// ==== GAME STATE ====
function updateGameState(state, isSpectator = false) {
    console.log('Game State:', state);
    console.log('Player Names - White:', state.whitePlayerName, 'Red:', state.redPlayerName);

    currentGameState = state; // Store for click handling
    // Spectator mode UI
    if (isSpectator || window.isSpectator) {
        document.body.classList.add('spectator-mode');
        // Optionally disable move controls here
    } else {
        document.body.classList.remove('spectator-mode');
    }

    // Update game ID
    const gameIdEl = document.getElementById('gameIdDisplay');
    if (gameIdEl) {
        gameIdEl.textContent = state.gameId || '-';
        currentGameId = state.gameId;
        // Save to localStorage for reconnection
        if (state.gameId) {
            localStorage.setItem('currentGameId', state.gameId);
        }
    }

    // Update turn indicator
    const turnEl = document.getElementById('turnIndicator');
    if (turnEl) {
        const isMyTurn = state.isYourTurn;
        const currentPlayerName = state.currentPlayer === 0 ? 'White' : 'Red';
        turnEl.textContent = isMyTurn ? "Your Turn!" : `${currentPlayerName}'s Turn`;
        turnEl.style.fontWeight = isMyTurn ? '700' : '400';
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

    // Update player names next to board
    const whiteNameEl = document.getElementById('whitePlayerName');
    const redNameEl = document.getElementById('redPlayerName');
    if (whiteNameEl) {
        const isYou = state.yourColor === 0; // White = 0
        const name = state.whitePlayerName || state.WhitePlayerName || 'White Player';
        whiteNameEl.textContent = name + (isYou ? ' (You)' : '');
    }
    if (redNameEl) {
        const isYou = state.yourColor === 1; // Red = 1
        const name = state.redPlayerName || state.RedPlayerName || 'Red Player';
        redNameEl.textContent = name + (isYou ? ' (You)' : '');
    }

    // Update dice (use currentDice alias if available, otherwise dice)
    const diceData = state.currentDice || state.dice;
    const diceDisplay = document.getElementById('diceDisplay');
    if (diceDisplay && diceData && diceData.length > 0) {
        // Show - for unrolled dice (0 values)
        const hasRolled = diceData.some(die => die > 0);
        if (hasRolled) {
            diceDisplay.innerHTML = diceData
                .map(die => `<div class="die">${die}</div>`)
                .join('');
        } else {
            diceDisplay.innerHTML = '<div class="die">-</div><div class="die">-</div>';
        }
    }

    // Update remaining moves
    const remainingMovesDisplay = document.getElementById('remainingMoves');
    if (remainingMovesDisplay) {
        if (state.remainingMoves && state.remainingMoves.length > 0) {
            remainingMovesDisplay.innerHTML = state.remainingMoves
                .map(move => `<div class="die">${move}</div>`)
                .join('');
        } else {
            remainingMovesDisplay.innerHTML = '<div class="die">-</div>';
        }
    }

    // Render board
    if (state.board) {
        renderBoard(state);
        setupBoardClickHandler(); // Enable click interactions
    }

    // Update controls
    const isMyTurn = state.isYourTurn;
    const hasRemainingMoves = state.remainingMoves && state.remainingMoves.length > 0;
    const hasDice = state.dice && state.dice.length > 0 && (state.dice[0] > 0 || state.dice[1] > 0);
    const movesMade = state.movesMadeThisTurn || 0;
    const isWaitingForPlayer = state.status === 0; // GameStatus.WaitingForPlayer = 0
    
    const rollBtn = document.getElementById('rollBtn');
    if (rollBtn) {
        // Can only roll at the START of your turn (no dice rolled yet)
        // Once you roll, you must end turn before rolling again
        // Also disable if waiting for another player to join
        rollBtn.disabled = isWaitingForPlayer || !isMyTurn || hasDice;
    }
    
    const undoBtn = document.getElementById('undoBtn');
    if (undoBtn) {
        // Can undo only if you've made at least one move this turn
        // Check movesMadeThisTurn or fallback to comparing dice vs remaining
        const movesMadeFromState = state.movesMadeThisTurn || 0;
        const totalDice = state.dice && state.dice[0] === state.dice[1] ? 4 : 2;
        const remainingCount = state.remainingMoves ? state.remainingMoves.length : 0;
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
        const noValidMoves = !state.validMoves || state.validMoves.length === 0;
        const canEndTurn = isMyTurn && hasDice && (allMovesMade || noValidMoves);
        endTurnBtn.disabled = !canEndTurn;
    }

    // Check for winner
    if (state.winner) {
        log(`🏆 Game Over! ${state.winner} wins!`, 'success');
        localStorage.removeItem('currentGameId'); // Clear completed game
        setTimeout(() => {
            if (confirm(`Game Over! ${state.winner} wins! Return to lobby?`)) {
                leaveGameAndReturn();
            }
        }, 2000);
    }
}

function resetGameUI() {
    const gameIdEl = document.getElementById('gameIdDisplay');
    const turnEl = document.getElementById('turnIndicator');
    const diceDisplay = document.getElementById('diceDisplay');
    const remainingMovesDisplay = document.getElementById('remainingMoves');
    const canvas = document.getElementById('boardCanvas');
    const placeholder = document.getElementById('boardPlaceholder');

    if (gameIdEl) gameIdEl.textContent = '-';
    if (turnEl) turnEl.textContent = '-';
    if (diceDisplay) diceDisplay.innerHTML = '<div class="die">?</div>';
    if (remainingMovesDisplay) remainingMovesDisplay.innerHTML = '<div class="die">-</div>';
    if (canvas) canvas.style.display = 'none';
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

// ==== BOARD RENDERING ====
function renderBoard(state) {
    const canvas = document.getElementById('boardCanvas');
    const placeholder = document.getElementById('boardPlaceholder');
    
    if (!canvas || !placeholder) {
        console.error('Canvas or placeholder element not found');
        return;
    }
    
    if (!state.board || state.board.length === 0) {
        placeholder.style.display = 'block';
        canvas.style.display = 'none';
        return;
    }
    
    // Debug: log board state
    console.log('Board data:', state.board);
    console.log('White on bar:', state.whiteCheckersOnBar, 'Red on bar:', state.redCheckersOnBar);
    console.log('White born off:', state.whiteBornOff, 'Red born off:', state.redBornOff);
    
    placeholder.style.display = 'none';
    canvas.style.display = 'block';
    
    // Set canvas size - wait for layout if needed
    const containerWidth = canvas.parentElement.clientWidth;
    const containerHeight = canvas.parentElement.clientHeight;
    
    if (!containerWidth || !containerHeight || containerWidth <= 0 || containerHeight <= 0) {
        console.warn('Container not ready, waiting for layout...', { containerWidth, containerHeight });
        // Wait for next animation frame and try again
        requestAnimationFrame(() => {
            requestAnimationFrame(() => renderBoard(state));
        });
        return;
    }
    const aspectRatio = 2;
    
    let width, height;
    if (containerWidth / containerHeight > aspectRatio) {
        height = containerHeight;
        width = height * aspectRatio;
    } else {
        width = containerWidth;
        height = width / aspectRatio;
    }
    
    canvas.width = width;
    canvas.height = height;
    
    const ctx = canvas.getContext('2d');
    
    // Colors
    const boardBg = '#8b4513';
    const pointLight = '#d2b48c';
    const pointDark = '#654321';
    const barColor = '#654321';
    const whiteChecker = '#ffffff';
    const redChecker = '#8b0000';
    
    // Dimensions
    const barWidth = width * 0.08;
    const sideMargin = width * 0.03;
    const playableWidth = width - (2 * sideMargin) - barWidth;
    const pointWidth = playableWidth / 12;
    const pointHeight = height * 0.42;
    const checkerRadius = pointWidth * 0.32;
    const padding = height * 0.03;
    
    // Clear canvas
    ctx.fillStyle = boardBg;
    ctx.fillRect(0, 0, width, height);
    
    // Draw border
    ctx.strokeStyle = '#2d1810';
    ctx.lineWidth = 4;
    ctx.strokeRect(0, 0, width, height);
    
    // Draw bar
    ctx.fillStyle = barColor;
    const barX = sideMargin + (6 * pointWidth);
    ctx.fillRect(barX, 0, barWidth, height);
    
    // Draw points (triangles)
    // Standard backgammon layout:
    // Bottom row (white's perspective): 12,11,10,9,8,7 | BAR | 6,5,4,3,2,1
    // Top row (red's perspective):    13,14,15,16,17,18 | BAR | 19,20,21,22,23,24
    for (let pointNum = 1; pointNum <= 24; pointNum++) {
        const isTop = pointNum >= 13;
        let positionIndex;
        
        if (isTop) {
            // Top row: 13-18 on left, 19-24 on right
            if (pointNum >= 13 && pointNum <= 18) {
                positionIndex = pointNum - 13; // 13->0, 14->1, ..., 18->5
            } else {
                positionIndex = pointNum - 19 + 6; // 19->6, 20->7, ..., 24->11
            }
        } else {
            // Bottom row: 12-7 on left, 6-1 on right  
            if (pointNum >= 7 && pointNum <= 12) {
                positionIndex = 12 - pointNum; // 12->0, 11->1, ..., 7->5
            } else {
                positionIndex = 6 - pointNum + 6; // 6->6, 5->7, ..., 1->11
            }
        }
        
        // Calculate x position (add bar width after first 6 points)
        const x = sideMargin + (positionIndex < 6 ? positionIndex : positionIndex + (barWidth / pointWidth)) * pointWidth;
        const y = isTop ? padding : height - padding;
        const direction = isTop ? 1 : -1;
        
        // Draw triangle
        ctx.fillStyle = (pointNum % 2 === 0) ? pointDark : pointLight;
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.lineTo(x + pointWidth, y);
        ctx.lineTo(x + pointWidth / 2, y + (direction * pointHeight));
        ctx.closePath();
        ctx.fill();
        
        // Draw point number
        ctx.fillStyle = 'rgba(255, 255, 255, 0.3)';
        ctx.font = `${pointWidth * 0.3}px Arial`;
        ctx.textAlign = 'center';
        ctx.fillText(pointNum, x + pointWidth / 2, y + (direction * pointHeight * 0.2));
    }
    
    // Highlight valid source points (checkers that can be moved)
    if (state.isYourTurn && state.remainingMoves && state.remainingMoves.length > 0 && !selectedChecker) {
        const validSources = new Set(state.validMoves.map(m => m.from));
        
        validSources.forEach(pointNum => {
            if (pointNum === 0) return; // Bar handled separately
            
            const isTop = pointNum >= 13;
            let positionIndex;
            
            if (isTop) {
                if (pointNum >= 13 && pointNum <= 18) {
                    positionIndex = pointNum - 13;
                } else {
                    positionIndex = pointNum - 19 + 6;
                }
            } else {
                if (pointNum >= 7 && pointNum <= 12) {
                    positionIndex = 12 - pointNum;
                } else {
                    positionIndex = 6 - pointNum + 6;
                }
            }
            
            const x = sideMargin + (positionIndex < 6 ? positionIndex : positionIndex + (barWidth / pointWidth)) * pointWidth;
            const y = isTop ? padding : height - padding;
            const direction = isTop ? 1 : -1;
            
            // Draw glow around valid checker
            ctx.fillStyle = 'rgba(255, 255, 0, 0.3)';
            ctx.beginPath();
            ctx.moveTo(x, y);
            ctx.lineTo(x + pointWidth, y);
            ctx.lineTo(x + pointWidth / 2, y + (direction * pointHeight));
            ctx.closePath();
            ctx.fill();
        });
    }
    
    // Highlight selected checker
    if (selectedChecker !== null) {
        const pointNum = selectedChecker.point;
        if (pointNum !== 0) {
            const isTop = pointNum >= 13;
            let positionIndex;
            
            if (isTop) {
                if (pointNum >= 13 && pointNum <= 18) {
                    positionIndex = pointNum - 13;
                } else {
                    positionIndex = pointNum - 19 + 6;
                }
            } else {
                if (pointNum >= 7 && pointNum <= 12) {
                    positionIndex = 12 - pointNum;
                } else {
                    positionIndex = 6 - pointNum + 6;
                }
            }
            
            const x = sideMargin + (positionIndex < 6 ? positionIndex : positionIndex + (barWidth / pointWidth)) * pointWidth;
            const y = isTop ? padding : height - padding;
            const direction = isTop ? 1 : -1;
            
            // Draw bright highlight for selected
            ctx.fillStyle = 'rgba(0, 255, 0, 0.4)';
            ctx.beginPath();
            ctx.moveTo(x, y);
            ctx.lineTo(x + pointWidth, y);
            ctx.lineTo(x + pointWidth / 2, y + (direction * pointHeight));
            ctx.closePath();
            ctx.fill();
        }
    }
    
    // Highlight valid destinations
    if (validDestinations.length > 0) {
        validDestinations.forEach(move => {
            const pointNum = move.to;
            if (pointNum === 0 || pointNum === 25) return; // Skip bar/bear-off for now
            
            const isTop = pointNum >= 13;
            let positionIndex;
            
            if (isTop) {
                if (pointNum >= 13 && pointNum <= 18) {
                    positionIndex = pointNum - 13;
                } else {
                    positionIndex = pointNum - 19 + 6;
                }
            } else {
                if (pointNum >= 7 && pointNum <= 12) {
                    positionIndex = 12 - pointNum;
                } else {
                    positionIndex = 6 - pointNum + 6;
                }
            }
            
            const x = sideMargin + (positionIndex < 6 ? positionIndex : positionIndex + (barWidth / pointWidth)) * pointWidth;
            const y = isTop ? padding : height - padding;
            const direction = isTop ? 1 : -1;
            
            // Draw blue highlight for valid destinations
            ctx.fillStyle = move.isHit ? 'rgba(255, 0, 0, 0.4)' : 'rgba(0, 150, 255, 0.4)';
            ctx.beginPath();
            ctx.moveTo(x, y);
            ctx.lineTo(x + pointWidth, y);
            ctx.lineTo(x + pointWidth / 2, y + (direction * pointHeight));
            ctx.closePath();
            ctx.fill();
        });
    }
    
    // Draw checkers on points
    state.board.forEach(point => {
        if (point.count > 0 && point.position >= 1 && point.position <= 24) {
            const pointNum = point.position;
            const isTop = pointNum >= 13;
            let positionIndex;
            
            if (isTop) {
                if (pointNum >= 13 && pointNum <= 18) {
                    positionIndex = pointNum - 13;
                } else {
                    positionIndex = pointNum - 19 + 6;
                }
            } else {
                if (pointNum >= 7 && pointNum <= 12) {
                    positionIndex = 12 - pointNum;
                } else {
                    positionIndex = 6 - pointNum + 6;
                }
            }
            
            const x = sideMargin + (positionIndex < 6 ? positionIndex : positionIndex + (barWidth / pointWidth)) * pointWidth;
            const centerX = x + pointWidth / 2;
            const baseY = isTop ? padding : height - padding;
            const direction = isTop ? 1 : -1;
            
            console.log(`Point ${pointNum}: ${point.count} ${point.color} checkers at position index ${positionIndex}`);
            
            // Color is sent as enum value: 0 = White, 1 = Red
            const color = point.color === 0 ? whiteChecker : redChecker;
            
            // Draw up to 5 checkers visually, show count if more
            const displayCount = Math.min(point.count, 5);
            for (let j = 0; j < displayCount; j++) {
                const checkerY = baseY + (direction * (checkerRadius * 0.8 + j * checkerRadius * 1.9));
                drawChecker(ctx, centerX, checkerY, checkerRadius, color);
            }
            
            // Show count if more than 5
            if (point.count > 5) {
                const textY = baseY + (direction * (checkerRadius * 1.1 + 4 * checkerRadius * 2));
                ctx.fillStyle = color === whiteChecker ? '#000' : '#fff';
                ctx.font = `bold ${checkerRadius * 1.2}px Arial`;
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillText(point.count, centerX, textY);
            }
        }
    });
    
    // Draw checkers on bar
    if (state.whiteCheckersOnBar > 0) {
        drawBarCheckers(ctx, barX, barWidth, height, checkerRadius, whiteChecker, state.whiteCheckersOnBar, true);
    }
    if (state.redCheckersOnBar > 0) {
        drawBarCheckers(ctx, barX, barWidth, height, checkerRadius, redChecker, state.redCheckersOnBar, false);
    }
    
    // Draw borne off checkers
    const bornOffX = width - sideMargin / 2;
    if (state.whiteBornOff > 0) {
        drawBornOff(ctx, bornOffX, height - padding, checkerRadius, whiteChecker, state.whiteBornOff);
    }
    if (state.redBornOff > 0) {
        drawBornOff(ctx, bornOffX, padding, checkerRadius, redChecker, state.redBornOff);
    }
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
    const canvas = document.getElementById('boardCanvas');
    if (!canvas || boardClickHandlerSetup) return;
    
    canvas.addEventListener('click', handleBoardClick);
    boardClickHandlerSetup = true;
}

async function handleBoardClick(event) {
    if (!currentGameState || !currentGameState.isYourTurn) return;
    if (currentGameState.remainingMoves.length === 0) return;
    
    const canvas = event.target;
    const rect = canvas.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    
    // Scale to canvas coordinates
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;
    const canvasX = x * scaleX;
    const canvasY = y * scaleY;
    
    const clickedPoint = getPointAtPosition(canvasX, canvasY, canvas.width, canvas.height);
    
    if (clickedPoint === null) {
        // Clicked outside any point - deselect
        selectedChecker = null;
        validDestinations = [];
        renderBoard(currentGameState);
        return;
    }
    
    // If we have a selection and clicked a valid destination
    if (selectedChecker !== null && validDestinations.some(m => m.to === clickedPoint)) {
        await executeMove(selectedChecker.point, clickedPoint);
        selectedChecker = null;
        validDestinations = [];
        return;
    }
    
    // Try to select this checker
    await selectChecker(clickedPoint);
}

async function selectChecker(point) {
    try {
        // Check if this point has our checkers
        const pointData = currentGameState.board.find(p => p.position === point);
        const myColorValue = myColor === 'White' ? 0 : 1;
        
        // Special case: check bar
        if (point === 0) {
            const onBar = myColor === 'White' ? currentGameState.whiteCheckersOnBar : currentGameState.redCheckersOnBar;
            if (onBar === 0) {
                selectedChecker = null;
                validDestinations = [];
                renderBoard(currentGameState);
                return;
            }
        } else if (!pointData || pointData.color !== myColorValue || pointData.count === 0) {
            selectedChecker = null;
            validDestinations = [];
            renderBoard(currentGameState);
            return;
        }
        
        // Get valid destinations from this point
        const destinations = await connection.invoke("GetValidDestinations", point);
        
        if (destinations.length === 0) {
            log(`No valid moves from point ${point}`, 'info');
            selectedChecker = null;
            validDestinations = [];
            renderBoard(currentGameState);
            return;
        }
        
        selectedChecker = { point };
        validDestinations = destinations;
        log(`Selected checker at point ${point}, ${destinations.length} valid move(s)`, 'info');
        renderBoard(currentGameState);
    } catch (err) {
        console.error('Error selecting checker:', err);
    }
}

async function executeMove(from, to) {
    try {
        await connection.invoke("MakeMove", from, to);
        log(`Moved checker from ${from} to ${to}`, 'success');
    } catch (err) {
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
