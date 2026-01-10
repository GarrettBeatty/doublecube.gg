using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Tapper;
using TypedSignalR.Client;

namespace Backgammon.Server.Hubs.Interfaces;

/// <summary>
/// Defines events sent from the server to connected clients.
/// This interface is used by TypedSignalR to generate TypeScript client code.
/// </summary>
[Receiver]
public interface IGameHubClient
{
    // ==================== Core Game Events ====================

    /// <summary>
    /// Sent when game state changes (moves, dice rolls, etc.)
    /// </summary>
    Task GameUpdate(GameState gameState);

    /// <summary>
    /// Sent when both players are ready and the game begins
    /// </summary>
    Task GameStart(GameState gameState);

    /// <summary>
    /// Sent when a game completes with a winner
    /// </summary>
    Task GameOver(GameState gameState);

    /// <summary>
    /// Sent when a game is created and waiting for an opponent
    /// </summary>
    Task WaitingForOpponent(string gameId);

    /// <summary>
    /// Sent when the second player joins the game
    /// </summary>
    Task OpponentJoined(string opponentId);

    /// <summary>
    /// Sent when the opponent disconnects
    /// </summary>
    Task OpponentLeft();

    /// <summary>
    /// Sent when a spectator joins the game
    /// </summary>
    Task SpectatorJoined(GameState gameState);

    // ==================== Doubling Events ====================

    /// <summary>
    /// Sent when the opponent offers to double the stakes
    /// </summary>
    Task DoubleOffered(DoubleOfferDto offer);

    /// <summary>
    /// Sent when a double offer is accepted
    /// </summary>
    Task DoubleAccepted(GameState gameState);

    // ==================== Chat Events ====================

    /// <summary>
    /// Sent when a chat message is received
    /// </summary>
    Task ReceiveChatMessage(string senderName, string message, string senderConnectionId);

    // ==================== Error/Info Events ====================

    /// <summary>
    /// Sent when an error occurs
    /// </summary>
    Task Error(string errorMessage);

    /// <summary>
    /// Sent for informational messages
    /// </summary>
    Task Info(string infoMessage);

    // ==================== Match Events ====================

    /// <summary>
    /// Sent when a match is created (includes first game ID)
    /// </summary>
    Task MatchCreated(MatchCreatedDto data);

    /// <summary>
    /// Sent to match creator when opponent joins
    /// </summary>
    Task OpponentJoinedMatch(OpponentJoinedMatchDto data);

    /// <summary>
    /// Sent when the next game in a match is starting
    /// </summary>
    Task MatchGameStarting(MatchGameStartingDto data);

    /// <summary>
    /// Sent when match score/state changes
    /// </summary>
    Task MatchUpdate(MatchUpdateDto data);

    /// <summary>
    /// Sent when continuing to the next game in a match
    /// </summary>
    Task MatchContinued(MatchContinuedDto data);

    /// <summary>
    /// Sent in response to GetMatchStatus request
    /// </summary>
    Task MatchStatus(MatchStatusDto data);

    /// <summary>
    /// Sent when an individual game in a match completes
    /// </summary>
    Task MatchGameCompleted(MatchGameCompletedDto data);

    /// <summary>
    /// Sent when the entire match completes
    /// </summary>
    Task MatchCompleted(MatchCompletedDto data);

    /// <summary>
    /// Sent when a friend challenges you to a match
    /// </summary>
    Task MatchInvite(MatchInviteDto data);

    /// <summary>
    /// Sent in response to GetMyMatches request
    /// </summary>
    Task MyMatches(List<MatchSummaryDto> matches);

    // ==================== Time Control Events ====================

    /// <summary>
    /// Sent periodically with current time state
    /// </summary>
    Task TimeUpdate(TimeUpdateDto data);

    /// <summary>
    /// Sent when a player runs out of time
    /// </summary>
    Task PlayerTimedOut(PlayerTimedOutDto data);

    // ==================== Correspondence Events ====================

    /// <summary>
    /// Sent when a friend challenges you to a correspondence match
    /// </summary>
    Task CorrespondenceMatchInvite(CorrespondenceMatchInviteDto data);

    /// <summary>
    /// Sent when it's your turn in a correspondence game
    /// </summary>
    Task CorrespondenceTurnNotification(CorrespondenceTurnNotificationDto data);

    /// <summary>
    /// Broadcast when a new correspondence lobby is created
    /// </summary>
    Task CorrespondenceLobbyCreated(CorrespondenceLobbyCreatedDto data);

    // ==================== Lobby Events ====================

    /// <summary>
    /// Broadcast when a new open lobby is created
    /// </summary>
    Task LobbyCreated(LobbyCreatedDto data);

    // ==================== Friend Events ====================

    /// <summary>
    /// Sent when you receive a friend request
    /// </summary>
    Task FriendRequestReceived();

    /// <summary>
    /// Sent when someone accepts your friend request
    /// </summary>
    Task FriendRequestAccepted();
}
