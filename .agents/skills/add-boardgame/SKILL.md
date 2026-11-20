---
name: add-boardgame
description: >-
  Use this skill when adding a new board game, puzzle, or play mode to the
  BoardGame C#/.NET 8 library, ASP.NET Core web app, and NUnit test suite.
---

# Adding a New Game or Mode to BoardGame

## 1. Engine Implementation (`BoardGame.Library`)
1. Add game logic under `BoardGame/BoardGame.Library/Implementations/<GameName>/`:
   - Implement `IMoveGameLogic` (supporting `Location`, `PieceMove`, `WordMove`, or `SudokuMove`).
   - Implement `IScoredGameLogic` if the game tracks player/puzzle points (used automatically by HUD score pills, Undo, SQLite persistence, and leaderboards).
   - Implement `ICustomStateGameLogic` if the game has extra internal state beyond the board grid (e.g., tile bags, racks, extra-turn flags, mine layouts, no-progress counters).
2. Add Easy/Smart AI players implementing `IPlayer` (return `PieceMove` directly from `GetMove` for piece-movement games).

## 2. Web Session Wiring (`BoardGame.Web`)
1. Register the `GameType` in `BoardGame/BoardGame.Web/Services/GameSession.cs` (`switch (GameType)` and `CreateAiPlayer`).
2. `GameSession.MakeMove` dispatches by payload shape (`toX`/`toY` -> `PieceMove`, `isPass`/`placements` -> `WordMove`, `digit` -> `SudokuMove`, else `Location`), so no per-game branch is needed unless adding a new payload shape.

## 3. Frontend (`BoardGame.Web/wwwroot`)
1. Add the game entry to `GAME_REGISTRY` in `BoardGame/BoardGame.Web/wwwroot/js/app.js` (`id`, `name`, `shortName`, `icon`, `category`, `supportsDaily`, `supportsSolo`, `supportsAiVsAi`, `defaultWidth`, `defaultHeight`, `variants`, `gridOptions`, `rules`).
2. Add the game pill button (with matching `data-category`) to `#group-game`, `#group-setting-fav`, and `#group-lb-filter` in `BoardGame/BoardGame.Web/wwwroot/index.html`.
3. Add a `render<GameName>(session)` renderer in `app.js` (gate interactive cell clicks with `!isSpectator`) and styles in `style.css`, then bump `?v=N` on `style.css` and `app.js` in `index.html`.

## 4. Verification
1. Add unit and session round-trip tests in `BoardGame/BoardGame.Tests/`.
2. Run:
   - `/home/linuxbrew/.linuxbrew/bin/node -c BoardGame/BoardGame.Web/wwwroot/js/app.js`
   - `export PATH="/var/home/david/.dotnet:/home/linuxbrew/.linuxbrew/bin:$PATH" && dotnet build BoardGame/BoardGame.sln --configuration Release --no-restore && dotnet test BoardGame/BoardGame.sln --configuration Release --no-build`
3. Restart `boardgame-web.service` if C# DTOs/records/endpoints changed, and smoke-test `POST http://localhost:5000/api/session/new`. If `./install-service.sh --start` fails with `CS0006` on `BoardGame.Library.dll`, run `dotnet clean BoardGame/BoardGame.sln -c Release` first.
4. (Optional) Wire the new game into `BoardGame/BoardGame/Program.cs` (interactive menu, `--game` CLI targets, and `InPlaceBoardRenderer` compatibility) and update `play-demo.sh` usage text.
