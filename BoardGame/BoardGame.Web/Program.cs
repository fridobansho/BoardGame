using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using BoardGame.Web.Models;
using BoardGame.Web.Services;

// Dynamically locate content root directory containing wwwroot
string? contentRoot = null;
var candidates = new[]
{
    AppContext.BaseDirectory,
    Directory.GetCurrentDirectory(),
    Path.Combine(AppContext.BaseDirectory, "wwwroot"),
    Path.Combine(Directory.GetCurrentDirectory(), "publish"),
    Path.Combine(Directory.GetCurrentDirectory(), "BoardGame", "BoardGame.Web"),
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..")
};

foreach (var dir in candidates)
{
    if (Directory.Exists(Path.Combine(dir, "wwwroot")))
    {
        contentRoot = Path.GetFullPath(dir);
        break;
    }
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot ?? Directory.GetCurrentDirectory()
});

// Native systemd service integration (sd_notify, lifecycle, journal logging)
builder.Host.UseSystemd();

// Ensure JSON serialization is consistently camelCase everywhere
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddSingleton<SessionManager>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Prevent mobile browser caching during local development
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
        ctx.Context.Response.Headers.Append("Expires", "0");
    }
});

var jsonOpts = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
};

// REST Endpoints
app.MapPost("/api/session/new", (CreateSessionRequest req, SessionManager sm) =>
{
    var session = sm.CreateSession(
        req.GameType,
        req.Mode,
        req.AiDifficulty,
        req.Player1Name,
        req.Player2Name,
        isPublic: req.IsPublic ?? true,
        customWidth: req.CustomWidth,
        customHeight: req.CustomHeight,
        timerSeconds: req.TimerSeconds
    );
    return Results.Ok(new
    {
        session = session.ToDto(),
        playerToken = session.Player1Token
    });
});

app.MapGet("/api/rooms", (SessionManager sm) =>
{
    return Results.Ok(sm.GetAvailableRooms());
});

app.MapGet("/api/session/{id}", (string id, SessionManager sm) =>
{
    var session = sm.GetSession(id);
    if (session == null) return Results.NotFound(new { error = "Session not found" });
    return Results.Ok(session.ToDto());
});

app.MapPost("/api/session/{id}/move", (string id, MoveRequest req, SessionManager sm) =>
{
    var session = sm.GetSession(id);
    if (session == null) return Results.NotFound(new { error = "Session not found" });

    var (success, error) = session.MakeMove(req.X, req.Y, req.PlayerToken);
    if (!success)
    {
        return Results.BadRequest(new { error, session = session.ToDto() });
    }

    return Results.Ok(new { success = true, session = session.ToDto() });
});

app.MapPost("/api/session/{id}/reset", (string id, ResetRequest? req, SessionManager sm) =>
{
    var session = sm.GetSession(id);
    if (session == null) return Results.NotFound(new { error = "Session not found" });

    session.Reset(req?.IsRematch ?? false);
    return Results.Ok(new { success = true, session = session.ToDto() });
});

app.MapPost("/api/session/{id}/reset-series", (string id, SessionManager sm) =>
{
    var session = sm.GetSession(id);
    if (session == null) return Results.NotFound(new { error = "Session not found" });

    session.ResetSeries();
    return Results.Ok(new { success = true, session = session.ToDto() });
});

app.MapPost("/api/session/{id}/undo", (string id, UndoRequest? req, SessionManager sm) =>
{
    var session = sm.GetSession(id);
    if (session == null) return Results.NotFound(new { error = "Session not found" });

    var (success, error) = session.Undo(req?.PlayerToken);
    if (!success)
    {
        return Results.BadRequest(new { error, session = session.ToDto() });
    }

    return Results.Ok(new { success = true, session = session.ToDto() });
});

app.MapPost("/api/session/{id}/reaction", (string id, ReactionRequest req, SessionManager sm) =>
{
    var session = sm.GetSession(id);
    if (session == null) return Results.NotFound(new { error = "Session not found" });

    if (!string.IsNullOrWhiteSpace(req.Emoji))
    {
        session.SendReaction(req.Emoji);
    }

    return Results.Ok(new { success = true });
});

app.MapPost("/api/session/{id}/chat", (string id, ChatRequest req, SessionManager sm) =>
{
    var session = sm.GetSession(id);
    if (session == null) return Results.NotFound(new { error = "Session not found" });

    if (!string.IsNullOrWhiteSpace(req.Text))
    {
        session.SendChat(req.Sender ?? "Player", req.Text);
    }

    return Results.Ok(new { success = true });
});

