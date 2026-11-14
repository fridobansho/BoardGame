namespace BoardGame.Web.Services;

using System.Collections.Concurrent;
using System.Text.Json;
using BoardGame.Web.Models;
using Microsoft.Data.Sqlite;

public class SessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _roomCodeToSessionId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Timer _timerCheck;
    private readonly string _connectionString;
    private readonly object _dbLock = new();

    public SessionManager()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "boardgame.db");
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
        int? timerSeconds = null
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
            timerSeconds: timerSeconds
        );
        _sessions[session.Id] = session;
        _roomCodeToSessionId[session.RoomCode] = session.Id;
        session.OnStateChanged += () => SaveSession(session);
        SaveSession(session);
        return session;
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
            var json = JsonSerializer.Serialize(session.ExportPersistedState());
            lock (_dbLock)
            {
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
            }
        }
        catch
        {
            // Ignore persistence errors to keep gameplay responsive
        }
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

    public void Dispose()
    {
        _timerCheck.Dispose();
    }
}
