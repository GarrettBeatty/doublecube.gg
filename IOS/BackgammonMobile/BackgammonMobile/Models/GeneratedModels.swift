// This file was generated from JSON Schema using quicktype, do not modify it directly.
// To parse the JSON, add this file to your project and do:
//
//   let checkerColor = try CheckerColor(json)
//   let bestMovesAnalysisDto = try BestMovesAnalysisDto(json)
//   let botInfoDto = try BotInfoDto(json)
//   let dailyPuzzleDto = try DailyPuzzleDto(json)
//   let friendDto = try FriendDto(json)
//   let friendshipStatus = try FriendshipStatus(json)
//   let gameHistoryDto = try GameHistoryDto(json)
//   let gameState = try GameState(json)
//   let gameStatus = try GameStatus(json)
//   let gameSummaryDto = try GameSummaryDto(json)
//   let leaderboardEntryDto = try LeaderboardEntryDto(json)
//   let matchConfig = try MatchConfig(json)
//   let matchGameSummary = try MatchGameSummary(json)
//   let matchStateDto = try MatchStateDto(json)
//   let moveDto = try MoveDto(json)
//   let moveSequenceDto = try MoveSequenceDto(json)
//   let onlinePlayerDto = try OnlinePlayerDto(json)
//   let onlinePlayerStatus = try OnlinePlayerStatus(json)
//   let playerProfileDto = try PlayerProfileDto(json)
//   let playerSearchResultDto = try PlayerSearchResultDto(json)
//   let pointState = try PointState(json)
//   let pointStateDto = try PointStateDto(json)
//   let positionEvaluationDto = try PositionEvaluationDto(json)
//   let positionFeaturesDto = try PositionFeaturesDto(json)
//   let profilePrivacyLevel = try ProfilePrivacyLevel(json)
//   let puzzleResultDto = try PuzzleResultDto(json)
//   let puzzleStreakInfo = try PuzzleStreakInfo(json)
//   let puzzleValidMovesRequest = try PuzzleValidMovesRequest(json)
//   let ratingBucketDto = try RatingBucketDto(json)
//   let ratingDistributionDto = try RatingDistributionDto(json)
//   let ratingHistoryEntryDto = try RatingHistoryEntryDto(json)
//   let recentOpponentDto = try RecentOpponentDto(json)
//   let turnSnapshotDto = try TurnSnapshotDto(json)
//   let userStats = try UserStats(json)
//   let activeGameBoardPointDto = try ActiveGameBoardPointDto(json)
//   let activeGameDto = try ActiveGameDto(json)
//   let activeMatchDto = try ActiveMatchDto(json)
//   let chatHistoryDto = try ChatHistoryDto(json)
//   let chatMessageDto = try ChatMessageDto(json)
//   let checkerColorDto = try CheckerColorDto(json)
//   let correspondenceLobbyCreatedDto = try CorrespondenceLobbyCreatedDto(json)
//   let correspondenceMatchInviteDto = try CorrespondenceMatchInviteDto(json)
//   let correspondenceTurnNotificationDto = try CorrespondenceTurnNotificationDto(json)
//   let doubleOfferDto = try DoubleOfferDto(json)
//   let gameStatusDto = try GameStatusDto(json)
//   let lobbyCreatedDto = try LobbyCreatedDto(json)
//   let matchCompletedDto = try MatchCompletedDto(json)
//   let matchContinuedDto = try MatchContinuedDto(json)
//   let matchCreatedDto = try MatchCreatedDto(json)
//   let matchFinalScoreDto = try MatchFinalScoreDto(json)
//   let matchGameCompletedDto = try MatchGameCompletedDto(json)
//   let matchGameDto = try MatchGameDto(json)
//   let matchGameStartingDto = try MatchGameStartingDto(json)
//   let matchInviteDto = try MatchInviteDto(json)
//   let matchLobbyDto = try MatchLobbyDto(json)
//   let matchResultsDto = try MatchResultsDto(json)
//   let matchScoreDto = try MatchScoreDto(json)
//   let matchStatusDto = try MatchStatusDto(json)
//   let matchSummaryDto = try MatchSummaryDto(json)
//   let matchUpdateDto = try MatchUpdateDto(json)
//   let opponentJoinedMatchDto = try OpponentJoinedMatchDto(json)
//   let opponentTypeDto = try OpponentTypeDto(json)
//   let playerTimedOutDto = try PlayerTimedOutDto(json)
//   let recentGameDto = try RecentGameDto(json)
//   let timeControlTypeDto = try TimeControlTypeDto(json)
//   let timeUpdateDto = try TimeUpdateDto(json)
//   let correspondenceGameDto = try CorrespondenceGameDto(json)
//   let correspondenceGamesResponse = try CorrespondenceGamesResponse(json)

import Foundation

/// Transpiled from Backgammon.Server.Models.BestMovesAnalysisDto
// MARK: - BestMovesAnalysisDto
public struct BestMovesAnalysisDto: Codable {
    /// Transpiled from Backgammon.Server.Models.PositionEvaluationDto
    public let initialEvaluation: InitialEvaluation
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.MoveSequenceDto>
    public let topMoves: [TopMove]
    /// Transpiled from int
    public let totalSequencesExplored: Double

    public init(initialEvaluation: InitialEvaluation, topMoves: [TopMove], totalSequencesExplored: Double) {
        self.initialEvaluation = initialEvaluation
        self.topMoves = topMoves
        self.totalSequencesExplored = totalSequencesExplored
    }
}

// MARK: BestMovesAnalysisDto convenience initializers and mutators

