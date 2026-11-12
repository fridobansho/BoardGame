namespace BoardGame.Web.Services;

using System.Collections.Concurrent;

public class SessionManager
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _roomCodeToSessionId = new(StringComparer.OrdinalIgnoreCase);

    public GameSession CreateSession(string? gameType, string? mode, string? aiDifficulty, string? p1Name, string? p2Name)
    {
        var session = new GameSession(gameType, mode, aiDifficulty, p1Name, p2Name);
        _sessions[session.Id] = session;
        _roomCodeToSessionId[session.RoomCode] = session.Id;
        return session;
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
}
