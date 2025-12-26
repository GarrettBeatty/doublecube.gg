let connection = null;
let myColor = null;

function updateStatus(message, type = 'disconnected') {
    const statusEl = document.getElementById('status');
    statusEl.textContent = message;
    statusEl.className = `connection-status status-${type}`;
}

function log(message, type = 'info') {
    const logEl = document.getElementById('log');
    const timestamp = new Date().toLocaleTimeString();
    const entry = document.createElement('div');
    entry.className = `log-entry log-${type}`;
    entry.innerHTML = `<span class="timestamp">[${timestamp}]</span>${message}`;
    logEl.appendChild(entry);
    logEl.scrollTop = logEl.scrollHeight;
}

function clearLog() {
    document.getElementById('log').innerHTML = '';
}

async function connect() {
    if (connection?.state === 'Connected') {
        log('Already connected', 'warning');
        return;
    }

    const serverUrl = document.getElementById('serverUrl').value;
    if (!serverUrl) {
        log('❌ Please enter server URL', 'error');
        return;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl(serverUrl)
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Setup event handlers
    connection.on("GameUpdate", (gameState) => {
        log('📥 Game state updated', 'info');
        updateGameState(gameState);
    });

    connection.on("GameStart", (gameState) => {
        log('🎮 Game started! Both players connected.', 'success');
        updateGameState(gameState);
        document.getElementById('rollBtn').disabled = false;
    });

    connection.on("WaitingForOpponent", (gameId) => {
        log(`⏳ Waiting for opponent... Game ID: ${gameId}`, 'warning');
        updateStatus(`Waiting for opponent (Game: ${gameId})`, 'waiting');
    });

    connection.on("OpponentJoined", (opponentId) => {
        log(`👋 Opponent joined: ${opponentId}`, 'success');
    });

    connection.on("OpponentLeft", () => {
        log('👋 Opponent left the game', 'warning');
        updateStatus('Opponent disconnected', 'waiting');
    });

    connection.on("Error", (errorMessage) => {
        log(`❌ Error: ${errorMessage}`, 'error');
    });

    connection.onreconnecting(() => {
        log('🔄 Reconnecting...', 'warning');
        updateStatus('Reconnecting...', 'waiting');
    });

    connection.onreconnected(() => {
        log('✅ Reconnected', 'success');
        updateStatus('Connected', 'connected');
    });

    connection.onclose(() => {
        log('🔌 Connection closed', 'warning');
        updateStatus('Disconnected', 'disconnected');
        resetUI();
    });

    try {
        await connection.start();
        log('✅ Connected to server', 'success');
        updateStatus('Connected - Ready to join game', 'connected');
        
        document.getElementById('connectBtn').disabled = true;
        document.getElementById('disconnectBtn').disabled = false;
        document.getElementById('joinBtn').disabled = false;
    } catch (err) {
        log(`❌ Connection failed: ${err}`, 'error');
        updateStatus('Connection failed', 'disconnected');
    }
}

async function disconnect() {
    if (connection) {
        await connection.stop();
        connection = null;
        log('🔌 Disconnected from server', 'info');
        updateStatus('Disconnected', 'disconnected');
        
        document.getElementById('connectBtn').disabled = false;
        document.getElementById('disconnectBtn').disabled = true;
        document.getElementById('joinBtn').disabled = true;
        resetUI();
    }
}

async function joinGame() {
    if (!connection || connection.state !== 'Connected') {
        log('❌ Not connected to server', 'error');
        return;
    }

    const gameId = document.getElementById('gameIdInput').value || null;

    try {
        await connection.invoke("JoinGame", gameId);
        log(gameId ? `🎮 Joining game: ${gameId}` : '🎮 Creating/joining game...', 'info');
        document.getElementById('joinBtn').disabled = true;
        document.getElementById('leaveBtn').disabled = false;
    } catch (err) {
        log(`❌ Failed to join game: ${err}`, 'error');
    }
}

async function leaveGame() {
    try {
        await connection.invoke("LeaveGame");
        log('👋 Leaving game...', 'info');
        resetUI();
        document.getElementById('joinBtn').disabled = false;
        document.getElementById('leaveBtn').disabled = true;
    } catch (err) {
        log(`❌ Failed to leave game: ${err}`, 'error');
    }
}

async function rollDice() {
    try {
        await connection.invoke("RollDice");
        log('🎲 Rolling dice...', 'info');
    } catch (err) {
        log(`❌ Failed to roll dice: ${err}`, 'error');
    }
}

async function makeMove() {
    const fromPoint = parseInt(document.getElementById('fromPoint').value);
    const toPoint = parseInt(document.getElementById('toPoint').value);

    if (isNaN(fromPoint) || isNaN(toPoint)) {
        log('❌ Please enter valid point numbers', 'error');
        return;
    }

    try {
        await connection.invoke("MakeMove", fromPoint, toPoint);
        log(`📍 Move: ${fromPoint} → ${toPoint}`, 'info');
        
        document.getElementById('fromPoint').value = '';
        document.getElementById('toPoint').value = '';
    } catch (err) {
        log(`❌ Move failed: ${err}`, 'error');
    }
}

async function endTurn() {
    try {
        await connection.invoke("EndTurn");
        log('✋ Ending turn...', 'info');
    } catch (err) {
        log(`❌ Failed to end turn: ${err}`, 'error');
    }
}

function updateGameState(state) {
    console.log('Game State:', state);

    // Update game info
    document.getElementById('gameId').textContent = state.gameId || '-';
    document.getElementById('playerColor').textContent = state.yourColor || '-';
    document.getElementById('currentTurn').textContent = state.currentPlayer || '-';
    document.getElementById('turnNumber').textContent = state.turnNumber || '-';

    myColor = state.yourColor;

    // Update dice
    const diceDisplay = document.getElementById('diceDisplay');
    if (state.currentDice && state.currentDice.length > 0) {
        diceDisplay.innerHTML = state.currentDice
            .map(die => `<div class="die">${die}</div>`)
            .join('');
    }

    // Update remaining moves
    const remainingMovesDisplay = document.getElementById('remainingMoves');
    if (state.remainingMoves && state.remainingMoves.length > 0) {
        remainingMovesDisplay.innerHTML = state.remainingMoves
            .map(move => `<div class="die">${move}</div>`)
            .join('');
    } else {
        remainingMovesDisplay.innerHTML = '<div class="die">-</div>';
    }

    // Update player cards
    updatePlayerCards(state);

    // Render board if available
    if (state.board) {
        renderBoard(state);
    }

    // Enable/disable controls
    const isMyTurn = state.isYourTurn;
    document.getElementById('rollBtn').disabled = !isMyTurn || (state.remainingMoves && state.remainingMoves.length > 0);
    document.getElementById('endTurnBtn').disabled = !isMyTurn;
    
    // Check for winner
    if (state.winner) {
        log(`🏆 Game Over! Winner: ${state.winner}`, 'success');
        updateStatus(`Game Over - ${state.winner} wins!`, 'connected');
    }
}

function updatePlayerCards(state) {
    const whiteCard = document.getElementById('whitePlayer');
    const redCard = document.getElementById('redPlayer');
    
    // Highlight active player
    whiteCard.classList.toggle('active', state.currentPlayer === 'White');
    redCard.classList.toggle('active', state.currentPlayer === 'Red');

    // Update checker counts if available
    if (state.board) {
        const whitePieces = countCheckers(state.board, 'White');
        const redPieces = countCheckers(state.board, 'Red');
        
        document.getElementById('whiteCheckers').textContent = `${whitePieces} checkers`;
        document.getElementById('redCheckers').textContent = `${redPieces} checkers`;
    }
}

function countCheckers(board, color) {
    if (!board) return 15;
    
    let count = 0;
    board.forEach(point => {
        if (point.color === color) {
            count += point.count;
        }
    });
    return count;
}

function renderBoard(state) {
    const canvas = document.getElementById('boardCanvas');
    const placeholder = document.getElementById('boardPlaceholder');
    
    if (!state.board || state.board.length === 0) {
        placeholder.style.display = 'block';
        canvas.style.display = 'none';
        return;
    }
    
    placeholder.style.display = 'none';
    canvas.style.display = 'block';
    
    // Set canvas size
    const containerWidth = canvas.parentElement.clientWidth - 40;
    const containerHeight = canvas.parentElement.clientHeight - 40;
    const aspectRatio = 2; // Backgammon boards are roughly 2:1
    
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
    const sideMargin = width * 0.05;
    const playableWidth = width - (2 * sideMargin) - barWidth;
    const pointWidth = playableWidth / 12;
    const pointHeight = height * 0.38;
    const checkerRadius = pointWidth * 0.35;
    const padding = height * 0.05;
    
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
    // Top row: points 13-24 (left to right)
    // Bottom row: points 12-1 (left to right)
    for (let pointNum = 1; pointNum <= 24; pointNum++) {
        const isTop = pointNum >= 13;
        let positionIndex;
        
        if (isTop) {
            // Top row: 13-18 on right, 19-24 on left
            if (pointNum >= 13 && pointNum <= 18) {
                positionIndex = 18 - pointNum; // 18->0, 17->1, ..., 13->5
            } else {
                positionIndex = 24 - pointNum + 6; // 24->6, 23->7, ..., 19->11
            }
        } else {
            // Bottom row: 1-6 on left, 7-12 on right
            if (pointNum >= 1 && pointNum <= 6) {
                positionIndex = pointNum - 1 + 6; // 1->6, 2->7, ..., 6->11
            } else {
                positionIndex = 12 - pointNum; // 12->0, 11->1, ..., 7->5
            }
        }
        
        // Calculate x position (add bar after first 6 points)
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
                    positionIndex = 18 - pointNum;
                } else {
                    positionIndex = 24 - pointNum + 6;
                }
            } else {
                if (pointNum >= 1 && pointNum <= 6) {
                    positionIndex = pointNum - 1 + 6;
                } else {
                    positionIndex = 12 - pointNum;
                }
            }
            
            const x = sideMargin + (positionIndex < 6 ? positionIndex : positionIndex + (barWidth / pointWidth)) * pointWidth;
            const centerX = x + pointWidth / 2;
            const baseY = isTop ? padding : height - padding;
            const direction = isTop ? 1 : -1;
            
            const color = point.color === 'White' ? whiteChecker : redChecker;
            
            // Draw up to 5 checkers visually, show count if more
            const displayCount = Math.min(point.count, 5);
            for (let j = 0; j < displayCount; j++) {
                const checkerY = baseY + (direction * (checkerRadius * 1.1 + j * checkerRadius * 2));
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

function resetUI() {
    document.getElementById('gameId').textContent = '-';
    document.getElementById('playerColor').textContent = '-';
    document.getElementById('currentTurn').textContent = '-';
    document.getElementById('turnNumber').textContent = '-';
    document.getElementById('diceDisplay').innerHTML = '<div class="die">?</div>';
    document.getElementById('remainingMoves').innerHTML = '<div class="die">-</div>';
    
    // Reset board
    const canvas = document.getElementById('boardCanvas');
    const placeholder = document.getElementById('boardPlaceholder');
    canvas.style.display = 'none';
    placeholder.style.display = 'block';
    
    document.getElementById('rollBtn').disabled = true;
    document.getElementById('endTurnBtn').disabled = true;
    
    document.getElementById('whitePlayer').classList.remove('active');
    document.getElementById('redPlayer').classList.remove('active');
}

// Initialize on load
window.addEventListener('load', () => {
    log('👋 Welcome to Backgammon! Enter server URL and click Connect.', 'info');
});
