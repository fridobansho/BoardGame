# BoardGame Workspace Guidelines

## Build, Test & Runtime
- `dotnet` is installed at `/var/home/david/.dotnet`; prepend `/var/home/david/.dotnet:/home/linuxbrew/.linuxbrew/bin` to `PATH` before running `dotnet build` or `dotnet test`.
- Validate frontend JavaScript syntax with `node -c BoardGame/BoardGame.Web/wwwroot/js/app.js` after editing `app.js`.
- `boardgame-web.service` runs `run-service.sh`, which serves `publish/BoardGame.Web.dll` whenever it exists. After any C# or `wwwroot/` change, republish with `/var/home/david/.dotnet/dotnet publish BoardGame/BoardGame.Web/BoardGame.Web.csproj -c Release -o publish --no-restore` and run `systemctl --user restart boardgame-web.service`.
- Bump the `?v=N` cache-busting query string on `style.css` and `app.js` in `BoardGame/BoardGame.Web/wwwroot/index.html` whenever static assets change.

## UI & Game Mode Invariants
- **Mode gating**: `Daily Puzzle` (`daily`) and `Solo` (`solo`) modes must only be offered for puzzle or set-piece games (`supportsDaily` / `supportsSolo` in `GAME_REGISTRY`), never for standard head-to-head games.
- **Responsive dropdown sync**: When synchronizing `.pill-group` controls into `<select class="responsive-select">` dropdowns, always query `.pill-btn:not(.hidden)` so category-filtered games and hidden mode options never appear in mobile dropdowns.
