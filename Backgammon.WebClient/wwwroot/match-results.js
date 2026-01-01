/* global */

// Match Results View - Post-match results and statistics

class MatchResultsView {
    constructor() {
        this.matchId = null;
        this.matchData = null;
        this.games = [];
    }

    // Initialize and show the results
    async show(matchId) {
        this.matchId = matchId;
        await this.loadMatchData();
        this.showContainer();
        this.render();
    }

    async loadMatchData() {
        // Get match data from MatchController
        const match = window.matchController.getCurrentMatch();
        if (match && match.matchId === this.matchId) {
            this.matchData = match;
        }

        // Note: Detailed game-by-game results not yet implemented
        // This would require storing individual game results in the match
        // and a GetMatchGames SignalR method
    }

    showContainer() {
        // Hide other containers
        const landingPage = document.getElementById('landingPage');
        const gamePage = document.getElementById('gamePage');
        const matchLobbyContainer = document.getElementById('match-lobby-container');
        const matchResultsContainer = document.getElementById('match-results-container');

        if (landingPage) landingPage.style.display = 'none';
        if (gamePage) gamePage.style.display = 'none';
        if (matchLobbyContainer) matchLobbyContainer.style.display = 'none';

        // Show match results container
        if (matchResultsContainer) {
            matchResultsContainer.style.display = 'block';
        }
    }

    render() {
        if (!this.matchData) return;

        const container = document.getElementById('match-results-content');
        if (!container) return;

        const winner = this.determineWinner();
        const duration = this.formatDuration();
        const totalGames = this.matchData.player1Score + this.matchData.player2Score;

        container.innerHTML = `
            <div class="card">
                <div class="card-header bg-success text-white">
                    <h3>Match Complete!</h3>
                </div>
                <div class="card-body">
                    <div class="text-center mb-4">
                        <h2 class="display-4">${winner.name} Wins!</h2>
                        <p class="lead text-muted">Match to ${this.matchData.targetScore}</p>
                    </div>

                    <div class="row mb-4">
                        <div class="col-md-6">
                            <div class="score-display ${winner.isPlayer1 ? 'winner' : ''}">
                                <h4>${this.matchData.player1Name || 'Player 1'}</h4>
                                <div class="score-value">${this.matchData.player1Score}</div>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="score-display ${!winner.isPlayer1 ? 'winner' : ''}">
                                <h4>${this.matchData.player2Name || 'Player 2'}</h4>
                                <div class="score-value">${this.matchData.player2Score}</div>
                            </div>
                        </div>
                    </div>

                    <div class="match-stats mb-4">
                        <h5>Match Statistics</h5>
                        <ul class="list-group">
                            <li class="list-group-item d-flex justify-content-between">
                                <span>Total Games Played</span>
                                <strong>${totalGames}</strong>
                            </li>
                            <li class="list-group-item d-flex justify-content-between">
                                <span>Match Duration</span>
                                <strong>${duration}</strong>
                            </li>
                            ${this.matchData.hasCrawfordGameBeenPlayed ? `
                                <li class="list-group-item d-flex justify-content-between">
                                    <span>Crawford Game</span>
                                    <strong><i class="bi bi-check-circle text-success"></i> Played</strong>
                                </li>
                            ` : ''}
                            <li class="list-group-item d-flex justify-content-between">
                                <span>Final Score</span>
                                <strong>${this.matchData.player1Score} - ${this.matchData.player2Score}</strong>
                            </li>
                        </ul>
                    </div>

                    ${this.renderGamesList()}

                    <div class="d-flex gap-2 justify-content-end mt-4">
                        <button class="btn btn-secondary" onclick="matchResultsView.backToHome()">
                            <i class="bi bi-house"></i> Back to Home
                        </button>
                        <button class="btn btn-primary" onclick="matchResultsView.requestRematch()">
                            <i class="bi bi-arrow-repeat"></i> Rematch
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    renderGamesList() {
        if (!this.games || this.games.length === 0) {
            return '';
        }

        const gamesHtml = this.games.map((game, index) => `
            <tr>
                <td>${index + 1}</td>
                <td>${game.winner}</td>
                <td>${game.stakes} ${game.winType ? '(' + game.winType + ')' : ''}</td>
                <td>${game.isCrawfordGame ? '<span class="badge bg-warning">Crawford</span>' : ''}</td>
            </tr>
        `).join('');

        return `
            <div class="game-history mb-4">
                <h5>Game History</h5>
                <div class="table-responsive">
                    <table class="table table-sm">
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>Winner</th>
                                <th>Points</th>
                                <th>Notes</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${gamesHtml}
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    }

    determineWinner() {
        const isPlayer1Winner = this.matchData.player1Score >= this.matchData.targetScore;
        return {
            name: isPlayer1Winner ?
                (this.matchData.player1Name || 'Player 1') :
                (this.matchData.player2Name || 'Player 2'),
            isPlayer1: isPlayer1Winner,
            score: isPlayer1Winner ? this.matchData.player1Score : this.matchData.player2Score
        };
    }

    formatDuration() {
        // Duration calculation not yet implemented
        // Would require tracking match start/end timestamps
        if (this.matchData && this.matchData.durationSeconds) {
            const minutes = Math.floor(this.matchData.durationSeconds / 60);
            const seconds = this.matchData.durationSeconds % 60;
            return `${minutes}m ${seconds}s`;
        }
        return 'N/A';
    }

    backToHome() {
        window.matchController.clearMatch();
        window.matchController.navigateToHome();
        window.location.href = '/';
    }

    async requestRematch() {
        if (!this.matchId) return;

        // Rematch feature not yet implemented
        alert('Rematch feature coming soon! You can create a new match from the home page.');
    }

    hide() {
        const container = document.getElementById('match-results-container');
        if (container) {
            container.style.display = 'none';
        }
    }
}

// Create singleton instance
const matchResultsView = new MatchResultsView();

// Make it globally available
if (typeof window !== 'undefined') {
    window.matchResultsView = matchResultsView;
}
