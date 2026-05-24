import SwiftUI

struct HomeView: View {
    @ObservedObject private var auth = AuthService.shared
    @ObservedObject private var hub = GameHubService.shared
    @State private var path = NavigationPath()
    @State private var isConnecting = false

    var body: some View {
        NavigationStack(path: $path) {
            VStack(spacing: 32) {
                Spacer()

                VStack(spacing: 4) {
                    Text("Backgammon")
                        .font(.largeTitle.bold())
                    if let user = auth.currentUser {
                        Text("Welcome, \(user.displayName)")
                            .foregroundStyle(.secondary)
                    }
                }

                connectionBadge

                VStack(spacing: 14) {
                    MenuButton(title: "Play vs AI", icon: "cpu") {
                        await startAiGame()
                    }

                    MenuButton(title: "Quick Match", icon: "person.2") {
                        await startQuickMatch()
                    }

                    MenuButton(title: "Join Game", icon: "gamecontroller") {
                        path.append(Route.joinGame)
                    }
                }
                .padding(.horizontal)

                Spacer()

                Button("Log Out", role: .destructive) {
                    auth.logout()
                }
                .font(.footnote)
                .padding(.bottom)
            }
            .navigationDestination(for: Route.self) { route in
                switch route {
                case .game(let id): GameView(gameId: id)
                case .joinGame: JoinGameView(path: $path)
                }
            }
        }
    }

    @ViewBuilder
    private var connectionBadge: some View {
        HStack(spacing: 6) {
            Circle()
                .fill(hub.connectionState == .connected ? .green : .orange)
                .frame(width: 8, height: 8)
            Text(hub.connectionState == .connected ? "Connected" : "Connecting…")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
    }

    private func startAiGame() async {
        await ensureConnected()
        await hub.createAiGame()
    }

    private func startQuickMatch() async {
        await ensureConnected()
        let config = MatchConfig(
            aiType: "greedy",
            displayName: nil,
            isCorrespondence: false,
            isRated: true,
            opponentID: nil,
            opponentType: "OpenLobby",
            targetScore: 7,
            timeControlType: "ChicagoPoint",
            timePerMoveDays: 3
        )
        await hub.createMatch(config: config)
    }

    private func ensureConnected() async {
        guard hub.connectionState != .connected, let token = auth.token else { return }
        try? await hub.connect(token: token)
    }
}

enum Route: Hashable {
    case game(String)
    case joinGame
}

// MARK: - Sub-views

struct MenuButton: View {
    let title: String
    let icon: String
    let action: () async -> Void

    var body: some View {
        Button {
            Task { await action() }
        } label: {
            Label(title, systemImage: icon)
                .frame(maxWidth: .infinity)
                .padding()
        }
        .buttonStyle(.bordered)
        .controlSize(.large)
    }
}

struct JoinGameView: View {
    @Binding var path: NavigationPath
    @State private var gameId = ""
    private let hub = GameHubService.shared

    var body: some View {
        VStack(spacing: 20) {
            TextField("Game ID", text: $gameId)
                .textInputAutocapitalization(.never)
                .autocorrectionDisabled()
                .textFieldStyle(.roundedBorder)
                .padding(.horizontal)

            Button("Join") {
                Task {
                    await hub.joinGame(gameId.isEmpty ? nil : gameId)
                }
            }
            .buttonStyle(.borderedProminent)
        }
        .navigationTitle("Join Game")
    }
}

#Preview {
    HomeView()
}
