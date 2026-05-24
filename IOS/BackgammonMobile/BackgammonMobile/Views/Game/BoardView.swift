import SwiftUI

/// Full backgammon board layout.
/// Points 13-24 run left→right on top; points 12-1 run left→right on bottom.
struct BoardView: View {
    @ObservedObject var vm: GameViewModel

    // board is indexed by position (1-24); position 0 = bar
    private func point(_ n: Int) -> GameStateBoard? {
        vm.board.first { Int($0.position) == n }
    }

    private func isSelected(_ n: Int) -> Bool { vm.selectedPoint == n }
    private func isValidDest(_ n: Int) -> Bool {
        vm.validDestinations.contains { Int($0.to) == n }
    }
    private func tap(_ n: Int) {
        Task { await vm.selectPoint(n) }
    }

    var body: some View {
        GeometryReader { geo in
            let boardW = geo.size.width
            let boardH = geo.size.height
            let colW = (boardW - barWidth) / 12
            let rowH = boardH / 2

            ZStack {
                // Board background
                RoundedRectangle(cornerRadius: 8)
                    .fill(Color(.systemGreen).opacity(0.15))
                    .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.brown, lineWidth: 3))

                VStack(spacing: 0) {
                    // Top row: points 13 (left) → 24 (right)
                    HStack(spacing: 0) {
                        ForEach(13...18, id: \.self) { n in
                            PointView(point: n, state: point(n),
                                      isSelected: isSelected(n), isValidDest: isValidDest(n),
                                      pointsDown: true) { tap(n) }
                                .frame(width: colW, height: rowH)
                        }
                        barView(isTop: true, width: barWidth, height: rowH)
                        ForEach(19...24, id: \.self) { n in
                            PointView(point: n, state: point(n),
                                      isSelected: isSelected(n), isValidDest: isValidDest(n),
                                      pointsDown: true) { tap(n) }
                                .frame(width: colW, height: rowH)
                        }
                    }

                    // Bottom row: points 12 (left) → 1 (right)
                    HStack(spacing: 0) {
                        ForEach((7...12).reversed(), id: \.self) { n in
                            PointView(point: n, state: point(n),
                                      isSelected: isSelected(n), isValidDest: isValidDest(n),
                                      pointsDown: false) { tap(n) }
                                .frame(width: colW, height: rowH)
                        }
                        barView(isTop: false, width: barWidth, height: rowH)
                        ForEach((1...6).reversed(), id: \.self) { n in
                            PointView(point: n, state: point(n),
                                      isSelected: isSelected(n), isValidDest: isValidDest(n),
                                      pointsDown: false) { tap(n) }
                                .frame(width: colW, height: rowH)
                        }
                    }
                }
            }
        }
        .aspectRatio(2.0, contentMode: .fit)
        .padding()
    }

    private let barWidth: CGFloat = 36

    @ViewBuilder
    private func barView(isTop: Bool, width: CGFloat, height: CGFloat) -> some View {
        VStack(spacing: 4) {
            if isTop {
                barCheckers(count: vm.redBar, color: .red, label: "R")
            } else {
                barCheckers(count: vm.whiteBar, color: .white, label: "W")
            }
        }
        .frame(width: width, height: height)
        .background(Color.brown.opacity(0.4))
        .onTapGesture {
            // Tapping bar enters a checker from the bar (point 0)
            Task { await vm.selectPoint(0) }
        }
    }

    @ViewBuilder
    private func barCheckers(count: Int, color: Color, label: String) -> some View {
        if count > 0 {
            VStack(spacing: 2) {
                ForEach(0..<min(count, 4), id: \.self) { _ in
                    Circle()
                        .fill(color)
                        .overlay(Circle().stroke(Color.black.opacity(0.3), lineWidth: 1))
                        .frame(width: 26, height: 26)
                }
                if count > 4 {
                    Text("+\(count - 4)")
                        .font(.system(size: 9, weight: .bold))
                        .foregroundStyle(.primary)
                }
            }
        }
    }
}
