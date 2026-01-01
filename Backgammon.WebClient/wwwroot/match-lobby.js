// Match Lobby View - Pre-game lobby screen

class MatchLobbyView {
    constructor() {
        this.matchId = null;
        this.matchData = null;
    }

    // Initialize and show the lobby
    async show(matchId) {
        this.matchId = matchId;
        this.showContainer();
        this.registerEventHandlers();
        await this.loadMatchData();
    }

    registerEventHandlers() {
        if (!window.connection) return;

        // Only register once
        if (this._handlersRegistered) {
            console.log('Match lobby handlers already registered, skipping');
            return;
        }
        this._handlersRegistered = true;

        // Register handler for when joining lobby succeeds
        window.connection.on('MatchLobbyJoined', (data) => {
            console.log('MatchLobbyJoined event received:', data);
            this.onLobbyJoined(data);
        });

        // Register handler for when another player joins
        window.connection.on('MatchLobbyPlayerJoined', (data) => {
            console.log('MatchLobbyPlayerJoined event received:', data);
            this.onPlayerJoined(data);
        });

        // Register handler for when match starts
        window.connection.on('MatchGameStarting', (data) => {
            console.log('MatchGameStarting event received:', data);
            this.onMatchStarting(data);
        });

        console.log('Match lobby event handlers registered');
    }

    async loadMatchData() {
        // Always join the lobby to ensure connection tracking on server
        console.log('Joining match lobby:', this.matchId);

        // Wait for connection to be ready
        if (!window.connection) {
            console.error('SignalR connection not initialized');
            return;
        }

        // Wait for connection if not ready
        if (window.connection.state !== 'Connected') {
            console.log('Waiting for SignalR connection...');
            const isReady = typeof waitForConnection === 'function'
                ? await waitForConnection(5000)
                : false;

            if (!isReady) {
                console.error('SignalR connection timeout');
                alert('Failed to connect to server. Please refresh the page.');
                return;
            }
        }

        try {
            // Get display name if available
            const displayName = typeof getDisplayName === 'function' ? getDisplayName() : null;
            console.log('Invoking JoinMatchLobby with:', { matchId: this.matchId, displayName });
            await window.connection.invoke('JoinMatchLobby', this.matchId, displayName);
            console.log('JoinMatchLobby invoked successfully');
        } catch (error) {
            console.error('Error joining match lobby:', error);
            alert('Failed to join match lobby: ' + error.message);
        }
    }

    showContainer() {
        // Hide other containers
        const landingPage = document.getElementById('landingPage');
        const gamePage = document.getElementById('gamePage');
        const matchResultsContainer = document.getElementById('match-results-container');
        const matchLobbyContainer = document.getElementById('match-lobby-container');

        if (landingPage) landingPage.style.display = 'none';
        if (gamePage) gamePage.style.display = 'none';
        if (matchResultsContainer) matchResultsContainer.style.display = 'none';

        // Show match lobby container
        if (matchLobbyContainer) {
            matchLobbyContainer.style.display = 'block';
        }
    }

