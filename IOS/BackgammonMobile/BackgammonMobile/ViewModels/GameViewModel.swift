import Foundation

@MainActor
final class GameViewModel: ObservableObject {
    @Published var gameState: GameState?
    @Published var selectedPoint: Int?
    @Published var validDestinations: [MoveDto] = []
    @Published var isLoading = false

    private let hub = GameHubService.shared

    // MARK: - Computed helpers

    var isMyTurn: Bool { gameState?.isYourTurn ?? false }

    var myColor: Double? { gameState?.yourColor }

    var board: [GameStateBoard] {
        gameState?.board.sorted { $0.position < $1.position } ?? []
    }

    var dice: [Int] { gameState?.remainingMoves.map { Int($0) } ?? [] }

    var whiteBar: Int { Int(gameState?.whiteCheckersOnBar ?? 0) }
    var redBar: Int { Int(gameState?.redCheckersOnBar ?? 0) }
    var whiteBornOff: Int { Int(gameState?.whiteBornOff ?? 0) }
    var redBornOff: Int { Int(gameState?.redBornOff ?? 0) }

    var gameOver: Bool { gameState?.status == 2 } // 2 = GameStatus.Completed

    // MARK: - Game actions

    func joinGame(id: String? = nil) async {
        isLoading = true
        await hub.joinGame(id)
        isLoading = false
    }

    func createAiGame() async {
        await hub.createAiGame()
    }

    func rollDice() async {
        await hub.rollDice()
    }

    func endTurn() async {
        selectedPoint = nil
        validDestinations = []
        await hub.endTurn()
    }

    func undoMove() async {
        await hub.undoLastMove()
        selectedPoint = nil
        validDestinations = []
    }

    func leaveGame() async {
        await hub.leaveGame()
        gameState = nil
        selectedPoint = nil
        validDestinations = []
    }

    // MARK: - Point selection / move flow

    func selectPoint(_ point: Int) async {
        guard isMyTurn else { return }

        if let selected = selectedPoint {
            // Second tap: attempt move
            let dest = validDestinations.first { Int($0.to) == point }
            if dest != nil {
                await makeMove(from: selected, to: point)
            } else {
                // Re-select
                await loadDestinations(from: point)
            }
        } else {
            await loadDestinations(from: point)
        }
    }

    func makeMove(from: Int, to: Int) async {
        selectedPoint = nil
        validDestinations = []
        await hub.makeMove(from: from, to: to)
    }

    private func loadDestinations(from point: Int) async {
        let dests = await hub.getValidDestinations(from: point)
        if dests.isEmpty {
            selectedPoint = nil
            validDestinations = []
        } else {
            selectedPoint = point
            validDestinations = dests
        }
    }

    // MARK: - Doubling cube

    func offerDouble() async { await hub.offerDouble() }
    func acceptDouble() async { await hub.acceptDouble() }
    func declineDouble() async { await hub.declineDouble() }
}
