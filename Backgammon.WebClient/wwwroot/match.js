// Match play functionality for Backgammon

// ==== MATCH STATE ====
// Note: Match state is now managed by MatchController (match-state.js)
// Use window.matchController.getCurrentMatch() / setCurrentMatch() instead
let matchHistory = [];

// ==== MATCH UI COMPONENTS ====
class MatchStatusDisplay {
    constructor(containerId) {
        this.container = document.getElementById(containerId);
    }

    render(match) {
        if (!match || !this.container) return;

        const isCrawford = match.isCrawfordGame;
        const crawfordIndicator = isCrawford 
            ? '<div class="crawford-indicator">Crawford Game - No Doubling</div>' 
            : '';

        this.container.innerHTML = `
            <div class="match-status">
                <div class="match-header">
                    <h3>Match to ${match.targetScore}</h3>
                </div>
                <div class="match-score">
                    <div class="player-score ${match.myScore > match.opponentScore ? 'winning' : ''}">
                        <span class="player-name">You</span>
                        <span class="score">${match.myScore}</span>
                    </div>
                    <div class="vs">vs</div>
                    <div class="player-score ${match.opponentScore > match.myScore ? 'winning' : ''}">
                        <span class="player-name">${match.opponentName}</span>
                        <span class="score">${match.opponentScore}</span>
                    </div>
                </div>
                ${crawfordIndicator}
                <div class="match-progress">
                    <div class="progress-bar">
                        <div class="progress-fill my-progress" style="width: ${(match.myScore / match.targetScore) * 100}%"></div>
                        <div class="progress-fill opponent-progress" style="width: ${(match.opponentScore / match.targetScore) * 100}%"></div>
                    </div>
                </div>
                <div class="match-info">
                    Game ${match.totalGames} of match
                </div>
            </div>
        `;
    }

    showMatchComplete(match) {
        if (!this.container) return;

        const isWinner = match.winnerId === myPlayerId;
        const finalScore = `${match.myScore} - ${match.opponentScore}`;

        this.container.innerHTML = `
            <div class="match-complete">
                <h2>${isWinner ? 'Match Victory!' : 'Match Lost'}</h2>
                <div class="final-score">${finalScore}</div>
                <p>${isWinner ? 'Congratulations!' : 'Better luck next time!'}</p>
                <button onclick="showMatchHistory()" class="btn btn-primary">View Match History</button>
                <button onclick="createNewMatch()" class="btn btn-secondary">New Match</button>
            </div>
        `;
    }
}

// ==== MATCH SIGNALR EVENTS ====
function initializeMatchEvents() {
    if (!connection) return;

    // Match lobby created (for Friend/OpenLobby matches)
    connection.on('MatchLobbyCreated', (data) => {
        debug('Match lobby created', data, 'success');
        if (typeof window.matchController !== 'undefined') {
            window.matchController.setCurrentMatch({
                matchId: data.matchId,
                targetScore: data.targetScore,
                opponentType: data.opponentType,
                isOpenLobby: data.isOpenLobby,
                player1Name: data.player1Name,
                player1Id: data.player1Id,
                player2Name: data.player2Name,
                player2Id: data.player2Id,
                player1Score: 0,
                player2Score: 0,
                status: 'InProgress',
                isCrawfordGame: false,
                hasCrawfordGameBeenPlayed: false
            });
            window.matchController.navigateToLobby(data.matchId);
            // Show the lobby view
            if (typeof window.matchLobbyView !== 'undefined') {
                window.matchLobbyView.show(data.matchId);
            }
        }
    });

    // Match game starting (for AI matches - skips lobby)
    connection.on('MatchGameStarting', (data) => {
        debug('Match game starting', data, 'success');
        if (typeof window.matchController !== 'undefined') {
            window.matchController.setCurrentMatch({
                matchId: data.matchId,
                targetScore: data.targetScore,
                player1Name: data.player1Name,
                player1Id: data.player1Id,
                player2Name: data.player2Name,
                player2Id: data.player2Id,
                player1Score: data.player1Score,
                player2Score: data.player2Score,
                currentGameId: data.gameId,
                status: 'InProgress',
                isCrawfordGame: data.isCrawfordGame,
                hasCrawfordGameBeenPlayed: false
            });
            window.matchController.navigateToGame(data.matchId, data.gameId);
            // Join the game directly without page reload
            if (typeof joinGame === 'function') {
                joinGame(data.gameId);
            }
        }
    });

    // Match created (legacy event)
    connection.on('MatchCreated', (data) => {
        debug('Match created', data, 'success');
        if (typeof window.matchController !== 'undefined') {
            window.matchController.handleMatchCreated(data);
        }
        setGameUrl(data.gameId);
        showMatchStatus();
    });

    // Match invite received
    connection.on('MatchInvite', (data) => {
        debug('Match invite received', data, 'info');
        showMatchInvite(data);
    });

    // Match status update
    connection.on('MatchUpdate', (data) => {
        debug('Match update', data, 'info');
        if (typeof window.matchController !== 'undefined') {
            window.matchController.handleMatchUpdate(data);
        }
        updateMatchStatus(data);

        if (data.matchComplete) {
            handleMatchComplete(data);
        }
    });

    // Match continued
    connection.on('MatchContinued', (data) => {
        debug('Match continued', data, 'info');
        if (typeof window.matchController !== 'undefined') {
            window.matchController.setCurrentMatch(data);
        }
        // The game will automatically join the new game
    });
}

