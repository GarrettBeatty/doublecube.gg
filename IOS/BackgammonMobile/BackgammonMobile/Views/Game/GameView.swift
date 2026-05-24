import SwiftUI

struct GameView: View {
    let gameId: String?
    @StateObject private var vm = GameViewModel()
    @ObservedObject private var hub = GameHubService.shared
    @Environment(\.dismiss) private var dismiss

    init(gameId: String? = nil) {
        self.gameId = gameId
    }

    var body: some View {
        Group {
            if let state = vm.gameState {
                gameContent(state)
            } else {
                waitingView
            }
        }
        .task { await vm.joinGame(id: gameId) }
        .onChange(of: hub.gameState) { _, state in vm.gameState = state }
        .navigationBarBackButtonHidden(vm.gameState != nil)
        .toolbar {
            if vm.gameState != nil {
                ToolbarItem(placement: .topBarLeading) {
                    Button("Leave", role: .destructive) {
                        Task {
                            await vm.leaveGame()
                            dismiss()
                        }
                    }
                }
            }
        }
        .alert("Error", isPresented: Binding(
            get: { hub.errorMessage != nil },
            set: { if !$0 { hub.errorMessage = nil } }
        )) {
            Button("OK") { hub.errorMessage = nil }
        } message: {
            Text(hub.errorMessage ?? "")
        }
    }

    // MARK: - Main game content

    @ViewBuilder
    private func gameContent(_ state: GameState) -> some View {
        VStack(spacing: 0) {
            playerHeader(name: state.redPlayerName, bornOff: vm.redBornOff,
                         bar: vm.redBar, isCurrentTurn: state.currentPlayer == 1)

            BoardView(vm: vm)

            playerHeader(name: state.whitePlayerName, bornOff: vm.whiteBornOff,
                         bar: vm.whiteBar, isCurrentTurn: state.currentPlayer == 0)

            actionBar(state)
        }
        .overlay(alignment: .center) {
            // GameStatus: 0=WaitingForPlayer, 1=InProgress, 2=Completed
            if state.status == 2 { gameOverOverlay(state) }
        }
        .overlay(alignment: .center) {
            if state.hasReceivedDoubleOffer { doubleOfferOverlay }
        }
    }

    @ViewBuilder
    private func playerHeader(name: String, bornOff: Int, bar: Int, isCurrentTurn: Bool) -> some View {
        HStack {
            Text(name.isEmpty ? "Opponent" : name)
                .font(.headline)
                .foregroundStyle(isCurrentTurn ? .primary : .secondary)
            if isCurrentTurn {
                Image(systemName: "arrowtriangle.right.fill")
                    .font(.caption)
                    .foregroundStyle(.blue)
            }
            Spacer()
            if bar > 0 { Text("Bar: \(bar)").font(.caption).foregroundStyle(.orange) }
            Text("Off: \(bornOff)").font(.caption).foregroundStyle(.secondary)
        }
        .padding(.horizontal)
        .padding(.vertical, 6)
        .background(isCurrentTurn ? Color.blue.opacity(0.08) : Color.clear)
    }

    @ViewBuilder
    private func actionBar(_ state: GameState) -> some View {
        HStack(spacing: 16) {
            DiceView(dice: vm.dice, isMyTurn: vm.isMyTurn)

            Spacer()

            if vm.isMyTurn {
                if state.remainingMoves.isEmpty {
                    // Dice not rolled yet
                    Button("Roll") { Task { await vm.rollDice() } }
                        .buttonStyle(.borderedProminent)
                } else {
                    Button("Undo") { Task { await vm.undoMove() } }
                        .buttonStyle(.bordered)
                    if !state.hasValidMoves {
                        Button("End Turn") { Task { await vm.endTurn() } }
                            .buttonStyle(.borderedProminent)
                    }
                }

                if state.canDouble {
                    Button("Double") { Task { await vm.offerDouble() } }
                        .buttonStyle(.bordered)
                        .tint(.orange)
                }
            } else {
                Text("Opponent's turn")
                    .foregroundStyle(.secondary)
                    .font(.subheadline)
            }
        }
        .padding()
        .background(Color(.systemBackground))
    }

    // MARK: - Overlays

    @ViewBuilder
    private var waitingView: some View {
        VStack(spacing: 16) {
            ProgressView()
            Text("Waiting for game…")
                .foregroundStyle(.secondary)
        }
    }

    @ViewBuilder
    private func gameOverOverlay(_ state: GameState) -> some View {
        VStack(spacing: 16) {
            let won = state.yourColor == state.winner
            Text(won == true ? "You Win! 🎉" : "You Lose")
                .font(.largeTitle.bold())
            if let winType = state.winType {
                Text(winType).foregroundStyle(.secondary)
            }
            Button("Back to Menu") {
                Task {
                    await vm.leaveGame()
                    dismiss()
                }
            }
            .buttonStyle(.borderedProminent)
        }
        .padding(32)
        .background(.regularMaterial, in: RoundedRectangle(cornerRadius: 16))
        .shadow(radius: 12)
    }

    @ViewBuilder
    private var doubleOfferOverlay: some View {
        VStack(spacing: 16) {
            Text("Double offered!")
                .font(.title2.bold())
            if let offer = hub.pendingDoubleOffer {
                Text("New cube value: \(Int(offer.newStakes))")
                    .foregroundStyle(.secondary)
            }
            HStack(spacing: 20) {
                Button("Accept") { Task { await vm.acceptDouble() } }
                    .buttonStyle(.borderedProminent)
                Button("Decline", role: .destructive) { Task { await vm.declineDouble() } }
                    .buttonStyle(.bordered)
            }
        }
        .padding(32)
        .background(.regularMaterial, in: RoundedRectangle(cornerRadius: 16))
        .shadow(radius: 12)
    }
}

#Preview {
    NavigationStack {
        GameView()
    }
}
