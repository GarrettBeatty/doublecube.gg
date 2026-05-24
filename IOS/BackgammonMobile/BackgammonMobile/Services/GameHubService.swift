import Foundation
import SignalRClient

// MARK: - GameHubService
// Wraps the SignalR HubConnection and mirrors IGameHub / IGameHubClient.

@MainActor
final class GameHubService: ObservableObject {
    static let shared = GameHubService()

    // MARK: - Published state
    @Published var gameState: GameState?
    @Published var connectionState: ConnectionState = .disconnected
    @Published var errorMessage: String?

    // Match events
    @Published var matchCreated: MatchCreatedDto?
    @Published var matchUpdate: MatchUpdateDto?
    @Published var matchGameStarting: MatchGameStartingDto?
    @Published var matchCompleted: MatchCompletedDto?

    // Double offers
    @Published var pendingDoubleOffer: DoubleOfferDto?

    // Chat
    @Published var chatMessages: [ChatMessage] = []

    struct ChatMessage: Identifiable {
        let id = UUID()
        let sender: String
        let text: String
    }

    // MARK: - Private
    private var connection: HubConnection?

    private init() {}

    // MARK: - Connection lifecycle

    func connect(token: String) async throws {
        connectionState = .connecting

        let hub = HubConnectionBuilder()
            .withUrl(url: Config.hubURL, httpConnectionOptions: { opts in
                opts.headers["Authorization"] = "Bearer \(token)"
            })
            .withAutomaticReconnect()
            .build()

        registerHandlers(on: hub)

        hub.onReconnecting { [weak self] _ in
            Task { @MainActor in self?.connectionState = .reconnecting }
        }
        hub.onReconnected { [weak self] in
            Task { @MainActor in self?.connectionState = .connected }
        }

        self.connection = hub
        try await hub.start()
        connectionState = .connected
    }

    func disconnect() async {
        try? await connection?.stop()
        connection = nil
        connectionState = .disconnected
        gameState = nil
    }

    // MARK: - Hub method invocations (IGameHub)

    func joinGame(_ gameId: String?) async {
        try? await connection?.invoke(method: "JoinGame", arguments: gameId as Any)
    }

    func createAiGame() async {
        try? await connection?.send(method: "CreateAiGame")
    }

    func rollDice() async {
        try? await connection?.send(method: "RollDice")
    }

    func makeMove(from: Int, to: Int) async {
        try? await connection?.send(method: "MakeMove", arguments: from, to)
    }

    func endTurn() async {
        try? await connection?.send(method: "EndTurn")
    }

    func undoLastMove() async {
        try? await connection?.send(method: "UndoLastMove")
    }

    func abandonGame() async {
        try? await connection?.send(method: "AbandonGame")
    }

    func leaveGame() async {
        try? await connection?.send(method: "LeaveGame")
    }

    func offerDouble() async {
        try? await connection?.send(method: "OfferDouble")
    }

    func acceptDouble() async {
        try? await connection?.send(method: "AcceptDouble")
    }

    func declineDouble() async {
        try? await connection?.send(method: "DeclineDouble")
    }

    func getValidSources() async -> [Int] {
        (try? await connection?.invoke(method: "GetValidSources")) ?? []
    }

    func getValidDestinations(from: Int) async -> [MoveDto] {
        (try? await connection?.invoke(method: "GetValidDestinations", arguments: from)) ?? []
    }

    func createMatch(config: MatchConfig) async {
        try? await connection?.send(method: "CreateMatch", arguments: config)
    }

    func joinMatch(_ matchId: String) async {
        try? await connection?.send(method: "JoinMatch", arguments: matchId)
    }

    func continueMatch(_ matchId: String) async {
        try? await connection?.send(method: "ContinueMatch", arguments: matchId)
    }

    func sendChatMessage(_ message: String) async {
        try? await connection?.send(method: "SendChatMessage", arguments: message)
    }

    // MARK: - Server → Client event handlers (IGameHubClient)

    private func registerHandlers(on hub: HubConnection) {
        // Core game events
        hub.on("GameUpdate") { [weak self] (state: GameState) in
            Task { @MainActor in self?.gameState = state }
        }
        hub.on("GameStart") { [weak self] (state: GameState) in
            Task { @MainActor in self?.gameState = state }
        }
        hub.on("GameOver") { [weak self] (state: GameState) in
            Task { @MainActor in self?.gameState = state }
        }

        // Doubling
        hub.on("DoubleOffered") { [weak self] (offer: DoubleOfferDto) in
            Task { @MainActor in self?.pendingDoubleOffer = offer }
        }
        hub.on("DoubleAccepted") { [weak self] (state: GameState) in
            Task { @MainActor in
                self?.gameState = state
                self?.pendingDoubleOffer = nil
            }
        }

        // Match events
        hub.on("MatchCreated") { [weak self] (data: MatchCreatedDto) in
            Task { @MainActor in self?.matchCreated = data }
        }
        hub.on("MatchUpdate") { [weak self] (data: MatchUpdateDto) in
            Task { @MainActor in self?.matchUpdate = data }
        }
        hub.on("MatchGameStarting") { [weak self] (data: MatchGameStartingDto) in
            Task { @MainActor in self?.matchGameStarting = data }
        }
        hub.on("MatchCompleted") { [weak self] (data: MatchCompletedDto) in
            Task { @MainActor in self?.matchCompleted = data }
        }

        // Chat
        hub.on("ReceiveChatMessage") { [weak self] (sender: String, message: String, _: String) in
            Task { @MainActor in
                self?.chatMessages.append(ChatMessage(sender: sender, text: message))
            }
        }

        // Errors
        hub.on("Error") { [weak self] (msg: String) in
            Task { @MainActor in self?.errorMessage = msg }
        }
    }
}

enum ConnectionState {
    case disconnected, connecting, connected, reconnecting
}
