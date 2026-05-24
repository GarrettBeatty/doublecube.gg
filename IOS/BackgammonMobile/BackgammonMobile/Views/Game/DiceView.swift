import SwiftUI

struct DiceView: View {
    let dice: [Int]
    let isMyTurn: Bool

    var body: some View {
        HStack(spacing: 10) {
            ForEach(Array(dice.enumerated()), id: \.offset) { _, value in
                DieFaceView(value: value, active: isMyTurn)
            }
        }
    }
}

struct DieFaceView: View {
    let value: Int
    let active: Bool

    var body: some View {
        ZStack {
            RoundedRectangle(cornerRadius: 6)
                .fill(active ? Color.white : Color.gray.opacity(0.4))
                .overlay(
                    RoundedRectangle(cornerRadius: 6)
                        .stroke(active ? Color.black : Color.gray, lineWidth: 1.5)
                )
                .frame(width: 40, height: 40)
                .shadow(radius: active ? 2 : 0)

            DotLayout(value: value)
                .foregroundStyle(active ? .black : .gray)
        }
    }
}

struct DotLayout: View {
    let value: Int

    var body: some View {
        switch value {
        case 1:
            dotsGrid([[false, false, false], [false, true, false], [false, false, false]])
        case 2:
            dotsGrid([[true, false, false], [false, false, false], [false, false, true]])
        case 3:
            dotsGrid([[true, false, false], [false, true, false], [false, false, true]])
        case 4:
            dotsGrid([[true, false, true], [false, false, false], [true, false, true]])
        case 5:
            dotsGrid([[true, false, true], [false, true, false], [true, false, true]])
        case 6:
            dotsGrid([[true, false, true], [true, false, true], [true, false, true]])
        default:
            Text("\(value)").font(.headline)
        }
    }

    private func dotsGrid(_ pattern: [[Bool]]) -> some View {
        VStack(spacing: 3) {
            ForEach(0..<3, id: \.self) { row in
                HStack(spacing: 3) {
                    ForEach(0..<3, id: \.self) { col in
                        Circle()
                            .frame(width: 8, height: 8)
                            .opacity(pattern[row][col] ? 1 : 0)
                    }
                }
            }
        }
        .padding(5)
    }
}

#Preview {
    DiceView(dice: [3, 5], isMyTurn: true)
        .padding()
}
