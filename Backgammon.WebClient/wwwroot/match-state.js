/* global */

// MatchController - Centralized match state management
// Singleton pattern for managing match state, persistence, and navigation

class MatchController {
    constructor() {
        this.currentMatch = null;
        this.loadFromLocalStorage();
    }

    // State management
    setCurrentMatch(match) {
        this.currentMatch = match;
        this.saveToLocalStorage();
    }

    getCurrentMatch() {
        return this.currentMatch;
    }

    clearMatch() {
        this.currentMatch = null;
        localStorage.removeItem('backgammon_current_match');
    }

    updateMatchScores(player1Score, player2Score) {
        if (this.currentMatch) {
            this.currentMatch.player1Score = player1Score;
            this.currentMatch.player2Score = player2Score;
            this.saveToLocalStorage();
        }
    }

    updateCrawfordState(isCrawfordGame, hasCrawfordGameBeenPlayed) {
        if (this.currentMatch) {
            this.currentMatch.isCrawfordGame = isCrawfordGame;
            this.currentMatch.hasCrawfordGameBeenPlayed = hasCrawfordGameBeenPlayed;
            this.saveToLocalStorage();
        }
    }

    isMatchComplete() {
        if (!this.currentMatch) return false;
        return this.currentMatch.player1Score >= this.currentMatch.targetScore ||
               this.currentMatch.player2Score >= this.currentMatch.targetScore;
    }

    // LocalStorage persistence
    saveToLocalStorage() {
        if (this.currentMatch) {
            const matchData = {
                matchId: this.currentMatch.matchId,
                targetScore: this.currentMatch.targetScore,
                player1Score: this.currentMatch.player1Score,
                player2Score: this.currentMatch.player2Score,
                myScore: this.currentMatch.myScore,
                opponentScore: this.currentMatch.opponentScore,
                isCrawfordGame: this.currentMatch.isCrawfordGame,
                hasCrawfordGameBeenPlayed: this.currentMatch.hasCrawfordGameBeenPlayed,
                currentGameId: this.currentMatch.currentGameId,
                status: this.currentMatch.status,
                player1Name: this.currentMatch.player1Name,
                player2Name: this.currentMatch.player2Name,
                // Match lobby fields
                opponentType: this.currentMatch.opponentType,
                isOpenLobby: this.currentMatch.isOpenLobby,
                player1Id: this.currentMatch.player1Id,
                player2Id: this.currentMatch.player2Id,
                lobbyStatus: this.currentMatch.lobbyStatus,
                lastUpdated: Date.now()
            };
            localStorage.setItem('backgammon_current_match', JSON.stringify(matchData));
        }
    }

    loadFromLocalStorage() {
        const stored = localStorage.getItem('backgammon_current_match');
        if (stored) {
            try {
                const matchData = JSON.parse(stored);
                // Only load if not too old (24 hours)
                const age = Date.now() - (matchData.lastUpdated || 0);
                if (age < 24 * 60 * 60 * 1000) {
                    this.currentMatch = matchData;
                } else {
                    this.clearMatch();
                }
            } catch (e) {
                console.error('Failed to load match from localStorage', e);
                this.clearMatch();
            }
        }
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
