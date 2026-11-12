namespace BoardGame.Web.Services;

using System.Collections.Concurrent;
using BoardGame.Web.Models;

public class SessionManager
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _roomCodeToSessionId = new(StringComparer.OrdinalIgnoreCase);

    public GameSession CreateSession(string? gameType, string? mode, string? aiDifficulty, string? p1Name, string? p2Name, bool isPublic = true)
    {
        var session = new GameSession(gameType, mode, aiDifficulty, p1Name, p2Name, isPublic: isPublic);
        _sessions[session.Id] = session;
        _roomCodeToSessionId[session.RoomCode] = session.Id;
        return session;
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
}
