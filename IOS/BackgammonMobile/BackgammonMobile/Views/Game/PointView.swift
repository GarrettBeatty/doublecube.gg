import SwiftUI

/// Renders a single point on the board (triangle + stack of checkers).
struct PointView: View {
    let point: Int          // 1-24
    let state: GameStateBoard?
    let isSelected: Bool
    let isValidDest: Bool
    let pointsDown: Bool    // true for top row (points 13-24)
    let onTap: () -> Void

    private var checkerCount: Int { Int(state?.count ?? 0) }
    private var checkerColor: Color {
        guard let c = state?.color else { return .clear }
        return c == 0 ? .white : .red   // CheckerColor.White=0, Red=1
    }

    var body: some View {
        ZStack(alignment: pointsDown ? .top : .bottom) {
            // Triangle
            Triangle(pointsDown: pointsDown)
                .fill(point.isMultiple(of: 2) ? Color.brown.opacity(0.7) : Color.teal.opacity(0.7))
                .overlay(
                    Triangle(pointsDown: pointsDown)
                        .stroke(isSelected ? Color.yellow : .clear, lineWidth: 2)
                )

            // Valid destination highlight
            if isValidDest {
                Circle()
                    .fill(Color.yellow.opacity(0.5))
                    .frame(width: 24, height: 24)
                    .padding(pointsDown ? .top : .bottom, 4)
            }

            // Checkers stacked from the base of the triangle
            if checkerCount > 0 {
                CheckerStackView(
                    count: checkerCount,
                    color: checkerColor,
                    pointsDown: pointsDown
                )
            }
        }
        .contentShape(Rectangle())
        .onTapGesture(perform: onTap)
    }
}

struct CheckerStackView: View {
    let count: Int
    let color: Color
    let pointsDown: Bool

    private let maxVisible = 5
    private let checkerSize: CGFloat = 22

    var body: some View {
        let visible = min(count, maxVisible)
        VStack(spacing: -6) {
            let items = pointsDown ? (0..<visible) : (0..<visible).reversed()
            ForEach(Array(items), id: \.self) { _ in
                Circle()
                    .fill(color)
                    .overlay(Circle().stroke(Color.black.opacity(0.4), lineWidth: 1))
                    .frame(width: checkerSize, height: checkerSize)
            }
            if count > maxVisible {
                Text("+\(count - maxVisible)")
                    .font(.system(size: 9, weight: .bold))
                    .foregroundStyle(.white)
            }
        }
        .padding(pointsDown ? .top : .bottom, 2)
    }
}

struct Triangle: Shape {
    let pointsDown: Bool

    func path(in rect: CGRect) -> Path {
        Path { p in
            if pointsDown {
                p.move(to: CGPoint(x: rect.midX, y: rect.maxY))
                p.addLine(to: CGPoint(x: rect.minX, y: rect.minY))
                p.addLine(to: CGPoint(x: rect.maxX, y: rect.minY))
            } else {
                p.move(to: CGPoint(x: rect.midX, y: rect.minY))
                p.addLine(to: CGPoint(x: rect.minX, y: rect.maxY))
                p.addLine(to: CGPoint(x: rect.maxX, y: rect.maxY))
            }
            p.closeSubpath()
        }
    }
}