// ==== MATCH ACTIONS ====
async function createMatch(opponentId, targetScore = 7) {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        showError('Not connected to server');
        return;
    }

    try {
        await connection.invoke('CreateMatch', opponentId, targetScore);
    } catch (error) {
        debug('Failed to create match', error, 'error');
        showError('Failed to create match: ' + error.toString());
    }
}

async function continueMatch(matchId) {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        showError('Not connected to server');
        return;
    }

    try {
        await connection.invoke('ContinueMatch', matchId);
    } catch (error) {
        debug('Failed to continue match', error, 'error');
        showError('Failed to continue match: ' + error.toString());
    }
}

async function getMyMatches(status = null) {
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        showError('Not connected to server');
        return;
    }

    try {
        await connection.invoke('GetMyMatches', status);
    } catch (error) {
        debug('Failed to get matches', error, 'error');
        showError('Failed to get matches: ' + error.toString());
    }
}

// ==== UI FUNCTIONS ====
function showMatchStatus() {
    const currentMatch = typeof window.matchController !== 'undefined'
        ? window.matchController.getCurrentMatch()
        : null;

    if (!currentMatch) return;

    const display = new MatchStatusDisplay('match-status-container');
    display.render(currentMatch);
}

function updateMatchStatus(matchData) {
    const currentMatch = typeof window.matchController !== 'undefined'
        ? window.matchController.getCurrentMatch()
        : null;

    if (!currentMatch) return;

    currentMatch.player1Score = matchData.player1Score;
    currentMatch.player2Score = matchData.player2Score;
    currentMatch.isCrawfordGame = matchData.isCrawfordGame;
    currentMatch.totalGames = (currentMatch.totalGames || 0) + 1;

    // Update scores based on player perspective
    if (currentMatch.player1Id === myPlayerId) {
        currentMatch.myScore = matchData.player1Score;
        currentMatch.opponentScore = matchData.player2Score;
    } else {
        currentMatch.myScore = matchData.player2Score;
        currentMatch.opponentScore = matchData.player1Score;
    }

    // Save updated match
    if (typeof window.matchController !== 'undefined') {
        window.matchController.setCurrentMatch(currentMatch);
    }

    showMatchStatus();
}

function handleMatchComplete(matchData) {
    const currentMatch = typeof window.matchController !== 'undefined'
        ? window.matchController.getCurrentMatch()
        : null;

    if (!currentMatch) return;

    const display = new MatchStatusDisplay('match-status-container');
    display.showMatchComplete({
        ...currentMatch,
        winnerId: matchData.matchWinner,
        myScore: currentMatch.myScore,
        opponentScore: currentMatch.opponentScore
    });

    // Clear current match via controller
    if (typeof window.matchController !== 'undefined') {
        window.matchController.handleMatchComplete(matchData);
    }
}

function showMatchInvite(inviteData) {
    const modal = document.getElementById('match-invite-modal');
    if (!modal) {
        // Create modal if it doesn't exist
        const modalHtml = `
            <div id="match-invite-modal" class="modal">
                <div class="modal-content">
                    <h3>Match Invitation</h3>
                    <p>${inviteData.challengerName} has invited you to a match to ${inviteData.targetScore} points.</p>
                    <div class="modal-actions">
                        <button onclick="acceptMatchInvite('${inviteData.matchId}', '${inviteData.gameId}')" class="btn btn-primary">Accept</button>
                        <button onclick="declineMatchInvite()" class="btn btn-secondary">Decline</button>
                    </div>
                </div>
            </div>
        `;
        document.body.insertAdjacentHTML('beforeend', modalHtml);
    } else {
        modal.style.display = 'block';
    }
}

