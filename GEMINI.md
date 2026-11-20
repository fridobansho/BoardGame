# BoardGame Workspace Guidelines

## Build, Test & Runtime
- `dotnet` is installed at `/var/home/david/.dotnet`; prepend `/var/home/david/.dotnet:/home/linuxbrew/.linuxbrew/bin` to `PATH` before running `dotnet build` or `dotnet test`.
- Validate frontend JavaScript syntax with `node -c BoardGame/BoardGame.Web/wwwroot/js/app.js` after editing `app.js`.
- `boardgame-web.service` (`Type=simple`) runs `run-service.sh`, which launches `dotnet watch --non-interactive run --no-launch-profile` with `ASPNETCORE_URLS` exported (binding `0.0.0.0:5000` and `0.0.0.0:5001` when `certs/boardgame.pfx` exists). Use `BypassSandbox: true` when running `systemctl --user` or `journalctl --user` so the user can approve rather than running them manually.
- Bump the `?v=N` cache-busting query string on `style.css` and `app.js` in `BoardGame/BoardGame.Web/wwwroot/index.html` whenever static assets change.

## UI & Game Mode Invariants
- **Mode gating**: `Daily Puzzle` (`daily`) and `Solo` (`solo`) modes must only be offered for puzzle or set-piece games (`supportsDaily` / `supportsSolo` in `GAME_REGISTRY`), never for standard head-to-head games.
- **Responsive dropdown sync**: When synchronizing `.pill-group` controls into `<select class="responsive-select">` dropdowns, always query `.pill-btn:not(.hidden)` so category-filtered games and hidden mode options never appear in mobile dropdowns.
