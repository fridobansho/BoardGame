namespace BoardGame.Web.Services;

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using BoardGame.Library.Interfaces;
using BoardGame.Web.Models;
using Microsoft.Data.Sqlite;

public class SessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _roomCodeToSessionId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Timer _timerCheck;
    private readonly string _connectionString;
    private readonly object _dbLock = new();

    public event Action<string, GameSession, string>? OnPlayerSessionChanged;

    public SessionManager(string? dbPath = null)
    {
        dbPath ??= Path.Combine(AppContext.BaseDirectory, "boardgame.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
        LoadSessions();

        _timerCheck = new Timer(_ =>
        {
            foreach (var session in _sessions.Values)
            {
                if (session.Status == "InProgress" && session.TimerSeconds > 0)
                {
                    session.CheckTimerExpired();
                }
            }
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public GameSession CreateSession(
        string? gameType,
        string? mode,
        string? aiDifficulty,
        string? p1Name,
        string? p2Name,
        bool isPublic = true,
        int? customWidth = null,
        int? customHeight = null,
        int? timerSeconds = null,
        string? variant = "standard"
    )
    {
        var session = new GameSession(
            gameType,
            mode,
            aiDifficulty,
            p1Name,
            p2Name,
            isPublic: isPublic,
            customWidth: customWidth,
            customHeight: customHeight,
            timerSeconds: timerSeconds,
            variant: variant
        );
        _sessions[session.Id] = session;
        _roomCodeToSessionId[session.RoomCode] = session.Id;
        session.OnStateChanged += () => SaveSession(session);
        SaveSession(session);

        if (session.Players.Count > 0 && session.Players[0] is WebHumanPlayer p1)
        {
            NotifyPlayerSessionChanged(p1.Name, session, session.Player1Token);
        }
        if (session.Players.Count > 1 && session.Players[1] is WebHumanPlayer p2 && session.Mode != "room")
        {
            var p1NameTrimmed = (session.Players[0] as WebHumanPlayer)?.Name?.Trim();
            if (!string.Equals(p1NameTrimmed, p2.Name?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                NotifyPlayerSessionChanged(p2.Name, session, session.Player2Token);
            }
        }

        return session;
    }

    public void NotifyPlayerSessionChanged(string? playerName, GameSession session, string playerToken)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;
        OnPlayerSessionChanged?.Invoke(playerName.Trim(), session, playerToken);
    }

    private void InitializeDatabase()
    {
        lock (_dbLock)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS sessions (
                    id TEXT PRIMARY KEY,
                    room_code TEXT NOT NULL,
                    data TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS leaderboard (
                    entry_key TEXT PRIMARY KEY,
                    game_type TEXT NOT NULL,
                    mode TEXT NOT NULL,
                    player_name TEXT NOT NULL,
                    score INTEGER NOT NULL,
                    duration_seconds INTEGER NOT NULL,
                    moves INTEGER NOT NULL,
                    puzzle_date TEXT NOT NULL,
                    created_at TEXT NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        }
    }

    private void LoadSessions()
    {
        lock (_dbLock)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT data FROM sessions;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    var json = reader.GetString(0);
                    var state = JsonSerializer.Deserialize<GameSession.PersistedSession>(json);
                    if (state != null)
                    {
                        var session = GameSession.FromPersistedState(state);
                        _sessions[session.Id] = session;
                        _roomCodeToSessionId[session.RoomCode] = session.Id;
                        session.OnStateChanged += () => SaveSession(session);
                    }
                }
                catch
                {
                    // Skip corrupted/incompatible session rows
                }
            }
        }
    }

    private void SaveSession(GameSession session)
    {
        try
        {
            lock (_dbLock)
            {
                var persisted = session.ExportPersistedState();
                var json = JsonSerializer.Serialize(persisted);
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO sessions (id, room_code, data, updated_at)
                    VALUES ($id, $roomCode, $data, $updatedAt)
                    ON CONFLICT(id) DO UPDATE SET
                        room_code = excluded.room_code,
                        data = excluded.data,
                        updated_at = excluded.updated_at;
                    """;
                cmd.Parameters.AddWithValue("$id", session.Id);
                cmd.Parameters.AddWithValue("$roomCode", session.RoomCode);
                cmd.Parameters.AddWithValue("$data", json);
                cmd.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));
                cmd.ExecuteNonQuery();

                if (session.Status == "Finished" && !string.IsNullOrWhiteSpace(session.Winner))
                {
                    RecordLeaderboardEntryInternal(conn, session);
                }
            }
        }
        catch
        {
            // Ignore persistence errors to keep gameplay responsive
        }
    }

    private static void RecordLeaderboardEntryInternal(SqliteConnection conn, GameSession session)
    {
        string playerName = session.Winner!;
        int durationSeconds = Math.Max(1, (int)Math.Round((DateTime.UtcNow - session.CreatedAt).TotalSeconds));
        int score = CalculateWinnerScore(session);
        string puzzleDate = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        string entryKey = $"{session.Id}_g{session.GameNumber}";
        if (session.GameType == "wordle" && session.Mode == "daily")
        {
            if (session.GameNumber > 1)
            {
                return;
            }
            entryKey = $"daily_wordle_{puzzleDate}_{playerName.Trim().ToLowerInvariant()}";
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO leaderboard (
                entry_key, game_type, mode, player_name, score, duration_seconds, moves, puzzle_date, created_at
            )
            VALUES (
                $entryKey, $gameType, $mode, $playerName, $score, $durationSeconds, $moves, $puzzleDate, $createdAt
            );
            """;
        cmd.Parameters.AddWithValue("$entryKey", entryKey);
        cmd.Parameters.AddWithValue("$gameType", session.GameType);
        cmd.Parameters.AddWithValue("$mode", session.Mode);
        cmd.Parameters.AddWithValue("$playerName", playerName);
        cmd.Parameters.AddWithValue("$score", score);
        cmd.Parameters.AddWithValue("$durationSeconds", durationSeconds);
        cmd.Parameters.AddWithValue("$moves", session.MoveCount);
        cmd.Parameters.AddWithValue("$puzzleDate", puzzleDate);
        cmd.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private static int CalculateWinnerScore(GameSession session)
    {
        var winnerPlayer = session.Players.FirstOrDefault(p => string.Equals(p.Name, session.Winner, StringComparison.Ordinal));
        if (session.GameLogic is IScoredGameLogic scored && winnerPlayer != null)
        {
            return scored.GetScore(winnerPlayer);
        }

        if (session.GameType == "othello" && winnerPlayer != null)
        {
            string sym = session.GameLogic.GetPiece(winnerPlayer).Value;
            int count = 0;
            for (int x = 0; x < session.Board.Width; x++)
            {
                for (int y = 0; y < session.Board.Height; y++)
                {
                    if (session.Board.PieceAt(x, y)?.Value == sym)
                    {
                        count++;
                    }
                }
            }
            return count * 10;
        }

        return Math.Max(10, 200 - session.MoveCount * 2);
    }

    public List<LeaderboardEntryDto> GetLeaderboard(string? gameType = null, string? mode = null, int limit = 25)
    {
        var list = new List<LeaderboardEntryDto>();
        bool filterGame = !string.IsNullOrWhiteSpace(gameType) && !string.Equals(gameType, "all", StringComparison.OrdinalIgnoreCase);
        bool filterMode = !string.IsNullOrWhiteSpace(mode) && !string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase);

        string whereClause = "";
        if (filterGame && filterMode)
        {
            whereClause = "WHERE game_type = $gameType AND mode = $mode";
        }
        else if (filterGame)
        {
            whereClause = "WHERE game_type = $gameType";
        }
        else if (filterMode)
        {
            whereClause = "WHERE mode = $mode";
        }

        bool isPuzzleOrder = string.Equals(mode, "daily", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gameType, "sudoku", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gameType, "wordle", StringComparison.OrdinalIgnoreCase)
            || string.Equals(gameType, "minesweeper", StringComparison.OrdinalIgnoreCase);

        string orderClause = isPuzzleOrder
            ? "ORDER BY score DESC, duration_seconds ASC, moves ASC"
            : "ORDER BY score DESC, duration_seconds ASC, created_at DESC";

        lock (_dbLock)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                SELECT player_name, game_type, mode, score, duration_seconds, moves, puzzle_date, created_at
                FROM leaderboard
                {whereClause}
                {orderClause}
                LIMIT $limit;
                """;
            if (filterGame) cmd.Parameters.AddWithValue("$gameType", gameType!.Trim().ToLowerInvariant());
            if (filterMode) cmd.Parameters.AddWithValue("$mode", mode!.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 100));

            using var reader = cmd.ExecuteReader();
            int rank = 1;
            while (reader.Read())
            {
                string createdRaw = reader.GetString(7);
                list.Add(new LeaderboardEntryDto(
                    Rank: rank++,
                    PlayerName: reader.GetString(0),
                    GameType: reader.GetString(1),
                    Mode: reader.GetString(2),
                    Score: reader.GetInt32(3),
                    DurationSeconds: reader.GetInt32(4),
                    Moves: reader.GetInt32(5),
                    PuzzleDate: reader.GetString(6),
                    TimeAgo: FormatTimeAgo(createdRaw)
                ));
            }
        }

        return list;
    }

    public List<PlayerStandingDto> GetPlayerStandings(string? gameType = null, int limit = 15)
    {
        var list = new List<PlayerStandingDto>();
        bool filterGame = !string.IsNullOrWhiteSpace(gameType) && !string.Equals(gameType, "all", StringComparison.OrdinalIgnoreCase);
        string whereClause = filterGame ? "WHERE game_type = $gameType" : "";

        lock (_dbLock)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                SELECT player_name,
                       COUNT(*) AS wins,
                       MAX(score) AS best_score,
                       SUM(score) AS total_score,
                       MIN(duration_seconds) AS fastest_sec
                FROM leaderboard
                {whereClause}
                GROUP BY player_name
                ORDER BY wins DESC, total_score DESC, fastest_sec ASC
                LIMIT $limit;
                """;
            if (filterGame) cmd.Parameters.AddWithValue("$gameType", gameType!.Trim().ToLowerInvariant());
            cmd.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 50));

            using var reader = cmd.ExecuteReader();
            int rank = 1;
            while (reader.Read())
            {
                int wins = reader.GetInt32(1);
                list.Add(new PlayerStandingDto(
                    Rank: rank++,
                    PlayerName: reader.GetString(0),
                    Wins: wins,
                    GamesPlayed: wins,
                    BestScore: reader.GetInt32(2),
                    TotalScore: reader.GetInt32(3),
                    FastestWinSeconds: reader.GetInt32(4)
                ));
            }
        }

        return list;
    }

    private static string FormatTimeAgo(string isoDate)
    {
        if (!DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
        {
            return "recently";
        }
        var span = DateTime.UtcNow - dt.ToUniversalTime();
        if (span.TotalSeconds < 60) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        return $"{(int)span.TotalDays}d ago";
    }

    public IEnumerable<RoomSummaryDto> GetAvailableRooms()
    {
        return _sessions.Values
            .Where(s => s.IsPublic && (DateTime.UtcNow - s.CreatedAt).TotalHours < 2)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.ToSummaryDto())
            .ToList();
    }

    public GameSession? GetSession(string id)
    {
        _sessions.TryGetValue(id, out var session);
        return session;
    }

    public GameSession? GetSessionByRoomCode(string? roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return null;
        if (_roomCodeToSessionId.TryGetValue(roomCode.Trim(), out var sessionId))
        {
            return GetSession(sessionId);
        }
        return null;
    }

    public (GameSession Session, string PlayerToken)? GetLatestSessionForPlayer(string? playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return null;
        string cleanName = playerName.Trim();

        var match = _sessions.Values
            .Select(s => (Session: s, Token: s.GetTokenForPlayerName(cleanName)))
            .Where(x => x.Token != null)
            .OrderByDescending(x => x.Session.UpdatedAt)
            .ThenByDescending(x => x.Session.CreatedAt)
            .FirstOrDefault();

        if (match.Session == null || match.Token == null) return null;
        return (match.Session, match.Token);
    }

    public void Dispose()
    {
        _timerCheck.Dispose();
    }
}
