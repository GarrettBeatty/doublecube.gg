/* global */

// MatchController - Centralized match state management
// Singleton pattern for managing match state and navigation
// Match state is now server-authoritative (no localStorage persistence)

class MatchController {
    constructor() {
        this.currentMatch = null;
        this.apiBaseUrl = 'http://localhost:5000'; // Default, will be updated from config
    }

    // State management
    setCurrentMatch(match) {
        this.currentMatch = match;
    }

    getCurrentMatch() {
        return this.currentMatch;
    }

    clearMatch() {
        this.currentMatch = null;
    }

    updateMatchScores(player1Score, player2Score) {
        if (this.currentMatch) {
            this.currentMatch.player1Score = player1Score;
            this.currentMatch.player2Score = player2Score;
        }
    }

    updateCrawfordState(isCrawfordGame, hasCrawfordGameBeenPlayed) {
        if (this.currentMatch) {
            this.currentMatch.isCrawfordGame = isCrawfordGame;
            this.currentMatch.hasCrawfordGameBeenPlayed = hasCrawfordGameBeenPlayed;
        }
    }

    isMatchComplete() {
        if (!this.currentMatch) return false;
        return this.currentMatch.player1Score >= this.currentMatch.targetScore ||
               this.currentMatch.player2Score >= this.currentMatch.targetScore;
    }

    // Server-based state loading
    async loadActiveMatch(playerId) {
        if (!playerId) return null;

        try {
            const response = await fetch(`${this.apiBaseUrl}/api/player/${playerId}/active-match`);
            const data = await response.json();

            if (data.hasActiveMatch) {
                this.currentMatch = {
                    matchId: data.matchId,
                    targetScore: data.targetScore,
                    player1Id: data.player1Id,
                    player2Id: data.player2Id,
                    player1Score: data.player1Score,
                    player2Score: data.player2Score,
                    status: data.status,
                    currentGameId: data.currentGameId,
                    isCrawfordGame: data.isCrawfordGame,
                    hasCrawfordGameBeenPlayed: data.hasCrawfordGameBeenPlayed
                };
                return this.currentMatch;
            } else {
                this.currentMatch = null;
                return null;
            }
        } catch (error) {
            console.error('Failed to load active match from server:', error);
            return null;
        }
    }

    setApiBaseUrl(url) {
        this.apiBaseUrl = url;
    }

    // Navigation helpers
    navigateToLobby(matchId) {
        window.history.pushState({ type: 'match-lobby', matchId }, '', `/match/${matchId}/lobby`);
    }

    navigateToGame(matchId, gameId) {
        window.history.pushState({ type: 'match-game', matchId, gameId }, '', `/match/${matchId}/game/${gameId}`);
    }

    navigateToResults(matchId) {
        window.history.pushState({ type: 'match-results', matchId }, '', `/match/${matchId}/results`);
    }

    navigateToMatch(matchId) {
        window.history.pushState({ type: 'match', matchId }, '', `/match/${matchId}`);
    }

    navigateToHome() {
        window.history.pushState({ type: 'home' }, '', '/');
    }

    // SignalR event handlers
    handleMatchCreated(data) {
        this.setCurrentMatch({
            matchId: data.matchId,
            targetScore: data.targetScore,
            player1Score: 0,
            player2Score: 0,
            myScore: 0,
            opponentScore: 0,
            isCrawfordGame: false,
            hasCrawfordGameBeenPlayed: false,
            currentGameId: data.gameId,
            status: 'InProgress',
            player1Name: data.player1Name,
            player2Name: data.player2Name
        });
    }

    handleMatchUpdate(data) {
        if (this.currentMatch && this.currentMatch.matchId === data.matchId) {
            this.currentMatch.player1Score = data.player1Score;
            this.currentMatch.player2Score = data.player2Score;
            this.currentMatch.myScore = data.myScore;
            this.currentMatch.opponentScore = data.opponentScore;
            this.currentMatch.isCrawfordGame = data.isCrawfordGame;
            this.currentMatch.hasCrawfordGameBeenPlayed = data.hasCrawfordGameBeenPlayed;
            this.currentMatch.status = data.status;
            this.saveToLocalStorage();
        }
    }

    handleMatchComplete(data) {
        if (this.currentMatch && this.currentMatch.matchId === data.matchId) {
            this.currentMatch.status = 'Completed';
            this.currentMatch.winnerId = data.winnerId;
            this.saveToLocalStorage();
        }
    }

    handleGameComplete(_data) {
        // Update current game ID if needed
        if (this.currentMatch) {
            this.saveToLocalStorage();
        }
    }
}

// Export singleton instance
const matchController = new MatchController();

// Make it globally available
if (typeof window !== 'undefined') {
    window.matchController = matchController;
}