function acceptMatchInvite(matchId, gameId) {
    const modal = document.getElementById('match-invite-modal');
    if (modal) modal.style.display = 'none';
    
    // Join the game
    setGameUrl(gameId);
    joinGame(gameId);
}

function declineMatchInvite() {
    const modal = document.getElementById('match-invite-modal');
    if (modal) modal.style.display = 'none';
}

function showMatchHistory() {
    connection.on('MyMatches', (matches) => {
        displayMatchList(matches);
    });
    
    getMyMatches();
}

function displayMatchList(matches) {
    const container = document.getElementById('match-list-container');
    if (!container) return;

    const matchesHtml = matches.map(match => `
        <div class="match-item ${match.status}">
            <div class="match-opponent">${match.opponentName}</div>
            <div class="match-score">${match.myScore} - ${match.opponentScore}</div>
            <div class="match-target">First to ${match.targetScore}</div>
            <div class="match-status">${match.status}</div>
            ${match.status === 'InProgress' ? 
                `<button onclick="continueMatch('${match.matchId}')" class="btn btn-sm">Continue</button>` : 
                ''}
        </div>
    `).join('');

    container.innerHTML = `
        <div class="match-list">
            <h3>Your Matches</h3>
            ${matchesHtml || '<p>No matches found</p>'}
        </div>
    `;
}

function createNewMatch() {
    showCreateMatchDialog();
}

// Dialog functions
function showCreateMatchDialog() {
    try {
        const dialog = document.getElementById('createMatchDialog');

        if (!dialog) {
            console.error('createMatchDialog element not found!');
            alert('Error: Match dialog not found. Please refresh the page.');
            return;
        }

        // Populate opponent list with friends
        populateOpponentList();
        dialog.showModal();

    } catch (error) {
        console.error('Error in showCreateMatchDialog:', error);
        alert('Error opening match dialog: ' + error.message);
    }
}

function hideCreateMatchDialog() {
    const dialog = document.getElementById('createMatchDialog');
    if (dialog) {
        dialog.close();
    }
}

async function populateOpponentList() {
    const select = document.getElementById('matchOpponentSelect');
    if (!select) return;

    // Clear existing options
    select.innerHTML = '<option value="" disabled selected>Choose an opponent...</option>';

    // Add AI opponent option
    const aiOption = document.createElement('option');
    aiOption.value = 'AI';
    aiOption.textContent = '🤖 Computer (AI)';
    select.appendChild(aiOption);

    // Add open lobby option
    const openOption = document.createElement('option');
    openOption.value = 'OPEN';
    openOption.textContent = '🌐 Open Lobby (Anyone)';
    select.appendChild(openOption);

    // Try to load friends if authenticated
    const isAuth = typeof isAuthenticated === 'function' && isAuthenticated();
    if (isAuth) {
        try {
            const token = getAuthToken();
            if (!token) return;

            const response = await fetch(`${apiBaseUrl}/api/friends`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });

            if (response.ok) {
                const friends = await response.json();
                if (friends && friends.length > 0) {
                    // Add separator
                    const separator = document.createElement('option');
                    separator.disabled = true;
                    separator.textContent = '--- Friends ---';
                    select.appendChild(separator);

                    // Add friends
                    friends.forEach(friend => {
                        const option = document.createElement('option');
                        option.value = `FRIEND:${friend.friendUserId}`;
                        option.textContent = friend.friendDisplayName || friend.friendUsername;
                        select.appendChild(option);
                    });
                }
            }
        } catch (error) {
            console.error('Failed to load friends:', error);
            // Don't fail - AI and open lobby options are still available
        }
    }
}

async function createMatchFromDialog() {
    const opponentSelect = document.getElementById('matchOpponentSelect');
    const scoreSelect = document.getElementById('matchTargetScore');

    if (!opponentSelect || !scoreSelect) return;

    const selectedValue = opponentSelect.value;
    const targetScore = parseInt(scoreSelect.value);

    if (!selectedValue) {
        alert('Please select an opponent');
        return;
    }

    // Parse the opponent selection
    let config = {
        targetScore: targetScore,
        opponentType: '',
        opponentId: null,
        displayName: null
    };

    if (selectedValue === 'AI') {
        config.opponentType = 'AI';
        config.opponentId = 'ai-opponent'; // Backend will handle AI player creation
    } else if (selectedValue === 'OPEN') {
        config.opponentType = 'OpenLobby';
    } else if (selectedValue.startsWith('FRIEND:')) {
        config.opponentType = 'Friend';
        config.opponentId = selectedValue.substring(7); // Remove "FRIEND:" prefix
    } else {
        alert('Invalid opponent selection');
        return;
    }

    hideCreateMatchDialog();

    // Call the CreateMatchWithConfig hub method
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
        alert('Not connected to server');
        return;
    }

    try {
        debug('Creating match with config', config, 'info');
        await connection.invoke('CreateMatchWithConfig', config);
    } catch (error) {
        debug('Failed to create match', error, 'error');
        alert('Failed to create match: ' + error.toString());
    }
}

