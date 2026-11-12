// ============================================================================
// BoardGame Web Client
// ============================================================================

(function () {
  'use strict';

  // State
  let currentSession = null;
  let playerToken = localStorage.getItem('bg_player_token') || null;
  let sseSource = null;
  let soundEnabled = localStorage.getItem('bg_sound') !== 'false';
  let audioCtx = null;

  // Selected config in modal
  let selectedGame = 'connect4';
  let selectedMode = 'pass-and-play';
  let selectedDiff = 'smart';
  let roomAction = 'host';

  // DOM Elements
  const gameTitle = document.getElementById('game-title');
  const modeBadge = document.getElementById('mode-badge');
  const statusText = document.getElementById('status-text');
  const turnDot = document.getElementById('turn-dot');
  const roomCodeTag = document.getElementById('room-code-tag');
  const roomCodeVal = document.getElementById('room-code-val');
  const boardContainer = document.getElementById('board-container');

  const btnOpenMenu = document.getElementById('btn-open-menu');
  const btnCloseMenu = document.getElementById('btn-close-menu');
  const modalMenu = document.getElementById('modal-menu');

  const btnShare = document.getElementById('btn-share');
  const btnCloseShare = document.getElementById('btn-close-share');
  const modalShare = document.getElementById('modal-share');
  const shareUrlText = document.getElementById('share-url-text');
  const btnCopyUrl = document.getElementById('btn-copy-url');
  const copyFeedback = document.getElementById('copy-feedback');
  const qrContainer = document.getElementById('qr-container');

  const btnSound = document.getElementById('btn-sound');
  const iconSoundOn = document.getElementById('icon-sound-on');
  const iconSoundOff = document.getElementById('icon-sound-off');

  const btnReset = document.getElementById('btn-reset');
  const btnNewGame = document.getElementById('btn-new-game');
  const btnStartGame = document.getElementById('btn-start-game');

  const gameOverModal = document.getElementById('game-over-modal');
  const gameOverTitle = document.getElementById('game-over-title');
  const gameOverMsg = document.getElementById('game-over-msg');
  const gameOverIcon = document.getElementById('game-over-icon');
  const btnPlayAgain = document.getElementById('btn-play-again');
  const btnChangeGame = document.getElementById('btn-change-game');

  const secAiDiff = document.getElementById('sec-ai-diff');
  const secRoom = document.getElementById('sec-room');
  const tabHost = document.getElementById('tab-host');
  const tabJoin = document.getElementById('tab-join');
  const roomHostOptions = document.getElementById('room-host-options');
  const roomJoinBox = document.getElementById('room-join-box');
  const chkSharePublic = document.getElementById('chk-share-public');
  const lobbyRoomList = document.getElementById('lobby-room-list');
  const btnRefreshRooms = document.getElementById('btn-refresh-rooms');
  const inputRoomCode = document.getElementById('input-room-code');
  const inputP1 = document.getElementById('input-p1');
  const inputP2 = document.getElementById('input-p2');

  // Normalize session object to handle both camelCase and PascalCase
  function normalizeSession(s) {
    if (!s) return null;
    const rawGame = s.gameType || s.GameType || 'connect4';
    let gameType = 'connect4';
    const gl = String(rawGame).toLowerCase();
    if (gl.includes('tic') || gl.includes('ttt')) gameType = 'tictactoe';
    else if (gl.includes('chomp')) gameType = 'chomp';
    else gameType = 'connect4';

    const width = Number(s.width || s.Width || (gameType === 'tictactoe' ? 3 : (gameType === 'chomp' ? 5 : 7)));
    const height = Number(s.height || s.Height || (gameType === 'tictactoe' ? 3 : (gameType === 'chomp' ? 4 : 6)));
    const rawGrid = s.grid || s.Grid;
    let grid = [];
    if (Array.isArray(rawGrid) && rawGrid.length >= height) {
      grid = rawGrid;
    } else {
      // Generate default blank grid
      for (let y = 0; y < height; y++) {
        grid.push(new Array(width).fill(' '));
      }
      if (gameType === 'chomp') {
        grid[0][0] = 'P';
        for (let y = 0; y < height; y++) {
          for (let x = 0; x < width; x++) {
            if (x !== 0 || y !== 0) grid[y][x] = 'C';
          }
        }
      }
    }

    return {
      id: s.id || s.Id || 'session-default',
      roomCode: s.roomCode || s.RoomCode || '',
      gameType: gameType,
      mode: (s.mode || s.Mode || 'pass-and-play').toLowerCase(),
      width: width,
      height: height,
      grid: grid,
      status: s.status || s.Status || 'InProgress',
      currentTurnPlayer: s.currentTurnPlayer || s.CurrentTurnPlayer || 'Player 1',
      currentTurnIndex: s.currentTurnIndex !== undefined ? s.currentTurnIndex : (s.CurrentTurnIndex !== undefined ? s.CurrentTurnIndex : 0),
      winner: s.winner || s.Winner || null,
      lastMoveText: s.lastMoveText || s.LastMoveText || null,
      moveCount: s.moveCount !== undefined ? s.moveCount : (s.MoveCount || 0),
      player1Name: s.player1Name || s.Player1Name || 'Player 1',
      player2Name: s.player2Name || s.Player2Name || 'Player 2',
      isAiTurn: s.isAiTurn !== undefined ? s.isAiTurn : (s.IsAiTurn || false)
    };
  }

  // Initialize Audio
  function initAudio() {
    if (!audioCtx && (window.AudioContext || window.webkitAudioContext)) {
      audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    }
    if (audioCtx && audioCtx.state === 'suspended') {
      audioCtx.resume();
    }
  }

  function playTone(freq, duration, type = 'sine', gainVal = 0.15) {
    if (!soundEnabled) return;
    try {
      initAudio();
      if (!audioCtx) return;
      const osc = audioCtx.createOscillator();
      const gain = audioCtx.createGain();
      osc.type = type;
      osc.frequency.setValueAtTime(freq, audioCtx.currentTime);
      gain.gain.setValueAtTime(gainVal, audioCtx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + duration);
      osc.connect(gain);
      gain.connect(audioCtx.destination);
      osc.start();
      osc.stop(audioCtx.currentTime + duration);
    } catch (e) { }
  }

  function playSound(name) {
    if (!soundEnabled) return;
    switch (name) {
      case 'tap':
        playTone(480, 0.08, 'triangle', 0.2);
        break;
      case 'drop':
        playTone(320, 0.12, 'sine', 0.25);
        setTimeout(() => playTone(240, 0.1, 'sine', 0.2), 60);
        break;
      case 'chomp':
        playTone(180, 0.06, 'square', 0.2);
        setTimeout(() => playTone(120, 0.08, 'sawtooth', 0.2), 50);
        break;
      case 'win':
        [523.25, 659.25, 783.99, 1046.5].forEach((f, i) => {
          setTimeout(() => playTone(f, 0.25, 'triangle', 0.3), i * 110);
        });
        break;
      case 'draw':
        playTone(300, 0.15, 'sawtooth', 0.2);
        setTimeout(() => playTone(220, 0.3, 'sawtooth', 0.2), 150);
        break;
    }
  }

  function triggerHaptic(duration = 20) {
    if (navigator.vibrate) {
      try { navigator.vibrate(duration); } catch (e) { }
    }
  }

  // Update sound icon state
  function updateSoundUI() {
    if (soundEnabled) {
      iconSoundOn.classList.remove('hidden');
      iconSoundOff.classList.add('hidden');
    } else {
      iconSoundOn.classList.add('hidden');
      iconSoundOff.classList.remove('hidden');
    }
  }

  // Check URL parameters for direct room joining
  function parseUrlParams() {
    const params = new URLSearchParams(window.location.search);
    const roomParam = params.get('room');
    if (roomParam) {
      joinRoomByCode(roomParam.trim().toUpperCase());
      return true;
    }
    return false;
  }

  // Create new session
  async function createSession(game, mode, diff, p1, p2, isPublic = true) {
    try {
      const res = await fetch('/api/session/new', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          gameType: game,
          mode: mode,
          aiDifficulty: diff,
          player1Name: p1,
          player2Name: p2,
          isPublic: Boolean(isPublic)
        })
      });

      const text = await res.text();
      let data = null;
      try { data = text ? JSON.parse(text) : null; } catch (_) { }

      if (!res.ok || !data) throw new Error((data && data.error) || 'Failed to create session');

      playerToken = data.playerToken || data.PlayerToken;
      localStorage.setItem('bg_player_token', playerToken);
      const sessionObj = data.session || data.Session;
      onSessionUpdated(sessionObj);
      connectStream(sessionObj.id || sessionObj.Id);
    } catch (e) {
      console.warn('Network session create failed, using local session:', e);
      const fallback = normalizeSession({
        gameType: game,
        mode: mode,
        player1Name: p1 || 'Player 1',
        player2Name: p2 || 'Player 2'
      });
      onSessionUpdated(fallback);
    }
  }

  // Fetch available public Wi-Fi rooms
  async function fetchAvailableRooms() {
    if (!lobbyRoomList) return;
    lobbyRoomList.innerHTML = '<div class="lobby-empty">Searching for games on local Wi-Fi...</div>';
    try {
      const res = await fetch('/api/rooms');
      if (!res.ok) throw new Error('Failed to fetch rooms');
      const rooms = await res.json();
      renderRoomList(rooms);
    } catch (e) {
      lobbyRoomList.innerHTML = '<div class="lobby-empty">Could not load available games. You can enter a code below.</div>';
    }
  }

  function renderRoomList(rooms) {
    if (!lobbyRoomList) return;
    if (!Array.isArray(rooms) || rooms.length === 0) {
      lobbyRoomList.innerHTML = '<div class="lobby-empty">No games currently waiting on Wi-Fi.<br><small style="color:var(--text-dim);display:inline-block;margin-top:4px;">Host a game above or enter code below.</small></div>';
      return;
    }

    lobbyRoomList.innerHTML = '';
    const icons = { connect4: '🔴', tictactoe: '❌', chomp: '🍫' };
    const names = { connect4: 'Connect 4', tictactoe: 'Tic-Tac-Toe', chomp: 'Chomp' };

    rooms.forEach(r => {
      const code = r.roomCode || r.RoomCode;
      const game = (r.gameType || r.GameType || 'connect4').toLowerCase();
      const host = r.hostName || r.HostName || 'Host';
      const time = r.timeAgo || r.TimeAgo || 'just now';

      const card = document.createElement('div');
      card.className = 'lobby-room-card';
      card.innerHTML = `
        <div class="lobby-card-left">
          <span class="lobby-card-icon">${icons[game] || '🎲'}</span>
          <div class="lobby-card-info">
            <h4>${names[game] || game} <span class="lobby-code-badge">${code}</span></h4>
            <div class="lobby-card-meta">Host: ${host} • ${time}</div>
          </div>
        </div>
      `;

      const joinBtn = document.createElement('button');
      joinBtn.className = 'btn btn-sm btn-primary';
      joinBtn.textContent = 'Join';
      joinBtn.type = 'button';
      joinBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        modalMenu.classList.add('hidden');
        joinRoomByCode(code);
      });

      card.appendChild(joinBtn);
      lobbyRoomList.appendChild(card);
    });
  }

  // Join room by code
  async function joinRoomByCode(code) {
    if (!code || !code.trim()) {
      alert('Please enter a room code.');
      return;
    }
    const cleanCode = code.trim().toUpperCase();

    try {
      const res = await fetch('/api/room/join', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          roomCode: cleanCode,
          playerName: 'Guest'
        })
      });

      const text = await res.text();
      let data = null;
      try { data = text ? JSON.parse(text) : null; } catch (_) { }

      if (!res.ok) {
        const errorMsg = (data && data.error) ? data.error : (text || `Error joining room (HTTP ${res.status})`);
        alert('Could not join room:\n' + errorMsg);
        return;
      }

      if (!data) {
        alert('Server returned an empty response. Please try again.');
        return;
      }

      playerToken = data.playerToken || data.PlayerToken;
      localStorage.setItem('bg_player_token', playerToken);
      const sessionObj = data.session || data.Session;
      onSessionUpdated(sessionObj);
      connectStream(sessionObj.id || sessionObj.Id);
    } catch (e) {
      alert('Network error joining room: ' + e.message);
    }
  }

  // Connect Server-Sent Events stream for live real-time sync
  function connectStream(sessionId) {
    if (!sessionId) return;
    if (sseSource) {
      sseSource.close();
      sseSource = null;
    }
    try {
      sseSource = new EventSource(`/api/session/${sessionId}/stream`);
      sseSource.onmessage = function (event) {
        try {
          const state = JSON.parse(event.data);
          onSessionUpdated(state);
        } catch (e) { }
      };
      sseSource.onerror = function () {
        // Stream reconnection is handled automatically by EventSource
      };
    } catch (e) { }
  }

  // Submit move
  async function submitMove(x, y) {
    if (!currentSession || currentSession.status !== 'InProgress') return;
    initAudio();
    triggerHaptic(15);

    try {
      const res = await fetch(`/api/session/${currentSession.id}/move`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          x: Number(x),
          y: Number(y),
          playerToken: playerToken
        })
      });
      const data = await res.json();
      if (!res.ok) {
        triggerHaptic([30, 50, 30]);
        return;
      }
      const sessionObj = data.session || data.Session;
      if (sessionObj) onSessionUpdated(sessionObj);
    } catch (e) { }
  }

  // Reset current session
  async function resetSession() {
    if (!currentSession) return;
    try {
      const res = await fetch(`/api/session/${currentSession.id}/reset`, { method: 'POST' });
      if (res.ok) {
        const data = await res.json();
        const sessionObj = data.session || data.Session;
        if (sessionObj) onSessionUpdated(sessionObj);
        gameOverModal.classList.add('hidden');
      }
    } catch (e) { }
  }

  // Handle session updates
  function onSessionUpdated(rawSession) {
    const session = normalizeSession(rawSession);
    if (!session) return;

    const prevStatus = currentSession ? currentSession.status : null;
    const prevMoveCount = currentSession ? currentSession.moveCount : 0;
    currentSession = session;

    // Header info
    const gameNames = { connect4: 'Connect Four', tictactoe: 'Tic-Tac-Toe', chomp: 'Chomp' };
    const modeNames = { 'pass-and-play': 'Pass & Play', 'vs-ai': 'vs AI', 'room': 'Wi-Fi Room' };
    gameTitle.textContent = gameNames[session.gameType] || 'Board Game';
    modeBadge.textContent = modeNames[session.mode] || session.mode;

    // Room tag
    if (session.mode === 'room') {
      roomCodeTag.classList.remove('hidden');
      roomCodeVal.textContent = session.roomCode;
    } else {
      roomCodeTag.classList.add('hidden');
    }

    // Status Banner
    updateStatusBanner(session);

    // Render Board
    renderBoard(session);

    // Sound effect on new move
    if (session.moveCount > prevMoveCount) {
      if (session.gameType === 'chomp') playSound('chomp');
      else if (session.gameType === 'connect4') playSound('drop');
      else playSound('tap');
    }

    // Check game over transition
    if (prevStatus === 'InProgress' && (session.status === 'Finished' || session.status === 'Draw')) {
      handleGameOver(session);
    }
  }

  function updateStatusBanner(session) {
    turnDot.className = 'turn-dot';
    if (session.currentTurnIndex === 1) turnDot.classList.add('p2');
    if (session.isAiTurn) turnDot.classList.add('ai');

    if (session.status === 'WaitingForPlayer') {
      statusText.textContent = `Room ${session.roomCode} • Waiting for player...`;
    } else if (session.status === 'InProgress') {
      if (session.isAiTurn) {
        statusText.textContent = `${session.currentTurnPlayer} is thinking...`;
      } else {
        statusText.textContent = `${session.currentTurnPlayer}'s Turn`;
      }
    } else if (session.status === 'Finished') {
      statusText.textContent = `🎉 ${session.winner} Wins!`;
    } else if (session.status === 'Draw') {
      statusText.textContent = `🤝 Game Draw!`;
    }
  }

  function handleGameOver(session) {
    if (session.status === 'Finished') {
      playSound('win');
      triggerHaptic([50, 100, 50, 100, 150]);
      gameOverIcon.textContent = '🏆';
      gameOverTitle.textContent = `${session.winner} Wins!`;
      gameOverMsg.textContent = session.gameType === 'chomp' 
        ? 'Ate the poison block! Better luck next time.' 
        : 'Congratulations on an amazing match!';
    } else {
      playSound('draw');
      gameOverIcon.textContent = '🤝';
      gameOverTitle.textContent = `It's a Draw!`;
      gameOverMsg.textContent = 'Well played from both players!';
    }
    gameOverModal.classList.remove('hidden');
  }

  // Board Renderers
  function renderBoard(session) {
    boardContainer.innerHTML = '';
    boardContainer.className = `board-container ${session.gameType}-mode`;

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
      default:
        renderConnectFour(session);
        break;
    }
  }

  function renderConnectFour(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'c4-wrapper';

    // Top column drop buttons
    const btnRow = document.createElement('div');
    btnRow.className = 'c4-column-buttons';
    for (let col = 0; col < session.width; col++) {
      const btn = document.createElement('button');
      btn.className = 'c4-col-btn';
      btn.innerHTML = '▼';
      btn.title = `Drop in column ${col + 1}`;
      btn.disabled = session.status !== 'InProgress' || session.isAiTurn;
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        submitMove(col, 0);
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
          if (session.status === 'InProgress' && !session.isAiTurn) {
            submitMove(x, 0);
          }
        });

        const piece = (session.grid && session.grid[y] && session.grid[y][x]) || ' ';
        if (piece === 'R') {
          const chip = document.createElement('div');
          chip.className = 'c4-chip red';
          slot.appendChild(chip);
        } else if (piece === 'Y') {
          const chip = document.createElement('div');
          chip.className = 'c4-chip yellow';
          slot.appendChild(chip);
        }

        rack.appendChild(slot);
      }
    }
    wrapper.appendChild(rack);
    boardContainer.appendChild(wrapper);
  }

  function renderTicTacToe(session) {
    const grid = document.createElement('div');
    grid.className = 'ttt-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;
    grid.style.gridTemplateRows = `repeat(${session.height}, 1fr)`;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const cell = document.createElement('div');
        cell.className = 'ttt-cell';
        const piece = (session.grid && session.grid[y] && session.grid[y][x]) || ' ';

        if (piece === 'X') {
          cell.innerHTML = '<span class="ttt-x">✕</span>';
        } else if (piece === 'O') {
          cell.innerHTML = '<span class="ttt-o">◯</span>';
        } else {
          cell.addEventListener('click', () => {
            if (session.status === 'InProgress' && !session.isAiTurn) {
              submitMove(x, y);
            }
          });
        }

        grid.appendChild(cell);
      }
    }
    boardContainer.appendChild(grid);
  }

  function renderChomp(session) {
    const grid = document.createElement('div');
    grid.className = 'chomp-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;
    grid.style.gridTemplateRows = `repeat(${session.height}, 1fr)`;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const block = document.createElement('div');
        block.className = 'chomp-block';
        const piece = (session.grid && session.grid[y] && session.grid[y][x]) || ' ';

        if (piece === 'P') {
          block.classList.add('chomp-poison');
          block.innerHTML = '☠️';
          block.title = 'Poison Block! (Eating this loses the game)';
          block.addEventListener('click', () => {
            if (session.status === 'InProgress' && !session.isAiTurn) {
              submitMove(x, y);
            }
          });
        } else if (piece === 'C') {
          block.title = `Chomp at (${x}, ${y})`;
          block.addEventListener('click', () => {
            if (session.status === 'InProgress' && !session.isAiTurn) {
              submitMove(x, y);
            }
          });
        } else {
          block.classList.add('chomp-eaten');
        }

        grid.appendChild(block);
      }
    }
    boardContainer.appendChild(grid);
  }

  // Lightweight QR Code Generator (SVG output)
  function renderSimpleQR(text, container) {
    const encoded = encodeURIComponent(text);
    container.innerHTML = `
      <img src="https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${encoded}&margin=2" 
           alt="QR Code" width="180" height="180" style="display:block;border-radius:8px;" 
           onerror="this.onerror=null;this.parentElement.innerHTML='<div style=\\'padding:20px;font-size:12px;color:#1e293b;word-break:break-all;\\'>${text}</div>'">
    `;
  }

  // Setup Event Listeners
  function setupEvents() {
    // Menu modal
    btnOpenMenu.addEventListener('click', () => modalMenu.classList.remove('hidden'));
    btnCloseMenu.addEventListener('click', () => modalMenu.classList.add('hidden'));
    btnNewGame.addEventListener('click', () => modalMenu.classList.remove('hidden'));

    // Sound toggle
    btnSound.addEventListener('click', () => {
      soundEnabled = !soundEnabled;
      localStorage.setItem('bg_sound', soundEnabled);
      updateSoundUI();
      if (soundEnabled) playSound('tap');
    });

    // Reset button
    btnReset.addEventListener('click', () => {
      if (confirm('Restart the current game?')) resetSession();
    });

    // Share modal
    btnShare.addEventListener('click', async () => {
      modalShare.classList.remove('hidden');
      copyFeedback.classList.add('hidden');

      let url = window.location.origin;
      try {
        const infoRes = await fetch('/api/info');
        if (infoRes.ok) {
          const info = await infoRes.json();
          const ips = info.localIps || info.LocalIps;
          if (ips && ips.length > 0) {
            const port = window.location.port ? `:${window.location.port}` : '';
            url = `http://${ips[0]}${port}`;
          }
        }
      } catch (e) { }

      if (currentSession && currentSession.mode === 'room') {
        url += `?room=${currentSession.roomCode}`;
      }

      shareUrlText.textContent = url;
      renderSimpleQR(url, qrContainer);
    });

    btnCloseShare.addEventListener('click', () => modalShare.classList.add('hidden'));

    btnCopyUrl.addEventListener('click', async () => {
      const url = shareUrlText.textContent;
      try {
        await navigator.clipboard.writeText(url);
        copyFeedback.classList.remove('hidden');
        setTimeout(() => copyFeedback.classList.add('hidden'), 2500);
      } catch (e) {
        prompt('Copy this link:', url);
      }
    });

    // Game Over buttons
    btnPlayAgain.addEventListener('click', () => {
      resetSession();
    });
    btnChangeGame.addEventListener('click', () => {
      gameOverModal.classList.add('hidden');
      modalMenu.classList.remove('hidden');
    });

    // Pill selections in menu
    document.querySelectorAll('#group-game .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-game .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        selectedGame = btn.dataset.game;
      });
    });

    document.querySelectorAll('#group-mode .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-mode .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        selectedMode = btn.dataset.mode;
        secAiDiff.classList.toggle('hidden', selectedMode !== 'vs-ai');
        secRoom.classList.toggle('hidden', selectedMode !== 'room');
        if (selectedMode === 'room' && roomAction === 'join') {
          fetchAvailableRooms();
        }
      });
    });

    document.querySelectorAll('#group-diff .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-diff .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        selectedDiff = btn.dataset.diff;
      });
    });

    tabHost.addEventListener('click', () => {
      tabHost.classList.add('active');
      tabJoin.classList.remove('active');
      if (roomHostOptions) roomHostOptions.classList.remove('hidden');
      if (roomJoinBox) roomJoinBox.classList.add('hidden');
      roomAction = 'host';
    });

    tabJoin.addEventListener('click', () => {
      tabJoin.classList.add('active');
      tabHost.classList.remove('active');
      if (roomHostOptions) roomHostOptions.classList.add('hidden');
      if (roomJoinBox) roomJoinBox.classList.remove('hidden');
      roomAction = 'join';
      fetchAvailableRooms();
    });

    if (btnRefreshRooms) {
      btnRefreshRooms.addEventListener('click', (e) => {
        e.preventDefault();
        fetchAvailableRooms();
      });
    }

    inputRoomCode.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        btnStartGame.click();
      }
    });

    // Start Game from Modal
    btnStartGame.addEventListener('click', () => {
      if (selectedMode === 'room' && roomAction === 'join') {
        const code = inputRoomCode.value.trim().toUpperCase();
        if (code.length < 3) {
          alert('Please select an available game from the list above, or enter a 4-letter room code.');
          return;
        }
        modalMenu.classList.add('hidden');
        joinRoomByCode(code);
        return;
      }

      const isPublic = chkSharePublic ? chkSharePublic.checked : true;
      modalMenu.classList.add('hidden');
      createSession(
        selectedGame,
        selectedMode,
        selectedDiff,
        inputP1.value.trim() || 'Player 1',
        inputP2.value.trim() || 'Player 2',
        isPublic
      );
    });

    // Quick Rules button
    document.getElementById('btn-quick-rules').addEventListener('click', () => {
      const rules = {
        connect4: '🔴 CONNECT FOUR:\nTake turns dropping discs into the 7 columns.\nFirst player to connect 4 discs horizontally, vertically, or diagonally wins!',
        tictactoe: '❌ TIC-TAC-TOE:\nTake turns marking squares in the 3x3 grid.\nAlign 3 marks in a row to win!',
        chomp: '🍫 CHOMP:\nTwo players eat blocks of chocolate. Chomping a block removes everything to its right and below.\nThe top-left block ☠️ is POISON!\nWhoever is forced to eat the poison block loses!'
      };
      alert(rules[currentSession?.gameType || 'connect4']);
    });
  }

  // App Initialization
  async function init() {
    updateSoundUI();
    setupEvents();

    const joinedFromUrl = parseUrlParams();
    if (!joinedFromUrl) {
      // Start default Connect Four pass-and-play session
      await createSession('connect4', 'pass-and-play', 'smart', 'Player 1', 'Player 2');
    }
  }

  window.addEventListener('DOMContentLoaded', init);
})();
