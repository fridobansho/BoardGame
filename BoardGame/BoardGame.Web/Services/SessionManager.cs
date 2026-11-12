namespace BoardGame.Web.Services;

using System.Collections.Concurrent;
using BoardGame.Web.Models;

public class SessionManager : IDisposable
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _roomCodeToSessionId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Timer _timerCheck;

    public SessionManager()
    {
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

    public void Dispose()
    {
        _timerCheck.Dispose();
    }
}