// Helper function to get auth token
function getAuthToken() {
    return localStorage.getItem('jwtToken');
}

// ==== MATCH CSS STYLES ====
const matchStyles = `
<style>
.match-status {
    background: rgba(0, 0, 0, 0.8);
    border: 2px solid #444;
    border-radius: 8px;
    padding: 15px;
    margin-bottom: 10px;
    color: white;
}

.match-header h3 {
    margin: 0 0 10px 0;
    text-align: center;
    font-size: 18px;
}

.match-score {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 10px;
}

.player-score {
    text-align: center;
    padding: 10px;
    background: rgba(255, 255, 255, 0.1);
    border-radius: 4px;
    flex: 1;
}

.player-score.winning {
    background: rgba(34, 197, 94, 0.2);
    border: 1px solid rgba(34, 197, 94, 0.4);
}

.player-name {
    display: block;
    font-size: 14px;
    margin-bottom: 5px;
}

.score {
    display: block;
    font-size: 24px;
    font-weight: bold;
}

.vs {
    margin: 0 10px;
    font-size: 14px;
    color: #666;
}

.crawford-indicator {
    background: rgba(245, 158, 11, 0.2);
    border: 1px solid rgba(245, 158, 11, 0.4);
    padding: 8px;
    text-align: center;
    border-radius: 4px;
    margin: 10px 0;
    font-size: 14px;
    color: #f59e0b;
}

.match-progress {
    margin: 10px 0;
}

.progress-bar {
    height: 20px;
    background: rgba(255, 255, 255, 0.1);
    border-radius: 10px;
    position: relative;
    overflow: hidden;
}

.progress-fill {
    height: 100%;
    transition: width 0.3s ease;
}

.my-progress {
    background: #3b82f6;
}

.opponent-progress {
    background: #ef4444;
    position: absolute;
    right: 0;
    top: 0;
}

.match-info {
    text-align: center;
    font-size: 14px;
    color: #999;
    margin-top: 10px;
}

.match-complete {
    text-align: center;
    padding: 20px;
    background: rgba(0, 0, 0, 0.9);
    border-radius: 8px;
    color: white;
}

.match-complete h2 {
    font-size: 28px;
    margin-bottom: 10px;
}

.final-score {
    font-size: 36px;
    font-weight: bold;
    margin: 20px 0;
}

.match-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 10px;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 4px;
    margin-bottom: 5px;
}

.match-item.InProgress {
    border-color: #3b82f6;
}

.match-item.Completed {
    opacity: 0.6;
}

.modal {
    display: none;
    position: fixed;
    z-index: 1000;
    left: 0;
    top: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.5);
}

.modal-content {
    background-color: #2a2a2a;
    margin: 15% auto;
    padding: 20px;
    border: 1px solid #444;
    border-radius: 8px;
    width: 80%;
    max-width: 400px;
    color: white;
}

.modal-actions {
    display: flex;
    justify-content: space-around;
    margin-top: 20px;
}

.btn {
    padding: 8px 16px;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 14px;
    transition: background-color 0.2s;
}

.btn-primary {
    background-color: #3b82f6;
    color: white;
}

.btn-primary:hover {
    background-color: #2563eb;
}

.btn-secondary {
    background-color: #6b7280;
    color: white;
}

.btn-secondary:hover {
    background-color: #4b5563;
}

.btn-sm {
    padding: 4px 8px;
    font-size: 12px;
}
</style>
`;

// Add styles to document
if (!document.getElementById('match-styles')) {
    const styleElement = document.createElement('div');
    styleElement.id = 'match-styles';
    styleElement.innerHTML = matchStyles;
    document.head.appendChild(styleElement);
}

// Explicitly expose functions to global scope
if (typeof window !== 'undefined') {
    window.showCreateMatchDialog = showCreateMatchDialog;
    window.hideCreateMatchDialog = hideCreateMatchDialog;
    window.createMatchFromDialog = createMatchFromDialog;
    window.initializeMatchEvents = initializeMatchEvents;
    window.showMatchStatus = showMatchStatus;
    window.createNewMatch = createNewMatch;
}