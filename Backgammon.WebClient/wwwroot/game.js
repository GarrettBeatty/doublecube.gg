// ==== STATE ====
let connection = null;
let myColor = null;
let currentGameId = null;
let gameRefreshInterval = null;

// ==== INITIALIZATION ====
window.addEventListener('load', () => {
    showLandingPage();
    autoConnect();
});

async function autoConnect() {
    const serverUrl = document.getElementById('serverUrl').value;
    
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
        refreshGamesList();
        gameRefreshInterval = setInterval(refreshGamesList, 3000);
    } catch (err) {
        updateConnectionStatus(false);
        log(`❌ Connection failed: ${err}`, 'error');
        setTimeout(autoConnect, 5000);
    }
}

function setupEventHandlers() {
    connection.on("GameUpdate", (gameState) => {
        updateGameState(gameState);
    });

    connection.on("GameStart", (gameState) => {
        log('🎮 Game started! Both players connected.', 'success');
        updateGameState(gameState);
    });

    connection.on("WaitingForOpponent", (gameId) => {
        log(`⏳ Waiting for opponent... Game ID: ${gameId}`, 'info');
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
        const response = await fetch('http://localhost:5000/stats');
        const data = await response.json();
        
        // For now, show placeholder - in future we'd get actual game list from server
        const gamesListEl = document.getElementById('gamesList');
        if (data.totalGames === 0) {
            gamesListEl.innerHTML = '<div class="loading-games">No active games. Create one to get started!</div>';
        } else {
            gamesListEl.innerHTML = `
                <div class="loading-games">
                    ${data.activeGames} active game(s) • ${data.waitingGames} waiting for players
                </div>
            `;
        }
    } catch (err) {
        // Server might not have stats endpoint ready
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
        await connection.invoke("JoinGame", null);
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
        await connection.invoke("JoinGame", null);
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
        await connection.invoke("JoinGame", gameId);
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
function updateGameState(state) {
    console.log('Game State:', state);

    // Update game ID
    const gameIdEl = document.getElementById('gameIdDisplay');
    if (gameIdEl) {
        gameIdEl.textContent = state.gameId || '-';
        currentGameId = state.gameId;
    }

    // Update turn indicator
    const turnEl = document.getElementById('turnIndicator');
    if (turnEl) {
        const isMyTurn = state.isYourTurn;
        turnEl.textContent = isMyTurn ? "Your Turn!" : `${state.currentPlayer}'s Turn`;
        turnEl.style.fontWeight = isMyTurn ? '700' : '400';
    }

    // Store player color
    myColor = state.yourColor;

    // Update player badges
    const whiteBadge = document.getElementById('whitePlayerBadge');
    const redBadge = document.getElementById('redPlayerBadge');
    if (whiteBadge && redBadge) {
        whiteBadge.classList.toggle('active', state.currentPlayer === 'White');
        redBadge.classList.toggle('active', state.currentPlayer === 'Red');
    }

    // Update dice
    const diceDisplay = document.getElementById('diceDisplay');
    if (diceDisplay && state.currentDice && state.currentDice.length > 0) {
        diceDisplay.innerHTML = state.currentDice
            .map(die => `<div class="die">${die}</div>`)
            .join('');
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
    }

    // Update controls
    const rollBtn = document.getElementById('rollBtn');
    if (rollBtn) {
        const isMyTurn = state.isYourTurn;
        const hasRemainingMoves = state.remainingMoves && state.remainingMoves.length > 0;
        rollBtn.disabled = !isMyTurn || hasRemainingMoves;
    }

    // Check for winner
    if (state.winner) {
        log(`🏆 Game Over! ${state.winner} wins!`, 'success');
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
    if (rollBtn) rollBtn.disabled = true;
}

// ==== BOARD RENDERING ====
function renderBoard(state) {
    const canvas = document.getElementById('boardCanvas');
    const placeholder = document.getElementById('boardPlaceholder');
    
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
    
    // Set canvas size
    const containerWidth = canvas.parentElement.clientWidth;
    const containerHeight = canvas.parentElement.clientHeight;
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
    const checkerRadius = pointWidth * 0.38;
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