public extension BestMovesAnalysisDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(BestMovesAnalysisDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        initialEvaluation: InitialEvaluation? = nil,
        topMoves: [TopMove]? = nil,
        totalSequencesExplored: Double? = nil
    ) -> BestMovesAnalysisDto {
        return BestMovesAnalysisDto(
            initialEvaluation: initialEvaluation ?? self.initialEvaluation,
            topMoves: topMoves ?? self.topMoves,
            totalSequencesExplored: totalSequencesExplored ?? self.totalSequencesExplored
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PositionEvaluationDto
// MARK: - InitialEvaluation
public struct InitialEvaluation: Codable {
    /// Transpiled from double
    public let backgammonProbability: Double
    /// Transpiled from double
    public let equity: Double
    /// Transpiled from string
    public let evaluatorName: String
    /// Transpiled from Backgammon.Server.Models.PositionFeaturesDto
    public let features: InitialEvaluationFeatures
    /// Transpiled from double
    public let gammonProbability: Double
    /// Transpiled from double
    public let winProbability: Double

    public init(backgammonProbability: Double, equity: Double, evaluatorName: String, features: InitialEvaluationFeatures, gammonProbability: Double, winProbability: Double) {
        self.backgammonProbability = backgammonProbability
        self.equity = equity
        self.evaluatorName = evaluatorName
        self.features = features
        self.gammonProbability = gammonProbability
        self.winProbability = winProbability
    }
}

// MARK: InitialEvaluation convenience initializers and mutators

public extension InitialEvaluation {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(InitialEvaluation.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        backgammonProbability: Double? = nil,
        equity: Double? = nil,
        evaluatorName: String? = nil,
        features: InitialEvaluationFeatures? = nil,
        gammonProbability: Double? = nil,
        winProbability: Double? = nil
    ) -> InitialEvaluation {
        return InitialEvaluation(
            backgammonProbability: backgammonProbability ?? self.backgammonProbability,
            equity: equity ?? self.equity,
            evaluatorName: evaluatorName ?? self.evaluatorName,
            features: features ?? self.features,
            gammonProbability: gammonProbability ?? self.gammonProbability,
            winProbability: winProbability ?? self.winProbability
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PositionFeaturesDto
// MARK: - InitialEvaluationFeatures
public struct InitialEvaluationFeatures: Codable {
    /// Transpiled from int
    public let anchorsInOpponentHome: Double
    /// Transpiled from double
    public let bearoffEfficiency: Double
    /// Transpiled from int
    public let blotCount: Double
    /// Transpiled from int
    public let blotExposure: Double
    /// Transpiled from int
    public let checkersBornOff: Double
    /// Transpiled from int
    public let checkersOnBar: Double
    /// Transpiled from double
    public let distribution: Double
    /// Transpiled from int
    public let homeboardCoverage: Double
    /// Transpiled from bool
    public let isContact: Bool
    /// Transpiled from bool
    public let isRace: Bool
    /// Transpiled from int
    public let pipCount: Double
    /// Transpiled from int
    public let pipDifference: Double
    /// Transpiled from int
    public let primeLength: Double
    /// Transpiled from int
    public let wastedPips: Double

    public init(anchorsInOpponentHome: Double, bearoffEfficiency: Double, blotCount: Double, blotExposure: Double, checkersBornOff: Double, checkersOnBar: Double, distribution: Double, homeboardCoverage: Double, isContact: Bool, isRace: Bool, pipCount: Double, pipDifference: Double, primeLength: Double, wastedPips: Double) {
        self.anchorsInOpponentHome = anchorsInOpponentHome
        self.bearoffEfficiency = bearoffEfficiency
        self.blotCount = blotCount
        self.blotExposure = blotExposure
        self.checkersBornOff = checkersBornOff
        self.checkersOnBar = checkersOnBar
        self.distribution = distribution
        self.homeboardCoverage = homeboardCoverage
        self.isContact = isContact
        self.isRace = isRace
        self.pipCount = pipCount
        self.pipDifference = pipDifference
        self.primeLength = primeLength
        self.wastedPips = wastedPips
    }
}

// MARK: InitialEvaluationFeatures convenience initializers and mutators

public extension InitialEvaluationFeatures {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(InitialEvaluationFeatures.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        anchorsInOpponentHome: Double? = nil,
        bearoffEfficiency: Double? = nil,
        blotCount: Double? = nil,
        blotExposure: Double? = nil,
        checkersBornOff: Double? = nil,
        checkersOnBar: Double? = nil,
        distribution: Double? = nil,
        homeboardCoverage: Double? = nil,
        isContact: Bool? = nil,
        isRace: Bool? = nil,
        pipCount: Double? = nil,
        pipDifference: Double? = nil,
        primeLength: Double? = nil,
        wastedPips: Double? = nil
    ) -> InitialEvaluationFeatures {
        return InitialEvaluationFeatures(
            anchorsInOpponentHome: anchorsInOpponentHome ?? self.anchorsInOpponentHome,
            bearoffEfficiency: bearoffEfficiency ?? self.bearoffEfficiency,
            blotCount: blotCount ?? self.blotCount,
            blotExposure: blotExposure ?? self.blotExposure,
            checkersBornOff: checkersBornOff ?? self.checkersBornOff,
            checkersOnBar: checkersOnBar ?? self.checkersOnBar,
            distribution: distribution ?? self.distribution,
            homeboardCoverage: homeboardCoverage ?? self.homeboardCoverage,
            isContact: isContact ?? self.isContact,
            isRace: isRace ?? self.isRace,
            pipCount: pipCount ?? self.pipCount,
            pipDifference: pipDifference ?? self.pipDifference,
            primeLength: primeLength ?? self.primeLength,
            wastedPips: wastedPips ?? self.wastedPips
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MoveSequenceDto
// MARK: - TopMove
public struct TopMove: Codable {
    /// Transpiled from double
    public let equity: Double
    /// Transpiled from double
    public let equityGain: Double
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.MoveDto>
    public let moves: [TopMoveMove]
    /// Transpiled from string
    public let notation: String

    public init(equity: Double, equityGain: Double, moves: [TopMoveMove], notation: String) {
        self.equity = equity
        self.equityGain = equityGain
        self.moves = moves
        self.notation = notation
    }
}

// MARK: TopMove convenience initializers and mutators

public extension TopMove {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(TopMove.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        equity: Double? = nil,
        equityGain: Double? = nil,
        moves: [TopMoveMove]? = nil,
        notation: String? = nil
    ) -> TopMove {
        return TopMove(
            equity: equity ?? self.equity,
            equityGain: equityGain ?? self.equityGain,
            moves: moves ?? self.moves,
            notation: notation ?? self.notation
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MoveDto
// MARK: - TopMoveMove
public struct TopMoveMove: Codable {
    /// Transpiled from int[]?
    public let diceUsed: [Double]?
    /// Transpiled from int
    public let dieValue: Double
    /// Transpiled from int
    public let from: Double
    /// Transpiled from int[]?
    public let intermediatePoints: [Double]?
    /// Transpiled from bool
    public let isCombinedMove: Bool
    /// Transpiled from bool
    public let isHit: Bool
    /// Transpiled from int
    public let to: Double

    public init(diceUsed: [Double]?, dieValue: Double, from: Double, intermediatePoints: [Double]?, isCombinedMove: Bool, isHit: Bool, to: Double) {
        self.diceUsed = diceUsed
        self.dieValue = dieValue
        self.from = from
        self.intermediatePoints = intermediatePoints
        self.isCombinedMove = isCombinedMove
        self.isHit = isHit
        self.to = to
    }
}

// MARK: TopMoveMove convenience initializers and mutators

public extension TopMoveMove {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(TopMoveMove.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        diceUsed: [Double]?? = nil,
        dieValue: Double? = nil,
        from: Double? = nil,
        intermediatePoints: [Double]?? = nil,
        isCombinedMove: Bool? = nil,
        isHit: Bool? = nil,
        to: Double? = nil
    ) -> TopMoveMove {
        return TopMoveMove(
            diceUsed: diceUsed ?? self.diceUsed,
            dieValue: dieValue ?? self.dieValue,
            from: from ?? self.from,
            intermediatePoints: intermediatePoints ?? self.intermediatePoints,
            isCombinedMove: isCombinedMove ?? self.isCombinedMove,
            isHit: isHit ?? self.isHit,
            to: to ?? self.to
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.BotInfoDto
// MARK: - BotInfoDto
public struct BotInfoDto: Codable {
    /// Transpiled from string
    public let description: String
    /// Transpiled from int
    public let difficulty: Double
    /// Transpiled from string
    public let icon: String
    /// Transpiled from string
    public let id: String
    /// Transpiled from bool
    public let isAvailable: Bool
    /// Transpiled from string
    public let name: String

    public init(description: String, difficulty: Double, icon: String, id: String, isAvailable: Bool, name: String) {
        self.description = description
        self.difficulty = difficulty
        self.icon = icon
        self.id = id
        self.isAvailable = isAvailable
        self.name = name
    }
}

// MARK: BotInfoDto convenience initializers and mutators

public extension BotInfoDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(BotInfoDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        description: String? = nil,
        difficulty: Double? = nil,
        icon: String? = nil,
        id: String? = nil,
        isAvailable: Bool? = nil,
        name: String? = nil
    ) -> BotInfoDto {
        return BotInfoDto(
            description: description ?? self.description,
            difficulty: difficulty ?? self.difficulty,
            icon: icon ?? self.icon,
            id: id ?? self.id,
            isAvailable: isAvailable ?? self.isAvailable,
            name: name ?? self.name
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.DailyPuzzleDto
// MARK: - DailyPuzzleDto
public struct DailyPuzzleDto: Codable {
    /// Transpiled from bool
    public let alreadySolved: Bool
    /// Transpiled from int
    public let attemptsToday: Double
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.MoveDto>?
    public let bestMoves: [DailyPuzzleDtoBestMove]?
    /// Transpiled from string?
    public let bestMovesNotation: String?
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.PointStateDto>
    public let boardState: [DailyPuzzleDtoBoardState]
    /// Transpiled from string
    public let currentPlayer: String
    /// Transpiled from int[]
    public let dice: [Double]
    /// Transpiled from string
    public let puzzleDate: String
    /// Transpiled from string
    public let puzzleID: String
    /// Transpiled from int
    public let redBornOff: Double
    /// Transpiled from int
    public let redCheckersOnBar: Double
    /// Transpiled from int
    public let whiteBornOff: Double
    /// Transpiled from int
    public let whiteCheckersOnBar: Double

    public enum CodingKeys: String, CodingKey {
        case alreadySolved, attemptsToday, bestMoves, bestMovesNotation, boardState, currentPlayer, dice, puzzleDate
        case puzzleID = "puzzleId"
        case redBornOff, redCheckersOnBar, whiteBornOff, whiteCheckersOnBar
    }

    public init(alreadySolved: Bool, attemptsToday: Double, bestMoves: [DailyPuzzleDtoBestMove]?, bestMovesNotation: String?, boardState: [DailyPuzzleDtoBoardState], currentPlayer: String, dice: [Double], puzzleDate: String, puzzleID: String, redBornOff: Double, redCheckersOnBar: Double, whiteBornOff: Double, whiteCheckersOnBar: Double) {
        self.alreadySolved = alreadySolved
        self.attemptsToday = attemptsToday
        self.bestMoves = bestMoves
        self.bestMovesNotation = bestMovesNotation
        self.boardState = boardState
        self.currentPlayer = currentPlayer
        self.dice = dice
        self.puzzleDate = puzzleDate
        self.puzzleID = puzzleID
        self.redBornOff = redBornOff
        self.redCheckersOnBar = redCheckersOnBar
        self.whiteBornOff = whiteBornOff
        self.whiteCheckersOnBar = whiteCheckersOnBar
    }
}

// MARK: DailyPuzzleDto convenience initializers and mutators

public extension DailyPuzzleDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(DailyPuzzleDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        alreadySolved: Bool? = nil,
        attemptsToday: Double? = nil,
        bestMoves: [DailyPuzzleDtoBestMove]?? = nil,
        bestMovesNotation: String?? = nil,
        boardState: [DailyPuzzleDtoBoardState]? = nil,
        currentPlayer: String? = nil,
        dice: [Double]? = nil,
        puzzleDate: String? = nil,
        puzzleID: String? = nil,
        redBornOff: Double? = nil,
        redCheckersOnBar: Double? = nil,
        whiteBornOff: Double? = nil,
        whiteCheckersOnBar: Double? = nil
    ) -> DailyPuzzleDto {
        return DailyPuzzleDto(
            alreadySolved: alreadySolved ?? self.alreadySolved,
            attemptsToday: attemptsToday ?? self.attemptsToday,
            bestMoves: bestMoves ?? self.bestMoves,
            bestMovesNotation: bestMovesNotation ?? self.bestMovesNotation,
            boardState: boardState ?? self.boardState,
            currentPlayer: currentPlayer ?? self.currentPlayer,
            dice: dice ?? self.dice,
            puzzleDate: puzzleDate ?? self.puzzleDate,
            puzzleID: puzzleID ?? self.puzzleID,
            redBornOff: redBornOff ?? self.redBornOff,
            redCheckersOnBar: redCheckersOnBar ?? self.redCheckersOnBar,
            whiteBornOff: whiteBornOff ?? self.whiteBornOff,
            whiteCheckersOnBar: whiteCheckersOnBar ?? self.whiteCheckersOnBar
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MoveDto
// MARK: - DailyPuzzleDtoBestMove
public struct DailyPuzzleDtoBestMove: Codable {
    /// Transpiled from int[]?
    public let diceUsed: [Double]?
    /// Transpiled from int
    public let dieValue: Double
    /// Transpiled from int
    public let from: Double
    /// Transpiled from int[]?
    public let intermediatePoints: [Double]?
    /// Transpiled from bool
    public let isCombinedMove: Bool
    /// Transpiled from bool
    public let isHit: Bool
    /// Transpiled from int
    public let to: Double

    public init(diceUsed: [Double]?, dieValue: Double, from: Double, intermediatePoints: [Double]?, isCombinedMove: Bool, isHit: Bool, to: Double) {
        self.diceUsed = diceUsed
        self.dieValue = dieValue
        self.from = from
        self.intermediatePoints = intermediatePoints
        self.isCombinedMove = isCombinedMove
        self.isHit = isHit
        self.to = to
    }
}

// MARK: DailyPuzzleDtoBestMove convenience initializers and mutators

public extension DailyPuzzleDtoBestMove {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(DailyPuzzleDtoBestMove.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        diceUsed: [Double]?? = nil,
        dieValue: Double? = nil,
        from: Double? = nil,
        intermediatePoints: [Double]?? = nil,
        isCombinedMove: Bool? = nil,
        isHit: Bool? = nil,
        to: Double? = nil
    ) -> DailyPuzzleDtoBestMove {
        return DailyPuzzleDtoBestMove(
            diceUsed: diceUsed ?? self.diceUsed,
            dieValue: dieValue ?? self.dieValue,
            from: from ?? self.from,
            intermediatePoints: intermediatePoints ?? self.intermediatePoints,
            isCombinedMove: isCombinedMove ?? self.isCombinedMove,
            isHit: isHit ?? self.isHit,
            to: to ?? self.to
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PointStateDto
// MARK: - DailyPuzzleDtoBoardState
public struct DailyPuzzleDtoBoardState: Codable {
    /// Transpiled from string?
    public let color: String?
    /// Transpiled from int
    public let count: Double
    /// Transpiled from int
    public let position: Double

    public init(color: String?, count: Double, position: Double) {
        self.color = color
        self.count = count
        self.position = position
    }
}

// MARK: DailyPuzzleDtoBoardState convenience initializers and mutators

public extension DailyPuzzleDtoBoardState {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(DailyPuzzleDtoBoardState.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        color: String?? = nil,
        count: Double? = nil,
        position: Double? = nil
    ) -> DailyPuzzleDtoBoardState {
        return DailyPuzzleDtoBoardState(
            color: color ?? self.color,
            count: count ?? self.count,
            position: position ?? self.position
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.FriendDto
// MARK: - FriendDto
public struct FriendDto: Codable {
    /// Transpiled from string
    public let displayName: String
    /// Transpiled from string
    public let initiatedBy: String
    /// Transpiled from bool
    public let isOnline: Bool
    /// Transpiled from Backgammon.Server.Models.FriendshipStatus
    public let status: Double
    /// Transpiled from string
    public let userID: String
    /// Transpiled from string
    public let username: String

    public enum CodingKeys: String, CodingKey {
        case displayName, initiatedBy, isOnline, status
        case userID = "userId"
        case username
    }

    public init(displayName: String, initiatedBy: String, isOnline: Bool, status: Double, userID: String, username: String) {
        self.displayName = displayName
        self.initiatedBy = initiatedBy
        self.isOnline = isOnline
        self.status = status
        self.userID = userID
        self.username = username
    }
}

// MARK: FriendDto convenience initializers and mutators

public extension FriendDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(FriendDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        displayName: String? = nil,
        initiatedBy: String? = nil,
        isOnline: Bool? = nil,
        status: Double? = nil,
        userID: String? = nil,
        username: String? = nil
    ) -> FriendDto {
        return FriendDto(
            displayName: displayName ?? self.displayName,
            initiatedBy: initiatedBy ?? self.initiatedBy,
            isOnline: isOnline ?? self.isOnline,
            status: status ?? self.status,
            userID: userID ?? self.userID,
            username: username ?? self.username
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.GameHistoryDto
// MARK: - GameHistoryDto
public struct GameHistoryDto: Codable {
    /// Transpiled from System.DateTime
    public let completedAt: String?
    /// Transpiled from System.DateTime
    public let createdAt: String
    /// Transpiled from int
    public let doublingCubeValue: Double
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from string?
    public let matchID: String?
    /// Transpiled from string?
    public let redPlayerName: String?
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.TurnSnapshotDto>
    public let turnHistory: [GameHistoryDtoTurnHistory]
    /// Transpiled from string?
    public let whitePlayerName: String?
    /// Transpiled from string?
    public let winner: String?
    /// Transpiled from string?
    public let winType: String?

    public enum CodingKeys: String, CodingKey {
        case completedAt, createdAt, doublingCubeValue
        case gameID = "gameId"
        case matchID = "matchId"
        case redPlayerName, turnHistory, whitePlayerName, winner, winType
    }

    public init(completedAt: String?, createdAt: String, doublingCubeValue: Double, gameID: String, matchID: String?, redPlayerName: String?, turnHistory: [GameHistoryDtoTurnHistory], whitePlayerName: String?, winner: String?, winType: String?) {
        self.completedAt = completedAt
        self.createdAt = createdAt
        self.doublingCubeValue = doublingCubeValue
        self.gameID = gameID
        self.matchID = matchID
        self.redPlayerName = redPlayerName
        self.turnHistory = turnHistory
        self.whitePlayerName = whitePlayerName
        self.winner = winner
        self.winType = winType
    }
}

// MARK: GameHistoryDto convenience initializers and mutators

public extension GameHistoryDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(GameHistoryDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        completedAt: String?? = nil,
        createdAt: String? = nil,
        doublingCubeValue: Double? = nil,
        gameID: String? = nil,
        matchID: String?? = nil,
        redPlayerName: String?? = nil,
        turnHistory: [GameHistoryDtoTurnHistory]? = nil,
        whitePlayerName: String?? = nil,
        winner: String?? = nil,
        winType: String?? = nil
    ) -> GameHistoryDto {
        return GameHistoryDto(
            completedAt: completedAt ?? self.completedAt,
            createdAt: createdAt ?? self.createdAt,
            doublingCubeValue: doublingCubeValue ?? self.doublingCubeValue,
            gameID: gameID ?? self.gameID,
            matchID: matchID ?? self.matchID,
            redPlayerName: redPlayerName ?? self.redPlayerName,
            turnHistory: turnHistory ?? self.turnHistory,
            whitePlayerName: whitePlayerName ?? self.whitePlayerName,
            winner: winner ?? self.winner,
            winType: winType ?? self.winType
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.TurnSnapshotDto
// MARK: - GameHistoryDtoTurnHistory
public struct GameHistoryDtoTurnHistory: Codable {
    /// Transpiled from string?
    public let cubeOwner: String?
    /// Transpiled from int
    public let cubeValue: Double
    /// Transpiled from int[]
    public let diceRolled: [Double]
    /// Transpiled from string?
    public let doublingAction: String?
    /// Transpiled from System.Collections.Generic.List<string>
    public let moves: [String]
    /// Transpiled from string
    public let player: String
    /// Transpiled from string
    public let positionSgf: String
    /// Transpiled from int
    public let turnNumber: Double

    public init(cubeOwner: String?, cubeValue: Double, diceRolled: [Double], doublingAction: String?, moves: [String], player: String, positionSgf: String, turnNumber: Double) {
        self.cubeOwner = cubeOwner
        self.cubeValue = cubeValue
        self.diceRolled = diceRolled
        self.doublingAction = doublingAction
        self.moves = moves
        self.player = player
        self.positionSgf = positionSgf
        self.turnNumber = turnNumber
    }
}

// MARK: GameHistoryDtoTurnHistory convenience initializers and mutators

public extension GameHistoryDtoTurnHistory {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(GameHistoryDtoTurnHistory.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        cubeOwner: String?? = nil,
        cubeValue: Double? = nil,
        diceRolled: [Double]? = nil,
        doublingAction: String?? = nil,
        moves: [String]? = nil,
        player: String? = nil,
        positionSgf: String? = nil,
        turnNumber: Double? = nil
    ) -> GameHistoryDtoTurnHistory {
        return GameHistoryDtoTurnHistory(
            cubeOwner: cubeOwner ?? self.cubeOwner,
            cubeValue: cubeValue ?? self.cubeValue,
            diceRolled: diceRolled ?? self.diceRolled,
            doublingAction: doublingAction ?? self.doublingAction,
            moves: moves ?? self.moves,
            player: player ?? self.player,
            positionSgf: positionSgf ?? self.positionSgf,
            turnNumber: turnNumber ?? self.turnNumber
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.GameState
// MARK: - GameState
public struct GameState: Codable {
    /// Transpiled from Backgammon.Server.Models.PointState[]
    public let board: [GameStateBoard]
    /// Transpiled from bool
    public let canDouble: Bool
    /// Transpiled from int[]
    public let currentDice: [Double]
    /// Transpiled from Backgammon.Core.CheckerColor
    public let currentPlayer: Double
    /// Transpiled from System.Collections.Generic.List<string>
    public let currentTurnMoves: [String]
    /// Transpiled from int
    public let delaySeconds: Double?
    /// Transpiled from int[]
    public let dice: [Double]
    /// Transpiled from string?
    public let doublingCubeOwner: String?
    /// Transpiled from int
    public let doublingCubeValue: Double
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let hasPendingDoubleOffer: Bool
    /// Transpiled from bool
    public let hasReceivedDoubleOffer: Bool
    /// Transpiled from bool
    public let hasValidMoves: Bool
    /// Transpiled from bool
    public let isAnalysisMode: Bool
    /// Transpiled from bool
    public let isAwaitingDoubleResponse: Bool
    /// Transpiled from bool
    public let isCorrespondence: Bool
    /// Transpiled from bool
    public let isCrawfordGame: Bool?
    /// Transpiled from bool
    public let isOpeningRoll: Bool
    /// Transpiled from bool
    public let isOpeningRollTie: Bool
    /// Transpiled from bool
    public let isRated: Bool
    /// Transpiled from bool
    public let isYourTurn: Bool
    /// Transpiled from string
    public let leaveGameAction: String
    /// Transpiled from string?
    public let matchID: String?
    /// Transpiled from int
    public let movesMadeThisTurn: Double
    /// Transpiled from int
    public let pendingDoubleNewValue: Double
    /// Transpiled from int
    public let player1Score: Double?
    /// Transpiled from int
    public let player2Score: Double?
    /// Transpiled from int
    public let redBornOff: Double
    /// Transpiled from int
    public let redCheckersOnBar: Double
    /// Transpiled from double
    public let redDelayRemaining: Double?
    /// Transpiled from bool
    public let redIsInDelay: Bool?
    /// Transpiled from int
    public let redOpeningRoll: Double?
    /// Transpiled from int
    public let redPipCount: Double
    /// Transpiled from string
    public let redPlayerID: String
    /// Transpiled from string
    public let redPlayerName: String
    /// Transpiled from int
    public let redRating: Double?
    /// Transpiled from int
    public let redRatingChange: Double?
    /// Transpiled from double
    public let redReserveSeconds: Double?
    /// Transpiled from string?
    public let redUsername: String?
    /// Transpiled from int[]
    public let remainingMoves: [Double]
    /// Transpiled from Backgammon.Server.Models.GameStatus
    public let status: Double
    /// Transpiled from int
    public let targetScore: Double?
    /// Transpiled from string?
    public let timeControlType: String?
    /// Transpiled from int
    public let timePerMoveDays: Double?
    /// Transpiled from System.DateTime
    public let turnDeadline: String?
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.TurnSnapshotDto>
    public let turnHistory: [GameStateTurnHistory]
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.MoveDto>
    public let validMoves: [GameStateValidMove]
    /// Transpiled from int
    public let whiteBornOff: Double
    /// Transpiled from int
    public let whiteCheckersOnBar: Double
    /// Transpiled from double
    public let whiteDelayRemaining: Double?
    /// Transpiled from bool
    public let whiteIsInDelay: Bool?
    /// Transpiled from int
    public let whiteOpeningRoll: Double?
    /// Transpiled from int
    public let whitePipCount: Double
    /// Transpiled from string
    public let whitePlayerID: String
    /// Transpiled from string
    public let whitePlayerName: String
    /// Transpiled from int
    public let whiteRating: Double?
    /// Transpiled from int
    public let whiteRatingChange: Double?
    /// Transpiled from double
    public let whiteReserveSeconds: Double?
    /// Transpiled from string?
    public let whiteUsername: String?
    /// Transpiled from Backgammon.Core.CheckerColor
    public let winner: Double?
    /// Transpiled from string?
    public let winType: String?
    /// Transpiled from Backgammon.Core.CheckerColor
    public let yourColor: Double?

    public enum CodingKeys: String, CodingKey {
        case board, canDouble, currentDice, currentPlayer, currentTurnMoves, delaySeconds, dice, doublingCubeOwner, doublingCubeValue
        case gameID = "gameId"
        case hasPendingDoubleOffer, hasReceivedDoubleOffer, hasValidMoves, isAnalysisMode, isAwaitingDoubleResponse, isCorrespondence, isCrawfordGame, isOpeningRoll, isOpeningRollTie, isRated, isYourTurn, leaveGameAction
        case matchID = "matchId"
        case movesMadeThisTurn, pendingDoubleNewValue, player1Score, player2Score, redBornOff, redCheckersOnBar, redDelayRemaining, redIsInDelay, redOpeningRoll, redPipCount
        case redPlayerID = "redPlayerId"
        case redPlayerName, redRating, redRatingChange, redReserveSeconds, redUsername, remainingMoves, status, targetScore, timeControlType, timePerMoveDays, turnDeadline, turnHistory, validMoves, whiteBornOff, whiteCheckersOnBar, whiteDelayRemaining, whiteIsInDelay, whiteOpeningRoll, whitePipCount
        case whitePlayerID = "whitePlayerId"
        case whitePlayerName, whiteRating, whiteRatingChange, whiteReserveSeconds, whiteUsername, winner, winType, yourColor
    }

    public init(board: [GameStateBoard], canDouble: Bool, currentDice: [Double], currentPlayer: Double, currentTurnMoves: [String], delaySeconds: Double?, dice: [Double], doublingCubeOwner: String?, doublingCubeValue: Double, gameID: String, hasPendingDoubleOffer: Bool, hasReceivedDoubleOffer: Bool, hasValidMoves: Bool, isAnalysisMode: Bool, isAwaitingDoubleResponse: Bool, isCorrespondence: Bool, isCrawfordGame: Bool?, isOpeningRoll: Bool, isOpeningRollTie: Bool, isRated: Bool, isYourTurn: Bool, leaveGameAction: String, matchID: String?, movesMadeThisTurn: Double, pendingDoubleNewValue: Double, player1Score: Double?, player2Score: Double?, redBornOff: Double, redCheckersOnBar: Double, redDelayRemaining: Double?, redIsInDelay: Bool?, redOpeningRoll: Double?, redPipCount: Double, redPlayerID: String, redPlayerName: String, redRating: Double?, redRatingChange: Double?, redReserveSeconds: Double?, redUsername: String?, remainingMoves: [Double], status: Double, targetScore: Double?, timeControlType: String?, timePerMoveDays: Double?, turnDeadline: String?, turnHistory: [GameStateTurnHistory], validMoves: [GameStateValidMove], whiteBornOff: Double, whiteCheckersOnBar: Double, whiteDelayRemaining: Double?, whiteIsInDelay: Bool?, whiteOpeningRoll: Double?, whitePipCount: Double, whitePlayerID: String, whitePlayerName: String, whiteRating: Double?, whiteRatingChange: Double?, whiteReserveSeconds: Double?, whiteUsername: String?, winner: Double?, winType: String?, yourColor: Double?) {
        self.board = board
        self.canDouble = canDouble
        self.currentDice = currentDice
        self.currentPlayer = currentPlayer
        self.currentTurnMoves = currentTurnMoves
        self.delaySeconds = delaySeconds
        self.dice = dice
        self.doublingCubeOwner = doublingCubeOwner
        self.doublingCubeValue = doublingCubeValue
        self.gameID = gameID
        self.hasPendingDoubleOffer = hasPendingDoubleOffer
        self.hasReceivedDoubleOffer = hasReceivedDoubleOffer
        self.hasValidMoves = hasValidMoves
        self.isAnalysisMode = isAnalysisMode
        self.isAwaitingDoubleResponse = isAwaitingDoubleResponse
        self.isCorrespondence = isCorrespondence
        self.isCrawfordGame = isCrawfordGame
        self.isOpeningRoll = isOpeningRoll
        self.isOpeningRollTie = isOpeningRollTie
        self.isRated = isRated
        self.isYourTurn = isYourTurn
        self.leaveGameAction = leaveGameAction
        self.matchID = matchID
        self.movesMadeThisTurn = movesMadeThisTurn
        self.pendingDoubleNewValue = pendingDoubleNewValue
        self.player1Score = player1Score
        self.player2Score = player2Score
        self.redBornOff = redBornOff
        self.redCheckersOnBar = redCheckersOnBar
        self.redDelayRemaining = redDelayRemaining
        self.redIsInDelay = redIsInDelay
        self.redOpeningRoll = redOpeningRoll
        self.redPipCount = redPipCount
        self.redPlayerID = redPlayerID
        self.redPlayerName = redPlayerName
        self.redRating = redRating
        self.redRatingChange = redRatingChange
        self.redReserveSeconds = redReserveSeconds
        self.redUsername = redUsername
        self.remainingMoves = remainingMoves
        self.status = status
        self.targetScore = targetScore
        self.timeControlType = timeControlType
        self.timePerMoveDays = timePerMoveDays
        self.turnDeadline = turnDeadline
        self.turnHistory = turnHistory
        self.validMoves = validMoves
        self.whiteBornOff = whiteBornOff
        self.whiteCheckersOnBar = whiteCheckersOnBar
        self.whiteDelayRemaining = whiteDelayRemaining
        self.whiteIsInDelay = whiteIsInDelay
        self.whiteOpeningRoll = whiteOpeningRoll
        self.whitePipCount = whitePipCount
        self.whitePlayerID = whitePlayerID
        self.whitePlayerName = whitePlayerName
        self.whiteRating = whiteRating
        self.whiteRatingChange = whiteRatingChange
        self.whiteReserveSeconds = whiteReserveSeconds
        self.whiteUsername = whiteUsername
        self.winner = winner
        self.winType = winType
        self.yourColor = yourColor
    }
}

// MARK: GameState convenience initializers and mutators

public extension GameState {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(GameState.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        board: [GameStateBoard]? = nil,
        canDouble: Bool? = nil,
        currentDice: [Double]? = nil,
        currentPlayer: Double? = nil,
        currentTurnMoves: [String]? = nil,
        delaySeconds: Double?? = nil,
        dice: [Double]? = nil,
        doublingCubeOwner: String?? = nil,
        doublingCubeValue: Double? = nil,
        gameID: String? = nil,
        hasPendingDoubleOffer: Bool? = nil,
        hasReceivedDoubleOffer: Bool? = nil,
        hasValidMoves: Bool? = nil,
        isAnalysisMode: Bool? = nil,
        isAwaitingDoubleResponse: Bool? = nil,
        isCorrespondence: Bool? = nil,
        isCrawfordGame: Bool?? = nil,
        isOpeningRoll: Bool? = nil,
        isOpeningRollTie: Bool? = nil,
        isRated: Bool? = nil,
        isYourTurn: Bool? = nil,
        leaveGameAction: String? = nil,
        matchID: String?? = nil,
        movesMadeThisTurn: Double? = nil,
        pendingDoubleNewValue: Double? = nil,
        player1Score: Double?? = nil,
        player2Score: Double?? = nil,
        redBornOff: Double? = nil,
        redCheckersOnBar: Double? = nil,
        redDelayRemaining: Double?? = nil,
        redIsInDelay: Bool?? = nil,
        redOpeningRoll: Double?? = nil,
        redPipCount: Double? = nil,
        redPlayerID: String? = nil,
        redPlayerName: String? = nil,
        redRating: Double?? = nil,
        redRatingChange: Double?? = nil,
        redReserveSeconds: Double?? = nil,
        redUsername: String?? = nil,
        remainingMoves: [Double]? = nil,
        status: Double? = nil,
        targetScore: Double?? = nil,
        timeControlType: String?? = nil,
        timePerMoveDays: Double?? = nil,
        turnDeadline: String?? = nil,
        turnHistory: [GameStateTurnHistory]? = nil,
        validMoves: [GameStateValidMove]? = nil,
        whiteBornOff: Double? = nil,
        whiteCheckersOnBar: Double? = nil,
        whiteDelayRemaining: Double?? = nil,
        whiteIsInDelay: Bool?? = nil,
        whiteOpeningRoll: Double?? = nil,
        whitePipCount: Double? = nil,
        whitePlayerID: String? = nil,
        whitePlayerName: String? = nil,
        whiteRating: Double?? = nil,
        whiteRatingChange: Double?? = nil,
        whiteReserveSeconds: Double?? = nil,
        whiteUsername: String?? = nil,
        winner: Double?? = nil,
        winType: String?? = nil,
        yourColor: Double?? = nil
    ) -> GameState {
        return GameState(
            board: board ?? self.board,
            canDouble: canDouble ?? self.canDouble,
            currentDice: currentDice ?? self.currentDice,
            currentPlayer: currentPlayer ?? self.currentPlayer,
            currentTurnMoves: currentTurnMoves ?? self.currentTurnMoves,
            delaySeconds: delaySeconds ?? self.delaySeconds,
            dice: dice ?? self.dice,
            doublingCubeOwner: doublingCubeOwner ?? self.doublingCubeOwner,
            doublingCubeValue: doublingCubeValue ?? self.doublingCubeValue,
            gameID: gameID ?? self.gameID,
            hasPendingDoubleOffer: hasPendingDoubleOffer ?? self.hasPendingDoubleOffer,
            hasReceivedDoubleOffer: hasReceivedDoubleOffer ?? self.hasReceivedDoubleOffer,
            hasValidMoves: hasValidMoves ?? self.hasValidMoves,
            isAnalysisMode: isAnalysisMode ?? self.isAnalysisMode,
            isAwaitingDoubleResponse: isAwaitingDoubleResponse ?? self.isAwaitingDoubleResponse,
            isCorrespondence: isCorrespondence ?? self.isCorrespondence,
            isCrawfordGame: isCrawfordGame ?? self.isCrawfordGame,
            isOpeningRoll: isOpeningRoll ?? self.isOpeningRoll,
            isOpeningRollTie: isOpeningRollTie ?? self.isOpeningRollTie,
            isRated: isRated ?? self.isRated,
            isYourTurn: isYourTurn ?? self.isYourTurn,
            leaveGameAction: leaveGameAction ?? self.leaveGameAction,
            matchID: matchID ?? self.matchID,
            movesMadeThisTurn: movesMadeThisTurn ?? self.movesMadeThisTurn,
            pendingDoubleNewValue: pendingDoubleNewValue ?? self.pendingDoubleNewValue,
            player1Score: player1Score ?? self.player1Score,
            player2Score: player2Score ?? self.player2Score,
            redBornOff: redBornOff ?? self.redBornOff,
            redCheckersOnBar: redCheckersOnBar ?? self.redCheckersOnBar,
            redDelayRemaining: redDelayRemaining ?? self.redDelayRemaining,
            redIsInDelay: redIsInDelay ?? self.redIsInDelay,
            redOpeningRoll: redOpeningRoll ?? self.redOpeningRoll,
            redPipCount: redPipCount ?? self.redPipCount,
            redPlayerID: redPlayerID ?? self.redPlayerID,
            redPlayerName: redPlayerName ?? self.redPlayerName,
            redRating: redRating ?? self.redRating,
            redRatingChange: redRatingChange ?? self.redRatingChange,
            redReserveSeconds: redReserveSeconds ?? self.redReserveSeconds,
            redUsername: redUsername ?? self.redUsername,
            remainingMoves: remainingMoves ?? self.remainingMoves,
            status: status ?? self.status,
            targetScore: targetScore ?? self.targetScore,
            timeControlType: timeControlType ?? self.timeControlType,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays,
            turnDeadline: turnDeadline ?? self.turnDeadline,
            turnHistory: turnHistory ?? self.turnHistory,
            validMoves: validMoves ?? self.validMoves,
            whiteBornOff: whiteBornOff ?? self.whiteBornOff,
            whiteCheckersOnBar: whiteCheckersOnBar ?? self.whiteCheckersOnBar,
            whiteDelayRemaining: whiteDelayRemaining ?? self.whiteDelayRemaining,
            whiteIsInDelay: whiteIsInDelay ?? self.whiteIsInDelay,
            whiteOpeningRoll: whiteOpeningRoll ?? self.whiteOpeningRoll,
            whitePipCount: whitePipCount ?? self.whitePipCount,
            whitePlayerID: whitePlayerID ?? self.whitePlayerID,
            whitePlayerName: whitePlayerName ?? self.whitePlayerName,
            whiteRating: whiteRating ?? self.whiteRating,
            whiteRatingChange: whiteRatingChange ?? self.whiteRatingChange,
            whiteReserveSeconds: whiteReserveSeconds ?? self.whiteReserveSeconds,
            whiteUsername: whiteUsername ?? self.whiteUsername,
            winner: winner ?? self.winner,
            winType: winType ?? self.winType,
            yourColor: yourColor ?? self.yourColor
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PointState
// MARK: - GameStateBoard
public struct GameStateBoard: Codable {
    /// Transpiled from Backgammon.Core.CheckerColor
    public let color: Double?
    /// Transpiled from int
    public let count: Double
    /// Transpiled from int
    public let position: Double

    public init(color: Double?, count: Double, position: Double) {
        self.color = color
        self.count = count
        self.position = position
    }
}

// MARK: GameStateBoard convenience initializers and mutators

public extension GameStateBoard {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(GameStateBoard.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        color: Double?? = nil,
        count: Double? = nil,
        position: Double? = nil
    ) -> GameStateBoard {
        return GameStateBoard(
            color: color ?? self.color,
            count: count ?? self.count,
            position: position ?? self.position
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.TurnSnapshotDto
// MARK: - GameStateTurnHistory
public struct GameStateTurnHistory: Codable {
    /// Transpiled from string?
    public let cubeOwner: String?
    /// Transpiled from int
    public let cubeValue: Double
    /// Transpiled from int[]
    public let diceRolled: [Double]
    /// Transpiled from string?
    public let doublingAction: String?
    /// Transpiled from System.Collections.Generic.List<string>
    public let moves: [String]
    /// Transpiled from string
    public let player: String
    /// Transpiled from string
    public let positionSgf: String
    /// Transpiled from int
    public let turnNumber: Double

    public init(cubeOwner: String?, cubeValue: Double, diceRolled: [Double], doublingAction: String?, moves: [String], player: String, positionSgf: String, turnNumber: Double) {
        self.cubeOwner = cubeOwner
        self.cubeValue = cubeValue
        self.diceRolled = diceRolled
        self.doublingAction = doublingAction
        self.moves = moves
        self.player = player
        self.positionSgf = positionSgf
        self.turnNumber = turnNumber
    }
}

// MARK: GameStateTurnHistory convenience initializers and mutators

public extension GameStateTurnHistory {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(GameStateTurnHistory.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        cubeOwner: String?? = nil,
        cubeValue: Double? = nil,
        diceRolled: [Double]? = nil,
        doublingAction: String?? = nil,
        moves: [String]? = nil,
        player: String? = nil,
        positionSgf: String? = nil,
        turnNumber: Double? = nil
    ) -> GameStateTurnHistory {
        return GameStateTurnHistory(
            cubeOwner: cubeOwner ?? self.cubeOwner,
            cubeValue: cubeValue ?? self.cubeValue,
            diceRolled: diceRolled ?? self.diceRolled,
            doublingAction: doublingAction ?? self.doublingAction,
            moves: moves ?? self.moves,
            player: player ?? self.player,
            positionSgf: positionSgf ?? self.positionSgf,
            turnNumber: turnNumber ?? self.turnNumber
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MoveDto
// MARK: - GameStateValidMove
public struct GameStateValidMove: Codable {
    /// Transpiled from int[]?
    public let diceUsed: [Double]?
    /// Transpiled from int
    public let dieValue: Double
    /// Transpiled from int
    public let from: Double
    /// Transpiled from int[]?
    public let intermediatePoints: [Double]?
    /// Transpiled from bool
    public let isCombinedMove: Bool
    /// Transpiled from bool
    public let isHit: Bool
    /// Transpiled from int
    public let to: Double

    public init(diceUsed: [Double]?, dieValue: Double, from: Double, intermediatePoints: [Double]?, isCombinedMove: Bool, isHit: Bool, to: Double) {
        self.diceUsed = diceUsed
        self.dieValue = dieValue
        self.from = from
        self.intermediatePoints = intermediatePoints
        self.isCombinedMove = isCombinedMove
        self.isHit = isHit
        self.to = to
    }
}

// MARK: GameStateValidMove convenience initializers and mutators

public extension GameStateValidMove {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(GameStateValidMove.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        diceUsed: [Double]?? = nil,
        dieValue: Double? = nil,
        from: Double? = nil,
        intermediatePoints: [Double]?? = nil,
        isCombinedMove: Bool? = nil,
        isHit: Bool? = nil,
        to: Double? = nil
    ) -> GameStateValidMove {
        return GameStateValidMove(
            diceUsed: diceUsed ?? self.diceUsed,
            dieValue: dieValue ?? self.dieValue,
            from: from ?? self.from,
            intermediatePoints: intermediatePoints ?? self.intermediatePoints,
            isCombinedMove: isCombinedMove ?? self.isCombinedMove,
            isHit: isHit ?? self.isHit,
            to: to ?? self.to
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.GameSummaryDto
// MARK: - GameSummaryDto
public struct GameSummaryDto: Codable {
    /// Transpiled from System.DateTime
    public let completedAt: String
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from string
    public let opponentUsername: String
    /// Transpiled from int
    public let stakes: Double
    /// Transpiled from string?
    public let winType: String?
    /// Transpiled from bool
    public let won: Bool

    public enum CodingKeys: String, CodingKey {
        case completedAt
        case gameID = "gameId"
        case opponentUsername, stakes, winType, won
    }

    public init(completedAt: String, gameID: String, opponentUsername: String, stakes: Double, winType: String?, won: Bool) {
        self.completedAt = completedAt
        self.gameID = gameID
        self.opponentUsername = opponentUsername
        self.stakes = stakes
        self.winType = winType
        self.won = won
    }
}

// MARK: GameSummaryDto convenience initializers and mutators

public extension GameSummaryDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(GameSummaryDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        completedAt: String? = nil,
        gameID: String? = nil,
        opponentUsername: String? = nil,
        stakes: Double? = nil,
        winType: String?? = nil,
        won: Bool? = nil
    ) -> GameSummaryDto {
        return GameSummaryDto(
            completedAt: completedAt ?? self.completedAt,
            gameID: gameID ?? self.gameID,
            opponentUsername: opponentUsername ?? self.opponentUsername,
            stakes: stakes ?? self.stakes,
            winType: winType ?? self.winType,
            won: won ?? self.won
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.LeaderboardEntryDto
// MARK: - LeaderboardEntryDto
public struct LeaderboardEntryDto: Codable {
    /// Transpiled from string
    public let displayName: String
    /// Transpiled from bool
    public let isOnline: Bool
    /// Transpiled from int
    public let losses: Double
    /// Transpiled from int
    public let rank: Double
    /// Transpiled from int
    public let rating: Double
    /// Transpiled from int
    public let totalGames: Double
    /// Transpiled from string
    public let userID: String
    /// Transpiled from string
    public let username: String
    /// Transpiled from double
    public let winRate: Double
    /// Transpiled from int
    public let wins: Double

    public enum CodingKeys: String, CodingKey {
        case displayName, isOnline, losses, rank, rating, totalGames
        case userID = "userId"
        case username, winRate, wins
    }

    public init(displayName: String, isOnline: Bool, losses: Double, rank: Double, rating: Double, totalGames: Double, userID: String, username: String, winRate: Double, wins: Double) {
        self.displayName = displayName
        self.isOnline = isOnline
        self.losses = losses
        self.rank = rank
        self.rating = rating
        self.totalGames = totalGames
        self.userID = userID
        self.username = username
        self.winRate = winRate
        self.wins = wins
    }
}

// MARK: LeaderboardEntryDto convenience initializers and mutators

public extension LeaderboardEntryDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(LeaderboardEntryDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        displayName: String? = nil,
        isOnline: Bool? = nil,
        losses: Double? = nil,
        rank: Double? = nil,
        rating: Double? = nil,
        totalGames: Double? = nil,
        userID: String? = nil,
        username: String? = nil,
        winRate: Double? = nil,
        wins: Double? = nil
    ) -> LeaderboardEntryDto {
        return LeaderboardEntryDto(
            displayName: displayName ?? self.displayName,
            isOnline: isOnline ?? self.isOnline,
            losses: losses ?? self.losses,
            rank: rank ?? self.rank,
            rating: rating ?? self.rating,
            totalGames: totalGames ?? self.totalGames,
            userID: userID ?? self.userID,
            username: username ?? self.username,
            winRate: winRate ?? self.winRate,
            wins: wins ?? self.wins
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MatchConfig
// MARK: - MatchConfig
public struct MatchConfig: Codable {
    /// Transpiled from string
    public let aiType: String
    /// Transpiled from string?
    public let displayName: String?
    /// Transpiled from bool
    public let isCorrespondence: Bool
    /// Transpiled from bool
    public let isRated: Bool
    /// Transpiled from string?
    public let opponentID: String?
    /// Transpiled from string
    public let opponentType: String
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from string
    public let timeControlType: String
    /// Transpiled from int
    public let timePerMoveDays: Double

    public enum CodingKeys: String, CodingKey {
        case aiType, displayName, isCorrespondence, isRated
        case opponentID = "opponentId"
        case opponentType, targetScore, timeControlType, timePerMoveDays
    }

    public init(aiType: String, displayName: String?, isCorrespondence: Bool, isRated: Bool, opponentID: String?, opponentType: String, targetScore: Double, timeControlType: String, timePerMoveDays: Double) {
        self.aiType = aiType
        self.displayName = displayName
        self.isCorrespondence = isCorrespondence
        self.isRated = isRated
        self.opponentID = opponentID
        self.opponentType = opponentType
        self.targetScore = targetScore
        self.timeControlType = timeControlType
        self.timePerMoveDays = timePerMoveDays
    }
}

// MARK: MatchConfig convenience initializers and mutators

public extension MatchConfig {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchConfig.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        aiType: String? = nil,
        displayName: String?? = nil,
        isCorrespondence: Bool? = nil,
        isRated: Bool? = nil,
        opponentID: String?? = nil,
        opponentType: String? = nil,
        targetScore: Double? = nil,
        timeControlType: String? = nil,
        timePerMoveDays: Double? = nil
    ) -> MatchConfig {
        return MatchConfig(
            aiType: aiType ?? self.aiType,
            displayName: displayName ?? self.displayName,
            isCorrespondence: isCorrespondence ?? self.isCorrespondence,
            isRated: isRated ?? self.isRated,
            opponentID: opponentID ?? self.opponentID,
            opponentType: opponentType ?? self.opponentType,
            targetScore: targetScore ?? self.targetScore,
            timeControlType: timeControlType ?? self.timeControlType,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MatchGameSummary
// MARK: - MatchGameSummary
public struct MatchGameSummary: Codable {
    /// Transpiled from System.DateTime
    public let completedAt: String?
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let isCrawford: Bool
    /// Transpiled from int
    public let stakes: Double
    /// Transpiled from string?
    public let winner: String?
    /// Transpiled from string?
    public let winType: String?

    public enum CodingKeys: String, CodingKey {
        case completedAt
        case gameID = "gameId"
        case isCrawford, stakes, winner, winType
    }

    public init(completedAt: String?, gameID: String, isCrawford: Bool, stakes: Double, winner: String?, winType: String?) {
        self.completedAt = completedAt
        self.gameID = gameID
        self.isCrawford = isCrawford
        self.stakes = stakes
        self.winner = winner
        self.winType = winType
    }
}

// MARK: MatchGameSummary convenience initializers and mutators

public extension MatchGameSummary {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchGameSummary.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        completedAt: String?? = nil,
        gameID: String? = nil,
        isCrawford: Bool? = nil,
        stakes: Double? = nil,
        winner: String?? = nil,
        winType: String?? = nil
    ) -> MatchGameSummary {
        return MatchGameSummary(
            completedAt: completedAt ?? self.completedAt,
            gameID: gameID ?? self.gameID,
            isCrawford: isCrawford ?? self.isCrawford,
            stakes: stakes ?? self.stakes,
            winner: winner ?? self.winner,
            winType: winType ?? self.winType
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MatchStateDto
// MARK: - MatchStateDto
public struct MatchStateDto: Codable {
    /// Transpiled from string?
    public let currentGameID: String?
    /// Transpiled from bool
    public let isCrawfordGame: Bool
    /// Transpiled from System.DateTime
    public let lastUpdatedAt: String
    /// Transpiled from bool
    public let matchComplete: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string?
    public let matchWinner: String?
    /// Transpiled from string
    public let player1Name: String
    /// Transpiled from int
    public let player1Score: Double
    /// Transpiled from string
    public let player2Name: String
    /// Transpiled from int
    public let player2Score: Double
    /// Transpiled from int
    public let targetScore: Double

    public enum CodingKeys: String, CodingKey {
        case currentGameID = "currentGameId"
        case isCrawfordGame, lastUpdatedAt, matchComplete
        case matchID = "matchId"
        case matchWinner, player1Name, player1Score, player2Name, player2Score, targetScore
    }

    public init(currentGameID: String?, isCrawfordGame: Bool, lastUpdatedAt: String, matchComplete: Bool, matchID: String, matchWinner: String?, player1Name: String, player1Score: Double, player2Name: String, player2Score: Double, targetScore: Double) {
        self.currentGameID = currentGameID
        self.isCrawfordGame = isCrawfordGame
        self.lastUpdatedAt = lastUpdatedAt
        self.matchComplete = matchComplete
        self.matchID = matchID
        self.matchWinner = matchWinner
        self.player1Name = player1Name
        self.player1Score = player1Score
        self.player2Name = player2Name
        self.player2Score = player2Score
        self.targetScore = targetScore
    }
}

// MARK: MatchStateDto convenience initializers and mutators

public extension MatchStateDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchStateDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        currentGameID: String?? = nil,
        isCrawfordGame: Bool? = nil,
        lastUpdatedAt: String? = nil,
        matchComplete: Bool? = nil,
        matchID: String? = nil,
        matchWinner: String?? = nil,
        player1Name: String? = nil,
        player1Score: Double? = nil,
        player2Name: String? = nil,
        player2Score: Double? = nil,
        targetScore: Double? = nil
    ) -> MatchStateDto {
        return MatchStateDto(
            currentGameID: currentGameID ?? self.currentGameID,
            isCrawfordGame: isCrawfordGame ?? self.isCrawfordGame,
            lastUpdatedAt: lastUpdatedAt ?? self.lastUpdatedAt,
            matchComplete: matchComplete ?? self.matchComplete,
            matchID: matchID ?? self.matchID,
            matchWinner: matchWinner ?? self.matchWinner,
            player1Name: player1Name ?? self.player1Name,
            player1Score: player1Score ?? self.player1Score,
            player2Name: player2Name ?? self.player2Name,
            player2Score: player2Score ?? self.player2Score,
            targetScore: targetScore ?? self.targetScore
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MoveDto
// MARK: - MoveDto
public struct MoveDto: Codable {
    /// Transpiled from int[]?
    public let diceUsed: [Double]?
    /// Transpiled from int
    public let dieValue: Double
    /// Transpiled from int
    public let from: Double
    /// Transpiled from int[]?
    public let intermediatePoints: [Double]?
    /// Transpiled from bool
    public let isCombinedMove: Bool
    /// Transpiled from bool
    public let isHit: Bool
    /// Transpiled from int
    public let to: Double

    public init(diceUsed: [Double]?, dieValue: Double, from: Double, intermediatePoints: [Double]?, isCombinedMove: Bool, isHit: Bool, to: Double) {
        self.diceUsed = diceUsed
        self.dieValue = dieValue
        self.from = from
        self.intermediatePoints = intermediatePoints
        self.isCombinedMove = isCombinedMove
        self.isHit = isHit
        self.to = to
    }
}

// MARK: MoveDto convenience initializers and mutators

public extension MoveDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MoveDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        diceUsed: [Double]?? = nil,
        dieValue: Double? = nil,
        from: Double? = nil,
        intermediatePoints: [Double]?? = nil,
        isCombinedMove: Bool? = nil,
        isHit: Bool? = nil,
        to: Double? = nil
    ) -> MoveDto {
        return MoveDto(
            diceUsed: diceUsed ?? self.diceUsed,
            dieValue: dieValue ?? self.dieValue,
            from: from ?? self.from,
            intermediatePoints: intermediatePoints ?? self.intermediatePoints,
            isCombinedMove: isCombinedMove ?? self.isCombinedMove,
            isHit: isHit ?? self.isHit,
            to: to ?? self.to
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MoveSequenceDto
// MARK: - MoveSequenceDto
public struct MoveSequenceDto: Codable {
    /// Transpiled from double
    public let equity: Double
    /// Transpiled from double
    public let equityGain: Double
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.MoveDto>
    public let moves: [MoveSequenceDtoMove]
    /// Transpiled from string
    public let notation: String

    public init(equity: Double, equityGain: Double, moves: [MoveSequenceDtoMove], notation: String) {
        self.equity = equity
        self.equityGain = equityGain
        self.moves = moves
        self.notation = notation
    }
}

// MARK: MoveSequenceDto convenience initializers and mutators

public extension MoveSequenceDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MoveSequenceDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        equity: Double? = nil,
        equityGain: Double? = nil,
        moves: [MoveSequenceDtoMove]? = nil,
        notation: String? = nil
    ) -> MoveSequenceDto {
        return MoveSequenceDto(
            equity: equity ?? self.equity,
            equityGain: equityGain ?? self.equityGain,
            moves: moves ?? self.moves,
            notation: notation ?? self.notation
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MoveDto
// MARK: - MoveSequenceDtoMove
public struct MoveSequenceDtoMove: Codable {
    /// Transpiled from int[]?
    public let diceUsed: [Double]?
    /// Transpiled from int
    public let dieValue: Double
    /// Transpiled from int
    public let from: Double
    /// Transpiled from int[]?
    public let intermediatePoints: [Double]?
    /// Transpiled from bool
    public let isCombinedMove: Bool
    /// Transpiled from bool
    public let isHit: Bool
    /// Transpiled from int
    public let to: Double

    public init(diceUsed: [Double]?, dieValue: Double, from: Double, intermediatePoints: [Double]?, isCombinedMove: Bool, isHit: Bool, to: Double) {
        self.diceUsed = diceUsed
        self.dieValue = dieValue
        self.from = from
        self.intermediatePoints = intermediatePoints
        self.isCombinedMove = isCombinedMove
        self.isHit = isHit
        self.to = to
    }
}

// MARK: MoveSequenceDtoMove convenience initializers and mutators

public extension MoveSequenceDtoMove {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MoveSequenceDtoMove.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        diceUsed: [Double]?? = nil,
        dieValue: Double? = nil,
        from: Double? = nil,
        intermediatePoints: [Double]?? = nil,
        isCombinedMove: Bool? = nil,
        isHit: Bool? = nil,
        to: Double? = nil
    ) -> MoveSequenceDtoMove {
        return MoveSequenceDtoMove(
            diceUsed: diceUsed ?? self.diceUsed,
            dieValue: dieValue ?? self.dieValue,
            from: from ?? self.from,
            intermediatePoints: intermediatePoints ?? self.intermediatePoints,
            isCombinedMove: isCombinedMove ?? self.isCombinedMove,
            isHit: isHit ?? self.isHit,
            to: to ?? self.to
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.OnlinePlayerDto
// MARK: - OnlinePlayerDto
public struct OnlinePlayerDto: Codable {
    /// Transpiled from string?
    public let currentGameID: String?
    /// Transpiled from string
    public let displayName: String
    /// Transpiled from bool
    public let isFriend: Bool
    /// Transpiled from int
    public let rating: Double
    /// Transpiled from Backgammon.Server.Models.OnlinePlayerStatus
    public let status: Double
    /// Transpiled from string
    public let userID: String
    /// Transpiled from string
    public let username: String

    public enum CodingKeys: String, CodingKey {
        case currentGameID = "currentGameId"
        case displayName, isFriend, rating, status
        case userID = "userId"
        case username
    }

    public init(currentGameID: String?, displayName: String, isFriend: Bool, rating: Double, status: Double, userID: String, username: String) {
        self.currentGameID = currentGameID
        self.displayName = displayName
        self.isFriend = isFriend
        self.rating = rating
        self.status = status
        self.userID = userID
        self.username = username
    }
}

// MARK: OnlinePlayerDto convenience initializers and mutators

public extension OnlinePlayerDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(OnlinePlayerDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        currentGameID: String?? = nil,
        displayName: String? = nil,
        isFriend: Bool? = nil,
        rating: Double? = nil,
        status: Double? = nil,
        userID: String? = nil,
        username: String? = nil
    ) -> OnlinePlayerDto {
        return OnlinePlayerDto(
            currentGameID: currentGameID ?? self.currentGameID,
            displayName: displayName ?? self.displayName,
            isFriend: isFriend ?? self.isFriend,
            rating: rating ?? self.rating,
            status: status ?? self.status,
            userID: userID ?? self.userID,
            username: username ?? self.username
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PlayerProfileDto
// MARK: - PlayerProfileDto
public struct PlayerProfileDto: Codable {
    /// Transpiled from System.DateTime
    public let createdAt: String
    /// Transpiled from string
    public let displayName: String
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.FriendDto>?
    public let friends: [Friend]?
    /// Transpiled from Backgammon.Server.Models.ProfilePrivacyLevel
    public let friendsListPrivacy: Double
    /// Transpiled from Backgammon.Server.Models.ProfilePrivacyLevel
    public let gameHistoryPrivacy: Double
    /// Transpiled from bool
    public let isFriend: Bool
    /// Transpiled from bool
    public let isPrivate: Bool
    /// Transpiled from int
    public let peakRating: Double
    /// Transpiled from Backgammon.Server.Models.ProfilePrivacyLevel
    public let profilePrivacy: Double
    /// Transpiled from int
    public let rating: Double
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.GameSummaryDto>?
    public let recentGames: [RecentGame]?
    /// Transpiled from Backgammon.Server.Models.UserStats
    public let stats: Stats?
    /// Transpiled from string
    public let userID: String
    /// Transpiled from string
    public let username: String

    public enum CodingKeys: String, CodingKey {
        case createdAt, displayName, friends, friendsListPrivacy, gameHistoryPrivacy, isFriend, isPrivate, peakRating, profilePrivacy, rating, recentGames, stats
        case userID = "userId"
        case username
    }

    public init(createdAt: String, displayName: String, friends: [Friend]?, friendsListPrivacy: Double, gameHistoryPrivacy: Double, isFriend: Bool, isPrivate: Bool, peakRating: Double, profilePrivacy: Double, rating: Double, recentGames: [RecentGame]?, stats: Stats?, userID: String, username: String) {
        self.createdAt = createdAt
        self.displayName = displayName
        self.friends = friends
        self.friendsListPrivacy = friendsListPrivacy
        self.gameHistoryPrivacy = gameHistoryPrivacy
        self.isFriend = isFriend
        self.isPrivate = isPrivate
        self.peakRating = peakRating
        self.profilePrivacy = profilePrivacy
        self.rating = rating
        self.recentGames = recentGames
        self.stats = stats
        self.userID = userID
        self.username = username
    }
}

// MARK: PlayerProfileDto convenience initializers and mutators

public extension PlayerProfileDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PlayerProfileDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        createdAt: String? = nil,
        displayName: String? = nil,
        friends: [Friend]?? = nil,
        friendsListPrivacy: Double? = nil,
        gameHistoryPrivacy: Double? = nil,
        isFriend: Bool? = nil,
        isPrivate: Bool? = nil,
        peakRating: Double? = nil,
        profilePrivacy: Double? = nil,
        rating: Double? = nil,
        recentGames: [RecentGame]?? = nil,
        stats: Stats?? = nil,
        userID: String? = nil,
        username: String? = nil
    ) -> PlayerProfileDto {
        return PlayerProfileDto(
            createdAt: createdAt ?? self.createdAt,
            displayName: displayName ?? self.displayName,
            friends: friends ?? self.friends,
            friendsListPrivacy: friendsListPrivacy ?? self.friendsListPrivacy,
            gameHistoryPrivacy: gameHistoryPrivacy ?? self.gameHistoryPrivacy,
            isFriend: isFriend ?? self.isFriend,
            isPrivate: isPrivate ?? self.isPrivate,
            peakRating: peakRating ?? self.peakRating,
            profilePrivacy: profilePrivacy ?? self.profilePrivacy,
            rating: rating ?? self.rating,
            recentGames: recentGames ?? self.recentGames,
            stats: stats ?? self.stats,
            userID: userID ?? self.userID,
            username: username ?? self.username
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.FriendDto
// MARK: - Friend
public struct Friend: Codable {
    /// Transpiled from string
    public let displayName: String
    /// Transpiled from string
    public let initiatedBy: String
    /// Transpiled from bool
    public let isOnline: Bool
    /// Transpiled from Backgammon.Server.Models.FriendshipStatus
    public let status: Double
    /// Transpiled from string
    public let userID: String
    /// Transpiled from string
    public let username: String

    public enum CodingKeys: String, CodingKey {
        case displayName, initiatedBy, isOnline, status
        case userID = "userId"
        case username
    }

    public init(displayName: String, initiatedBy: String, isOnline: Bool, status: Double, userID: String, username: String) {
        self.displayName = displayName
        self.initiatedBy = initiatedBy
        self.isOnline = isOnline
        self.status = status
        self.userID = userID
        self.username = username
    }
}

// MARK: Friend convenience initializers and mutators

public extension Friend {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(Friend.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        displayName: String? = nil,
        initiatedBy: String? = nil,
        isOnline: Bool? = nil,
        status: Double? = nil,
        userID: String? = nil,
        username: String? = nil
    ) -> Friend {
        return Friend(
            displayName: displayName ?? self.displayName,
            initiatedBy: initiatedBy ?? self.initiatedBy,
            isOnline: isOnline ?? self.isOnline,
            status: status ?? self.status,
            userID: userID ?? self.userID,
            username: username ?? self.username
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.GameSummaryDto
// MARK: - RecentGame
public struct RecentGame: Codable {
    /// Transpiled from System.DateTime
    public let completedAt: String
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from string
    public let opponentUsername: String
    /// Transpiled from int
    public let stakes: Double
    /// Transpiled from string?
    public let winType: String?
    /// Transpiled from bool
    public let won: Bool

    public enum CodingKeys: String, CodingKey {
        case completedAt
        case gameID = "gameId"
        case opponentUsername, stakes, winType, won
    }

    public init(completedAt: String, gameID: String, opponentUsername: String, stakes: Double, winType: String?, won: Bool) {
        self.completedAt = completedAt
        self.gameID = gameID
        self.opponentUsername = opponentUsername
        self.stakes = stakes
        self.winType = winType
        self.won = won
    }
}

// MARK: RecentGame convenience initializers and mutators

public extension RecentGame {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(RecentGame.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        completedAt: String? = nil,
        gameID: String? = nil,
        opponentUsername: String? = nil,
        stakes: Double? = nil,
        winType: String?? = nil,
        won: Bool? = nil
    ) -> RecentGame {
        return RecentGame(
            completedAt: completedAt ?? self.completedAt,
            gameID: gameID ?? self.gameID,
            opponentUsername: opponentUsername ?? self.opponentUsername,
            stakes: stakes ?? self.stakes,
            winType: winType ?? self.winType,
            won: won ?? self.won
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.UserStats
// MARK: - Stats
public struct Stats: Codable {
    /// Transpiled from int
    public let backgammonWINS: Double
    /// Transpiled from int
    public let bestWinStreak: Double
    /// Transpiled from int
    public let gammonWINS: Double
    /// Transpiled from int
    public let losses: Double
    /// Transpiled from int
    public let normalWINS: Double
    /// Transpiled from int
    public let totalGames: Double
    /// Transpiled from int
    public let totalStakes: Double
    /// Transpiled from int
    public let wins: Double
    /// Transpiled from int
    public let winStreak: Double

    public enum CodingKeys: String, CodingKey {
        case backgammonWINS = "backgammonWins"
        case bestWinStreak
        case gammonWINS = "gammonWins"
        case losses
        case normalWINS = "normalWins"
        case totalGames, totalStakes, wins, winStreak
    }

    public init(backgammonWINS: Double, bestWinStreak: Double, gammonWINS: Double, losses: Double, normalWINS: Double, totalGames: Double, totalStakes: Double, wins: Double, winStreak: Double) {
        self.backgammonWINS = backgammonWINS
        self.bestWinStreak = bestWinStreak
        self.gammonWINS = gammonWINS
        self.losses = losses
        self.normalWINS = normalWINS
        self.totalGames = totalGames
        self.totalStakes = totalStakes
        self.wins = wins
        self.winStreak = winStreak
    }
}

// MARK: Stats convenience initializers and mutators

public extension Stats {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(Stats.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        backgammonWINS: Double? = nil,
        bestWinStreak: Double? = nil,
        gammonWINS: Double? = nil,
        losses: Double? = nil,
        normalWINS: Double? = nil,
        totalGames: Double? = nil,
        totalStakes: Double? = nil,
        wins: Double? = nil,
        winStreak: Double? = nil
    ) -> Stats {
        return Stats(
            backgammonWINS: backgammonWINS ?? self.backgammonWINS,
            bestWinStreak: bestWinStreak ?? self.bestWinStreak,
            gammonWINS: gammonWINS ?? self.gammonWINS,
            losses: losses ?? self.losses,
            normalWINS: normalWINS ?? self.normalWINS,
            totalGames: totalGames ?? self.totalGames,
            totalStakes: totalStakes ?? self.totalStakes,
            wins: wins ?? self.wins,
            winStreak: winStreak ?? self.winStreak
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PlayerSearchResultDto
// MARK: - PlayerSearchResultDto
public struct PlayerSearchResultDto: Codable {
    /// Transpiled from string
    public let displayName: String
    /// Transpiled from bool
    public let isOnline: Bool
    /// Transpiled from int
    public let rating: Double
    /// Transpiled from int
    public let totalGames: Double
    /// Transpiled from string
    public let userID: String
    /// Transpiled from string
    public let username: String

    public enum CodingKeys: String, CodingKey {
        case displayName, isOnline, rating, totalGames
        case userID = "userId"
        case username
    }

    public init(displayName: String, isOnline: Bool, rating: Double, totalGames: Double, userID: String, username: String) {
        self.displayName = displayName
        self.isOnline = isOnline
        self.rating = rating
        self.totalGames = totalGames
        self.userID = userID
        self.username = username
    }
}

// MARK: PlayerSearchResultDto convenience initializers and mutators

public extension PlayerSearchResultDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PlayerSearchResultDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        displayName: String? = nil,
        isOnline: Bool? = nil,
        rating: Double? = nil,
        totalGames: Double? = nil,
        userID: String? = nil,
        username: String? = nil
    ) -> PlayerSearchResultDto {
        return PlayerSearchResultDto(
            displayName: displayName ?? self.displayName,
            isOnline: isOnline ?? self.isOnline,
            rating: rating ?? self.rating,
            totalGames: totalGames ?? self.totalGames,
            userID: userID ?? self.userID,
            username: username ?? self.username
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PointState
// MARK: - PointState
public struct PointState: Codable {
    /// Transpiled from Backgammon.Core.CheckerColor
    public let color: Double?
    /// Transpiled from int
    public let count: Double
    /// Transpiled from int
    public let position: Double

    public init(color: Double?, count: Double, position: Double) {
        self.color = color
        self.count = count
        self.position = position
    }
}

// MARK: PointState convenience initializers and mutators

public extension PointState {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PointState.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        color: Double?? = nil,
        count: Double? = nil,
        position: Double? = nil
    ) -> PointState {
        return PointState(
            color: color ?? self.color,
            count: count ?? self.count,
            position: position ?? self.position
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PointStateDto
// MARK: - PointStateDto
public struct PointStateDto: Codable {
    /// Transpiled from string?
    public let color: String?
    /// Transpiled from int
    public let count: Double
    /// Transpiled from int
    public let position: Double

    public init(color: String?, count: Double, position: Double) {
        self.color = color
        self.count = count
        self.position = position
    }
}

// MARK: PointStateDto convenience initializers and mutators

public extension PointStateDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PointStateDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        color: String?? = nil,
        count: Double? = nil,
        position: Double? = nil
    ) -> PointStateDto {
        return PointStateDto(
            color: color ?? self.color,
            count: count ?? self.count,
            position: position ?? self.position
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PositionEvaluationDto
// MARK: - PositionEvaluationDto
public struct PositionEvaluationDto: Codable {
    /// Transpiled from double
    public let backgammonProbability: Double
    /// Transpiled from double
    public let equity: Double
    /// Transpiled from string
    public let evaluatorName: String
    /// Transpiled from Backgammon.Server.Models.PositionFeaturesDto
    public let features: PositionEvaluationDtoFeatures
    /// Transpiled from double
    public let gammonProbability: Double
    /// Transpiled from double
    public let winProbability: Double

    public init(backgammonProbability: Double, equity: Double, evaluatorName: String, features: PositionEvaluationDtoFeatures, gammonProbability: Double, winProbability: Double) {
        self.backgammonProbability = backgammonProbability
        self.equity = equity
        self.evaluatorName = evaluatorName
        self.features = features
        self.gammonProbability = gammonProbability
        self.winProbability = winProbability
    }
}

// MARK: PositionEvaluationDto convenience initializers and mutators

public extension PositionEvaluationDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PositionEvaluationDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        backgammonProbability: Double? = nil,
        equity: Double? = nil,
        evaluatorName: String? = nil,
        features: PositionEvaluationDtoFeatures? = nil,
        gammonProbability: Double? = nil,
        winProbability: Double? = nil
    ) -> PositionEvaluationDto {
        return PositionEvaluationDto(
            backgammonProbability: backgammonProbability ?? self.backgammonProbability,
            equity: equity ?? self.equity,
            evaluatorName: evaluatorName ?? self.evaluatorName,
            features: features ?? self.features,
            gammonProbability: gammonProbability ?? self.gammonProbability,
            winProbability: winProbability ?? self.winProbability
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PositionFeaturesDto
// MARK: - PositionEvaluationDtoFeatures
public struct PositionEvaluationDtoFeatures: Codable {
    /// Transpiled from int
    public let anchorsInOpponentHome: Double
    /// Transpiled from double
    public let bearoffEfficiency: Double
    /// Transpiled from int
    public let blotCount: Double
    /// Transpiled from int
    public let blotExposure: Double
    /// Transpiled from int
    public let checkersBornOff: Double
    /// Transpiled from int
    public let checkersOnBar: Double
    /// Transpiled from double
    public let distribution: Double
    /// Transpiled from int
    public let homeboardCoverage: Double
    /// Transpiled from bool
    public let isContact: Bool
    /// Transpiled from bool
    public let isRace: Bool
    /// Transpiled from int
    public let pipCount: Double
    /// Transpiled from int
    public let pipDifference: Double
    /// Transpiled from int
    public let primeLength: Double
    /// Transpiled from int
    public let wastedPips: Double

    public init(anchorsInOpponentHome: Double, bearoffEfficiency: Double, blotCount: Double, blotExposure: Double, checkersBornOff: Double, checkersOnBar: Double, distribution: Double, homeboardCoverage: Double, isContact: Bool, isRace: Bool, pipCount: Double, pipDifference: Double, primeLength: Double, wastedPips: Double) {
        self.anchorsInOpponentHome = anchorsInOpponentHome
        self.bearoffEfficiency = bearoffEfficiency
        self.blotCount = blotCount
        self.blotExposure = blotExposure
        self.checkersBornOff = checkersBornOff
        self.checkersOnBar = checkersOnBar
        self.distribution = distribution
        self.homeboardCoverage = homeboardCoverage
        self.isContact = isContact
        self.isRace = isRace
        self.pipCount = pipCount
        self.pipDifference = pipDifference
        self.primeLength = primeLength
        self.wastedPips = wastedPips
    }
}

// MARK: PositionEvaluationDtoFeatures convenience initializers and mutators

public extension PositionEvaluationDtoFeatures {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PositionEvaluationDtoFeatures.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        anchorsInOpponentHome: Double? = nil,
        bearoffEfficiency: Double? = nil,
        blotCount: Double? = nil,
        blotExposure: Double? = nil,
        checkersBornOff: Double? = nil,
        checkersOnBar: Double? = nil,
        distribution: Double? = nil,
        homeboardCoverage: Double? = nil,
        isContact: Bool? = nil,
        isRace: Bool? = nil,
        pipCount: Double? = nil,
        pipDifference: Double? = nil,
        primeLength: Double? = nil,
        wastedPips: Double? = nil
    ) -> PositionEvaluationDtoFeatures {
        return PositionEvaluationDtoFeatures(
            anchorsInOpponentHome: anchorsInOpponentHome ?? self.anchorsInOpponentHome,
            bearoffEfficiency: bearoffEfficiency ?? self.bearoffEfficiency,
            blotCount: blotCount ?? self.blotCount,
            blotExposure: blotExposure ?? self.blotExposure,
            checkersBornOff: checkersBornOff ?? self.checkersBornOff,
            checkersOnBar: checkersOnBar ?? self.checkersOnBar,
            distribution: distribution ?? self.distribution,
            homeboardCoverage: homeboardCoverage ?? self.homeboardCoverage,
            isContact: isContact ?? self.isContact,
            isRace: isRace ?? self.isRace,
            pipCount: pipCount ?? self.pipCount,
            pipDifference: pipDifference ?? self.pipDifference,
            primeLength: primeLength ?? self.primeLength,
            wastedPips: wastedPips ?? self.wastedPips
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PositionFeaturesDto
// MARK: - PositionFeaturesDto
public struct PositionFeaturesDto: Codable {
    /// Transpiled from int
    public let anchorsInOpponentHome: Double
    /// Transpiled from double
    public let bearoffEfficiency: Double
    /// Transpiled from int
    public let blotCount: Double
    /// Transpiled from int
    public let blotExposure: Double
    /// Transpiled from int
    public let checkersBornOff: Double
    /// Transpiled from int
    public let checkersOnBar: Double
    /// Transpiled from double
    public let distribution: Double
    /// Transpiled from int
    public let homeboardCoverage: Double
    /// Transpiled from bool
    public let isContact: Bool
    /// Transpiled from bool
    public let isRace: Bool
    /// Transpiled from int
    public let pipCount: Double
    /// Transpiled from int
    public let pipDifference: Double
    /// Transpiled from int
    public let primeLength: Double
    /// Transpiled from int
    public let wastedPips: Double

    public init(anchorsInOpponentHome: Double, bearoffEfficiency: Double, blotCount: Double, blotExposure: Double, checkersBornOff: Double, checkersOnBar: Double, distribution: Double, homeboardCoverage: Double, isContact: Bool, isRace: Bool, pipCount: Double, pipDifference: Double, primeLength: Double, wastedPips: Double) {
        self.anchorsInOpponentHome = anchorsInOpponentHome
        self.bearoffEfficiency = bearoffEfficiency
        self.blotCount = blotCount
        self.blotExposure = blotExposure
        self.checkersBornOff = checkersBornOff
        self.checkersOnBar = checkersOnBar
        self.distribution = distribution
        self.homeboardCoverage = homeboardCoverage
        self.isContact = isContact
        self.isRace = isRace
        self.pipCount = pipCount
        self.pipDifference = pipDifference
        self.primeLength = primeLength
        self.wastedPips = wastedPips
    }
}

// MARK: PositionFeaturesDto convenience initializers and mutators

public extension PositionFeaturesDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PositionFeaturesDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        anchorsInOpponentHome: Double? = nil,
        bearoffEfficiency: Double? = nil,
        blotCount: Double? = nil,
        blotExposure: Double? = nil,
        checkersBornOff: Double? = nil,
        checkersOnBar: Double? = nil,
        distribution: Double? = nil,
        homeboardCoverage: Double? = nil,
        isContact: Bool? = nil,
        isRace: Bool? = nil,
        pipCount: Double? = nil,
        pipDifference: Double? = nil,
        primeLength: Double? = nil,
        wastedPips: Double? = nil
    ) -> PositionFeaturesDto {
        return PositionFeaturesDto(
            anchorsInOpponentHome: anchorsInOpponentHome ?? self.anchorsInOpponentHome,
            bearoffEfficiency: bearoffEfficiency ?? self.bearoffEfficiency,
            blotCount: blotCount ?? self.blotCount,
            blotExposure: blotExposure ?? self.blotExposure,
            checkersBornOff: checkersBornOff ?? self.checkersBornOff,
            checkersOnBar: checkersOnBar ?? self.checkersOnBar,
            distribution: distribution ?? self.distribution,
            homeboardCoverage: homeboardCoverage ?? self.homeboardCoverage,
            isContact: isContact ?? self.isContact,
            isRace: isRace ?? self.isRace,
            pipCount: pipCount ?? self.pipCount,
            pipDifference: pipDifference ?? self.pipDifference,
            primeLength: primeLength ?? self.primeLength,
            wastedPips: wastedPips ?? self.wastedPips
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PuzzleResultDto
// MARK: - PuzzleResultDto
public struct PuzzleResultDto: Codable {
    /// Transpiled from int
    public let attemptCount: Double
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.MoveDto>?
    public let bestMoves: [PuzzleResultDtoBestMove]?
    /// Transpiled from string?
    public let bestMovesNotation: String?
    /// Transpiled from int
    public let currentStreak: Double
    /// Transpiled from double
    public let equityLoss: Double
    /// Transpiled from string
    public let feedback: String
    /// Transpiled from bool
    public let isCorrect: Bool
    /// Transpiled from bool
    public let streakBroken: Bool

    public init(attemptCount: Double, bestMoves: [PuzzleResultDtoBestMove]?, bestMovesNotation: String?, currentStreak: Double, equityLoss: Double, feedback: String, isCorrect: Bool, streakBroken: Bool) {
        self.attemptCount = attemptCount
        self.bestMoves = bestMoves
        self.bestMovesNotation = bestMovesNotation
        self.currentStreak = currentStreak
        self.equityLoss = equityLoss
        self.feedback = feedback
        self.isCorrect = isCorrect
        self.streakBroken = streakBroken
    }
}

// MARK: PuzzleResultDto convenience initializers and mutators

public extension PuzzleResultDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PuzzleResultDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        attemptCount: Double? = nil,
        bestMoves: [PuzzleResultDtoBestMove]?? = nil,
        bestMovesNotation: String?? = nil,
        currentStreak: Double? = nil,
        equityLoss: Double? = nil,
        feedback: String? = nil,
        isCorrect: Bool? = nil,
        streakBroken: Bool? = nil
    ) -> PuzzleResultDto {
        return PuzzleResultDto(
            attemptCount: attemptCount ?? self.attemptCount,
            bestMoves: bestMoves ?? self.bestMoves,
            bestMovesNotation: bestMovesNotation ?? self.bestMovesNotation,
            currentStreak: currentStreak ?? self.currentStreak,
            equityLoss: equityLoss ?? self.equityLoss,
            feedback: feedback ?? self.feedback,
            isCorrect: isCorrect ?? self.isCorrect,
            streakBroken: streakBroken ?? self.streakBroken
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MoveDto
// MARK: - PuzzleResultDtoBestMove
public struct PuzzleResultDtoBestMove: Codable {
    /// Transpiled from int[]?
    public let diceUsed: [Double]?
    /// Transpiled from int
    public let dieValue: Double
    /// Transpiled from int
    public let from: Double
    /// Transpiled from int[]?
    public let intermediatePoints: [Double]?
    /// Transpiled from bool
    public let isCombinedMove: Bool
    /// Transpiled from bool
    public let isHit: Bool
    /// Transpiled from int
    public let to: Double

    public init(diceUsed: [Double]?, dieValue: Double, from: Double, intermediatePoints: [Double]?, isCombinedMove: Bool, isHit: Bool, to: Double) {
        self.diceUsed = diceUsed
        self.dieValue = dieValue
        self.from = from
        self.intermediatePoints = intermediatePoints
        self.isCombinedMove = isCombinedMove
        self.isHit = isHit
        self.to = to
    }
}

// MARK: PuzzleResultDtoBestMove convenience initializers and mutators

public extension PuzzleResultDtoBestMove {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PuzzleResultDtoBestMove.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        diceUsed: [Double]?? = nil,
        dieValue: Double? = nil,
        from: Double? = nil,
        intermediatePoints: [Double]?? = nil,
        isCombinedMove: Bool? = nil,
        isHit: Bool? = nil,
        to: Double? = nil
    ) -> PuzzleResultDtoBestMove {
        return PuzzleResultDtoBestMove(
            diceUsed: diceUsed ?? self.diceUsed,
            dieValue: dieValue ?? self.dieValue,
            from: from ?? self.from,
            intermediatePoints: intermediatePoints ?? self.intermediatePoints,
            isCombinedMove: isCombinedMove ?? self.isCombinedMove,
            isHit: isHit ?? self.isHit,
            to: to ?? self.to
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PuzzleStreakInfo
// MARK: - PuzzleStreakInfo
public struct PuzzleStreakInfo: Codable {
    /// Transpiled from int
    public let bestStreak: Double
    /// Transpiled from int
    public let currentStreak: Double
    /// Transpiled from string?
    public let lastSolvedDate: String?
    /// Transpiled from int
    public let totalAttempts: Double
    /// Transpiled from int
    public let totalSolved: Double
    /// Transpiled from string
    public let userID: String

    public enum CodingKeys: String, CodingKey {
        case bestStreak, currentStreak, lastSolvedDate, totalAttempts, totalSolved
        case userID = "userId"
    }

    public init(bestStreak: Double, currentStreak: Double, lastSolvedDate: String?, totalAttempts: Double, totalSolved: Double, userID: String) {
        self.bestStreak = bestStreak
        self.currentStreak = currentStreak
        self.lastSolvedDate = lastSolvedDate
        self.totalAttempts = totalAttempts
        self.totalSolved = totalSolved
        self.userID = userID
    }
}

// MARK: PuzzleStreakInfo convenience initializers and mutators

public extension PuzzleStreakInfo {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PuzzleStreakInfo.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        bestStreak: Double? = nil,
        currentStreak: Double? = nil,
        lastSolvedDate: String?? = nil,
        totalAttempts: Double? = nil,
        totalSolved: Double? = nil,
        userID: String? = nil
    ) -> PuzzleStreakInfo {
        return PuzzleStreakInfo(
            bestStreak: bestStreak ?? self.bestStreak,
            currentStreak: currentStreak ?? self.currentStreak,
            lastSolvedDate: lastSolvedDate ?? self.lastSolvedDate,
            totalAttempts: totalAttempts ?? self.totalAttempts,
            totalSolved: totalSolved ?? self.totalSolved,
            userID: userID ?? self.userID
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PuzzleValidMovesRequest
// MARK: - PuzzleValidMovesRequest
public struct PuzzleValidMovesRequest: Codable {
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.PointStateDto>
    public let boardState: [PuzzleValidMovesRequestBoardState]
    /// Transpiled from string
    public let currentPlayer: String
    /// Transpiled from int[]
    public let dice: [Double]
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.MoveDto>
    public let pendingMoves: [PendingMove]
    /// Transpiled from int
    public let redBornOff: Double
    /// Transpiled from int
    public let redCheckersOnBar: Double
    /// Transpiled from int
    public let whiteBornOff: Double
    /// Transpiled from int
    public let whiteCheckersOnBar: Double

    public init(boardState: [PuzzleValidMovesRequestBoardState], currentPlayer: String, dice: [Double], pendingMoves: [PendingMove], redBornOff: Double, redCheckersOnBar: Double, whiteBornOff: Double, whiteCheckersOnBar: Double) {
        self.boardState = boardState
        self.currentPlayer = currentPlayer
        self.dice = dice
        self.pendingMoves = pendingMoves
        self.redBornOff = redBornOff
        self.redCheckersOnBar = redCheckersOnBar
        self.whiteBornOff = whiteBornOff
        self.whiteCheckersOnBar = whiteCheckersOnBar
    }
}

// MARK: PuzzleValidMovesRequest convenience initializers and mutators

public extension PuzzleValidMovesRequest {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PuzzleValidMovesRequest.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        boardState: [PuzzleValidMovesRequestBoardState]? = nil,
        currentPlayer: String? = nil,
        dice: [Double]? = nil,
        pendingMoves: [PendingMove]? = nil,
        redBornOff: Double? = nil,
        redCheckersOnBar: Double? = nil,
        whiteBornOff: Double? = nil,
        whiteCheckersOnBar: Double? = nil
    ) -> PuzzleValidMovesRequest {
        return PuzzleValidMovesRequest(
            boardState: boardState ?? self.boardState,
            currentPlayer: currentPlayer ?? self.currentPlayer,
            dice: dice ?? self.dice,
            pendingMoves: pendingMoves ?? self.pendingMoves,
            redBornOff: redBornOff ?? self.redBornOff,
            redCheckersOnBar: redCheckersOnBar ?? self.redCheckersOnBar,
            whiteBornOff: whiteBornOff ?? self.whiteBornOff,
            whiteCheckersOnBar: whiteCheckersOnBar ?? self.whiteCheckersOnBar
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PointStateDto
// MARK: - PuzzleValidMovesRequestBoardState
public struct PuzzleValidMovesRequestBoardState: Codable {
    /// Transpiled from string?
    public let color: String?
    /// Transpiled from int
    public let count: Double
    /// Transpiled from int
    public let position: Double

    public init(color: String?, count: Double, position: Double) {
        self.color = color
        self.count = count
        self.position = position
    }
}

// MARK: PuzzleValidMovesRequestBoardState convenience initializers and mutators

public extension PuzzleValidMovesRequestBoardState {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PuzzleValidMovesRequestBoardState.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        color: String?? = nil,
        count: Double? = nil,
        position: Double? = nil
    ) -> PuzzleValidMovesRequestBoardState {
        return PuzzleValidMovesRequestBoardState(
            color: color ?? self.color,
            count: count ?? self.count,
            position: position ?? self.position
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MoveDto
// MARK: - PendingMove
public struct PendingMove: Codable {
    /// Transpiled from int[]?
    public let diceUsed: [Double]?
    /// Transpiled from int
    public let dieValue: Double
    /// Transpiled from int
    public let from: Double
    /// Transpiled from int[]?
    public let intermediatePoints: [Double]?
    /// Transpiled from bool
    public let isCombinedMove: Bool
    /// Transpiled from bool
    public let isHit: Bool
    /// Transpiled from int
    public let to: Double

    public init(diceUsed: [Double]?, dieValue: Double, from: Double, intermediatePoints: [Double]?, isCombinedMove: Bool, isHit: Bool, to: Double) {
        self.diceUsed = diceUsed
        self.dieValue = dieValue
        self.from = from
        self.intermediatePoints = intermediatePoints
        self.isCombinedMove = isCombinedMove
        self.isHit = isHit
        self.to = to
    }
}

// MARK: PendingMove convenience initializers and mutators

public extension PendingMove {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PendingMove.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        diceUsed: [Double]?? = nil,
        dieValue: Double? = nil,
        from: Double? = nil,
        intermediatePoints: [Double]?? = nil,
        isCombinedMove: Bool? = nil,
        isHit: Bool? = nil,
        to: Double? = nil
    ) -> PendingMove {
        return PendingMove(
            diceUsed: diceUsed ?? self.diceUsed,
            dieValue: dieValue ?? self.dieValue,
            from: from ?? self.from,
            intermediatePoints: intermediatePoints ?? self.intermediatePoints,
            isCombinedMove: isCombinedMove ?? self.isCombinedMove,
            isHit: isHit ?? self.isHit,
            to: to ?? self.to
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.RatingBucketDto
// MARK: - RatingBucketDto
public struct RatingBucketDto: Codable {
    /// Transpiled from int
    public let count: Double
    /// Transpiled from bool
    public let isUserBucket: Bool
    /// Transpiled from string
    public let label: String
    /// Transpiled from int
    public let maxRating: Double
    /// Transpiled from int
    public let minRating: Double
    /// Transpiled from double
    public let percentage: Double

    public init(count: Double, isUserBucket: Bool, label: String, maxRating: Double, minRating: Double, percentage: Double) {
        self.count = count
        self.isUserBucket = isUserBucket
        self.label = label
        self.maxRating = maxRating
        self.minRating = minRating
        self.percentage = percentage
    }
}

// MARK: RatingBucketDto convenience initializers and mutators

public extension RatingBucketDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(RatingBucketDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        count: Double? = nil,
        isUserBucket: Bool? = nil,
        label: String? = nil,
        maxRating: Double? = nil,
        minRating: Double? = nil,
        percentage: Double? = nil
    ) -> RatingBucketDto {
        return RatingBucketDto(
            count: count ?? self.count,
            isUserBucket: isUserBucket ?? self.isUserBucket,
            label: label ?? self.label,
            maxRating: maxRating ?? self.maxRating,
            minRating: minRating ?? self.minRating,
            percentage: percentage ?? self.percentage
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.RatingDistributionDto
// MARK: - RatingDistributionDto
public struct RatingDistributionDto: Codable {
    /// Transpiled from double
    public let averageRating: Double
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.RatingBucketDto>
    public let buckets: [Bucket]
    /// Transpiled from int
    public let medianRating: Double
    /// Transpiled from int
    public let totalPlayers: Double
    /// Transpiled from double
    public let userPercentile: Double?
    /// Transpiled from int
    public let userRating: Double?

    public init(averageRating: Double, buckets: [Bucket], medianRating: Double, totalPlayers: Double, userPercentile: Double?, userRating: Double?) {
        self.averageRating = averageRating
        self.buckets = buckets
        self.medianRating = medianRating
        self.totalPlayers = totalPlayers
        self.userPercentile = userPercentile
        self.userRating = userRating
    }
}

// MARK: RatingDistributionDto convenience initializers and mutators

public extension RatingDistributionDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(RatingDistributionDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        averageRating: Double? = nil,
        buckets: [Bucket]? = nil,
        medianRating: Double? = nil,
        totalPlayers: Double? = nil,
        userPercentile: Double?? = nil,
        userRating: Double?? = nil
    ) -> RatingDistributionDto {
        return RatingDistributionDto(
            averageRating: averageRating ?? self.averageRating,
            buckets: buckets ?? self.buckets,
            medianRating: medianRating ?? self.medianRating,
            totalPlayers: totalPlayers ?? self.totalPlayers,
            userPercentile: userPercentile ?? self.userPercentile,
            userRating: userRating ?? self.userRating
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.RatingBucketDto
// MARK: - Bucket
public struct Bucket: Codable {
    /// Transpiled from int
    public let count: Double
    /// Transpiled from bool
    public let isUserBucket: Bool
    /// Transpiled from string
    public let label: String
    /// Transpiled from int
    public let maxRating: Double
    /// Transpiled from int
    public let minRating: Double
    /// Transpiled from double
    public let percentage: Double

    public init(count: Double, isUserBucket: Bool, label: String, maxRating: Double, minRating: Double, percentage: Double) {
        self.count = count
        self.isUserBucket = isUserBucket
        self.label = label
        self.maxRating = maxRating
        self.minRating = minRating
        self.percentage = percentage
    }
}

// MARK: Bucket convenience initializers and mutators

public extension Bucket {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(Bucket.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        count: Double? = nil,
        isUserBucket: Bool? = nil,
        label: String? = nil,
        maxRating: Double? = nil,
        minRating: Double? = nil,
        percentage: Double? = nil
    ) -> Bucket {
        return Bucket(
            count: count ?? self.count,
            isUserBucket: isUserBucket ?? self.isUserBucket,
            label: label ?? self.label,
            maxRating: maxRating ?? self.maxRating,
            minRating: minRating ?? self.minRating,
            percentage: percentage ?? self.percentage
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.RatingHistoryEntryDto
// MARK: - RatingHistoryEntryDto
public struct RatingHistoryEntryDto: Codable {
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from string?
    public let opponentUsername: String?
    /// Transpiled from int
    public let rating: Double
    /// Transpiled from int
    public let ratingChange: Double
    /// Transpiled from System.DateTime
    public let timestamp: String
    /// Transpiled from bool
    public let won: Bool

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case opponentUsername, rating, ratingChange, timestamp, won
    }

    public init(gameID: String, opponentUsername: String?, rating: Double, ratingChange: Double, timestamp: String, won: Bool) {
        self.gameID = gameID
        self.opponentUsername = opponentUsername
        self.rating = rating
        self.ratingChange = ratingChange
        self.timestamp = timestamp
        self.won = won
    }
}

// MARK: RatingHistoryEntryDto convenience initializers and mutators

public extension RatingHistoryEntryDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(RatingHistoryEntryDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String? = nil,
        opponentUsername: String?? = nil,
        rating: Double? = nil,
        ratingChange: Double? = nil,
        timestamp: String? = nil,
        won: Bool? = nil
    ) -> RatingHistoryEntryDto {
        return RatingHistoryEntryDto(
            gameID: gameID ?? self.gameID,
            opponentUsername: opponentUsername ?? self.opponentUsername,
            rating: rating ?? self.rating,
            ratingChange: ratingChange ?? self.ratingChange,
            timestamp: timestamp ?? self.timestamp,
            won: won ?? self.won
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.RecentOpponentDto
// MARK: - RecentOpponentDto
public struct RecentOpponentDto: Codable {
    /// Transpiled from bool
    public let isAI: Bool
    /// Transpiled from System.DateTime
    public let lastPlayedAt: String
    /// Transpiled from int
    public let losses: Double
    /// Transpiled from string
    public let opponentID: String
    /// Transpiled from string
    public let opponentName: String
    /// Transpiled from int
    public let opponentRating: Double
    /// Transpiled from string
    public let record: String
    /// Transpiled from int
    public let totalMatches: Double
    /// Transpiled from double
    public let winRate: Double
    /// Transpiled from int
    public let wins: Double

    public enum CodingKeys: String, CodingKey {
        case isAI = "isAi"
        case lastPlayedAt, losses
        case opponentID = "opponentId"
        case opponentName, opponentRating, record, totalMatches, winRate, wins
    }

    public init(isAI: Bool, lastPlayedAt: String, losses: Double, opponentID: String, opponentName: String, opponentRating: Double, record: String, totalMatches: Double, winRate: Double, wins: Double) {
        self.isAI = isAI
        self.lastPlayedAt = lastPlayedAt
        self.losses = losses
        self.opponentID = opponentID
        self.opponentName = opponentName
        self.opponentRating = opponentRating
        self.record = record
        self.totalMatches = totalMatches
        self.winRate = winRate
        self.wins = wins
    }
}

// MARK: RecentOpponentDto convenience initializers and mutators

public extension RecentOpponentDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(RecentOpponentDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        isAI: Bool? = nil,
        lastPlayedAt: String? = nil,
        losses: Double? = nil,
        opponentID: String? = nil,
        opponentName: String? = nil,
        opponentRating: Double? = nil,
        record: String? = nil,
        totalMatches: Double? = nil,
        winRate: Double? = nil,
        wins: Double? = nil
    ) -> RecentOpponentDto {
        return RecentOpponentDto(
            isAI: isAI ?? self.isAI,
            lastPlayedAt: lastPlayedAt ?? self.lastPlayedAt,
            losses: losses ?? self.losses,
            opponentID: opponentID ?? self.opponentID,
            opponentName: opponentName ?? self.opponentName,
            opponentRating: opponentRating ?? self.opponentRating,
            record: record ?? self.record,
            totalMatches: totalMatches ?? self.totalMatches,
            winRate: winRate ?? self.winRate,
            wins: wins ?? self.wins
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.TurnSnapshotDto
// MARK: - TurnSnapshotDto
public struct TurnSnapshotDto: Codable {
    /// Transpiled from string?
    public let cubeOwner: String?
    /// Transpiled from int
    public let cubeValue: Double
    /// Transpiled from int[]
    public let diceRolled: [Double]
    /// Transpiled from string?
    public let doublingAction: String?
    /// Transpiled from System.Collections.Generic.List<string>
    public let moves: [String]
    /// Transpiled from string
    public let player: String
    /// Transpiled from string
    public let positionSgf: String
    /// Transpiled from int
    public let turnNumber: Double

    public init(cubeOwner: String?, cubeValue: Double, diceRolled: [Double], doublingAction: String?, moves: [String], player: String, positionSgf: String, turnNumber: Double) {
        self.cubeOwner = cubeOwner
        self.cubeValue = cubeValue
        self.diceRolled = diceRolled
        self.doublingAction = doublingAction
        self.moves = moves
        self.player = player
        self.positionSgf = positionSgf
        self.turnNumber = turnNumber
    }
}

// MARK: TurnSnapshotDto convenience initializers and mutators

public extension TurnSnapshotDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(TurnSnapshotDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        cubeOwner: String?? = nil,
        cubeValue: Double? = nil,
        diceRolled: [Double]? = nil,
        doublingAction: String?? = nil,
        moves: [String]? = nil,
        player: String? = nil,
        positionSgf: String? = nil,
        turnNumber: Double? = nil
    ) -> TurnSnapshotDto {
        return TurnSnapshotDto(
            cubeOwner: cubeOwner ?? self.cubeOwner,
            cubeValue: cubeValue ?? self.cubeValue,
            diceRolled: diceRolled ?? self.diceRolled,
            doublingAction: doublingAction ?? self.doublingAction,
            moves: moves ?? self.moves,
            player: player ?? self.player,
            positionSgf: positionSgf ?? self.positionSgf,
            turnNumber: turnNumber ?? self.turnNumber
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.UserStats
// MARK: - UserStats
public struct UserStats: Codable {
    /// Transpiled from int
    public let backgammonWINS: Double
    /// Transpiled from int
    public let bestWinStreak: Double
    /// Transpiled from int
    public let gammonWINS: Double
    /// Transpiled from int
    public let losses: Double
    /// Transpiled from int
    public let normalWINS: Double
    /// Transpiled from int
    public let totalGames: Double
    /// Transpiled from int
    public let totalStakes: Double
    /// Transpiled from int
    public let wins: Double
    /// Transpiled from int
    public let winStreak: Double

    public enum CodingKeys: String, CodingKey {
        case backgammonWINS = "backgammonWins"
        case bestWinStreak
        case gammonWINS = "gammonWins"
        case losses
        case normalWINS = "normalWins"
        case totalGames, totalStakes, wins, winStreak
    }

    public init(backgammonWINS: Double, bestWinStreak: Double, gammonWINS: Double, losses: Double, normalWINS: Double, totalGames: Double, totalStakes: Double, wins: Double, winStreak: Double) {
        self.backgammonWINS = backgammonWINS
        self.bestWinStreak = bestWinStreak
        self.gammonWINS = gammonWINS
        self.losses = losses
        self.normalWINS = normalWINS
        self.totalGames = totalGames
        self.totalStakes = totalStakes
        self.wins = wins
        self.winStreak = winStreak
    }
}

// MARK: UserStats convenience initializers and mutators

public extension UserStats {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(UserStats.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        backgammonWINS: Double? = nil,
        bestWinStreak: Double? = nil,
        gammonWINS: Double? = nil,
        losses: Double? = nil,
        normalWINS: Double? = nil,
        totalGames: Double? = nil,
        totalStakes: Double? = nil,
        wins: Double? = nil,
        winStreak: Double? = nil
    ) -> UserStats {
        return UserStats(
            backgammonWINS: backgammonWINS ?? self.backgammonWINS,
            bestWinStreak: bestWinStreak ?? self.bestWinStreak,
            gammonWINS: gammonWINS ?? self.gammonWINS,
            losses: losses ?? self.losses,
            normalWINS: normalWINS ?? self.normalWINS,
            totalGames: totalGames ?? self.totalGames,
            totalStakes: totalStakes ?? self.totalStakes,
            wins: wins ?? self.wins,
            winStreak: winStreak ?? self.winStreak
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.ActiveGameBoardPointDto
// MARK: - ActiveGameBoardPointDto
public struct ActiveGameBoardPointDto: Codable {
    /// Transpiled from string?
    public let color: String?
    /// Transpiled from int
    public let count: Double
    /// Transpiled from int
    public let position: Double

    public init(color: String?, count: Double, position: Double) {
        self.color = color
        self.count = count
        self.position = position
    }
}

// MARK: ActiveGameBoardPointDto convenience initializers and mutators

public extension ActiveGameBoardPointDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(ActiveGameBoardPointDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        color: String?? = nil,
        count: Double? = nil,
        position: Double? = nil
    ) -> ActiveGameBoardPointDto {
        return ActiveGameBoardPointDto(
            color: color ?? self.color,
            count: count ?? self.count,
            position: position ?? self.position
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.ActiveGameDto
// MARK: - ActiveGameDto
public struct ActiveGameDto: Codable {
    /// Transpiled from Backgammon.Server.Models.SignalR.ActiveGameBoardPointDto[]?
    public let board: [ActiveGameDtoBoard]?
    /// Transpiled from string
    public let cubeOwner: String
    /// Transpiled from int
    public let cubeValue: Double
    /// Transpiled from string
    public let currentPlayer: String
    /// Transpiled from string?
    public let gameID: String?
    /// Transpiled from bool
    public let isCrawford: Bool
    /// Transpiled from bool
    public let isYourTurn: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from int
    public let matchLength: Double
    /// Transpiled from string
    public let matchScore: String
    /// Transpiled from string
    public let myColor: String
    /// Transpiled from string
    public let player1Name: String
    /// Transpiled from int
    public let player1Rating: Double
    /// Transpiled from string
    public let player2Name: String
    /// Transpiled from int
    public let player2Rating: Double
    /// Transpiled from int
    public let redBornOff: Double
    /// Transpiled from int
    public let redCheckersOnBar: Double
    /// Transpiled from string
    public let timeControl: String
    /// Transpiled from int
    public let viewers: Double
    /// Transpiled from int
    public let whiteBornOff: Double
    /// Transpiled from int
    public let whiteCheckersOnBar: Double

    public enum CodingKeys: String, CodingKey {
        case board, cubeOwner, cubeValue, currentPlayer
        case gameID = "gameId"
        case isCrawford, isYourTurn
        case matchID = "matchId"
        case matchLength, matchScore, myColor, player1Name, player1Rating, player2Name, player2Rating, redBornOff, redCheckersOnBar, timeControl, viewers, whiteBornOff, whiteCheckersOnBar
    }

    public init(board: [ActiveGameDtoBoard]?, cubeOwner: String, cubeValue: Double, currentPlayer: String, gameID: String?, isCrawford: Bool, isYourTurn: Bool, matchID: String, matchLength: Double, matchScore: String, myColor: String, player1Name: String, player1Rating: Double, player2Name: String, player2Rating: Double, redBornOff: Double, redCheckersOnBar: Double, timeControl: String, viewers: Double, whiteBornOff: Double, whiteCheckersOnBar: Double) {
        self.board = board
        self.cubeOwner = cubeOwner
        self.cubeValue = cubeValue
        self.currentPlayer = currentPlayer
        self.gameID = gameID
        self.isCrawford = isCrawford
        self.isYourTurn = isYourTurn
        self.matchID = matchID
        self.matchLength = matchLength
        self.matchScore = matchScore
        self.myColor = myColor
        self.player1Name = player1Name
        self.player1Rating = player1Rating
        self.player2Name = player2Name
        self.player2Rating = player2Rating
        self.redBornOff = redBornOff
        self.redCheckersOnBar = redCheckersOnBar
        self.timeControl = timeControl
        self.viewers = viewers
        self.whiteBornOff = whiteBornOff
        self.whiteCheckersOnBar = whiteCheckersOnBar
    }
}

// MARK: ActiveGameDto convenience initializers and mutators

public extension ActiveGameDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(ActiveGameDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        board: [ActiveGameDtoBoard]?? = nil,
        cubeOwner: String? = nil,
        cubeValue: Double? = nil,
        currentPlayer: String? = nil,
        gameID: String?? = nil,
        isCrawford: Bool? = nil,
        isYourTurn: Bool? = nil,
        matchID: String? = nil,
        matchLength: Double? = nil,
        matchScore: String? = nil,
        myColor: String? = nil,
        player1Name: String? = nil,
        player1Rating: Double? = nil,
        player2Name: String? = nil,
        player2Rating: Double? = nil,
        redBornOff: Double? = nil,
        redCheckersOnBar: Double? = nil,
        timeControl: String? = nil,
        viewers: Double? = nil,
        whiteBornOff: Double? = nil,
        whiteCheckersOnBar: Double? = nil
    ) -> ActiveGameDto {
        return ActiveGameDto(
            board: board ?? self.board,
            cubeOwner: cubeOwner ?? self.cubeOwner,
            cubeValue: cubeValue ?? self.cubeValue,
            currentPlayer: currentPlayer ?? self.currentPlayer,
            gameID: gameID ?? self.gameID,
            isCrawford: isCrawford ?? self.isCrawford,
            isYourTurn: isYourTurn ?? self.isYourTurn,
            matchID: matchID ?? self.matchID,
            matchLength: matchLength ?? self.matchLength,
            matchScore: matchScore ?? self.matchScore,
            myColor: myColor ?? self.myColor,
            player1Name: player1Name ?? self.player1Name,
            player1Rating: player1Rating ?? self.player1Rating,
            player2Name: player2Name ?? self.player2Name,
            player2Rating: player2Rating ?? self.player2Rating,
            redBornOff: redBornOff ?? self.redBornOff,
            redCheckersOnBar: redCheckersOnBar ?? self.redCheckersOnBar,
            timeControl: timeControl ?? self.timeControl,
            viewers: viewers ?? self.viewers,
            whiteBornOff: whiteBornOff ?? self.whiteBornOff,
            whiteCheckersOnBar: whiteCheckersOnBar ?? self.whiteCheckersOnBar
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.ActiveGameBoardPointDto
// MARK: - ActiveGameDtoBoard
public struct ActiveGameDtoBoard: Codable {
    /// Transpiled from string?
    public let color: String?
    /// Transpiled from int
    public let count: Double
    /// Transpiled from int
    public let position: Double

    public init(color: String?, count: Double, position: Double) {
        self.color = color
        self.count = count
        self.position = position
    }
}

// MARK: ActiveGameDtoBoard convenience initializers and mutators

public extension ActiveGameDtoBoard {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(ActiveGameDtoBoard.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        color: String?? = nil,
        count: Double? = nil,
        position: Double? = nil
    ) -> ActiveGameDtoBoard {
        return ActiveGameDtoBoard(
            color: color ?? self.color,
            count: count ?? self.count,
            position: position ?? self.position
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.ActiveMatchDto
// MARK: - ActiveMatchDto
public struct ActiveMatchDto: Codable {
    /// Transpiled from System.DateTime
    public let createdAt: String
    /// Transpiled from string?
    public let currentGameID: String?
    /// Transpiled from int
    public let gamesPlayed: Double
    /// Transpiled from bool
    public let isCorrespondence: Bool
    /// Transpiled from bool
    public let isCrawford: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from int
    public let myScore: Double
    /// Transpiled from string
    public let opponentName: String
    /// Transpiled from int
    public let opponentScore: Double
    /// Transpiled from int
    public let targetScore: Double

    public enum CodingKeys: String, CodingKey {
        case createdAt
        case currentGameID = "currentGameId"
        case gamesPlayed, isCorrespondence, isCrawford
        case matchID = "matchId"
        case myScore, opponentName, opponentScore, targetScore
    }

    public init(createdAt: String, currentGameID: String?, gamesPlayed: Double, isCorrespondence: Bool, isCrawford: Bool, matchID: String, myScore: Double, opponentName: String, opponentScore: Double, targetScore: Double) {
        self.createdAt = createdAt
        self.currentGameID = currentGameID
        self.gamesPlayed = gamesPlayed
        self.isCorrespondence = isCorrespondence
        self.isCrawford = isCrawford
        self.matchID = matchID
        self.myScore = myScore
        self.opponentName = opponentName
        self.opponentScore = opponentScore
        self.targetScore = targetScore
    }
}

// MARK: ActiveMatchDto convenience initializers and mutators

public extension ActiveMatchDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(ActiveMatchDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        createdAt: String? = nil,
        currentGameID: String?? = nil,
        gamesPlayed: Double? = nil,
        isCorrespondence: Bool? = nil,
        isCrawford: Bool? = nil,
        matchID: String? = nil,
        myScore: Double? = nil,
        opponentName: String? = nil,
        opponentScore: Double? = nil,
        targetScore: Double? = nil
    ) -> ActiveMatchDto {
        return ActiveMatchDto(
            createdAt: createdAt ?? self.createdAt,
            currentGameID: currentGameID ?? self.currentGameID,
            gamesPlayed: gamesPlayed ?? self.gamesPlayed,
            isCorrespondence: isCorrespondence ?? self.isCorrespondence,
            isCrawford: isCrawford ?? self.isCrawford,
            matchID: matchID ?? self.matchID,
            myScore: myScore ?? self.myScore,
            opponentName: opponentName ?? self.opponentName,
            opponentScore: opponentScore ?? self.opponentScore,
            targetScore: targetScore ?? self.targetScore
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.ChatHistoryDto
// MARK: - ChatHistoryDto
public struct ChatHistoryDto: Codable {
    /// Transpiled from
    /// System.Collections.Generic.List<Backgammon.Server.Models.SignalR.ChatMessageDto>
    public let messages: [Message]

    public init(messages: [Message]) {
        self.messages = messages
    }
}

// MARK: ChatHistoryDto convenience initializers and mutators

public extension ChatHistoryDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(ChatHistoryDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        messages: [Message]? = nil
    ) -> ChatHistoryDto {
        return ChatHistoryDto(
            messages: messages ?? self.messages
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.ChatMessageDto
// MARK: - Message
public struct Message: Codable {
    /// Transpiled from bool
    public let isOwn: Bool
    /// Transpiled from string
    public let message: String
    /// Transpiled from string
    public let senderName: String
    /// Transpiled from System.DateTime
    public let timestamp: String

    public init(isOwn: Bool, message: String, senderName: String, timestamp: String) {
        self.isOwn = isOwn
        self.message = message
        self.senderName = senderName
        self.timestamp = timestamp
    }
}

// MARK: Message convenience initializers and mutators

public extension Message {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(Message.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        isOwn: Bool? = nil,
        message: String? = nil,
        senderName: String? = nil,
        timestamp: String? = nil
    ) -> Message {
        return Message(
            isOwn: isOwn ?? self.isOwn,
            message: message ?? self.message,
            senderName: senderName ?? self.senderName,
            timestamp: timestamp ?? self.timestamp
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.ChatMessageDto
// MARK: - ChatMessageDto
public struct ChatMessageDto: Codable {
    /// Transpiled from bool
    public let isOwn: Bool
    /// Transpiled from string
    public let message: String
    /// Transpiled from string
    public let senderName: String
    /// Transpiled from System.DateTime
    public let timestamp: String

    public init(isOwn: Bool, message: String, senderName: String, timestamp: String) {
        self.isOwn = isOwn
        self.message = message
        self.senderName = senderName
        self.timestamp = timestamp
    }
}

// MARK: ChatMessageDto convenience initializers and mutators

public extension ChatMessageDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(ChatMessageDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        isOwn: Bool? = nil,
        message: String? = nil,
        senderName: String? = nil,
        timestamp: String? = nil
    ) -> ChatMessageDto {
        return ChatMessageDto(
            isOwn: isOwn ?? self.isOwn,
            message: message ?? self.message,
            senderName: senderName ?? self.senderName,
            timestamp: timestamp ?? self.timestamp
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.CorrespondenceLobbyCreatedDto
// MARK: - CorrespondenceLobbyCreatedDto
public struct CorrespondenceLobbyCreatedDto: Codable {
    /// Transpiled from string
    public let creatorPlayerID: String
    /// Transpiled from string
    public let creatorUsername: String
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let isRated: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let timePerMoveDays: Double

    public enum CodingKeys: String, CodingKey {
        case creatorPlayerID = "creatorPlayerId"
        case creatorUsername
        case gameID = "gameId"
        case isRated
        case matchID = "matchId"
        case targetScore, timePerMoveDays
    }

    public init(creatorPlayerID: String, creatorUsername: String, gameID: String, isRated: Bool, matchID: String, targetScore: Double, timePerMoveDays: Double) {
        self.creatorPlayerID = creatorPlayerID
        self.creatorUsername = creatorUsername
        self.gameID = gameID
        self.isRated = isRated
        self.matchID = matchID
        self.targetScore = targetScore
        self.timePerMoveDays = timePerMoveDays
    }
}

// MARK: CorrespondenceLobbyCreatedDto convenience initializers and mutators

public extension CorrespondenceLobbyCreatedDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(CorrespondenceLobbyCreatedDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        creatorPlayerID: String? = nil,
        creatorUsername: String? = nil,
        gameID: String? = nil,
        isRated: Bool? = nil,
        matchID: String? = nil,
        targetScore: Double? = nil,
        timePerMoveDays: Double? = nil
    ) -> CorrespondenceLobbyCreatedDto {
        return CorrespondenceLobbyCreatedDto(
            creatorPlayerID: creatorPlayerID ?? self.creatorPlayerID,
            creatorUsername: creatorUsername ?? self.creatorUsername,
            gameID: gameID ?? self.gameID,
            isRated: isRated ?? self.isRated,
            matchID: matchID ?? self.matchID,
            targetScore: targetScore ?? self.targetScore,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.CorrespondenceMatchInviteDto
// MARK: - CorrespondenceMatchInviteDto
public struct CorrespondenceMatchInviteDto: Codable {
    /// Transpiled from string
    public let challengerID: String
    /// Transpiled from string
    public let challengerName: String
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let timePerMoveDays: Double

    public enum CodingKeys: String, CodingKey {
        case challengerID = "challengerId"
        case challengerName
        case gameID = "gameId"
        case matchID = "matchId"
        case targetScore, timePerMoveDays
    }

    public init(challengerID: String, challengerName: String, gameID: String, matchID: String, targetScore: Double, timePerMoveDays: Double) {
        self.challengerID = challengerID
        self.challengerName = challengerName
        self.gameID = gameID
        self.matchID = matchID
        self.targetScore = targetScore
        self.timePerMoveDays = timePerMoveDays
    }
}

// MARK: CorrespondenceMatchInviteDto convenience initializers and mutators

public extension CorrespondenceMatchInviteDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(CorrespondenceMatchInviteDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        challengerID: String? = nil,
        challengerName: String? = nil,
        gameID: String? = nil,
        matchID: String? = nil,
        targetScore: Double? = nil,
        timePerMoveDays: Double? = nil
    ) -> CorrespondenceMatchInviteDto {
        return CorrespondenceMatchInviteDto(
            challengerID: challengerID ?? self.challengerID,
            challengerName: challengerName ?? self.challengerName,
            gameID: gameID ?? self.gameID,
            matchID: matchID ?? self.matchID,
            targetScore: targetScore ?? self.targetScore,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.CorrespondenceTurnNotificationDto
// MARK: - CorrespondenceTurnNotificationDto
public struct CorrespondenceTurnNotificationDto: Codable {
    /// Transpiled from string?
    public let gameID: String?
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string
    public let message: String

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case matchID = "matchId"
        case message
    }

    public init(gameID: String?, matchID: String, message: String) {
        self.gameID = gameID
        self.matchID = matchID
        self.message = message
    }
}

// MARK: CorrespondenceTurnNotificationDto convenience initializers and mutators

public extension CorrespondenceTurnNotificationDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(CorrespondenceTurnNotificationDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String?? = nil,
        matchID: String? = nil,
        message: String? = nil
    ) -> CorrespondenceTurnNotificationDto {
        return CorrespondenceTurnNotificationDto(
            gameID: gameID ?? self.gameID,
            matchID: matchID ?? self.matchID,
            message: message ?? self.message
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.DoubleOfferDto
// MARK: - DoubleOfferDto
public struct DoubleOfferDto: Codable {
    /// Transpiled from int
    public let currentStakes: Double
    /// Transpiled from int
    public let newStakes: Double

    public init(currentStakes: Double, newStakes: Double) {
        self.currentStakes = currentStakes
        self.newStakes = newStakes
    }
}

// MARK: DoubleOfferDto convenience initializers and mutators

public extension DoubleOfferDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(DoubleOfferDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        currentStakes: Double? = nil,
        newStakes: Double? = nil
    ) -> DoubleOfferDto {
        return DoubleOfferDto(
            currentStakes: currentStakes ?? self.currentStakes,
            newStakes: newStakes ?? self.newStakes
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.LobbyCreatedDto
// MARK: - LobbyCreatedDto
public struct LobbyCreatedDto: Codable {
    /// Transpiled from string
    public let creatorName: String
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let isRated: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from int
    public let targetScore: Double

    public enum CodingKeys: String, CodingKey {
        case creatorName
        case gameID = "gameId"
        case isRated
        case matchID = "matchId"
        case targetScore
    }

    public init(creatorName: String, gameID: String, isRated: Bool, matchID: String, targetScore: Double) {
        self.creatorName = creatorName
        self.gameID = gameID
        self.isRated = isRated
        self.matchID = matchID
        self.targetScore = targetScore
    }
}

// MARK: LobbyCreatedDto convenience initializers and mutators

public extension LobbyCreatedDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(LobbyCreatedDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        creatorName: String? = nil,
        gameID: String? = nil,
        isRated: Bool? = nil,
        matchID: String? = nil,
        targetScore: Double? = nil
    ) -> LobbyCreatedDto {
        return LobbyCreatedDto(
            creatorName: creatorName ?? self.creatorName,
            gameID: gameID ?? self.gameID,
            isRated: isRated ?? self.isRated,
            matchID: matchID ?? self.matchID,
            targetScore: targetScore ?? self.targetScore
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchCompletedDto
// MARK: - MatchCompletedDto
public struct MatchCompletedDto: Codable {
    /// Transpiled from Backgammon.Server.Models.SignalR.MatchFinalScoreDto
    public let finalScore: MatchCompletedDtoFinalScore
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string
    public let winner: String

    public enum CodingKeys: String, CodingKey {
        case finalScore
        case matchID = "matchId"
        case winner
    }

    public init(finalScore: MatchCompletedDtoFinalScore, matchID: String, winner: String) {
        self.finalScore = finalScore
        self.matchID = matchID
        self.winner = winner
    }
}

// MARK: MatchCompletedDto convenience initializers and mutators

public extension MatchCompletedDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchCompletedDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        finalScore: MatchCompletedDtoFinalScore? = nil,
        matchID: String? = nil,
        winner: String? = nil
    ) -> MatchCompletedDto {
        return MatchCompletedDto(
            finalScore: finalScore ?? self.finalScore,
            matchID: matchID ?? self.matchID,
            winner: winner ?? self.winner
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchFinalScoreDto
// MARK: - MatchCompletedDtoFinalScore
public struct MatchCompletedDtoFinalScore: Codable {
    /// Transpiled from int
    public let player1: Double
    /// Transpiled from int
    public let player2: Double

    public init(player1: Double, player2: Double) {
        self.player1 = player1
        self.player2 = player2
    }
}

// MARK: MatchCompletedDtoFinalScore convenience initializers and mutators

public extension MatchCompletedDtoFinalScore {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchCompletedDtoFinalScore.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        player1: Double? = nil,
        player2: Double? = nil
    ) -> MatchCompletedDtoFinalScore {
        return MatchCompletedDtoFinalScore(
            player1: player1 ?? self.player1,
            player2: player2 ?? self.player2
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchContinuedDto
// MARK: - MatchContinuedDto
public struct MatchContinuedDto: Codable {
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let isCrawfordGame: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from int
    public let player1Score: Double
    /// Transpiled from int
    public let player2Score: Double
    /// Transpiled from int
    public let targetScore: Double

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case isCrawfordGame
        case matchID = "matchId"
        case player1Score, player2Score, targetScore
    }

    public init(gameID: String, isCrawfordGame: Bool, matchID: String, player1Score: Double, player2Score: Double, targetScore: Double) {
        self.gameID = gameID
        self.isCrawfordGame = isCrawfordGame
        self.matchID = matchID
        self.player1Score = player1Score
        self.player2Score = player2Score
        self.targetScore = targetScore
    }
}

// MARK: MatchContinuedDto convenience initializers and mutators

public extension MatchContinuedDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchContinuedDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String? = nil,
        isCrawfordGame: Bool? = nil,
        matchID: String? = nil,
        player1Score: Double? = nil,
        player2Score: Double? = nil,
        targetScore: Double? = nil
    ) -> MatchContinuedDto {
        return MatchContinuedDto(
            gameID: gameID ?? self.gameID,
            isCrawfordGame: isCrawfordGame ?? self.isCrawfordGame,
            matchID: matchID ?? self.matchID,
            player1Score: player1Score ?? self.player1Score,
            player2Score: player2Score ?? self.player2Score,
            targetScore: targetScore ?? self.targetScore
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchCreatedDto
// MARK: - MatchCreatedDto
public struct MatchCreatedDto: Codable {
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let isCorrespondence: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string
    public let opponentType: String
    /// Transpiled from string
    public let player1ID: String
    /// Transpiled from string
    public let player1Name: String
    /// Transpiled from string?
    public let player2ID: String?
    /// Transpiled from string?
    public let player2Name: String?
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let timePerMoveDays: Double?
    /// Transpiled from System.DateTime
    public let turnDeadline: String?

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case isCorrespondence
        case matchID = "matchId"
        case opponentType
        case player1ID = "player1Id"
        case player1Name
        case player2ID = "player2Id"
        case player2Name, targetScore, timePerMoveDays, turnDeadline
    }

    public init(gameID: String, isCorrespondence: Bool, matchID: String, opponentType: String, player1ID: String, player1Name: String, player2ID: String?, player2Name: String?, targetScore: Double, timePerMoveDays: Double?, turnDeadline: String?) {
        self.gameID = gameID
        self.isCorrespondence = isCorrespondence
        self.matchID = matchID
        self.opponentType = opponentType
        self.player1ID = player1ID
        self.player1Name = player1Name
        self.player2ID = player2ID
        self.player2Name = player2Name
        self.targetScore = targetScore
        self.timePerMoveDays = timePerMoveDays
        self.turnDeadline = turnDeadline
    }
}

// MARK: MatchCreatedDto convenience initializers and mutators

public extension MatchCreatedDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchCreatedDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String? = nil,
        isCorrespondence: Bool? = nil,
        matchID: String? = nil,
        opponentType: String? = nil,
        player1ID: String? = nil,
        player1Name: String? = nil,
        player2ID: String?? = nil,
        player2Name: String?? = nil,
        targetScore: Double? = nil,
        timePerMoveDays: Double?? = nil,
        turnDeadline: String?? = nil
    ) -> MatchCreatedDto {
        return MatchCreatedDto(
            gameID: gameID ?? self.gameID,
            isCorrespondence: isCorrespondence ?? self.isCorrespondence,
            matchID: matchID ?? self.matchID,
            opponentType: opponentType ?? self.opponentType,
            player1ID: player1ID ?? self.player1ID,
            player1Name: player1Name ?? self.player1Name,
            player2ID: player2ID ?? self.player2ID,
            player2Name: player2Name ?? self.player2Name,
            targetScore: targetScore ?? self.targetScore,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays,
            turnDeadline: turnDeadline ?? self.turnDeadline
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchFinalScoreDto
// MARK: - MatchFinalScoreDto
public struct MatchFinalScoreDto: Codable {
    /// Transpiled from int
    public let player1: Double
    /// Transpiled from int
    public let player2: Double

    public init(player1: Double, player2: Double) {
        self.player1 = player1
        self.player2 = player2
    }
}

// MARK: MatchFinalScoreDto convenience initializers and mutators

public extension MatchFinalScoreDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchFinalScoreDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        player1: Double? = nil,
        player2: Double? = nil
    ) -> MatchFinalScoreDto {
        return MatchFinalScoreDto(
            player1: player1 ?? self.player1,
            player2: player2 ?? self.player2
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchGameCompletedDto
// MARK: - MatchGameCompletedDto
public struct MatchGameCompletedDto: Codable {
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from int
    public let points: Double
    /// Transpiled from string
    public let winner: String

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case matchID = "matchId"
        case points, winner
    }

    public init(gameID: String, matchID: String, points: Double, winner: String) {
        self.gameID = gameID
        self.matchID = matchID
        self.points = points
        self.winner = winner
    }
}

// MARK: MatchGameCompletedDto convenience initializers and mutators

public extension MatchGameCompletedDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchGameCompletedDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String? = nil,
        matchID: String? = nil,
        points: Double? = nil,
        winner: String? = nil
    ) -> MatchGameCompletedDto {
        return MatchGameCompletedDto(
            gameID: gameID ?? self.gameID,
            matchID: matchID ?? self.matchID,
            points: points ?? self.points,
            winner: winner ?? self.winner
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchGameDto
// MARK: - MatchGameDto
public struct MatchGameDto: Codable {
    /// Transpiled from System.DateTime
    public let completedAt: String?
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from int
    public let gameNumber: Double
    /// Transpiled from bool
    public let isCrawford: Bool
    /// Transpiled from int
    public let pointsScored: Double
    /// Transpiled from string
    public let status: String
    /// Transpiled from string?
    public let winner: String?
    /// Transpiled from string?
    public let winType: String?

    public enum CodingKeys: String, CodingKey {
        case completedAt
        case gameID = "gameId"
        case gameNumber, isCrawford, pointsScored, status, winner, winType
    }

    public init(completedAt: String?, gameID: String, gameNumber: Double, isCrawford: Bool, pointsScored: Double, status: String, winner: String?, winType: String?) {
        self.completedAt = completedAt
        self.gameID = gameID
        self.gameNumber = gameNumber
        self.isCrawford = isCrawford
        self.pointsScored = pointsScored
        self.status = status
        self.winner = winner
        self.winType = winType
    }
}

// MARK: MatchGameDto convenience initializers and mutators

public extension MatchGameDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchGameDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        completedAt: String?? = nil,
        gameID: String? = nil,
        gameNumber: Double? = nil,
        isCrawford: Bool? = nil,
        pointsScored: Double? = nil,
        status: String? = nil,
        winner: String?? = nil,
        winType: String?? = nil
    ) -> MatchGameDto {
        return MatchGameDto(
            completedAt: completedAt ?? self.completedAt,
            gameID: gameID ?? self.gameID,
            gameNumber: gameNumber ?? self.gameNumber,
            isCrawford: isCrawford ?? self.isCrawford,
            pointsScored: pointsScored ?? self.pointsScored,
            status: status ?? self.status,
            winner: winner ?? self.winner,
            winType: winType ?? self.winType
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchGameStartingDto
// MARK: - MatchGameStartingDto
public struct MatchGameStartingDto: Codable {
    /// Transpiled from Backgammon.Server.Models.SignalR.MatchScoreDto
    public let currentScore: CurrentScore
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from int
    public let gameNumber: Double
    /// Transpiled from bool
    public let isCrawfordGame: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from Backgammon.Server.Models.GameState
    public let state: State

    public enum CodingKeys: String, CodingKey {
        case currentScore
        case gameID = "gameId"
        case gameNumber, isCrawfordGame
        case matchID = "matchId"
        case state
    }

    public init(currentScore: CurrentScore, gameID: String, gameNumber: Double, isCrawfordGame: Bool, matchID: String, state: State) {
        self.currentScore = currentScore
        self.gameID = gameID
        self.gameNumber = gameNumber
        self.isCrawfordGame = isCrawfordGame
        self.matchID = matchID
        self.state = state
    }
}

// MARK: MatchGameStartingDto convenience initializers and mutators

public extension MatchGameStartingDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchGameStartingDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        currentScore: CurrentScore? = nil,
        gameID: String? = nil,
        gameNumber: Double? = nil,
        isCrawfordGame: Bool? = nil,
        matchID: String? = nil,
        state: State? = nil
    ) -> MatchGameStartingDto {
        return MatchGameStartingDto(
            currentScore: currentScore ?? self.currentScore,
            gameID: gameID ?? self.gameID,
            gameNumber: gameNumber ?? self.gameNumber,
            isCrawfordGame: isCrawfordGame ?? self.isCrawfordGame,
            matchID: matchID ?? self.matchID,
            state: state ?? self.state
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchScoreDto
// MARK: - CurrentScore
public struct CurrentScore: Codable {
    /// Transpiled from int
    public let player1: Double
    /// Transpiled from int
    public let player2: Double

    public init(player1: Double, player2: Double) {
        self.player1 = player1
        self.player2 = player2
    }
}

// MARK: CurrentScore convenience initializers and mutators

public extension CurrentScore {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(CurrentScore.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        player1: Double? = nil,
        player2: Double? = nil
    ) -> CurrentScore {
        return CurrentScore(
            player1: player1 ?? self.player1,
            player2: player2 ?? self.player2
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.GameState
// MARK: - State
public struct State: Codable {
    /// Transpiled from Backgammon.Server.Models.PointState[]
    public let board: [StateBoard]
    /// Transpiled from bool
    public let canDouble: Bool
    /// Transpiled from int[]
    public let currentDice: [Double]
    /// Transpiled from Backgammon.Core.CheckerColor
    public let currentPlayer: Double
    /// Transpiled from System.Collections.Generic.List<string>
    public let currentTurnMoves: [String]
    /// Transpiled from int
    public let delaySeconds: Double?
    /// Transpiled from int[]
    public let dice: [Double]
    /// Transpiled from string?
    public let doublingCubeOwner: String?
    /// Transpiled from int
    public let doublingCubeValue: Double
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let hasPendingDoubleOffer: Bool
    /// Transpiled from bool
    public let hasReceivedDoubleOffer: Bool
    /// Transpiled from bool
    public let hasValidMoves: Bool
    /// Transpiled from bool
    public let isAnalysisMode: Bool
    /// Transpiled from bool
    public let isAwaitingDoubleResponse: Bool
    /// Transpiled from bool
    public let isCorrespondence: Bool
    /// Transpiled from bool
    public let isCrawfordGame: Bool?
    /// Transpiled from bool
    public let isOpeningRoll: Bool
    /// Transpiled from bool
    public let isOpeningRollTie: Bool
    /// Transpiled from bool
    public let isRated: Bool
    /// Transpiled from bool
    public let isYourTurn: Bool
    /// Transpiled from string
    public let leaveGameAction: String
    /// Transpiled from string?
    public let matchID: String?
    /// Transpiled from int
    public let movesMadeThisTurn: Double
    /// Transpiled from int
    public let pendingDoubleNewValue: Double
    /// Transpiled from int
    public let player1Score: Double?
    /// Transpiled from int
    public let player2Score: Double?
    /// Transpiled from int
    public let redBornOff: Double
    /// Transpiled from int
    public let redCheckersOnBar: Double
    /// Transpiled from double
    public let redDelayRemaining: Double?
    /// Transpiled from bool
    public let redIsInDelay: Bool?
    /// Transpiled from int
    public let redOpeningRoll: Double?
    /// Transpiled from int
    public let redPipCount: Double
    /// Transpiled from string
    public let redPlayerID: String
    /// Transpiled from string
    public let redPlayerName: String
    /// Transpiled from int
    public let redRating: Double?
    /// Transpiled from int
    public let redRatingChange: Double?
    /// Transpiled from double
    public let redReserveSeconds: Double?
    /// Transpiled from string?
    public let redUsername: String?
    /// Transpiled from int[]
    public let remainingMoves: [Double]
    /// Transpiled from Backgammon.Server.Models.GameStatus
    public let status: Double
    /// Transpiled from int
    public let targetScore: Double?
    /// Transpiled from string?
    public let timeControlType: String?
    /// Transpiled from int
    public let timePerMoveDays: Double?
    /// Transpiled from System.DateTime
    public let turnDeadline: String?
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.TurnSnapshotDto>
    public let turnHistory: [StateTurnHistory]
    /// Transpiled from System.Collections.Generic.List<Backgammon.Server.Models.MoveDto>
    public let validMoves: [StateValidMove]
    /// Transpiled from int
    public let whiteBornOff: Double
    /// Transpiled from int
    public let whiteCheckersOnBar: Double
    /// Transpiled from double
    public let whiteDelayRemaining: Double?
    /// Transpiled from bool
    public let whiteIsInDelay: Bool?
    /// Transpiled from int
    public let whiteOpeningRoll: Double?
    /// Transpiled from int
    public let whitePipCount: Double
    /// Transpiled from string
    public let whitePlayerID: String
    /// Transpiled from string
    public let whitePlayerName: String
    /// Transpiled from int
    public let whiteRating: Double?
    /// Transpiled from int
    public let whiteRatingChange: Double?
    /// Transpiled from double
    public let whiteReserveSeconds: Double?
    /// Transpiled from string?
    public let whiteUsername: String?
    /// Transpiled from Backgammon.Core.CheckerColor
    public let winner: Double?
    /// Transpiled from string?
    public let winType: String?
    /// Transpiled from Backgammon.Core.CheckerColor
    public let yourColor: Double?

    public enum CodingKeys: String, CodingKey {
        case board, canDouble, currentDice, currentPlayer, currentTurnMoves, delaySeconds, dice, doublingCubeOwner, doublingCubeValue
        case gameID = "gameId"
        case hasPendingDoubleOffer, hasReceivedDoubleOffer, hasValidMoves, isAnalysisMode, isAwaitingDoubleResponse, isCorrespondence, isCrawfordGame, isOpeningRoll, isOpeningRollTie, isRated, isYourTurn, leaveGameAction
        case matchID = "matchId"
        case movesMadeThisTurn, pendingDoubleNewValue, player1Score, player2Score, redBornOff, redCheckersOnBar, redDelayRemaining, redIsInDelay, redOpeningRoll, redPipCount
        case redPlayerID = "redPlayerId"
        case redPlayerName, redRating, redRatingChange, redReserveSeconds, redUsername, remainingMoves, status, targetScore, timeControlType, timePerMoveDays, turnDeadline, turnHistory, validMoves, whiteBornOff, whiteCheckersOnBar, whiteDelayRemaining, whiteIsInDelay, whiteOpeningRoll, whitePipCount
        case whitePlayerID = "whitePlayerId"
        case whitePlayerName, whiteRating, whiteRatingChange, whiteReserveSeconds, whiteUsername, winner, winType, yourColor
    }

    public init(board: [StateBoard], canDouble: Bool, currentDice: [Double], currentPlayer: Double, currentTurnMoves: [String], delaySeconds: Double?, dice: [Double], doublingCubeOwner: String?, doublingCubeValue: Double, gameID: String, hasPendingDoubleOffer: Bool, hasReceivedDoubleOffer: Bool, hasValidMoves: Bool, isAnalysisMode: Bool, isAwaitingDoubleResponse: Bool, isCorrespondence: Bool, isCrawfordGame: Bool?, isOpeningRoll: Bool, isOpeningRollTie: Bool, isRated: Bool, isYourTurn: Bool, leaveGameAction: String, matchID: String?, movesMadeThisTurn: Double, pendingDoubleNewValue: Double, player1Score: Double?, player2Score: Double?, redBornOff: Double, redCheckersOnBar: Double, redDelayRemaining: Double?, redIsInDelay: Bool?, redOpeningRoll: Double?, redPipCount: Double, redPlayerID: String, redPlayerName: String, redRating: Double?, redRatingChange: Double?, redReserveSeconds: Double?, redUsername: String?, remainingMoves: [Double], status: Double, targetScore: Double?, timeControlType: String?, timePerMoveDays: Double?, turnDeadline: String?, turnHistory: [StateTurnHistory], validMoves: [StateValidMove], whiteBornOff: Double, whiteCheckersOnBar: Double, whiteDelayRemaining: Double?, whiteIsInDelay: Bool?, whiteOpeningRoll: Double?, whitePipCount: Double, whitePlayerID: String, whitePlayerName: String, whiteRating: Double?, whiteRatingChange: Double?, whiteReserveSeconds: Double?, whiteUsername: String?, winner: Double?, winType: String?, yourColor: Double?) {
        self.board = board
        self.canDouble = canDouble
        self.currentDice = currentDice
        self.currentPlayer = currentPlayer
        self.currentTurnMoves = currentTurnMoves
        self.delaySeconds = delaySeconds
        self.dice = dice
        self.doublingCubeOwner = doublingCubeOwner
        self.doublingCubeValue = doublingCubeValue
        self.gameID = gameID
        self.hasPendingDoubleOffer = hasPendingDoubleOffer
        self.hasReceivedDoubleOffer = hasReceivedDoubleOffer
        self.hasValidMoves = hasValidMoves
        self.isAnalysisMode = isAnalysisMode
        self.isAwaitingDoubleResponse = isAwaitingDoubleResponse
        self.isCorrespondence = isCorrespondence
        self.isCrawfordGame = isCrawfordGame
        self.isOpeningRoll = isOpeningRoll
        self.isOpeningRollTie = isOpeningRollTie
        self.isRated = isRated
        self.isYourTurn = isYourTurn
        self.leaveGameAction = leaveGameAction
        self.matchID = matchID
        self.movesMadeThisTurn = movesMadeThisTurn
        self.pendingDoubleNewValue = pendingDoubleNewValue
        self.player1Score = player1Score
        self.player2Score = player2Score
        self.redBornOff = redBornOff
        self.redCheckersOnBar = redCheckersOnBar
        self.redDelayRemaining = redDelayRemaining
        self.redIsInDelay = redIsInDelay
        self.redOpeningRoll = redOpeningRoll
        self.redPipCount = redPipCount
        self.redPlayerID = redPlayerID
        self.redPlayerName = redPlayerName
        self.redRating = redRating
        self.redRatingChange = redRatingChange
        self.redReserveSeconds = redReserveSeconds
        self.redUsername = redUsername
        self.remainingMoves = remainingMoves
        self.status = status
        self.targetScore = targetScore
        self.timeControlType = timeControlType
        self.timePerMoveDays = timePerMoveDays
        self.turnDeadline = turnDeadline
        self.turnHistory = turnHistory
        self.validMoves = validMoves
        self.whiteBornOff = whiteBornOff
        self.whiteCheckersOnBar = whiteCheckersOnBar
        self.whiteDelayRemaining = whiteDelayRemaining
        self.whiteIsInDelay = whiteIsInDelay
        self.whiteOpeningRoll = whiteOpeningRoll
        self.whitePipCount = whitePipCount
        self.whitePlayerID = whitePlayerID
        self.whitePlayerName = whitePlayerName
        self.whiteRating = whiteRating
        self.whiteRatingChange = whiteRatingChange
        self.whiteReserveSeconds = whiteReserveSeconds
        self.whiteUsername = whiteUsername
        self.winner = winner
        self.winType = winType
        self.yourColor = yourColor
    }
}

// MARK: State convenience initializers and mutators

public extension State {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(State.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        board: [StateBoard]? = nil,
        canDouble: Bool? = nil,
        currentDice: [Double]? = nil,
        currentPlayer: Double? = nil,
        currentTurnMoves: [String]? = nil,
        delaySeconds: Double?? = nil,
        dice: [Double]? = nil,
        doublingCubeOwner: String?? = nil,
        doublingCubeValue: Double? = nil,
        gameID: String? = nil,
        hasPendingDoubleOffer: Bool? = nil,
        hasReceivedDoubleOffer: Bool? = nil,
        hasValidMoves: Bool? = nil,
        isAnalysisMode: Bool? = nil,
        isAwaitingDoubleResponse: Bool? = nil,
        isCorrespondence: Bool? = nil,
        isCrawfordGame: Bool?? = nil,
        isOpeningRoll: Bool? = nil,
        isOpeningRollTie: Bool? = nil,
        isRated: Bool? = nil,
        isYourTurn: Bool? = nil,
        leaveGameAction: String? = nil,
        matchID: String?? = nil,
        movesMadeThisTurn: Double? = nil,
        pendingDoubleNewValue: Double? = nil,
        player1Score: Double?? = nil,
        player2Score: Double?? = nil,
        redBornOff: Double? = nil,
        redCheckersOnBar: Double? = nil,
        redDelayRemaining: Double?? = nil,
        redIsInDelay: Bool?? = nil,
        redOpeningRoll: Double?? = nil,
        redPipCount: Double? = nil,
        redPlayerID: String? = nil,
        redPlayerName: String? = nil,
        redRating: Double?? = nil,
        redRatingChange: Double?? = nil,
        redReserveSeconds: Double?? = nil,
        redUsername: String?? = nil,
        remainingMoves: [Double]? = nil,
        status: Double? = nil,
        targetScore: Double?? = nil,
        timeControlType: String?? = nil,
        timePerMoveDays: Double?? = nil,
        turnDeadline: String?? = nil,
        turnHistory: [StateTurnHistory]? = nil,
        validMoves: [StateValidMove]? = nil,
        whiteBornOff: Double? = nil,
        whiteCheckersOnBar: Double? = nil,
        whiteDelayRemaining: Double?? = nil,
        whiteIsInDelay: Bool?? = nil,
        whiteOpeningRoll: Double?? = nil,
        whitePipCount: Double? = nil,
        whitePlayerID: String? = nil,
        whitePlayerName: String? = nil,
        whiteRating: Double?? = nil,
        whiteRatingChange: Double?? = nil,
        whiteReserveSeconds: Double?? = nil,
        whiteUsername: String?? = nil,
        winner: Double?? = nil,
        winType: String?? = nil,
        yourColor: Double?? = nil
    ) -> State {
        return State(
            board: board ?? self.board,
            canDouble: canDouble ?? self.canDouble,
            currentDice: currentDice ?? self.currentDice,
            currentPlayer: currentPlayer ?? self.currentPlayer,
            currentTurnMoves: currentTurnMoves ?? self.currentTurnMoves,
            delaySeconds: delaySeconds ?? self.delaySeconds,
            dice: dice ?? self.dice,
            doublingCubeOwner: doublingCubeOwner ?? self.doublingCubeOwner,
            doublingCubeValue: doublingCubeValue ?? self.doublingCubeValue,
            gameID: gameID ?? self.gameID,
            hasPendingDoubleOffer: hasPendingDoubleOffer ?? self.hasPendingDoubleOffer,
            hasReceivedDoubleOffer: hasReceivedDoubleOffer ?? self.hasReceivedDoubleOffer,
            hasValidMoves: hasValidMoves ?? self.hasValidMoves,
            isAnalysisMode: isAnalysisMode ?? self.isAnalysisMode,
            isAwaitingDoubleResponse: isAwaitingDoubleResponse ?? self.isAwaitingDoubleResponse,
            isCorrespondence: isCorrespondence ?? self.isCorrespondence,
            isCrawfordGame: isCrawfordGame ?? self.isCrawfordGame,
            isOpeningRoll: isOpeningRoll ?? self.isOpeningRoll,
            isOpeningRollTie: isOpeningRollTie ?? self.isOpeningRollTie,
            isRated: isRated ?? self.isRated,
            isYourTurn: isYourTurn ?? self.isYourTurn,
            leaveGameAction: leaveGameAction ?? self.leaveGameAction,
            matchID: matchID ?? self.matchID,
            movesMadeThisTurn: movesMadeThisTurn ?? self.movesMadeThisTurn,
            pendingDoubleNewValue: pendingDoubleNewValue ?? self.pendingDoubleNewValue,
            player1Score: player1Score ?? self.player1Score,
            player2Score: player2Score ?? self.player2Score,
            redBornOff: redBornOff ?? self.redBornOff,
            redCheckersOnBar: redCheckersOnBar ?? self.redCheckersOnBar,
            redDelayRemaining: redDelayRemaining ?? self.redDelayRemaining,
            redIsInDelay: redIsInDelay ?? self.redIsInDelay,
            redOpeningRoll: redOpeningRoll ?? self.redOpeningRoll,
            redPipCount: redPipCount ?? self.redPipCount,
            redPlayerID: redPlayerID ?? self.redPlayerID,
            redPlayerName: redPlayerName ?? self.redPlayerName,
            redRating: redRating ?? self.redRating,
            redRatingChange: redRatingChange ?? self.redRatingChange,
            redReserveSeconds: redReserveSeconds ?? self.redReserveSeconds,
            redUsername: redUsername ?? self.redUsername,
            remainingMoves: remainingMoves ?? self.remainingMoves,
            status: status ?? self.status,
            targetScore: targetScore ?? self.targetScore,
            timeControlType: timeControlType ?? self.timeControlType,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays,
            turnDeadline: turnDeadline ?? self.turnDeadline,
            turnHistory: turnHistory ?? self.turnHistory,
            validMoves: validMoves ?? self.validMoves,
            whiteBornOff: whiteBornOff ?? self.whiteBornOff,
            whiteCheckersOnBar: whiteCheckersOnBar ?? self.whiteCheckersOnBar,
            whiteDelayRemaining: whiteDelayRemaining ?? self.whiteDelayRemaining,
            whiteIsInDelay: whiteIsInDelay ?? self.whiteIsInDelay,
            whiteOpeningRoll: whiteOpeningRoll ?? self.whiteOpeningRoll,
            whitePipCount: whitePipCount ?? self.whitePipCount,
            whitePlayerID: whitePlayerID ?? self.whitePlayerID,
            whitePlayerName: whitePlayerName ?? self.whitePlayerName,
            whiteRating: whiteRating ?? self.whiteRating,
            whiteRatingChange: whiteRatingChange ?? self.whiteRatingChange,
            whiteReserveSeconds: whiteReserveSeconds ?? self.whiteReserveSeconds,
            whiteUsername: whiteUsername ?? self.whiteUsername,
            winner: winner ?? self.winner,
            winType: winType ?? self.winType,
            yourColor: yourColor ?? self.yourColor
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.PointState
// MARK: - StateBoard
public struct StateBoard: Codable {
    /// Transpiled from Backgammon.Core.CheckerColor
    public let color: Double?
    /// Transpiled from int
    public let count: Double
    /// Transpiled from int
    public let position: Double

    public init(color: Double?, count: Double, position: Double) {
        self.color = color
        self.count = count
        self.position = position
    }
}

// MARK: StateBoard convenience initializers and mutators

public extension StateBoard {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(StateBoard.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        color: Double?? = nil,
        count: Double? = nil,
        position: Double? = nil
    ) -> StateBoard {
        return StateBoard(
            color: color ?? self.color,
            count: count ?? self.count,
            position: position ?? self.position
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.TurnSnapshotDto
// MARK: - StateTurnHistory
public struct StateTurnHistory: Codable {
    /// Transpiled from string?
    public let cubeOwner: String?
    /// Transpiled from int
    public let cubeValue: Double
    /// Transpiled from int[]
    public let diceRolled: [Double]
    /// Transpiled from string?
    public let doublingAction: String?
    /// Transpiled from System.Collections.Generic.List<string>
    public let moves: [String]
    /// Transpiled from string
    public let player: String
    /// Transpiled from string
    public let positionSgf: String
    /// Transpiled from int
    public let turnNumber: Double

    public init(cubeOwner: String?, cubeValue: Double, diceRolled: [Double], doublingAction: String?, moves: [String], player: String, positionSgf: String, turnNumber: Double) {
        self.cubeOwner = cubeOwner
        self.cubeValue = cubeValue
        self.diceRolled = diceRolled
        self.doublingAction = doublingAction
        self.moves = moves
        self.player = player
        self.positionSgf = positionSgf
        self.turnNumber = turnNumber
    }
}

// MARK: StateTurnHistory convenience initializers and mutators

public extension StateTurnHistory {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(StateTurnHistory.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        cubeOwner: String?? = nil,
        cubeValue: Double? = nil,
        diceRolled: [Double]? = nil,
        doublingAction: String?? = nil,
        moves: [String]? = nil,
        player: String? = nil,
        positionSgf: String? = nil,
        turnNumber: Double? = nil
    ) -> StateTurnHistory {
        return StateTurnHistory(
            cubeOwner: cubeOwner ?? self.cubeOwner,
            cubeValue: cubeValue ?? self.cubeValue,
            diceRolled: diceRolled ?? self.diceRolled,
            doublingAction: doublingAction ?? self.doublingAction,
            moves: moves ?? self.moves,
            player: player ?? self.player,
            positionSgf: positionSgf ?? self.positionSgf,
            turnNumber: turnNumber ?? self.turnNumber
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.MoveDto
// MARK: - StateValidMove
public struct StateValidMove: Codable {
    /// Transpiled from int[]?
    public let diceUsed: [Double]?
    /// Transpiled from int
    public let dieValue: Double
    /// Transpiled from int
    public let from: Double
    /// Transpiled from int[]?
    public let intermediatePoints: [Double]?
    /// Transpiled from bool
    public let isCombinedMove: Bool
    /// Transpiled from bool
    public let isHit: Bool
    /// Transpiled from int
    public let to: Double

    public init(diceUsed: [Double]?, dieValue: Double, from: Double, intermediatePoints: [Double]?, isCombinedMove: Bool, isHit: Bool, to: Double) {
        self.diceUsed = diceUsed
        self.dieValue = dieValue
        self.from = from
        self.intermediatePoints = intermediatePoints
        self.isCombinedMove = isCombinedMove
        self.isHit = isHit
        self.to = to
    }
}

// MARK: StateValidMove convenience initializers and mutators

public extension StateValidMove {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(StateValidMove.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        diceUsed: [Double]?? = nil,
        dieValue: Double? = nil,
        from: Double? = nil,
        intermediatePoints: [Double]?? = nil,
        isCombinedMove: Bool? = nil,
        isHit: Bool? = nil,
        to: Double? = nil
    ) -> StateValidMove {
        return StateValidMove(
            diceUsed: diceUsed ?? self.diceUsed,
            dieValue: dieValue ?? self.dieValue,
            from: from ?? self.from,
            intermediatePoints: intermediatePoints ?? self.intermediatePoints,
            isCombinedMove: isCombinedMove ?? self.isCombinedMove,
            isHit: isHit ?? self.isHit,
            to: to ?? self.to
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchInviteDto
// MARK: - MatchInviteDto
public struct MatchInviteDto: Codable {
    /// Transpiled from string
    public let challengerID: String
    /// Transpiled from string
    public let challengerName: String
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from int
    public let targetScore: Double

    public enum CodingKeys: String, CodingKey {
        case challengerID = "challengerId"
        case challengerName
        case gameID = "gameId"
        case matchID = "matchId"
        case targetScore
    }

    public init(challengerID: String, challengerName: String, gameID: String, matchID: String, targetScore: Double) {
        self.challengerID = challengerID
        self.challengerName = challengerName
        self.gameID = gameID
        self.matchID = matchID
        self.targetScore = targetScore
    }
}

// MARK: MatchInviteDto convenience initializers and mutators

public extension MatchInviteDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchInviteDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        challengerID: String? = nil,
        challengerName: String? = nil,
        gameID: String? = nil,
        matchID: String? = nil,
        targetScore: Double? = nil
    ) -> MatchInviteDto {
        return MatchInviteDto(
            challengerID: challengerID ?? self.challengerID,
            challengerName: challengerName ?? self.challengerName,
            gameID: gameID ?? self.gameID,
            matchID: matchID ?? self.matchID,
            targetScore: targetScore ?? self.targetScore
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchLobbyDto
// MARK: - MatchLobbyDto
public struct MatchLobbyDto: Codable {
    /// Transpiled from string
    public let createdAt: String
    /// Transpiled from string
    public let creatorPlayerID: String
    /// Transpiled from string
    public let creatorUsername: String
    /// Transpiled from bool
    public let isCorrespondence: Bool
    /// Transpiled from bool
    public let isOpenLobby: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string?
    public let opponentPlayerID: String?
    /// Transpiled from string
    public let opponentType: String
    /// Transpiled from string?
    public let opponentUsername: String?
    /// Transpiled from string
    public let status: String
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let timePerMoveDays: Double?

    public enum CodingKeys: String, CodingKey {
        case createdAt
        case creatorPlayerID = "creatorPlayerId"
        case creatorUsername, isCorrespondence, isOpenLobby
        case matchID = "matchId"
        case opponentPlayerID = "opponentPlayerId"
        case opponentType, opponentUsername, status, targetScore, timePerMoveDays
    }

    public init(createdAt: String, creatorPlayerID: String, creatorUsername: String, isCorrespondence: Bool, isOpenLobby: Bool, matchID: String, opponentPlayerID: String?, opponentType: String, opponentUsername: String?, status: String, targetScore: Double, timePerMoveDays: Double?) {
        self.createdAt = createdAt
        self.creatorPlayerID = creatorPlayerID
        self.creatorUsername = creatorUsername
        self.isCorrespondence = isCorrespondence
        self.isOpenLobby = isOpenLobby
        self.matchID = matchID
        self.opponentPlayerID = opponentPlayerID
        self.opponentType = opponentType
        self.opponentUsername = opponentUsername
        self.status = status
        self.targetScore = targetScore
        self.timePerMoveDays = timePerMoveDays
    }
}

// MARK: MatchLobbyDto convenience initializers and mutators

public extension MatchLobbyDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchLobbyDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        createdAt: String? = nil,
        creatorPlayerID: String? = nil,
        creatorUsername: String? = nil,
        isCorrespondence: Bool? = nil,
        isOpenLobby: Bool? = nil,
        matchID: String? = nil,
        opponentPlayerID: String?? = nil,
        opponentType: String? = nil,
        opponentUsername: String?? = nil,
        status: String? = nil,
        targetScore: Double? = nil,
        timePerMoveDays: Double?? = nil
    ) -> MatchLobbyDto {
        return MatchLobbyDto(
            createdAt: createdAt ?? self.createdAt,
            creatorPlayerID: creatorPlayerID ?? self.creatorPlayerID,
            creatorUsername: creatorUsername ?? self.creatorUsername,
            isCorrespondence: isCorrespondence ?? self.isCorrespondence,
            isOpenLobby: isOpenLobby ?? self.isOpenLobby,
            matchID: matchID ?? self.matchID,
            opponentPlayerID: opponentPlayerID ?? self.opponentPlayerID,
            opponentType: opponentType ?? self.opponentType,
            opponentUsername: opponentUsername ?? self.opponentUsername,
            status: status ?? self.status,
            targetScore: targetScore ?? self.targetScore,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchResultsDto
// MARK: - MatchResultsDto
public struct MatchResultsDto: Codable {
    /// Transpiled from System.DateTime
    public let completedAt: String?
    /// Transpiled from string
    public let duration: String
    /// Transpiled from Backgammon.Server.Models.SignalR.MatchScoreDto
    public let finalScore: MatchResultsDtoFinalScore
    /// Transpiled from
    /// System.Collections.Generic.List<Backgammon.Server.Models.SignalR.MatchGameDto>
    public let games: [Game]
    /// Transpiled from string?
    public let loserUsername: String?
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let totalGames: Double
    /// Transpiled from string?
    public let winnerUserID: String?
    /// Transpiled from string?
    public let winnerUsername: String?

    public enum CodingKeys: String, CodingKey {
        case completedAt, duration, finalScore, games, loserUsername
        case matchID = "matchId"
        case targetScore, totalGames
        case winnerUserID = "winnerUserId"
        case winnerUsername
    }

    public init(completedAt: String?, duration: String, finalScore: MatchResultsDtoFinalScore, games: [Game], loserUsername: String?, matchID: String, targetScore: Double, totalGames: Double, winnerUserID: String?, winnerUsername: String?) {
        self.completedAt = completedAt
        self.duration = duration
        self.finalScore = finalScore
        self.games = games
        self.loserUsername = loserUsername
        self.matchID = matchID
        self.targetScore = targetScore
        self.totalGames = totalGames
        self.winnerUserID = winnerUserID
        self.winnerUsername = winnerUsername
    }
}

// MARK: MatchResultsDto convenience initializers and mutators

public extension MatchResultsDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchResultsDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        completedAt: String?? = nil,
        duration: String? = nil,
        finalScore: MatchResultsDtoFinalScore? = nil,
        games: [Game]? = nil,
        loserUsername: String?? = nil,
        matchID: String? = nil,
        targetScore: Double? = nil,
        totalGames: Double? = nil,
        winnerUserID: String?? = nil,
        winnerUsername: String?? = nil
    ) -> MatchResultsDto {
        return MatchResultsDto(
            completedAt: completedAt ?? self.completedAt,
            duration: duration ?? self.duration,
            finalScore: finalScore ?? self.finalScore,
            games: games ?? self.games,
            loserUsername: loserUsername ?? self.loserUsername,
            matchID: matchID ?? self.matchID,
            targetScore: targetScore ?? self.targetScore,
            totalGames: totalGames ?? self.totalGames,
            winnerUserID: winnerUserID ?? self.winnerUserID,
            winnerUsername: winnerUsername ?? self.winnerUsername
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchScoreDto
// MARK: - MatchResultsDtoFinalScore
public struct MatchResultsDtoFinalScore: Codable {
    /// Transpiled from int
    public let player1: Double
    /// Transpiled from int
    public let player2: Double

    public init(player1: Double, player2: Double) {
        self.player1 = player1
        self.player2 = player2
    }
}

// MARK: MatchResultsDtoFinalScore convenience initializers and mutators

public extension MatchResultsDtoFinalScore {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchResultsDtoFinalScore.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        player1: Double? = nil,
        player2: Double? = nil
    ) -> MatchResultsDtoFinalScore {
        return MatchResultsDtoFinalScore(
            player1: player1 ?? self.player1,
            player2: player2 ?? self.player2
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchGameDto
// MARK: - Game
public struct Game: Codable {
    /// Transpiled from System.DateTime
    public let completedAt: String?
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from int
    public let gameNumber: Double
    /// Transpiled from bool
    public let isCrawford: Bool
    /// Transpiled from int
    public let pointsScored: Double
    /// Transpiled from string
    public let status: String
    /// Transpiled from string?
    public let winner: String?
    /// Transpiled from string?
    public let winType: String?

    public enum CodingKeys: String, CodingKey {
        case completedAt
        case gameID = "gameId"
        case gameNumber, isCrawford, pointsScored, status, winner, winType
    }

    public init(completedAt: String?, gameID: String, gameNumber: Double, isCrawford: Bool, pointsScored: Double, status: String, winner: String?, winType: String?) {
        self.completedAt = completedAt
        self.gameID = gameID
        self.gameNumber = gameNumber
        self.isCrawford = isCrawford
        self.pointsScored = pointsScored
        self.status = status
        self.winner = winner
        self.winType = winType
    }
}

// MARK: Game convenience initializers and mutators

public extension Game {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(Game.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        completedAt: String?? = nil,
        gameID: String? = nil,
        gameNumber: Double? = nil,
        isCrawford: Bool? = nil,
        pointsScored: Double? = nil,
        status: String? = nil,
        winner: String?? = nil,
        winType: String?? = nil
    ) -> Game {
        return Game(
            completedAt: completedAt ?? self.completedAt,
            gameID: gameID ?? self.gameID,
            gameNumber: gameNumber ?? self.gameNumber,
            isCrawford: isCrawford ?? self.isCrawford,
            pointsScored: pointsScored ?? self.pointsScored,
            status: status ?? self.status,
            winner: winner ?? self.winner,
            winType: winType ?? self.winType
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchScoreDto
// MARK: - MatchScoreDto
public struct MatchScoreDto: Codable {
    /// Transpiled from int
    public let player1: Double
    /// Transpiled from int
    public let player2: Double

    public init(player1: Double, player2: Double) {
        self.player1 = player1
        self.player2 = player2
    }
}

// MARK: MatchScoreDto convenience initializers and mutators

public extension MatchScoreDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchScoreDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        player1: Double? = nil,
        player2: Double? = nil
    ) -> MatchScoreDto {
        return MatchScoreDto(
            player1: player1 ?? self.player1,
            player2: player2 ?? self.player2
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchStatusDto
// MARK: - MatchStatusDto
public struct MatchStatusDto: Codable {
    /// Transpiled from string?
    public let currentGameID: String?
    /// Transpiled from bool
    public let hasCrawfordGameBeenPlayed: Bool
    /// Transpiled from bool
    public let isCrawfordGame: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string
    public let player1Name: String
    /// Transpiled from int
    public let player1Score: Double
    /// Transpiled from string
    public let player2Name: String
    /// Transpiled from int
    public let player2Score: Double
    /// Transpiled from string
    public let status: String
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let totalGames: Double
    /// Transpiled from string?
    public let winnerID: String?

    public enum CodingKeys: String, CodingKey {
        case currentGameID = "currentGameId"
        case hasCrawfordGameBeenPlayed, isCrawfordGame
        case matchID = "matchId"
        case player1Name, player1Score, player2Name, player2Score, status, targetScore, totalGames
        case winnerID = "winnerId"
    }

    public init(currentGameID: String?, hasCrawfordGameBeenPlayed: Bool, isCrawfordGame: Bool, matchID: String, player1Name: String, player1Score: Double, player2Name: String, player2Score: Double, status: String, targetScore: Double, totalGames: Double, winnerID: String?) {
        self.currentGameID = currentGameID
        self.hasCrawfordGameBeenPlayed = hasCrawfordGameBeenPlayed
        self.isCrawfordGame = isCrawfordGame
        self.matchID = matchID
        self.player1Name = player1Name
        self.player1Score = player1Score
        self.player2Name = player2Name
        self.player2Score = player2Score
        self.status = status
        self.targetScore = targetScore
        self.totalGames = totalGames
        self.winnerID = winnerID
    }
}

// MARK: MatchStatusDto convenience initializers and mutators

public extension MatchStatusDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchStatusDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        currentGameID: String?? = nil,
        hasCrawfordGameBeenPlayed: Bool? = nil,
        isCrawfordGame: Bool? = nil,
        matchID: String? = nil,
        player1Name: String? = nil,
        player1Score: Double? = nil,
        player2Name: String? = nil,
        player2Score: Double? = nil,
        status: String? = nil,
        targetScore: Double? = nil,
        totalGames: Double? = nil,
        winnerID: String?? = nil
    ) -> MatchStatusDto {
        return MatchStatusDto(
            currentGameID: currentGameID ?? self.currentGameID,
            hasCrawfordGameBeenPlayed: hasCrawfordGameBeenPlayed ?? self.hasCrawfordGameBeenPlayed,
            isCrawfordGame: isCrawfordGame ?? self.isCrawfordGame,
            matchID: matchID ?? self.matchID,
            player1Name: player1Name ?? self.player1Name,
            player1Score: player1Score ?? self.player1Score,
            player2Name: player2Name ?? self.player2Name,
            player2Score: player2Score ?? self.player2Score,
            status: status ?? self.status,
            targetScore: targetScore ?? self.targetScore,
            totalGames: totalGames ?? self.totalGames,
            winnerID: winnerID ?? self.winnerID
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchSummaryDto
// MARK: - MatchSummaryDto
public struct MatchSummaryDto: Codable {
    /// Transpiled from System.DateTime
    public let createdAt: String
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from int
    public let myScore: Double
    /// Transpiled from string?
    public let opponentID: String?
    /// Transpiled from string?
    public let opponentName: String?
    /// Transpiled from int
    public let opponentScore: Double
    /// Transpiled from string
    public let status: String
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let totalGames: Double

    public enum CodingKeys: String, CodingKey {
        case createdAt
        case matchID = "matchId"
        case myScore
        case opponentID = "opponentId"
        case opponentName, opponentScore, status, targetScore, totalGames
    }

    public init(createdAt: String, matchID: String, myScore: Double, opponentID: String?, opponentName: String?, opponentScore: Double, status: String, targetScore: Double, totalGames: Double) {
        self.createdAt = createdAt
        self.matchID = matchID
        self.myScore = myScore
        self.opponentID = opponentID
        self.opponentName = opponentName
        self.opponentScore = opponentScore
        self.status = status
        self.targetScore = targetScore
        self.totalGames = totalGames
    }
}

// MARK: MatchSummaryDto convenience initializers and mutators

public extension MatchSummaryDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchSummaryDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        createdAt: String? = nil,
        matchID: String? = nil,
        myScore: Double? = nil,
        opponentID: String?? = nil,
        opponentName: String?? = nil,
        opponentScore: Double? = nil,
        status: String? = nil,
        targetScore: Double? = nil,
        totalGames: Double? = nil
    ) -> MatchSummaryDto {
        return MatchSummaryDto(
            createdAt: createdAt ?? self.createdAt,
            matchID: matchID ?? self.matchID,
            myScore: myScore ?? self.myScore,
            opponentID: opponentID ?? self.opponentID,
            opponentName: opponentName ?? self.opponentName,
            opponentScore: opponentScore ?? self.opponentScore,
            status: status ?? self.status,
            targetScore: targetScore ?? self.targetScore,
            totalGames: totalGames ?? self.totalGames
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.MatchUpdateDto
// MARK: - MatchUpdateDto
public struct MatchUpdateDto: Codable {
    /// Transpiled from bool
    public let isCrawfordGame: Bool
    /// Transpiled from bool
    public let matchComplete: Bool
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string?
    public let matchWinner: String?
    /// Transpiled from string?
    public let nextGameID: String?
    /// Transpiled from int
    public let player1Score: Double
    /// Transpiled from int
    public let player2Score: Double
    /// Transpiled from int
    public let targetScore: Double

    public enum CodingKeys: String, CodingKey {
        case isCrawfordGame, matchComplete
        case matchID = "matchId"
        case matchWinner
        case nextGameID = "nextGameId"
        case player1Score, player2Score, targetScore
    }

    public init(isCrawfordGame: Bool, matchComplete: Bool, matchID: String, matchWinner: String?, nextGameID: String?, player1Score: Double, player2Score: Double, targetScore: Double) {
        self.isCrawfordGame = isCrawfordGame
        self.matchComplete = matchComplete
        self.matchID = matchID
        self.matchWinner = matchWinner
        self.nextGameID = nextGameID
        self.player1Score = player1Score
        self.player2Score = player2Score
        self.targetScore = targetScore
    }
}

// MARK: MatchUpdateDto convenience initializers and mutators

public extension MatchUpdateDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MatchUpdateDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        isCrawfordGame: Bool? = nil,
        matchComplete: Bool? = nil,
        matchID: String? = nil,
        matchWinner: String?? = nil,
        nextGameID: String?? = nil,
        player1Score: Double? = nil,
        player2Score: Double? = nil,
        targetScore: Double? = nil
    ) -> MatchUpdateDto {
        return MatchUpdateDto(
            isCrawfordGame: isCrawfordGame ?? self.isCrawfordGame,
            matchComplete: matchComplete ?? self.matchComplete,
            matchID: matchID ?? self.matchID,
            matchWinner: matchWinner ?? self.matchWinner,
            nextGameID: nextGameID ?? self.nextGameID,
            player1Score: player1Score ?? self.player1Score,
            player2Score: player2Score ?? self.player2Score,
            targetScore: targetScore ?? self.targetScore
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.OpponentJoinedMatchDto
// MARK: - OpponentJoinedMatchDto
public struct OpponentJoinedMatchDto: Codable {
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string
    public let player2ID: String
    /// Transpiled from string
    public let player2Name: String

    public enum CodingKeys: String, CodingKey {
        case matchID = "matchId"
        case player2ID = "player2Id"
        case player2Name
    }

    public init(matchID: String, player2ID: String, player2Name: String) {
        self.matchID = matchID
        self.player2ID = player2ID
        self.player2Name = player2Name
    }
}

// MARK: OpponentJoinedMatchDto convenience initializers and mutators

public extension OpponentJoinedMatchDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(OpponentJoinedMatchDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        matchID: String? = nil,
        player2ID: String? = nil,
        player2Name: String? = nil
    ) -> OpponentJoinedMatchDto {
        return OpponentJoinedMatchDto(
            matchID: matchID ?? self.matchID,
            player2ID: player2ID ?? self.player2ID,
            player2Name: player2Name ?? self.player2Name
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.PlayerTimedOutDto
// MARK: - PlayerTimedOutDto
public struct PlayerTimedOutDto: Codable {
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from string
    public let timedOutPlayer: String
    /// Transpiled from string
    public let winner: String

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case timedOutPlayer, winner
    }

    public init(gameID: String, timedOutPlayer: String, winner: String) {
        self.gameID = gameID
        self.timedOutPlayer = timedOutPlayer
        self.winner = winner
    }
}

// MARK: PlayerTimedOutDto convenience initializers and mutators

public extension PlayerTimedOutDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(PlayerTimedOutDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String? = nil,
        timedOutPlayer: String? = nil,
        winner: String? = nil
    ) -> PlayerTimedOutDto {
        return PlayerTimedOutDto(
            gameID: gameID ?? self.gameID,
            timedOutPlayer: timedOutPlayer ?? self.timedOutPlayer,
            winner: winner ?? self.winner
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.RecentGameDto
// MARK: - RecentGameDto
public struct RecentGameDto: Codable {
    /// Transpiled from System.DateTime
    public let completedAt: String?
    /// Transpiled from System.DateTime
    public let createdAt: String
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string
    public let matchLength: String
    /// Transpiled from string
    public let matchScore: String
    /// Transpiled from int
    public let myScore: Double
    /// Transpiled from string?
    public let opponentID: String?
    /// Transpiled from string
    public let opponentName: String
    /// Transpiled from int
    public let opponentRating: Double
    /// Transpiled from int
    public let opponentScore: Double
    /// Transpiled from int
    public let ratingChange: Double
    /// Transpiled from string
    public let result: String
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from string
    public let timeControl: String

    public enum CodingKeys: String, CodingKey {
        case completedAt, createdAt
        case matchID = "matchId"
        case matchLength, matchScore, myScore
        case opponentID = "opponentId"
        case opponentName, opponentRating, opponentScore, ratingChange, result, targetScore, timeControl
    }

    public init(completedAt: String?, createdAt: String, matchID: String, matchLength: String, matchScore: String, myScore: Double, opponentID: String?, opponentName: String, opponentRating: Double, opponentScore: Double, ratingChange: Double, result: String, targetScore: Double, timeControl: String) {
        self.completedAt = completedAt
        self.createdAt = createdAt
        self.matchID = matchID
        self.matchLength = matchLength
        self.matchScore = matchScore
        self.myScore = myScore
        self.opponentID = opponentID
        self.opponentName = opponentName
        self.opponentRating = opponentRating
        self.opponentScore = opponentScore
        self.ratingChange = ratingChange
        self.result = result
        self.targetScore = targetScore
        self.timeControl = timeControl
    }
}

// MARK: RecentGameDto convenience initializers and mutators

public extension RecentGameDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(RecentGameDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        completedAt: String?? = nil,
        createdAt: String? = nil,
        matchID: String? = nil,
        matchLength: String? = nil,
        matchScore: String? = nil,
        myScore: Double? = nil,
        opponentID: String?? = nil,
        opponentName: String? = nil,
        opponentRating: Double? = nil,
        opponentScore: Double? = nil,
        ratingChange: Double? = nil,
        result: String? = nil,
        targetScore: Double? = nil,
        timeControl: String? = nil
    ) -> RecentGameDto {
        return RecentGameDto(
            completedAt: completedAt ?? self.completedAt,
            createdAt: createdAt ?? self.createdAt,
            matchID: matchID ?? self.matchID,
            matchLength: matchLength ?? self.matchLength,
            matchScore: matchScore ?? self.matchScore,
            myScore: myScore ?? self.myScore,
            opponentID: opponentID ?? self.opponentID,
            opponentName: opponentName ?? self.opponentName,
            opponentRating: opponentRating ?? self.opponentRating,
            opponentScore: opponentScore ?? self.opponentScore,
            ratingChange: ratingChange ?? self.ratingChange,
            result: result ?? self.result,
            targetScore: targetScore ?? self.targetScore,
            timeControl: timeControl ?? self.timeControl
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Models.SignalR.TimeUpdateDto
// MARK: - TimeUpdateDto
public struct TimeUpdateDto: Codable {
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from double
    public let redDelayRemaining: Double
    /// Transpiled from bool
    public let redIsInDelay: Bool
    /// Transpiled from double
    public let redReserveSeconds: Double
    /// Transpiled from double
    public let whiteDelayRemaining: Double
    /// Transpiled from bool
    public let whiteIsInDelay: Bool
    /// Transpiled from double
    public let whiteReserveSeconds: Double

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case redDelayRemaining, redIsInDelay, redReserveSeconds, whiteDelayRemaining, whiteIsInDelay, whiteReserveSeconds
    }

    public init(gameID: String, redDelayRemaining: Double, redIsInDelay: Bool, redReserveSeconds: Double, whiteDelayRemaining: Double, whiteIsInDelay: Bool, whiteReserveSeconds: Double) {
        self.gameID = gameID
        self.redDelayRemaining = redDelayRemaining
        self.redIsInDelay = redIsInDelay
        self.redReserveSeconds = redReserveSeconds
        self.whiteDelayRemaining = whiteDelayRemaining
        self.whiteIsInDelay = whiteIsInDelay
        self.whiteReserveSeconds = whiteReserveSeconds
    }
}

// MARK: TimeUpdateDto convenience initializers and mutators

public extension TimeUpdateDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(TimeUpdateDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String? = nil,
        redDelayRemaining: Double? = nil,
        redIsInDelay: Bool? = nil,
        redReserveSeconds: Double? = nil,
        whiteDelayRemaining: Double? = nil,
        whiteIsInDelay: Bool? = nil,
        whiteReserveSeconds: Double? = nil
    ) -> TimeUpdateDto {
        return TimeUpdateDto(
            gameID: gameID ?? self.gameID,
            redDelayRemaining: redDelayRemaining ?? self.redDelayRemaining,
            redIsInDelay: redIsInDelay ?? self.redIsInDelay,
            redReserveSeconds: redReserveSeconds ?? self.redReserveSeconds,
            whiteDelayRemaining: whiteDelayRemaining ?? self.whiteDelayRemaining,
            whiteIsInDelay: whiteIsInDelay ?? self.whiteIsInDelay,
            whiteReserveSeconds: whiteReserveSeconds ?? self.whiteReserveSeconds
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Services.CorrespondenceGameDto
// MARK: - CorrespondenceGameDto
public struct CorrespondenceGameDto: Codable {
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let isRated: Bool
    /// Transpiled from bool
    public let isYourTurn: Bool
    /// Transpiled from System.DateTime
    public let lastUpdatedAt: String
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string
    public let matchScore: String
    /// Transpiled from int
    public let moveCount: Double
    /// Transpiled from string
    public let opponentID: String
    /// Transpiled from string
    public let opponentName: String
    /// Transpiled from int
    public let opponentRating: Double
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let timePerMoveDays: Double
    /// Transpiled from string?
    public let timeRemaining: String?
    /// Transpiled from System.DateTime
    public let turnDeadline: String?

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case isRated, isYourTurn, lastUpdatedAt
        case matchID = "matchId"
        case matchScore, moveCount
        case opponentID = "opponentId"
        case opponentName, opponentRating, targetScore, timePerMoveDays, timeRemaining, turnDeadline
    }

    public init(gameID: String, isRated: Bool, isYourTurn: Bool, lastUpdatedAt: String, matchID: String, matchScore: String, moveCount: Double, opponentID: String, opponentName: String, opponentRating: Double, targetScore: Double, timePerMoveDays: Double, timeRemaining: String?, turnDeadline: String?) {
        self.gameID = gameID
        self.isRated = isRated
        self.isYourTurn = isYourTurn
        self.lastUpdatedAt = lastUpdatedAt
        self.matchID = matchID
        self.matchScore = matchScore
        self.moveCount = moveCount
        self.opponentID = opponentID
        self.opponentName = opponentName
        self.opponentRating = opponentRating
        self.targetScore = targetScore
        self.timePerMoveDays = timePerMoveDays
        self.timeRemaining = timeRemaining
        self.turnDeadline = turnDeadline
    }
}

// MARK: CorrespondenceGameDto convenience initializers and mutators

public extension CorrespondenceGameDto {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(CorrespondenceGameDto.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String? = nil,
        isRated: Bool? = nil,
        isYourTurn: Bool? = nil,
        lastUpdatedAt: String? = nil,
        matchID: String? = nil,
        matchScore: String? = nil,
        moveCount: Double? = nil,
        opponentID: String? = nil,
        opponentName: String? = nil,
        opponentRating: Double? = nil,
        targetScore: Double? = nil,
        timePerMoveDays: Double? = nil,
        timeRemaining: String?? = nil,
        turnDeadline: String?? = nil
    ) -> CorrespondenceGameDto {
        return CorrespondenceGameDto(
            gameID: gameID ?? self.gameID,
            isRated: isRated ?? self.isRated,
            isYourTurn: isYourTurn ?? self.isYourTurn,
            lastUpdatedAt: lastUpdatedAt ?? self.lastUpdatedAt,
            matchID: matchID ?? self.matchID,
            matchScore: matchScore ?? self.matchScore,
            moveCount: moveCount ?? self.moveCount,
            opponentID: opponentID ?? self.opponentID,
            opponentName: opponentName ?? self.opponentName,
            opponentRating: opponentRating ?? self.opponentRating,
            targetScore: targetScore ?? self.targetScore,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays,
            timeRemaining: timeRemaining ?? self.timeRemaining,
            turnDeadline: turnDeadline ?? self.turnDeadline
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Services.CorrespondenceGamesResponse
// MARK: - CorrespondenceGamesResponse
public struct CorrespondenceGamesResponse: Codable {
    /// Transpiled from
    /// System.Collections.Generic.List<Backgammon.Server.Services.CorrespondenceGameDto>
    public let myLobbies: [MyLobby]
    /// Transpiled from int
    public let totalMyLobbies: Double
    /// Transpiled from int
    public let totalWaiting: Double
    /// Transpiled from int
    public let totalYourTurn: Double
    /// Transpiled from
    /// System.Collections.Generic.List<Backgammon.Server.Services.CorrespondenceGameDto>
    public let waitingGames: [WaitingGame]
    /// Transpiled from
    /// System.Collections.Generic.List<Backgammon.Server.Services.CorrespondenceGameDto>
    public let yourTurnGames: [YourTurnGame]

    public init(myLobbies: [MyLobby], totalMyLobbies: Double, totalWaiting: Double, totalYourTurn: Double, waitingGames: [WaitingGame], yourTurnGames: [YourTurnGame]) {
        self.myLobbies = myLobbies
        self.totalMyLobbies = totalMyLobbies
        self.totalWaiting = totalWaiting
        self.totalYourTurn = totalYourTurn
        self.waitingGames = waitingGames
        self.yourTurnGames = yourTurnGames
    }
}

// MARK: CorrespondenceGamesResponse convenience initializers and mutators

public extension CorrespondenceGamesResponse {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(CorrespondenceGamesResponse.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        myLobbies: [MyLobby]? = nil,
        totalMyLobbies: Double? = nil,
        totalWaiting: Double? = nil,
        totalYourTurn: Double? = nil,
        waitingGames: [WaitingGame]? = nil,
        yourTurnGames: [YourTurnGame]? = nil
    ) -> CorrespondenceGamesResponse {
        return CorrespondenceGamesResponse(
            myLobbies: myLobbies ?? self.myLobbies,
            totalMyLobbies: totalMyLobbies ?? self.totalMyLobbies,
            totalWaiting: totalWaiting ?? self.totalWaiting,
            totalYourTurn: totalYourTurn ?? self.totalYourTurn,
            waitingGames: waitingGames ?? self.waitingGames,
            yourTurnGames: yourTurnGames ?? self.yourTurnGames
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Services.CorrespondenceGameDto
// MARK: - MyLobby
public struct MyLobby: Codable {
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let isRated: Bool
    /// Transpiled from bool
    public let isYourTurn: Bool
    /// Transpiled from System.DateTime
    public let lastUpdatedAt: String
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string
    public let matchScore: String
    /// Transpiled from int
    public let moveCount: Double
    /// Transpiled from string
    public let opponentID: String
    /// Transpiled from string
    public let opponentName: String
    /// Transpiled from int
    public let opponentRating: Double
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let timePerMoveDays: Double
    /// Transpiled from string?
    public let timeRemaining: String?
    /// Transpiled from System.DateTime
    public let turnDeadline: String?

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case isRated, isYourTurn, lastUpdatedAt
        case matchID = "matchId"
        case matchScore, moveCount
        case opponentID = "opponentId"
        case opponentName, opponentRating, targetScore, timePerMoveDays, timeRemaining, turnDeadline
    }

    public init(gameID: String, isRated: Bool, isYourTurn: Bool, lastUpdatedAt: String, matchID: String, matchScore: String, moveCount: Double, opponentID: String, opponentName: String, opponentRating: Double, targetScore: Double, timePerMoveDays: Double, timeRemaining: String?, turnDeadline: String?) {
        self.gameID = gameID
        self.isRated = isRated
        self.isYourTurn = isYourTurn
        self.lastUpdatedAt = lastUpdatedAt
        self.matchID = matchID
        self.matchScore = matchScore
        self.moveCount = moveCount
        self.opponentID = opponentID
        self.opponentName = opponentName
        self.opponentRating = opponentRating
        self.targetScore = targetScore
        self.timePerMoveDays = timePerMoveDays
        self.timeRemaining = timeRemaining
        self.turnDeadline = turnDeadline
    }
}

// MARK: MyLobby convenience initializers and mutators

public extension MyLobby {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(MyLobby.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String? = nil,
        isRated: Bool? = nil,
        isYourTurn: Bool? = nil,
        lastUpdatedAt: String? = nil,
        matchID: String? = nil,
        matchScore: String? = nil,
        moveCount: Double? = nil,
        opponentID: String? = nil,
        opponentName: String? = nil,
        opponentRating: Double? = nil,
        targetScore: Double? = nil,
        timePerMoveDays: Double? = nil,
        timeRemaining: String?? = nil,
        turnDeadline: String?? = nil
    ) -> MyLobby {
        return MyLobby(
            gameID: gameID ?? self.gameID,
            isRated: isRated ?? self.isRated,
            isYourTurn: isYourTurn ?? self.isYourTurn,
            lastUpdatedAt: lastUpdatedAt ?? self.lastUpdatedAt,
            matchID: matchID ?? self.matchID,
            matchScore: matchScore ?? self.matchScore,
            moveCount: moveCount ?? self.moveCount,
            opponentID: opponentID ?? self.opponentID,
            opponentName: opponentName ?? self.opponentName,
            opponentRating: opponentRating ?? self.opponentRating,
            targetScore: targetScore ?? self.targetScore,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays,
            timeRemaining: timeRemaining ?? self.timeRemaining,
            turnDeadline: turnDeadline ?? self.turnDeadline
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Services.CorrespondenceGameDto
// MARK: - WaitingGame
public struct WaitingGame: Codable {
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let isRated: Bool
    /// Transpiled from bool
    public let isYourTurn: Bool
    /// Transpiled from System.DateTime
    public let lastUpdatedAt: String
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string
    public let matchScore: String
    /// Transpiled from int
    public let moveCount: Double
    /// Transpiled from string
    public let opponentID: String
    /// Transpiled from string
    public let opponentName: String
    /// Transpiled from int
    public let opponentRating: Double
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let timePerMoveDays: Double
    /// Transpiled from string?
    public let timeRemaining: String?
    /// Transpiled from System.DateTime
    public let turnDeadline: String?

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case isRated, isYourTurn, lastUpdatedAt
        case matchID = "matchId"
        case matchScore, moveCount
        case opponentID = "opponentId"
        case opponentName, opponentRating, targetScore, timePerMoveDays, timeRemaining, turnDeadline
    }

    public init(gameID: String, isRated: Bool, isYourTurn: Bool, lastUpdatedAt: String, matchID: String, matchScore: String, moveCount: Double, opponentID: String, opponentName: String, opponentRating: Double, targetScore: Double, timePerMoveDays: Double, timeRemaining: String?, turnDeadline: String?) {
        self.gameID = gameID
        self.isRated = isRated
        self.isYourTurn = isYourTurn
        self.lastUpdatedAt = lastUpdatedAt
        self.matchID = matchID
        self.matchScore = matchScore
        self.moveCount = moveCount
        self.opponentID = opponentID
        self.opponentName = opponentName
        self.opponentRating = opponentRating
        self.targetScore = targetScore
        self.timePerMoveDays = timePerMoveDays
        self.timeRemaining = timeRemaining
        self.turnDeadline = turnDeadline
    }
}

// MARK: WaitingGame convenience initializers and mutators

public extension WaitingGame {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(WaitingGame.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String? = nil,
        isRated: Bool? = nil,
        isYourTurn: Bool? = nil,
        lastUpdatedAt: String? = nil,
        matchID: String? = nil,
        matchScore: String? = nil,
        moveCount: Double? = nil,
        opponentID: String? = nil,
        opponentName: String? = nil,
        opponentRating: Double? = nil,
        targetScore: Double? = nil,
        timePerMoveDays: Double? = nil,
        timeRemaining: String?? = nil,
        turnDeadline: String?? = nil
    ) -> WaitingGame {
        return WaitingGame(
            gameID: gameID ?? self.gameID,
            isRated: isRated ?? self.isRated,
            isYourTurn: isYourTurn ?? self.isYourTurn,
            lastUpdatedAt: lastUpdatedAt ?? self.lastUpdatedAt,
            matchID: matchID ?? self.matchID,
            matchScore: matchScore ?? self.matchScore,
            moveCount: moveCount ?? self.moveCount,
            opponentID: opponentID ?? self.opponentID,
            opponentName: opponentName ?? self.opponentName,
            opponentRating: opponentRating ?? self.opponentRating,
            targetScore: targetScore ?? self.targetScore,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays,
            timeRemaining: timeRemaining ?? self.timeRemaining,
            turnDeadline: turnDeadline ?? self.turnDeadline
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

/// Transpiled from Backgammon.Server.Services.CorrespondenceGameDto
// MARK: - YourTurnGame
public struct YourTurnGame: Codable {
    /// Transpiled from string
    public let gameID: String
    /// Transpiled from bool
    public let isRated: Bool
    /// Transpiled from bool
    public let isYourTurn: Bool
    /// Transpiled from System.DateTime
    public let lastUpdatedAt: String
    /// Transpiled from string
    public let matchID: String
    /// Transpiled from string
    public let matchScore: String
    /// Transpiled from int
    public let moveCount: Double
    /// Transpiled from string
    public let opponentID: String
    /// Transpiled from string
    public let opponentName: String
    /// Transpiled from int
    public let opponentRating: Double
    /// Transpiled from int
    public let targetScore: Double
    /// Transpiled from int
    public let timePerMoveDays: Double
    /// Transpiled from string?
    public let timeRemaining: String?
    /// Transpiled from System.DateTime
    public let turnDeadline: String?

    public enum CodingKeys: String, CodingKey {
        case gameID = "gameId"
        case isRated, isYourTurn, lastUpdatedAt
        case matchID = "matchId"
        case matchScore, moveCount
        case opponentID = "opponentId"
        case opponentName, opponentRating, targetScore, timePerMoveDays, timeRemaining, turnDeadline
    }

    public init(gameID: String, isRated: Bool, isYourTurn: Bool, lastUpdatedAt: String, matchID: String, matchScore: String, moveCount: Double, opponentID: String, opponentName: String, opponentRating: Double, targetScore: Double, timePerMoveDays: Double, timeRemaining: String?, turnDeadline: String?) {
        self.gameID = gameID
        self.isRated = isRated
        self.isYourTurn = isYourTurn
        self.lastUpdatedAt = lastUpdatedAt
        self.matchID = matchID
        self.matchScore = matchScore
        self.moveCount = moveCount
        self.opponentID = opponentID
        self.opponentName = opponentName
        self.opponentRating = opponentRating
        self.targetScore = targetScore
        self.timePerMoveDays = timePerMoveDays
        self.timeRemaining = timeRemaining
        self.turnDeadline = turnDeadline
    }
}

// MARK: YourTurnGame convenience initializers and mutators

public extension YourTurnGame {
    init(data: Data) throws {
        self = try newJSONDecoder().decode(YourTurnGame.self, from: data)
    }

    init(_ json: String, using encoding: String.Encoding = .utf8) throws {
        guard let data = json.data(using: encoding) else {
            throw NSError(domain: "JSONDecoding", code: 0, userInfo: nil)
        }
        try self.init(data: data)
    }

    init(fromURL url: URL) throws {
        try self.init(data: try Data(contentsOf: url))
    }

    func with(
        gameID: String? = nil,
        isRated: Bool? = nil,
        isYourTurn: Bool? = nil,
        lastUpdatedAt: String? = nil,
        matchID: String? = nil,
        matchScore: String? = nil,
        moveCount: Double? = nil,
        opponentID: String? = nil,
        opponentName: String? = nil,
        opponentRating: Double? = nil,
        targetScore: Double? = nil,
        timePerMoveDays: Double? = nil,
        timeRemaining: String?? = nil,
        turnDeadline: String?? = nil
    ) -> YourTurnGame {
        return YourTurnGame(
            gameID: gameID ?? self.gameID,
            isRated: isRated ?? self.isRated,
            isYourTurn: isYourTurn ?? self.isYourTurn,
            lastUpdatedAt: lastUpdatedAt ?? self.lastUpdatedAt,
            matchID: matchID ?? self.matchID,
            matchScore: matchScore ?? self.matchScore,
            moveCount: moveCount ?? self.moveCount,
            opponentID: opponentID ?? self.opponentID,
            opponentName: opponentName ?? self.opponentName,
            opponentRating: opponentRating ?? self.opponentRating,
            targetScore: targetScore ?? self.targetScore,
            timePerMoveDays: timePerMoveDays ?? self.timePerMoveDays,
            timeRemaining: timeRemaining ?? self.timeRemaining,
            turnDeadline: turnDeadline ?? self.turnDeadline
        )
    }

    func jsonData() throws -> Data {
        return try newJSONEncoder().encode(self)
    }

    func jsonString(encoding: String.Encoding = .utf8) throws -> String? {
        return String(data: try self.jsonData(), encoding: encoding)
    }
}

public typealias CheckerColor = Double
public typealias FriendshipStatus = Double
public typealias GameStatus = Double
public typealias OnlinePlayerStatus = Double
public typealias ProfilePrivacyLevel = Double
public typealias CheckerColorDto = Double
public typealias GameStatusDto = Double
public typealias OpponentTypeDto = Double
public typealias TimeControlTypeDto = Double

// MARK: - Helper functions for creating encoders and decoders

func newJSONDecoder() -> JSONDecoder {
    let decoder = JSONDecoder()
    if #available(iOS 10.0, OSX 10.12, tvOS 10.0, watchOS 3.0, *) {
        decoder.dateDecodingStrategy = .iso8601
    }
    return decoder
}

func newJSONEncoder() -> JSONEncoder {
    let encoder = JSONEncoder()
    if #available(iOS 10.0, OSX 10.12, tvOS 10.0, watchOS 3.0, *) {
        encoder.dateEncodingStrategy = .iso8601
    }
    return encoder
}
