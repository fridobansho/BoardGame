# BoardGame Workspace Guidelines

## Build, Test & Runtime
- `dotnet` is installed at `/var/home/david/.dotnet`; prepend `/var/home/david/.dotnet:/home/linuxbrew/.linuxbrew/bin` to `PATH` before running `dotnet build` or `dotnet test`. Always pass `--no-restore` to `dotnet build` and `dotnet test` in sandbox mode (automatic NuGet restore fails on mutex locks like `'NuGet-Migrations'`).
- Use `TARGET_DIR="." ./setup-and-test.sh` (or `./setup-and-test.sh --publish`) when running locally to avoid creating a nested `BoardGame-workspace/` clone.
- In `BoardGame.Tests`, use `CreateIsolatedSessionManager()` (unique temp SQLite DB) instead of `new SessionManager()` so tests do not share `boardgame.db`.
- Validate frontend JavaScript syntax with `/home/linuxbrew/.linuxbrew/bin/node -c BoardGame/BoardGame.Web/wwwroot/js/app.js` after editing `app.js`.
- `boardgame-web.service` (`Type=simple`) runs `run-service.sh`, which launches `dotnet watch --non-interactive run --no-launch-profile` with `ASPNETCORE_URLS` exported (binding `0.0.0.0:5000` and `0.0.0.0:5001` when `certs/boardgame.pfx` exists). Use `BypassSandbox: true` when running `systemctl --user` or `journalctl --user` so the user can approve rather than running them manually.
- Bump the `?v=N` cache-busting query string on `style.css` and `app.js` in `BoardGame/BoardGame.Web/wwwroot/index.html` whenever static assets change.
- Git operations: `.git` internals are read-only in sandbox mode; commands modifying git state or index (`git add`, `git commit`, `git checkout`, `git stash`) must run with `BypassSandbox: true`. Read-only commands (`git status`, `git diff`, `git log`, `git show`) run sandboxed.

## UI & Game Mode Invariants
- **Mode gating**: `Daily Puzzle` (`daily`) and `Solo` (`solo`) modes must only be offered for puzzle or set-piece games (`supportsDaily` / `supportsSolo` in `GAME_REGISTRY`), never for standard head-to-head games.
- **Responsive dropdown sync**: When synchronizing `.pill-group` controls into `<select class="responsive-select">` dropdowns, always query `.pill-btn:not(.hidden)` so category-filtered games and hidden mode options never appear in mobile dropdowns.

## Console & TUI Invariants
- **Scroll-proof in-place rendering**: In-place console rendering must never rely on absolute `Console.SetCursorPosition(left, top)`. Use relative ANSI cursor movement (`\x1b[{N}F` - Cursor Previous Line) and line clearing (`\x1b[2K`) so rendering remains intact regardless of viewport position.
- **Alternate screen buffer**: Interactive full-screen animations or demo runners must use the xterm alternate screen buffer (`\x1b[?1049h` / `\x1b[?1049l`) to prevent polluting the user's terminal scrollback, auto-falling back to stream mode when output is redirected.
- **Terminal recovery**: Always register `Console.CancelKeyPress` (Ctrl+C) and `IDisposable` cleanup when hiding the cursor (`\x1b[?25l`) or entering the alternate screen buffer, guaranteeing the host terminal is restored even if interrupted.
- **Sequential demo pacing**: Clear the screen (`Console.Clear()`) between sequential games in multi-game TUI demos so tall grids (like Connect Four) always start at row 1.
