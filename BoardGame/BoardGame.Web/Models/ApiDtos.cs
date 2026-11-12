namespace BoardGame.Web.Models;

public record CreateSessionRequest(
    string? GameType,       // "connect4", "tictactoe", "chomp"
    string? Mode,           // "pass-and-play", "vs-ai", "room"
    string? AiDifficulty,  // "easy", "smart"
    string? Player1Name,
    string? Player2Name,
    bool? IsPublic = true
);

public record RoomSummaryDto(
    string RoomCode,
    string SessionId,
    string GameType,
    string Mode,
    string HostName,
    string Status,
    bool CanJoin,
    bool CanSpectate,
    DateTime CreatedAt,
    string TimeAgo
);

public record MoveRequest(
    int X,
    int Y,
    string? PlayerToken
);

public record JoinRoomRequest(
    string? RoomCode,
    string? PlayerName
);

public record ResetRequest(
    bool? IsRematch = false
);

public record ReactionRequest(
    string Emoji
);

public record UndoRequest(
    string? PlayerToken
);

public record SessionStateDto(
    string Id,
    string RoomCode,
    string GameType,
    string Mode,
    int Width,
    int Height,
    string[][] Grid,
    string Status,          // "WaitingForPlayer", "InProgress", "Finished", "Draw"
    string CurrentTurnPlayer, // Name of player whose turn it is
    int CurrentTurnIndex,   // 0 for Player 1, 1 for Player 2
    string? Winner,
    string? LastMoveText,
    int MoveCount,
    string Player1Name,
    string Player2Name,
    bool IsAiTurn,
    int? LastMoveX = null,
    int? LastMoveY = null,
    int SpectatorCount = 0,
    int Player1Score = 0,
    int Player2Score = 0,
    int DrawScore = 0,
    int GameNumber = 1,
    bool CanUndo = false
);
