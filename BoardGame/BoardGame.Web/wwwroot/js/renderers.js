// ============================================================================
// Game Board Renderers (all 14 games)
// ============================================================================

export function createBoardRenderer(ctx) {
  // Board Renderers
  function renderBoard(session) {
    ctx.boardContainer.innerHTML = '';
    ctx.boardContainer.className = `board-container ${session.gameType}-mode` + (ctx.isSpectator ? ' spectator-mode' : '');

    if (session.mode === 'ai-vs-ai' && session.status === 'InProgress' && !ctx.isSpectator && ctx.replayIndex === null) {
      const aiBar = document.createElement('div');
      aiBar.className = 'ai-exhibition-bar';

      const btnPause = document.createElement('button');
      btnPause.type = 'button';
      btnPause.className = 'btn btn-xs btn-secondary';
      btnPause.textContent = ctx.aiExhibitionPaused ? '▶ Resume AI' : '⏸ Pause AI';
      btnPause.addEventListener('click', () => {
        ctx.aiExhibitionPaused = !ctx.aiExhibitionPaused;
        ctx.syncAiExhibitionLoop(session);
        renderBoard(session);
      });

      const btnStep = document.createElement('button');
      btnStep.type = 'button';
      btnStep.className = 'btn btn-xs btn-primary';
      btnStep.textContent = '⏭ Step Move';
      btnStep.addEventListener('click', () => ctx.stepAiExhibition(session.id));

      aiBar.appendChild(btnPause);
      aiBar.appendChild(btnStep);
      ctx.boardContainer.appendChild(aiBar);
    }

    switch (session.gameType) {
      case 'connect4':
        renderConnectFour(session);
        break;
      case 'tictactoe':
        renderTicTacToe(session);
        break;
      case 'chomp':
        renderChomp(session);
        break;
      case 'gomoku':
        renderGomoku(session);
        break;
      case 'othello':
        renderOthello(session);
        break;
      case 'nim':
        renderNim(session);
        break;
      case 'hex':
        renderHex(session);
        break;
      case 'checkers':
        renderCheckers(session);
        break;
      case 'scrabble':
        renderScrabble(session);
        break;
      case 'sudoku':
        renderSudoku(session);
        break;
      case 'breakthrough':
        renderBreakthrough(session);
        break;
      case 'mancala':
        renderMancala(session);
        break;
      case 'wordle':
        renderWordle(session);
        break;
      case 'minesweeper':
        renderMinesweeper(session);
        break;
      default:
        renderConnectFour(session);
        break;
    }
  }

  function renderBreakthrough(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'checkers-wrapper';

    const hint = document.createElement('div');
    hint.className = 'checkers-hint';
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;
    if (!canPlay) {
      hint.textContent = session.lastMoveText || 'Breakthrough';
    } else if (ctx.selectedBreakthroughPiece) {
      hint.textContent = `Pawn (${ctx.selectedBreakthroughPiece.x}, ${ctx.selectedBreakthroughPiece.y}) selected — tap forward square`;
    } else {
      hint.textContent = 'Tap one of your pawns, then tap 1 square forward or diagonally forward';
    }
    wrapper.appendChild(hint);

    const grid = document.createElement('div');
    grid.className = 'bt-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const val = (session.grid[y] && session.grid[y][x]) ? session.grid[y][x] : ' ';
        const isAlt = (x + y) % 2 === 1;
        const isSelected = ctx.selectedBreakthroughPiece && ctx.selectedBreakthroughPiece.x === x && ctx.selectedBreakthroughPiece.y === y;
        const isLastMove = session.lastMoveX === x && session.lastMoveY === y;

        const cell = document.createElement('button');
        cell.type = 'button';
        cell.className = 'bt-cell' +
          (isAlt ? ' alt-square' : '') +
          (isSelected ? ' selected' : '') +
          (isLastMove ? ' last-move' : '');

        if (val === 'W' || val === 'B') {
          const piece = document.createElement('span');
          piece.className = val === 'W' ? 'bt-piece-w' : 'bt-piece-b';
          cell.appendChild(piece);
        }

        const isOwn = session.currentTurnIndex === 0 ? val === 'W' : val === 'B';
        if (canPlay) {
          cell.addEventListener('click', () => {
            if (isOwn) {
              ctx.selectedBreakthroughPiece = (ctx.selectedBreakthroughPiece && ctx.selectedBreakthroughPiece.x === x && ctx.selectedBreakthroughPiece.y === y)
                ? null
                : { x, y };
              ctx.playSound('tap');
              renderBoard(session);
            } else if (ctx.selectedBreakthroughPiece) {
              const fromX = ctx.selectedBreakthroughPiece.x;
              const fromY = ctx.selectedBreakthroughPiece.y;
              ctx.selectedBreakthroughPiece = null;
              ctx.submitPieceMove(fromX, fromY, x, y);
            }
          });
        } else {
          cell.disabled = true;
        }

        grid.appendChild(cell);
      }
    }

    wrapper.appendChild(grid);
    ctx.boardContainer.appendChild(wrapper);
  }

  function renderMancala(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'checkers-wrapper';
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    const hint = document.createElement('div');
    hint.className = 'checkers-hint';
    hint.textContent = session.lastMoveText || 'Tap a pit on your row to sow stones counter-clockwise';
    wrapper.appendChild(hint);

    const board = document.createElement('div');
    board.className = 'mancala-board';

    // Left Store = Player 2
    const p2Store = document.createElement('div');
    p2Store.className = 'mancala-store';
    p2Store.innerHTML = `<span class="mancala-store-name">${ctx.escapeHtml(session.player2Name)}</span><span class="mancala-store-val">${session.player2GamePoints || 0}</span>`;
    board.appendChild(p2Store);

    // Center 2x6 Pits
    const pitsWrap = document.createElement('div');
    pitsWrap.className = 'mancala-pits';
    for (let y = 0; y < 2; y++) {
      for (let x = 0; x < 6; x++) {
        const count = Number((session.grid[y] && session.grid[y][x]) || 0);
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'mancala-pit';
        btn.textContent = String(count);
        const isOwnRow = (session.currentTurnIndex === 0 && y === 1) || (session.currentTurnIndex === 1 && y === 0);
        btn.disabled = !canPlay || !isOwnRow || count <= 0;
        btn.addEventListener('click', () => ctx.submitMove(x, y));
        pitsWrap.appendChild(btn);
      }
    }
    board.appendChild(pitsWrap);

    // Right Store = Player 1
    const p1Store = document.createElement('div');
    p1Store.className = 'mancala-store';
    p1Store.innerHTML = `<span class="mancala-store-name">${ctx.escapeHtml(session.player1Name)}</span><span class="mancala-store-val">${session.player1GamePoints || 0}</span>`;
    board.appendChild(p1Store);

    wrapper.appendChild(board);
    ctx.boardContainer.appendChild(wrapper);
  }

  function renderWordle(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'wordle-wrapper';
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    // HUD
    const hud = document.createElement('div');
    hud.className = 'sudoku-hud';
    hud.innerHTML = `
      <span class="scrabble-score-pill active">${ctx.escapeHtml(session.player1Name)}: <strong>${session.player1GamePoints || 0} pts</strong></span>
      <button type="button" class="btn btn-xs btn-secondary wordle-lb-btn">🏆 Leaderboard</button>
    `;
    const lbBtn = hud.querySelector('.wordle-lb-btn');
    if (lbBtn) {
      lbBtn.addEventListener('click', () => ctx.openLeaderboard('wordle', session.mode === 'daily' ? 'daily' : 'all'));
    }
    wrapper.appendChild(hud);

    // Find active row & letter color map
    let activeRow = session.height;
    const keyStates = {};
    const priority = { green: 3, yellow: 2, gray: 1 };

    for (let y = 0; y < session.height; y++) {
      const first = (session.grid[y] && session.grid[y][0]) ? session.grid[y][0].trim() : '';
      if (!first && activeRow === session.height) {
        activeRow = y;
      }
    }

    const grid = document.createElement('div');
    grid.className = 'wordle-grid';
    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const raw = (session.grid[y] && session.grid[y][x]) ? session.grid[y][x].trim() : '';
        const cell = document.createElement('div');
        cell.className = 'wordle-cell';
        if (raw.length === 2) {
          const code = raw[0];
          const ch = raw[1];
          const cls = code === 'G' ? 'green' : (code === 'Y' ? 'yellow' : 'gray');
          cell.classList.add(cls);
          cell.textContent = ch;
          if (!keyStates[ch] || priority[cls] > priority[keyStates[ch]]) {
            keyStates[ch] = cls;
          }
        } else if (canPlay && y === activeRow && x < ctx.wordleCurrentGuess.length) {
          cell.classList.add('filled');
          cell.textContent = ctx.wordleCurrentGuess[x];
        }
        grid.appendChild(cell);
      }
    }
    wrapper.appendChild(grid);

    // On-screen QWERTY Keyboard
    const kbRows = [
      ['Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P'],
      ['A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L'],
      ['ENTER', 'Z', 'X', 'C', 'V', 'B', 'N', 'M', '⌫']
    ];
    const kb = document.createElement('div');
    kb.className = 'wordle-keyboard';

    const submitWordleGuess = () => {
      if (!canPlay || activeRow >= session.height || ctx.wordleCurrentGuess.length !== 5) return;
      const placements = Array.from(ctx.wordleCurrentGuess).map((ch, x) => ({ x, y: activeRow, letter: ch }));
      ctx.submitWordMove(placements, false);
    };

    kbRows.forEach(rowKeys => {
      const rowEl = document.createElement('div');
      rowEl.className = 'wordle-kb-row';
      rowKeys.forEach(k => {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'wordle-key' + (k.length > 1 || k === '⌫' ? ' wide' : '');
        if (k.length === 1 && keyStates[k]) {
          btn.classList.add(keyStates[k]);
        }
        btn.textContent = k;
        btn.disabled = !canPlay;
        btn.addEventListener('click', () => {
          if (!canPlay) return;
          if (k === 'ENTER') {
            submitWordleGuess();
          } else if (k === '⌫') {
            if (ctx.wordleCurrentGuess.length > 0) {
              ctx.wordleCurrentGuess = ctx.wordleCurrentGuess.slice(0, -1);
              ctx.playSound('tap');
              renderBoard(session);
            }
          } else if (ctx.wordleCurrentGuess.length < 5) {
            ctx.wordleCurrentGuess += k;
            ctx.playSound('tap');
            renderBoard(session);
          }
        });
        rowEl.appendChild(btn);
      });
      kb.appendChild(rowEl);
    });
    wrapper.appendChild(kb);
    ctx.boardContainer.appendChild(wrapper);
  }

  function renderMinesweeper(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'ms-wrapper';
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    const toolbar = document.createElement('div');
    toolbar.className = 'ms-toolbar';

    const scorePill = document.createElement('span');
    scorePill.className = 'scrabble-score-pill active';
    scorePill.innerHTML = `Score: <strong>${session.player1GamePoints || 0}</strong>`;

    const btnFlag = document.createElement('button');
    btnFlag.type = 'button';
    btnFlag.className = `btn btn-xs ${ctx.minesweeperFlagMode ? 'btn-primary' : 'btn-secondary'}`;
    btnFlag.textContent = ctx.minesweeperFlagMode ? '🚩 Flag Mode: ON' : '⛏️ Reveal Mode';
    btnFlag.disabled = !canPlay;
    btnFlag.addEventListener('click', () => {
      ctx.minesweeperFlagMode = !ctx.minesweeperFlagMode;
      ctx.playSound('tap');
      renderBoard(session);
    });

    const btnLb = document.createElement('button');
    btnLb.type = 'button';
    btnLb.className = 'btn btn-xs btn-secondary';
    btnLb.textContent = '🏆 Ranks';
    btnLb.addEventListener('click', () => ctx.openLeaderboard('minesweeper', session.mode === 'daily' ? 'daily' : 'all'));

    toolbar.appendChild(scorePill);
    toolbar.appendChild(btnFlag);
    toolbar.appendChild(btnLb);
    wrapper.appendChild(toolbar);

    const grid = document.createElement('div');
    grid.className = 'ms-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const val = (session.grid[y] && session.grid[y][x]) ? session.grid[y][x].trim() : 'H';
        const cell = document.createElement('button');
        cell.type = 'button';
        cell.className = 'ms-cell';

        if (val === 'H') {
          cell.textContent = '';
        } else if (val === 'F') {
          cell.textContent = '🚩';
        } else if (val === 'M' || val === '*') {
          cell.classList.add('revealed', 'mine');
          cell.textContent = val === 'M' ? '💥' : '💣';
        } else {
          cell.classList.add('revealed');
          if (val !== '0') {
            cell.classList.add(`n${val}`);
            cell.textContent = val;
          }
        }

        if (canPlay && (val === 'H' || val === 'F')) {
          cell.addEventListener('click', () => {
            if (ctx.minesweeperFlagMode) {
              ctx.submitSudokuMove(x, y, 1);
            } else if (val === 'H') {
              ctx.submitMove(x, y);
            }
          });
          cell.addEventListener('contextmenu', (e) => {
            e.preventDefault();
            ctx.submitSudokuMove(x, y, 1);
          });
        } else {
          cell.disabled = true;
        }

        grid.appendChild(cell);
      }
    }

    wrapper.appendChild(grid);
    ctx.boardContainer.appendChild(wrapper);
  }

  function renderSudoku(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'sudoku-wrapper';

    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    // HUD
    const hud = document.createElement('div');
    hud.className = 'sudoku-hud';
    const isSoloDaily = session.mode === 'daily' || session.mode === 'solo';
    if (isSoloDaily) {
      hud.innerHTML = `
        <span class="scrabble-score-pill active">${ctx.escapeHtml(session.player1Name)}: <strong>${session.player1GamePoints || 0} pts</strong></span>
        <button type="button" class="btn btn-xs btn-secondary sudoku-lb-btn">🏆 Leaderboard</button>
      `;
    } else {
      hud.innerHTML = `
        <span class="scrabble-score-pill ${session.currentTurnIndex === 0 ? 'active' : ''}">${ctx.escapeHtml(session.player1Name)}: <strong>${session.player1GamePoints || 0}</strong></span>
        <button type="button" class="btn btn-xs btn-secondary sudoku-lb-btn">🏆 Ranks</button>
        <span class="scrabble-score-pill ${session.currentTurnIndex === 1 ? 'active' : ''}">${ctx.escapeHtml(session.player2Name)}: <strong>${session.player2GamePoints || 0}</strong></span>
      `;
    }
    const lbShortcut = hud.querySelector('.sudoku-lb-btn');
    if (lbShortcut) {
      lbShortcut.addEventListener('click', () => {
        ctx.openLeaderboard('sudoku', session.mode === 'daily' ? 'daily' : 'all');
      });
    }
    wrapper.appendChild(hud);

    // 9x9 Grid
    const grid = document.createElement('div');
    grid.className = 'sudoku-grid';

    for (let y = 0; y < 9; y++) {
      for (let x = 0; x < 9; x++) {
        const rawVal = (session.grid[y] && session.grid[y][x]) ? session.grid[y][x] : ' ';
        const isGiven = typeof rawVal === 'string' && rawVal.startsWith('G') && rawVal.length === 2;
        const digit = isGiven ? rawVal[1] : (rawVal && rawVal !== ' ' ? rawVal : '');
        const isSelected = ctx.selectedSudokuCell && ctx.selectedSudokuCell.x === x && ctx.selectedSudokuCell.y === y;
        const isSameDigit = ctx.highlightedSudokuDigit && digit === ctx.highlightedSudokuDigit;
        const isLastMove = session.lastMoveX === x && session.lastMoveY === y;

        const cell = document.createElement('button');
        cell.type = 'button';
        cell.className = 'sudoku-cell' +
          (isGiven ? ' given' : (digit ? ' placed' : '')) +
          (x === 2 || x === 5 ? ' subgrid-right' : '') +
          (y === 2 || y === 5 ? ' subgrid-bottom' : '') +
          (isSelected ? ' selected' : '') +
          (isSameDigit ? ' same-digit' : '') +
          (isLastMove ? ' last-move' : '');
        cell.textContent = digit;
        cell.setAttribute('aria-label', `Row ${y + 1}, Column ${x + 1}${digit ? `, ${digit}` : ''}`);

        cell.addEventListener('click', () => {
          if (digit) {
            ctx.highlightedSudokuDigit = ctx.highlightedSudokuDigit === digit ? null : digit;
            ctx.selectedSudokuCell = null;
            ctx.playSound('tap');
            renderBoard(session);
          } else if (canPlay) {
            ctx.selectedSudokuCell = { x, y };
            ctx.highlightedSudokuDigit = null;
            ctx.playSound('tap');
            renderBoard(session);
          }
        });

        grid.appendChild(cell);
      }
    }
    wrapper.appendChild(grid);

    // 1-9 Number Pad
    const numpad = document.createElement('div');
    numpad.className = 'sudoku-numpad';
    for (let d = 1; d <= 9; d++) {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'sudoku-num-btn';
      btn.textContent = String(d);
      btn.disabled = !canPlay || !ctx.selectedSudokuCell;
      btn.addEventListener('click', () => {
        if (!canPlay || !ctx.selectedSudokuCell) return;
        const { x, y } = ctx.selectedSudokuCell;
        ctx.submitSudokuMove(x, y, d);
      });
      numpad.appendChild(btn);
    }
    wrapper.appendChild(numpad);

    ctx.boardContainer.appendChild(wrapper);
  }

  function isOwnCheckerPiece(val, turnIndex) {
    if (turnIndex === 0) return val === 'R' || val === 'RK';
    return val === 'B' || val === 'BK';
  }

  function renderCheckers(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'checkers-wrapper';

    const hint = document.createElement('div');
    hint.className = 'checkers-hint';
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;
    if (!canPlay) {
      hint.textContent = session.lastMoveText || 'Checkers';
    } else if (ctx.selectedChecker) {
      hint.textContent = `Piece (${ctx.selectedChecker.x}, ${ctx.selectedChecker.y}) selected — tap destination square`;
    } else {
      hint.textContent = 'Tap one of your pieces, then tap destination (captures mandatory)';
    }
    wrapper.appendChild(hint);

    const grid = document.createElement('div');
    grid.className = 'checkers-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const val = (session.grid[y] && session.grid[y][x]) ? session.grid[y][x] : ' ';
        const isDark = (x + y) % 2 === 1;
        const isSelected = ctx.selectedChecker && ctx.selectedChecker.x === x && ctx.selectedChecker.y === y;
        const isLastMove = session.lastMoveX === x && session.lastMoveY === y;

        const cell = document.createElement('button');
        cell.type = 'button';
        cell.className = 'checkers-cell ' + (isDark ? 'dark-square' : 'light-square') +
          (isSelected ? ' selected' : '') +
          (isLastMove ? ' last-move' : '');
        cell.setAttribute('aria-label', `Row ${y + 1}, Column ${x + 1}`);

        if (val === 'R' || val === 'RK' || val === 'B' || val === 'BK') {
          const piece = document.createElement('span');
          const isRed = val === 'R' || val === 'RK';
          const isKing = val === 'RK' || val === 'BK';
          piece.className = (isRed ? 'checkers-piece-r' : 'checkers-piece-b') + (isKing ? ' checkers-king' : '');
          if (isKing) {
            piece.textContent = '👑';
          }
          cell.appendChild(piece);
        }

        if (canPlay && isDark) {
          cell.addEventListener('click', () => {
            if (isOwnCheckerPiece(val, session.currentTurnIndex)) {
              if (ctx.selectedChecker && ctx.selectedChecker.x === x && ctx.selectedChecker.y === y) {
                ctx.selectedChecker = null;
              } else {
                ctx.selectedChecker = { x, y };
              }
              ctx.playSound('tap');
              renderBoard(session);
            } else if (ctx.selectedChecker && (val === '' || val === ' ')) {
              const fromX = ctx.selectedChecker.x;
              const fromY = ctx.selectedChecker.y;
              ctx.selectedChecker = null;
              ctx.submitPieceMove(fromX, fromY, x, y);
            }
          });
        } else {
          cell.disabled = true;
        }

        grid.appendChild(cell);
      }
    }

    wrapper.appendChild(grid);
    ctx.boardContainer.appendChild(wrapper);
  }

  const SCRABBLE_LETTER_POINTS = {
    A: 1, B: 3, C: 3, D: 2, E: 1, F: 4, G: 2, H: 4, I: 1, J: 8, K: 5, L: 1, M: 3,
    N: 1, O: 1, P: 3, Q: 10, R: 1, S: 1, T: 1, U: 1, V: 4, W: 4, X: 8, Y: 4, Z: 10
  };

  function renderScrabble(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'scrabble-wrapper';

    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;
    const rack = Array.isArray(session.currentPlayerRack) ? session.currentPlayerRack : [];
    const usedIndices = new Set(ctx.pendingScrabblePlacements.map(p => p.rackIndex));

    // Score & Bag HUD
    const hud = document.createElement('div');
    hud.className = 'scrabble-hud';
    hud.innerHTML = `
      <span class="scrabble-score-pill ${session.currentTurnIndex === 0 ? 'active' : ''}">${ctx.escapeHtml(session.player1Name)}: <strong>${session.player1GamePoints || 0}</strong></span>
      <span class="scrabble-bag-pill">Bag: <strong>${session.tileBagCount || 0}</strong></span>
      <span class="scrabble-score-pill ${session.currentTurnIndex === 1 ? 'active' : ''}">${ctx.escapeHtml(session.player2Name)}: <strong>${session.player2GamePoints || 0}</strong></span>
    `;
    wrapper.appendChild(hud);

    // 15x15 Board
    const grid = document.createElement('div');
    grid.className = 'scrabble-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const boardVal = (session.grid[y] && session.grid[y][x]) ? session.grid[y][x] : ' ';
        const pending = ctx.pendingScrabblePlacements.find(p => p.x === x && p.y === y);
        const bonus = (session.bonusGrid && session.bonusGrid[y] && session.bonusGrid[y][x]) ? session.bonusGrid[y][x] : '';
        const isCenter = x === Math.floor(session.width / 2) && y === Math.floor(session.height / 2);

        const cell = document.createElement('button');
        cell.type = 'button';
        cell.className = 'scrabble-cell' +
          (bonus ? ` bonus-${bonus.toLowerCase()}` : '') +
          (isCenter ? ' center-star' : '');
        cell.setAttribute('aria-label', `Row ${y + 1}, Column ${x + 1}`);

        if (boardVal && boardVal !== ' ') {
          const tile = document.createElement('span');
          tile.className = 'scrabble-tile';
          const pts = SCRABBLE_LETTER_POINTS[boardVal.toUpperCase()] || 0;
          tile.innerHTML = `${ctx.escapeHtml(boardVal)}<sub>${pts}</sub>`;
          cell.appendChild(tile);
          cell.disabled = true;
        } else if (pending) {
          const tile = document.createElement('span');
          tile.className = 'scrabble-tile pending-tile';
          const pts = SCRABBLE_LETTER_POINTS[pending.letter.toUpperCase()] || 0;
          tile.innerHTML = `${ctx.escapeHtml(pending.letter)}<sub>${pts}</sub>`;
          cell.appendChild(tile);
          if (canPlay) {
            cell.addEventListener('click', () => {
              ctx.pendingScrabblePlacements = ctx.pendingScrabblePlacements.filter(p => !(p.x === x && p.y === y));
              ctx.playSound('tap');
              renderBoard(session);
            });
          } else {
            cell.disabled = true;
          }
        } else {
          if (isCenter) {
            cell.textContent = '★';
          } else if (bonus) {
            cell.textContent = bonus;
          }
          if (canPlay) {
            cell.addEventListener('click', () => {
              if (ctx.selectedScrabbleTileIndex === null || usedIndices.has(ctx.selectedScrabbleTileIndex)) {
                // Auto-select first unused rack tile if user taps the board first
                const nextIdx = rack.findIndex((_, idx) => !usedIndices.has(idx));
                if (nextIdx === -1) return;
                ctx.selectedScrabbleTileIndex = nextIdx;
              }
              const letter = rack[ctx.selectedScrabbleTileIndex];
              if (!letter) return;
              ctx.pendingScrabblePlacements.push({ x, y, letter, rackIndex: ctx.selectedScrabbleTileIndex });
              ctx.selectedScrabbleTileIndex = null;
              ctx.playSound('tap');
              renderBoard(session);
            });
          } else {
            cell.disabled = true;
          }
        }

        grid.appendChild(cell);
      }
    }
    wrapper.appendChild(grid);

    // Tile Rack
    const rackBar = document.createElement('div');
    rackBar.className = 'scrabble-rack-bar';
    if (rack.length === 0) {
      rackBar.innerHTML = '<span class="scrabble-rack-empty">Rack empty</span>';
    } else {
      rack.forEach((letter, idx) => {
        const isUsed = usedIndices.has(idx);
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'scrabble-rack-tile' +
          (idx === ctx.selectedScrabbleTileIndex ? ' selected' : '') +
          (isUsed ? ' used' : '');
        btn.disabled = !canPlay || isUsed;
        const pts = SCRABBLE_LETTER_POINTS[letter.toUpperCase()] || 0;
        btn.innerHTML = `${ctx.escapeHtml(letter)}<sub>${pts}</sub>`;
        btn.addEventListener('click', () => {
          ctx.selectedScrabbleTileIndex = (ctx.selectedScrabbleTileIndex === idx) ? null : idx;
          ctx.playSound('tap');
          renderBoard(session);
        });
        rackBar.appendChild(btn);
      });
    }
    wrapper.appendChild(rackBar);

    // Action Buttons
    const actions = document.createElement('div');
    actions.className = 'scrabble-actions';

    const btnSubmit = document.createElement('button');
    btnSubmit.type = 'button';
    btnSubmit.className = 'btn btn-sm btn-primary';
    btnSubmit.textContent = 'Submit Word';
    btnSubmit.disabled = !canPlay || ctx.pendingScrabblePlacements.length === 0;
    btnSubmit.addEventListener('click', () => {
      const payload = ctx.pendingScrabblePlacements.map(p => ({ x: p.x, y: p.y, letter: p.letter }));
      ctx.submitWordMove(payload, false);
    });

    const btnClear = document.createElement('button');
    btnClear.type = 'button';
    btnClear.className = 'btn btn-sm btn-secondary';
    btnClear.textContent = 'Clear';
    btnClear.disabled = !canPlay || ctx.pendingScrabblePlacements.length === 0;
    btnClear.addEventListener('click', () => {
      ctx.pendingScrabblePlacements = [];
      ctx.selectedScrabbleTileIndex = null;
      ctx.playSound('tap');
      renderBoard(session);
    });

    const btnPass = document.createElement('button');
    btnPass.type = 'button';
    btnPass.className = 'btn btn-sm btn-secondary';
    btnPass.textContent = 'Pass Turn';
    btnPass.disabled = !canPlay;
    btnPass.addEventListener('click', () => {
      ctx.pendingScrabblePlacements = [];
      ctx.selectedScrabbleTileIndex = null;
      ctx.submitWordMove([], true);
    });

    actions.appendChild(btnSubmit);
    actions.appendChild(btnClear);
    actions.appendChild(btnPass);
    wrapper.appendChild(actions);

    ctx.boardContainer.appendChild(wrapper);
  }

  function renderGomoku(session) {
    const grid = document.createElement('div');
    grid.className = 'gomoku-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const val = session.grid[y] && session.grid[y][x] ? session.grid[y][x] : '';
        const isLastMove = session.lastMoveX === x && session.lastMoveY === y;
        const cell = document.createElement('button');
        cell.className = 'gomoku-cell' + (isLastMove ? ' last-move' : '');
        cell.disabled = !canPlay || (val !== '' && val !== ' ');
        cell.setAttribute('aria-label', `Row ${y + 1}, Column ${x + 1}`);

        if (val === 'B') {
          const stone = document.createElement('span');
          stone.className = 'gomoku-stone-b';
          cell.appendChild(stone);
        } else if (val === 'W') {
          const stone = document.createElement('span');
          stone.className = 'gomoku-stone-w';
          cell.appendChild(stone);
        }

        if (canPlay && (val === '' || val === ' ')) {
          cell.addEventListener('click', () => ctx.submitMove(x, y));
        }

        grid.appendChild(cell);
      }
    }

    ctx.boardContainer.appendChild(grid);
  }

  function renderOthello(session) {
    const grid = document.createElement('div');
    grid.className = 'othello-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const val = session.grid[y] && session.grid[y][x] ? session.grid[y][x] : '';
        const isLastMove = session.lastMoveX === x && session.lastMoveY === y;
        const cell = document.createElement('button');
        cell.className = 'othello-cell' + (isLastMove ? ' last-move' : '');
        cell.disabled = !canPlay || (val !== '' && val !== ' ');
        cell.setAttribute('aria-label', `Row ${y + 1}, Column ${x + 1}`);

        if (val === 'B') {
          const disc = document.createElement('span');
          disc.className = 'gomoku-stone-b';
          cell.appendChild(disc);
        } else if (val === 'W') {
          const disc = document.createElement('span');
          disc.className = 'gomoku-stone-w';
          cell.appendChild(disc);
        }

        if (canPlay && (val === '' || val === ' ')) {
          cell.addEventListener('click', () => ctx.submitMove(x, y));
        }

        grid.appendChild(cell);
      }
    }

    ctx.boardContainer.appendChild(grid);
  }

  function renderNim(session) {
    const grid = document.createElement('div');
    grid.className = 'nim-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const val = session.grid[y] && session.grid[y][x] ? session.grid[y][x] : '';
        const hasStone = val === 'S';
        const cell = document.createElement('button');
        cell.className = 'nim-cell' + (hasStone ? '' : ' nim-empty');
        cell.disabled = !canPlay || !hasStone;
        cell.setAttribute('aria-label', `Row ${y + 1}, Stone ${x + 1}`);

        if (canPlay && hasStone) {
          cell.addEventListener('click', () => ctx.submitMove(x, y));
        }

        grid.appendChild(cell);
      }
    }

    ctx.boardContainer.appendChild(grid);
  }

  function renderHex(session) {
    const grid = document.createElement('div');
    grid.className = 'hex-grid';

    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    for (let y = 0; y < session.height; y++) {
      const row = document.createElement('div');
      row.className = 'hex-row';
      row.style.marginLeft = `${y * 16}px`;

      for (let x = 0; x < session.width; x++) {
        const val = session.grid[y] && session.grid[y][x] ? session.grid[y][x] : '';
        const isLastMove = session.lastMoveX === x && session.lastMoveY === y;
        const cell = document.createElement('button');
        cell.className = 'hex-cell' + (isLastMove ? ' last-move' : '');
        cell.disabled = !canPlay || (val !== '' && val !== ' ');
        cell.setAttribute('aria-label', `Row ${y + 1}, Column ${x + 1}`);

        if (val === 'B') {
          const stone = document.createElement('span');
          stone.className = 'gomoku-stone-b';
          cell.appendChild(stone);
        } else if (val === 'W') {
          const stone = document.createElement('span');
          stone.className = 'gomoku-stone-w';
          cell.appendChild(stone);
        }

        if (canPlay && (val === '' || val === ' ')) {
          cell.addEventListener('click', () => ctx.submitMove(x, y));
        }

        row.appendChild(cell);
      }
      grid.appendChild(row);
    }

    ctx.boardContainer.appendChild(grid);
  }

  function renderConnectFour(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'c4-wrapper';
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    // Top column drop buttons
    const btnRow = document.createElement('div');
    btnRow.className = 'c4-column-buttons';
    for (let col = 0; col < session.width; col++) {
      const btn = document.createElement('button');
      btn.className = 'c4-col-btn';
      btn.innerHTML = '▼';
      btn.title = `Drop in column ${col + 1}`;
      btn.disabled = !canPlay;
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        if (canPlay) ctx.submitMove(col, 0);
      });
      btnRow.appendChild(btn);
    }
    wrapper.appendChild(btnRow);

    // 7x6 Grid Rack
    const rack = document.createElement('div');
    rack.className = 'c4-grid-rack';
    rack.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;
    rack.style.gridTemplateRows = `repeat(${session.height}, 1fr)`;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const slot = document.createElement('div');
        slot.className = 'c4-slot';
        slot.dataset.col = x;

        // Allow tapping anywhere on a column to drop
        slot.addEventListener('click', () => {
          if (canPlay) {
            ctx.submitMove(x, 0);
          }
        });

        const isLastMove = (session.lastMoveX !== null && session.lastMoveY !== null &&
                            x === session.lastMoveX && y === session.lastMoveY);

        const piece = (session.grid && session.grid[y] && session.grid[y][x]) || ' ';
        if (piece === 'R') {
          const chip = document.createElement('div');
          chip.className = 'c4-chip red' + (isLastMove ? ' last-move' : '');
          slot.appendChild(chip);
        } else if (piece === 'Y') {
          const chip = document.createElement('div');
          chip.className = 'c4-chip yellow' + (isLastMove ? ' last-move' : '');
          slot.appendChild(chip);
        }

        rack.appendChild(slot);
      }
    }
    wrapper.appendChild(rack);
    ctx.boardContainer.appendChild(wrapper);
  }

  function renderTicTacToe(session) {
    const grid = document.createElement('div');
    grid.className = 'ttt-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;
    grid.style.gridTemplateRows = `repeat(${session.height}, 1fr)`;
    if (session.width >= 5) {
      grid.classList.add('ttt-5x5');
    } else if (session.width >= 4) {
      grid.classList.add('ttt-4x4');
    }

    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const cell = document.createElement('div');
        const isLastMove = (session.lastMoveX !== null && session.lastMoveY !== null &&
                            x === session.lastMoveX && y === session.lastMoveY);
        cell.className = 'ttt-cell' + (isLastMove ? ' last-move' : '');
        const piece = (session.grid && session.grid[y] && session.grid[y][x]) || ' ';

        if (piece === 'X') {
          cell.innerHTML = '<span class="ttt-x">✕</span>';
        } else if (piece === 'O') {
          cell.innerHTML = '<span class="ttt-o">◯</span>';
        } else {
          cell.addEventListener('click', () => {
            if (canPlay) {
              ctx.submitMove(x, y);
            }
          });
        }

        grid.appendChild(cell);
      }
    }
    ctx.boardContainer.appendChild(grid);
  }

  function renderChomp(session) {
    const grid = document.createElement('div');
    grid.className = 'chomp-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;
    grid.style.gridTemplateRows = `repeat(${session.height}, 1fr)`;

    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !ctx.isSpectator && ctx.replayIndex === null;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const block = document.createElement('div');
        const isLastMove = (session.lastMoveX !== null && session.lastMoveY !== null &&
                            x === session.lastMoveX && y === session.lastMoveY);
        block.className = 'chomp-block' + (isLastMove ? ' last-move' : '');
        const piece = (session.grid && session.grid[y] && session.grid[y][x]) || ' ';

        if (piece === 'P') {
          block.classList.add('chomp-poison');
          block.innerHTML = '☠️';
          block.title = 'Poison Block! (Eating this loses the game)';
          block.addEventListener('click', () => {
            if (canPlay) {
              ctx.submitMove(x, y);
            }
          });
        } else if (piece === 'C') {
          block.title = `Chomp at (${x}, ${y})`;
          block.addEventListener('click', () => {
            if (canPlay) {
              ctx.submitMove(x, y);
            }
          });
        } else {
          block.classList.add('chomp-eaten');
        }

        grid.appendChild(block);
      }
    }
    ctx.boardContainer.appendChild(grid);
  }

  return { renderBoard };
}