    render() {
        if (!this.matchData) return;

        const container = document.getElementById('match-lobby-content');
        if (!container) return;

        console.log('Rendering match lobby with data:', this.matchData);

        const isReady = this.matchData.player2Name && this.matchData.player2Name !== '';

        // Determine if current user is the creator
        const myPlayerId = typeof window.myPlayerId !== 'undefined' ? window.myPlayerId : null;
        const isCreator = myPlayerId === this.matchData.player1Id;

        // Format opponent type for display
        const formatOpponentType = (type) => {
            console.log('Formatting opponent type:', type);
            switch(type) {
                case 'OpenLobby': return 'Open Lobby (Anyone)';
                case 'AI': return 'Computer (AI)';
                case 'Friend': return 'Friend (Invite Only)';
                default: return type || 'Unknown';
            }
        };

        container.innerHTML = `
            <div class="card">
                <div class="card-header">
                    <h3>Match Lobby</h3>
                </div>
                <div class="card-body">
                    <div class="row mb-4">
                        <div class="col-md-6">
                            <h5>Match Configuration</h5>
                            <p><strong>Match ID:</strong> ${this.matchData.matchId.substring(0, 8)}...</p>
                            <p><strong>Target Score:</strong> ${this.matchData.targetScore} points</p>
                            <p><strong>Match Type:</strong> ${formatOpponentType(this.matchData.opponentType)}</p>
                        </div>
                        <div class="col-md-6">
                            <h5>Players</h5>
                            <div class="player-status">
                                <div class="player-item ${this.matchData.player1Name ? 'ready' : 'waiting'}">
                                    <i class="bi bi-person-fill"></i>
                                    <span>${this.matchData.player1Name || 'Waiting...'}</span>
                                    ${this.matchData.player1Name ? '<i class="bi bi-check-circle-fill text-success"></i>' : ''}
                                </div>
                                <div class="player-item ${this.matchData.player2Name ? 'ready' : 'waiting'}">
                                    <i class="bi bi-person-fill"></i>
                                    <span>${this.matchData.player2Name || 'Waiting for opponent...'}</span>
                                    ${this.matchData.player2Name ? '<i class="bi bi-check-circle-fill text-success"></i>' : ''}
                                </div>
                            </div>
                        </div>
                    </div>

                    ${!isReady ? `
                        <div class="alert alert-info">
                            <i class="bi bi-info-circle"></i>
                            Waiting for opponent to join...
                        </div>
                    ` : `
                        <div class="alert alert-success">
                            <i class="bi bi-check-circle"></i>
                            Both players ready! Match can begin.
                        </div>
                    `}

                    <div class="d-flex gap-2 justify-content-end">
                        <button class="btn btn-secondary" onclick="matchLobbyView.leaveLobby()">
                            Leave Lobby
                        </button>
                        ${isCreator && isReady ? `
                            <button class="btn btn-primary" onclick="matchLobbyView.startMatch()">
                                Start Match
                            </button>
                        ` : ''}
                    </div>
                </div>
            </div>
        `;
    }

    async startMatch() {
        if (!this.matchId) return;

        try {
            // Call SignalR to start the match
            await window.connection.invoke('StartMatchFirstGame', this.matchId);
        } catch (error) {
            console.error('Error starting match:', error);
            alert('Failed to start match: ' + error.message);
        }
    }

    async leaveLobby() {
        if (!this.matchId) return;

        if (confirm('Are you sure you want to leave this match lobby?')) {
            try {
                await window.connection.invoke('LeaveMatchLobby', this.matchId);
                window.matchController.clearMatch();
                window.matchController.navigateToHome();
                window.location.href = '/';
            } catch (error) {
                console.error('Error leaving lobby:', error);
                alert('Failed to leave lobby: ' + error.message);
            }
        }
    }

    // Handle SignalR events
    onLobbyJoined(data) {
        console.log('Match lobby joined successfully:', data);

        // Store match data
        this.matchData = {
            matchId: data.matchId,
            targetScore: data.targetScore,
            opponentType: data.opponentType,
            isOpenLobby: data.isOpenLobby,
            player1Name: data.player1Name,
            player1Id: data.player1Id,
            player2Name: data.player2Name,
            player2Id: data.player2Id,
            lobbyStatus: data.lobbyStatus,
            player1Score: 0,
            player2Score: 0,
            status: 'InProgress'
        };

        // Save to MatchController
        if (typeof window.matchController !== 'undefined') {
            window.matchController.setCurrentMatch(this.matchData);
        }

        // Render the lobby UI
        this.render();
    }

    onPlayerJoined(data) {
        console.log('Player joined lobby:', data);
        console.log('Current matchId:', this.matchId);
        console.log('Current matchData:', this.matchData);

        if (data.matchId === this.matchId) {
            if (!this.matchData) {
                console.warn('matchData is null, cannot update');
                return;
            }

            console.log('Updating match data with player 2 info');
            this.matchData.player2Name = data.player2Name;
            this.matchData.player2Id = data.player2Id;
            this.matchData.lobbyStatus = data.lobbyStatus;

            // Also update in MatchController
            if (typeof window.matchController !== 'undefined') {
                window.matchController.setCurrentMatch(this.matchData);
            }

            this.render();
            console.log('Lobby re-rendered with updated player 2');
        } else {
            console.log('Match ID mismatch, ignoring event');
        }
    }

    onMatchStarting(data) {
        if (data.matchId === this.matchId) {
            // Navigate to the game with match context
            window.matchController.navigateToGame(data.matchId, data.gameId);
            // Let game.js handle the actual game joining
            window.location.href = `/match/${data.matchId}/game/${data.gameId}`;
        }
    }

    hide() {
        const container = document.getElementById('match-lobby-container');
        if (container) {
            container.style.display = 'none';
        }
    }
}

// Create singleton instance
const matchLobbyView = new MatchLobbyView();

// Make it globally available
if (typeof window !== 'undefined') {
    window.matchLobbyView = matchLobbyView;
}
