using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using BoardGame.Web.Models;
using BoardGame.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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
    var session = sm.CreateSession(req.GameType, req.Mode, req.AiDifficulty, req.Player1Name, req.Player2Name);
    return Results.Ok(new
    {
        session = session.ToDto(),
        playerToken = session.Player1Token
    });
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

app.MapPost("/api/session/{id}/reset", (string id, SessionManager sm) =>
{
    var session = sm.GetSession(id);
    if (session == null) return Results.NotFound(new { error = "Session not found" });

    session.Reset();
    return Results.Ok(new { success = true, session = session.ToDto() });
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

    Action stateHandler = () =>
    {
        _ = SendStateAsync();
    };

    session.OnStateChanged += stateHandler;

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

app.Run();