app.MapPost("/api/session/{id}/predict", (string id, PredictionRequest req, SessionManager sm) =>
{
    var session = sm.GetSession(id);
    if (session == null) return Results.NotFound(new { error = "Session not found" });

    if (req.PlayerIndex == 0 || req.PlayerIndex == 1)
    {
        session.AddPrediction(req.PlayerIndex);
    }

    return Results.Ok(new { success = true, p1 = session.Player1Predictions, p2 = session.Player2Predictions });
});

app.MapPost("/api/room/join", (JoinRoomRequest req, SessionManager sm) =>
{
    if (string.IsNullOrWhiteSpace(req.RoomCode))
    {
        return Results.BadRequest(new { error = "Please provide a valid 4-letter room code." });
    }

    var code = req.RoomCode.Trim().ToUpperInvariant();
    var session = sm.GetSessionByRoomCode(code);
    if (session == null)
    {
        return Results.NotFound(new { error = $"Room not found with code '{code}'. Make sure the host has started a 'Wi-Fi Room' on their screen." });
    }

    if (session.JoinRoomAsGuest(req.PlayerName ?? "Guest", out var token))
    {
        return Results.Ok(new
        {
            success = true,
            session = session.ToDto(),
            playerToken = token
        });
    }

    return Results.BadRequest(new { error = "Unable to join room (room may already have two players)." });
});

// Server-Sent Events endpoint for real-time live game state updates
app.MapGet("/api/session/{id}/stream", async (string id, HttpContext context, SessionManager sm, CancellationToken ct) =>
{
    var session = sm.GetSession(id);
    if (session == null)
    {
        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        return;
    }

    bool isSpectator = context.Request.Query["spectator"] == "true" || context.Request.Query["role"] == "spectator";
    if (isSpectator)
    {
        session.IncrementSpectators();
    }

    context.Response.Headers.ContentType = "text/event-stream";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";

    var tcs = new TaskCompletionSource<bool>();
    ct.Register(() => tcs.TrySetResult(true));

    async Task SendStateAsync()
    {
        try
        {
            var dto = session.ToDto();
            var json = JsonSerializer.Serialize(dto, jsonOpts);
            await context.Response.WriteAsync($"data: {json}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
        catch
        {
            tcs.TrySetResult(true);
        }
    }

    async Task SendReactionAsync(string emoji)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { emoji }, jsonOpts);
            await context.Response.WriteAsync($"event: reaction\ndata: {json}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
        catch
        {
            tcs.TrySetResult(true);
        }
    }

    async Task SendChatAsync(string sender, string text)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { sender, text }, jsonOpts);
            await context.Response.WriteAsync($"event: chat\ndata: {json}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
        catch
        {
            tcs.TrySetResult(true);
        }
    }

    async Task SendPredictionAsync(int p1, int p2)
    {
        try
        {
            var json = JsonSerializer.Serialize(new { p1, p2 }, jsonOpts);
            await context.Response.WriteAsync($"event: prediction\ndata: {json}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
        catch
        {
            tcs.TrySetResult(true);
        }
    }

    Action stateHandler = () =>
    {
        _ = SendStateAsync();
    };

    Action<string> reactionHandler = emoji =>
    {
        _ = SendReactionAsync(emoji);
    };

    Action<string, string> chatHandler = (sender, text) =>
    {
        _ = SendChatAsync(sender, text);
    };

    Action<int, int> predictionHandler = (p1, p2) =>
    {
        _ = SendPredictionAsync(p1, p2);
    };

    session.OnStateChanged += stateHandler;
    session.OnReaction += reactionHandler;
    session.OnChat += chatHandler;
    session.OnPrediction += predictionHandler;

    try
    {
        // Send initial state immediately
        await SendStateAsync();

        // Keep-alive loop
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(15000, ct);
            await context.Response.WriteAsync(": ping\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
    }
    catch (OperationCanceledException) { }
    finally
    {
        session.OnStateChanged -= stateHandler;
        session.OnReaction -= reactionHandler;
        session.OnChat -= chatHandler;
        session.OnPrediction -= predictionHandler;
        if (isSpectator)
        {
            session.DecrementSpectators();
        }
    }
});

// Network / Local IP info endpoint
app.MapGet("/api/info", () =>
{
    var ips = new List<string>();
    try
    {
        foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
            {
                ips.Add(ip.ToString());
            }
        }
    }
    catch { }

    return Results.Ok(new
    {
        hostName = Environment.MachineName,
        localIps = ips
    });
});

app.MapFallbackToFile("index.html");

app.Run();
