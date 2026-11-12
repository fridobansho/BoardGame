// ============================================================================
// BoardGame Web Client
// ============================================================================

(function () {
  'use strict';

  // State
  let currentSession = null;
  let playerToken = localStorage.getItem('bg_player_token') || null;
  let isSpectator = false;
  let sseSource = null;
  let soundEnabled = localStorage.getItem('bg_sound') !== 'false';
  let audioCtx = null;

  // Client Preferences (Persisted in localStorage)
  let clientPlayerName = localStorage.getItem('bg_player_name') || 'Player 1';
  let clientTheme = localStorage.getItem('bg_theme') || 'nebula';
  let clientBoardTheme = localStorage.getItem('bg_board_theme') || 'auto';
  let clientPieceTheme = localStorage.getItem('bg_piece_theme') || 'auto';
  let clientFavGame = localStorage.getItem('bg_fav_game') || 'connect4';
  let clientDefaultRole = localStorage.getItem('bg_default_role') || 'play';
  let clientSoundPack = localStorage.getItem('bg_sound_pack') || 'arcade';

  // Selected config in modal
  let selectedGame = clientFavGame;
  let selectedMode = 'pass-and-play';
  let selectedDiff = 'smart';
  let selectedGridWidth = 7;
  let selectedGridHeight = 6;
  let selectedTimerSeconds = 0;
  let roomAction = 'host';

  // Runtime features state
  let timerInterval = null;
  let remainingTimerSeconds = 0;
  let replayIndex = null;
  let replayAutoPlayTimer = null;
  let hasVotedPrediction = false;
  let confettiAnimId = null;

  // DOM Elements
  const gameTitle = document.getElementById('game-title');
  const modeBadge = document.getElementById('mode-badge');
  const seriesScore = document.getElementById('series-score');
  const connectionPill = document.getElementById('connection-pill');
  const statusText = document.getElementById('status-text');
  const turnDot = document.getElementById('turn-dot');
  const spectatorCounter = document.getElementById('spectator-counter');
  const roomCodeTag = document.getElementById('room-code-tag');
  const roomCodeVal = document.getElementById('room-code-val');
  const boardContainer = document.getElementById('board-container');
  const reactionContainer = document.getElementById('reaction-container');
  const reactionWidget = document.getElementById('reaction-widget');
  const reactionPicker = document.getElementById('reaction-picker');
  const btnToggleReactions = document.getElementById('btn-toggle-reactions');

  // Turn Timer DOM
  const timerBarWrap = document.getElementById('timer-bar-wrap');
  const timerLabel = document.getElementById('timer-label');
  const timerCountdown = document.getElementById('timer-countdown');
  const timerProgressFill = document.getElementById('timer-progress-fill');

  // Crowd Predictions DOM
  const predictionWrap = document.getElementById('prediction-wrap');
  const predictionCounts = document.getElementById('prediction-counts');
  const predictionFillP1 = document.getElementById('prediction-fill-p1');
  const predictionFillP2 = document.getElementById('prediction-fill-p2');
  const predictionActions = document.getElementById('prediction-actions');
  const btnPredictP1 = document.getElementById('btn-predict-p1');
  const btnPredictP2 = document.getElementById('btn-predict-p2');

  // Replay Scrubber DOM
  const replayBar = document.getElementById('replay-bar');
  const replayStepText = document.getElementById('replay-step-text');
  const btnReplayStart = document.getElementById('btn-replay-start');
  const btnReplayPrev = document.getElementById('btn-replay-prev');
  const btnReplayPlay = document.getElementById('btn-replay-play');
  const btnReplayNext = document.getElementById('btn-replay-next');
  const btnReplayLive = document.getElementById('btn-replay-live');

  // Confetti Canvas & Speech Bubbles & Quick Chat DOM
  const confettiCanvas = document.getElementById('confetti-canvas');
  const speechBubbleContainer = document.getElementById('speech-bubble-container');
  const chatWidget = document.getElementById('chat-widget');
  const chatPicker = document.getElementById('chat-picker');
  const btnToggleChat = document.getElementById('btn-toggle-chat');

  // Modal Setup Sections DOM
  const groupGridSize = document.getElementById('group-grid-size');
  const groupTurnTimer = document.getElementById('group-turn-timer');

  // Stats & Trophies DOM
  const statMatches = document.getElementById('stat-matches');
  const statWinrate = document.getElementById('stat-winrate');
  const statStreak = document.getElementById('stat-streak');
  const trophyGrid = document.getElementById('trophy-grid');
  const btnResetStats = document.getElementById('btn-reset-stats');

  const btnOpenMenu = document.getElementById('btn-open-menu');
  const btnCloseMenu = document.getElementById('btn-close-menu');
  const modalMenu = document.getElementById('modal-menu');

  const btnSettings = document.getElementById('btn-settings');
  const modalSettings = document.getElementById('modal-settings');
  const btnCloseSettings = document.getElementById('btn-close-settings');
  const btnOpenSettingsMenu = document.getElementById('btn-open-settings-menu');
  const settingPlayerName = document.getElementById('setting-player-name');
  const btnSaveSettings = document.getElementById('btn-save-settings');

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

  const btnUndo = document.getElementById('btn-undo');
  const btnReset = document.getElementById('btn-reset');
  const btnNewGame = document.getElementById('btn-new-game');
  const btnOpenLobby = document.getElementById('btn-open-lobby');
  const btnStartGame = document.getElementById('btn-start-game');
  const btnResetSeries = document.getElementById('btn-reset-series');

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
      lastMoveX: s.lastMoveX !== undefined ? s.lastMoveX : (s.LastMoveX !== undefined ? s.LastMoveX : null),
      lastMoveY: s.lastMoveY !== undefined ? s.lastMoveY : (s.LastMoveY !== undefined ? s.LastMoveY : null),
      moveCount: s.moveCount !== undefined ? s.moveCount : (s.MoveCount || 0),
      spectatorCount: Number(s.spectatorCount !== undefined ? s.spectatorCount : (s.SpectatorCount || 0)),
      player1Score: Number(s.player1Score !== undefined ? s.player1Score : (s.Player1Score || 0)),
      player2Score: Number(s.player2Score !== undefined ? s.player2Score : (s.Player2Score || 0)),
      drawScore: Number(s.drawScore !== undefined ? s.drawScore : (s.DrawScore || 0)),
      gameNumber: Number(s.gameNumber !== undefined ? s.gameNumber : (s.GameNumber || 1)),
      canUndo: Boolean(s.canUndo !== undefined ? s.canUndo : s.CanUndo),
      player1Name: s.player1Name || s.Player1Name || 'Player 1',
      player2Name: s.player2Name || s.Player2Name || 'Player 2',
      isAiTurn: s.isAiTurn !== undefined ? s.isAiTurn : (s.IsAiTurn || false),
      timerSeconds: Number(s.timerSeconds !== undefined ? s.timerSeconds : (s.TimerSeconds || 0)),
      turnTimeRemaining: Number(s.turnTimeRemaining !== undefined ? s.turnTimeRemaining : (s.TurnTimeRemaining || 0)),
      player1Predictions: Number(s.player1Predictions !== undefined ? s.player1Predictions : (s.Player1Predictions || 0)),
      player2Predictions: Number(s.player2Predictions !== undefined ? s.player2Predictions : (s.Player2Predictions || 0)),
      moveHistory: s.moveHistory || s.MoveHistory || []
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
    const pack = clientSoundPack || 'arcade';

    if (pack === 'arcade') {
      // 8-Bit Retro Chiptune
      switch (name) {
        case 'tap':
          playTone(587.33, 0.05, 'square', 0.16);
          break;
        case 'drop':
          playTone(440, 0.06, 'square', 0.2);
          setTimeout(() => playTone(220, 0.09, 'square', 0.18), 45);
          break;
        case 'chomp':
          playTone(220, 0.05, 'square', 0.22);
          setTimeout(() => playTone(146.83, 0.07, 'sawtooth', 0.22), 35);
          break;
        case 'win':
          [440, 554.37, 659.25, 880, 1108.73].forEach((f, i) => {
            setTimeout(() => playTone(f, 0.16, 'square', 0.22), i * 85);
          });
          break;
        case 'draw':
          playTone(293.66, 0.14, 'sawtooth', 0.2);
          setTimeout(() => playTone(207.65, 0.24, 'sawtooth', 0.2), 120);
          break;
        case 'tick':
          playTone(784, 0.03, 'square', 0.1);
          break;
        case 'warning':
          playTone(880, 0.09, 'sawtooth', 0.25);
          break;
        case 'chat':
          playTone(523.25, 0.05, 'square', 0.16);
          setTimeout(() => playTone(783.99, 0.08, 'square', 0.18), 50);
          break;
      }
    } else if (pack === 'marimba') {
      // Warm Acoustic Marimba
      switch (name) {
        case 'tap':
          playTone(523.25, 0.12, 'sine', 0.25);
          break;
        case 'drop':
          playTone(392, 0.16, 'sine', 0.28);
          setTimeout(() => playTone(261.63, 0.2, 'sine', 0.22), 55);
          break;
        case 'chomp':
          playTone(246.94, 0.1, 'sine', 0.26);
          setTimeout(() => playTone(174.61, 0.16, 'triangle', 0.22), 40);
          break;
        case 'win':
          [523.25, 659.25, 783.99, 1046.5].forEach((f, i) => {
            setTimeout(() => playTone(f, 0.32, 'sine', 0.26), i * 110);
          });
          break;
        case 'draw':
          playTone(349.23, 0.2, 'triangle', 0.22);
          setTimeout(() => playTone(261.63, 0.35, 'sine', 0.2), 140);
          break;
        case 'tick':
          playTone(880, 0.04, 'sine', 0.12);
          break;
        case 'warning':
          playTone(698.46, 0.12, 'sine', 0.28);
          break;
        case 'chat':
          playTone(659.25, 0.1, 'sine', 0.2);
          setTimeout(() => playTone(880, 0.18, 'sine', 0.22), 60);
          break;
      }
    } else {
      // Tactile Mechanical Clicks
      switch (name) {
        case 'tap':
          playTone(950, 0.025, 'triangle', 0.28);
          break;
        case 'drop':
          playTone(220, 0.04, 'triangle', 0.3);
          setTimeout(() => playTone(110, 0.06, 'sine', 0.32), 30);
          break;
        case 'chomp':
          playTone(130, 0.04, 'square', 0.28);
          setTimeout(() => playTone(85, 0.05, 'triangle', 0.28), 25);
          break;
        case 'win':
          [440, 660, 880, 1320].forEach((f, i) => {
            setTimeout(() => playTone(f, 0.1, 'triangle', 0.26), i * 75);
          });
          break;
        case 'draw':
          playTone(165, 0.09, 'sawtooth', 0.26);
          setTimeout(() => playTone(110, 0.14, 'square', 0.22), 90);
          break;
        case 'tick':
          playTone(1200, 0.018, 'triangle', 0.15);
          break;
        case 'warning':
          playTone(493.88, 0.06, 'sawtooth', 0.32);
          break;
        case 'chat':
          playTone(1100, 0.025, 'triangle', 0.22);
          setTimeout(() => playTone(1650, 0.035, 'triangle', 0.24), 40);
          break;
      }
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
    document.querySelectorAll('#group-setting-sound .pill-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.sound === (soundEnabled ? 'on' : 'off'));
    });
  }

  // Client Preferences / Theme application
  function applyTheme(theme, boardTheme, pieceTheme) {
    if (theme !== undefined) clientTheme = theme || 'nebula';
    if (boardTheme !== undefined) clientBoardTheme = boardTheme || 'auto';
    if (pieceTheme !== undefined) clientPieceTheme = pieceTheme || 'auto';

    // UI Color Theme
    if (clientTheme === 'nebula') {
      delete document.documentElement.dataset.theme;
    } else {
      document.documentElement.dataset.theme = clientTheme;
    }

    // Board Style Override
    if (clientBoardTheme === 'auto') {
      delete document.documentElement.dataset.boardTheme;
    } else {
      document.documentElement.dataset.boardTheme = clientBoardTheme;
    }

    // Piece & Chip Style Override
    if (clientPieceTheme === 'auto') {
      delete document.documentElement.dataset.pieceTheme;
    } else {
      document.documentElement.dataset.pieceTheme = clientPieceTheme;
    }

    const themeColors = {
      nebula: '#0f172a',
      oled: '#000000',
      emerald: '#04140e',
      sunset: '#15081b'
    };
    const metaTheme = document.querySelector('meta[name="theme-color"]');
    if (metaTheme && themeColors[clientTheme]) {
      metaTheme.content = themeColors[clientTheme];
    }
  }

  function escapeHtml(str) {
    if (!str) return '';
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  // Lifetime Stats & Trophies System
  const DEFAULT_STATS = {
    matches: 0,
    wins: 0,
    losses: 0,
    draws: 0,
    streak: 0,
    bestStreak: 0,
    gameWins: { connect4: 0, tictactoe: 0, chomp: 0 },
    unlockedTrophies: []
  };

  const TROPHIES = [
    { id: 'first_win', icon: '🥉', name: 'First Victory', desc: 'Win your first game in any mode' },
    { id: 'streak_3', icon: '🔥', name: 'On Fire', desc: 'Achieve a 3-match win streak' },
    { id: 'streak_5', icon: '⚡', name: 'Unstoppable', desc: 'Achieve a 5-match win streak' },
    { id: 'master_c4', icon: '🔴', name: 'Connect King', desc: 'Win 3 Connect Four games' },
    { id: 'master_ttt', icon: '❌', name: 'Grid Master', desc: 'Win 3 Tic-Tac-Toe games' },
    { id: 'master_chomp', icon: '🍫', name: 'Sweet Tooth', desc: 'Win 3 Chomp games' },
    { id: 'blitz_win', icon: '⏱️', name: 'Blitz Buster', desc: 'Win a game with turn timer enabled' },
    { id: 'seer', icon: '🔮', name: 'Crowd Oracle', desc: 'Cast a prediction vote as a spectator' }
  ];

  function getStats() {
    try {
      const raw = localStorage.getItem('bg_stats');
      if (raw) return { ...DEFAULT_STATS, ...JSON.parse(raw) };
    } catch (e) { }
    return { ...DEFAULT_STATS };
  }

  function saveStats(stats) {
    try {
      localStorage.setItem('bg_stats', JSON.stringify(stats));
    } catch (e) { }
  }

  function recordGameResult(session) {
    if (!session || isSpectator) return;
    const stats = getStats();
    stats.matches++;

    const isP1 = (session.mode === 'vs-ai' || session.mode === 'pass-and-play' || !playerToken || playerToken === 'p1');
    const myName = isP1 ? session.player1Name : session.player2Name;
    const isWinner = session.winner && (session.winner === myName || (session.status === 'Finished' && ((isP1 && session.winner === session.player1Name) || (!isP1 && session.winner === session.player2Name))));

    if (session.status === 'Draw') {
      stats.draws++;
      stats.streak = 0;
    } else if (isWinner) {
      stats.wins++;
      stats.streak++;
      if (stats.streak > stats.bestStreak) stats.bestStreak = stats.streak;
      if (!stats.gameWins) stats.gameWins = { connect4: 0, tictactoe: 0, chomp: 0 };
      stats.gameWins[session.gameType] = (stats.gameWins[session.gameType] || 0) + 1;

      unlockTrophy(stats, 'first_win');
      if (stats.streak >= 3) unlockTrophy(stats, 'streak_3');
      if (stats.streak >= 5) unlockTrophy(stats, 'streak_5');
      if ((stats.gameWins.connect4 || 0) >= 3) unlockTrophy(stats, 'master_c4');
      if ((stats.gameWins.tictactoe || 0) >= 3) unlockTrophy(stats, 'master_ttt');
      if ((stats.gameWins.chomp || 0) >= 3) unlockTrophy(stats, 'master_chomp');
      if (session.timerSeconds > 0) unlockTrophy(stats, 'blitz_win');
    } else {
      stats.losses++;
      stats.streak = 0;
    }

    saveStats(stats);
  }

  function unlockTrophy(stats, trophyId) {
    if (!stats.unlockedTrophies) stats.unlockedTrophies = [];
    if (!stats.unlockedTrophies.includes(trophyId)) {
      stats.unlockedTrophies.push(trophyId);
      const trophy = TROPHIES.find(t => t.id === trophyId);
      if (trophy) {
        showSpeechBubble(`🏆 Trophy Unlocked: ${trophy.name}!`, 'System');
      }
    }
  }

  function renderStatsUI() {
    const stats = getStats();
    if (statMatches) statMatches.textContent = stats.matches;
    const winrate = stats.matches > 0 ? Math.round((stats.wins / stats.matches) * 100) : 0;
    if (statWinrate) statWinrate.textContent = `${winrate}%`;
    if (statStreak) statStreak.textContent = `🔥 ${stats.bestStreak}`;

    if (trophyGrid) {
      trophyGrid.innerHTML = '';
      const unlockedSet = new Set(stats.unlockedTrophies || []);
      TROPHIES.forEach(t => {
        const isUnlocked = unlockedSet.has(t.id);
        const card = document.createElement('div');
        card.className = `trophy-card ${isUnlocked ? 'unlocked' : 'locked'}`;
        card.innerHTML = `
          <div class="trophy-icon">${t.icon}</div>
          <div class="trophy-details">
            <span class="trophy-name">${t.name}</span>
            <span class="trophy-desc">${t.desc}</span>
          </div>
        `;
        trophyGrid.appendChild(card);
      });
    }
  }

  function resetStats() {
    if (confirm('Reset all lifetime stats and unlocked trophies?')) {
      localStorage.removeItem('bg_stats');
      renderStatsUI();
      playSound('tap');
    }
  }

  // Grid Size Options & Dynamic Pills
  const GRID_OPTIONS = {
    connect4: [
      { label: '7×6 (Standard)', width: 7, height: 6 },
      { label: '6×5 (Quick)', width: 6, height: 5 },
      { label: '8×7 (Expanded)', width: 8, height: 7 }
    ],
    tictactoe: [
      { label: '3×3 (Classic)', width: 3, height: 3 },
      { label: '4×4 (4-in-a-row)', width: 4, height: 4 },
      { label: '5×5 (Grand)', width: 5, height: 5 }
    ],
    chomp: [
      { label: '5×4 (Standard)', width: 5, height: 4 },
      { label: '4×3 (Bite-sized)', width: 4, height: 3 },
      { label: '6×5 (Large)', width: 6, height: 5 },
      { label: '7×4 (Wide)', width: 7, height: 4 }
    ]
  };

  function updateGridSizePills(game) {
    if (!groupGridSize) return;
    groupGridSize.innerHTML = '';
    const opts = GRID_OPTIONS[game] || GRID_OPTIONS.connect4;

    let match = opts.find(o => o.width === selectedGridWidth && o.height === selectedGridHeight);
    if (!match) {
      match = opts[0];
      selectedGridWidth = match.width;
      selectedGridHeight = match.height;
    }

    opts.forEach(opt => {
      const btn = document.createElement('button');
      btn.className = `pill-btn ${opt === match ? 'active' : ''}`;
      btn.type = 'button';
      btn.innerHTML = `<span>${opt.label}</span>`;
      btn.addEventListener('click', () => {
        groupGridSize.querySelectorAll('.pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        selectedGridWidth = opt.width;
        selectedGridHeight = opt.height;
        playSound('tap');
      });
      groupGridSize.appendChild(btn);
    });
  }

  // Fullscreen Confetti Particle FX
  function startConfetti() {
    if (!confettiCanvas) return;
    const ctx = confettiCanvas.getContext('2d');
    if (!ctx) return;
    confettiCanvas.width = window.innerWidth;
    confettiCanvas.height = window.innerHeight;

    const colors = ['#6366f1', '#ec4899', '#f59e0b', '#10b981', '#38bdf8', '#a855f7', '#fbbf24'];
    const particles = [];
    const count = 130;

    for (let i = 0; i < count; i++) {
      particles.push({
        x: confettiCanvas.width * Math.random(),
        y: confettiCanvas.height * 0.25 + (Math.random() * 20),
        w: 6 + Math.random() * 8,
        h: 6 + Math.random() * 8,
        vx: (Math.random() - 0.5) * 8,
        vy: -(Math.random() * 9 + 4),
        color: colors[Math.floor(Math.random() * colors.length)],
        rotation: Math.random() * 360,
        rSpeed: (Math.random() - 0.5) * 10,
        opacity: 1,
        shape: Math.random() > 0.4 ? 'rect' : 'circle'
      });
    }

    if (confettiAnimId) cancelAnimationFrame(confettiAnimId);

    function frame() {
      ctx.clearRect(0, 0, confettiCanvas.width, confettiCanvas.height);
      let aliveCount = 0;

      for (const p of particles) {
        p.x += p.vx;
        p.y += p.vy;
        p.vy += 0.26;
        p.vx *= 0.99;
        p.rotation += p.rSpeed;
        if (p.y > confettiCanvas.height * 0.6) {
          p.opacity -= 0.016;
        }

        if (p.opacity > 0 && p.y < confettiCanvas.height + 60) {
          aliveCount++;
          ctx.save();
          ctx.translate(p.x, p.y);
          ctx.rotate((p.rotation * Math.PI) / 180);
          ctx.globalAlpha = Math.max(0, p.opacity);
          ctx.fillStyle = p.color;

          if (p.shape === 'circle') {
            ctx.beginPath();
            ctx.arc(0, 0, p.w / 2, 0, Math.PI * 2);
            ctx.fill();
          } else {
            ctx.fillRect(-p.w / 2, -p.h / 2, p.w, p.h);
          }
          ctx.restore();
        }
      }

      if (aliveCount > 0) {
        confettiAnimId = requestAnimationFrame(frame);
      } else {
        ctx.clearRect(0, 0, confettiCanvas.width, confettiCanvas.height);
        confettiAnimId = null;
      }
    }

    confettiAnimId = requestAnimationFrame(frame);
  }

  // Quick Speech Bubbles & Chat
  function showSpeechBubble(text, sender) {
    if (!speechBubbleContainer) return;
    const bubble = document.createElement('div');
    bubble.className = 'speech-bubble';
    bubble.innerHTML = `
      <span class="speech-bubble-sender">${escapeHtml(sender || 'Player')}</span>
      <span>${escapeHtml(text)}</span>
    `;
    speechBubbleContainer.appendChild(bubble);
    playSound('chat');
    setTimeout(() => {
      bubble.remove();
    }, 3800);
  }

  async function sendQuickChat(text) {
    if (!currentSession || !text) return;
    try {
      await fetch(`/api/session/${currentSession.id}/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          text: text,
          sender: clientPlayerName
        })
      });
    } catch (e) {
      console.warn('Chat send failed:', e);
    }
  }

  // Spectator Predictions Poll
  function updatePredictionBar(p1Votes, p2Votes) {
    if (!predictionWrap) return;
    const p1 = Number(p1Votes || 0);
    const p2 = Number(p2Votes || 0);
    const total = p1 + p2;
    if (total === 0) {
      if (predictionFillP1) predictionFillP1.style.width = '50%';
      if (predictionFillP2) predictionFillP2.style.width = '50%';
      if (predictionCounts) predictionCounts.textContent = '0 votes';
    } else {
      const p1Pct = Math.round((p1 / total) * 100);
      const p2Pct = 100 - p1Pct;
      if (predictionFillP1) predictionFillP1.style.width = `${p1Pct}%`;
      if (predictionFillP2) predictionFillP2.style.width = `${p2Pct}%`;
      if (predictionCounts) predictionCounts.textContent = `${p1} (${p1Pct}%) vs ${p2} (${p2Pct}%)`;
    }
  }

  async function sendPrediction(predictedPlayer) {
    if (!currentSession) return;
    try {
      const res = await fetch(`/api/session/${currentSession.id}/predict`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          predictedPlayer: Number(predictedPlayer)
        })
      });
      if (res.ok) {
        hasVotedPrediction = true;
        if (btnPredictP1) btnPredictP1.disabled = true;
        if (btnPredictP2) btnPredictP2.disabled = true;
        playSound('tap');
        triggerHaptic(25);
        const stats = getStats();
        unlockTrophy(stats, 'seer');
        saveStats(stats);
      }
    } catch (e) {
      console.warn('Prediction send failed:', e);
    }
  }

  // Turn Timer Loop
  function updateTurnTimer(session) {
    if (!timerBarWrap) return;
    if (!session || session.timerSeconds <= 0 || session.status !== 'InProgress') {
      timerBarWrap.classList.add('hidden');
      if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
      }
      return;
    }

    timerBarWrap.classList.remove('hidden');
    remainingTimerSeconds = session.turnTimeRemaining !== undefined ? session.turnTimeRemaining : session.timerSeconds;

    if (timerCountdown) timerCountdown.textContent = `${Math.ceil(remainingTimerSeconds)}s`;
    const pct = Math.max(0, Math.min(100, (remainingTimerSeconds / session.timerSeconds) * 100));
    if (timerProgressFill) {
      timerProgressFill.style.width = `${pct}%`;
      timerProgressFill.classList.toggle('warning', remainingTimerSeconds <= 5);
    }
    if (timerCountdown) {
      timerCountdown.classList.toggle('warning', remainingTimerSeconds <= 5);
    }

    if (timerInterval) clearInterval(timerInterval);

    let lastSec = Math.ceil(remainingTimerSeconds);
    timerInterval = setInterval(() => {
      remainingTimerSeconds -= 0.25;
      if (remainingTimerSeconds < 0) remainingTimerSeconds = 0;

      const currentSec = Math.ceil(remainingTimerSeconds);
      if (timerCountdown) timerCountdown.textContent = `${currentSec}s`;
      const curPct = Math.max(0, Math.min(100, (remainingTimerSeconds / session.timerSeconds) * 100));
      if (timerProgressFill) {
        timerProgressFill.style.width = `${curPct}%`;
        timerProgressFill.classList.toggle('warning', remainingTimerSeconds <= 5);
      }
      if (timerCountdown) {
        timerCountdown.classList.toggle('warning', remainingTimerSeconds <= 5);
      }

      if (currentSec !== lastSec) {
        lastSec = currentSec;
        if (currentSec <= 5 && currentSec > 0) {
          playSound('tick');
          triggerHaptic(15);
        }
      }

      if (remainingTimerSeconds <= 0) {
        clearInterval(timerInterval);
        timerInterval = null;
        playSound('warning');
      }
    }, 250);
  }

  // Replay Scrubber & Board Simulation
  function simulateBoardAtStep(session, stepCount) {
    const width = session.width;
    const height = session.height;
    const gameType = session.gameType;

    const grid = [];
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

    const history = session.moveHistory || [];
    const limit = Math.min(stepCount, history.length);
    let lastX = null;
    let lastY = null;

    for (let i = 0; i < limit; i++) {
      const step = history[i];
      const px = Number(step.x !== undefined ? step.x : step.X);
      const py = Number(step.y !== undefined ? step.y : step.Y);
      const piece = step.piece || step.Piece || (i % 2 === 0 ? 'R' : 'Y');

      if (gameType === 'connect4') {
        for (let r = height - 1; r >= 0; r--) {
          if (grid[r][px] === ' ') {
            grid[r][px] = (piece === 'R' || piece === 'X' || piece === 'P1') ? 'R' : 'Y';
            lastX = px;
            lastY = r;
            break;
          }
        }
      } else if (gameType === 'tictactoe') {
        if (py >= 0 && py < height && px >= 0 && px < width) {
          grid[py][px] = (piece === 'X' || piece === 'R' || piece === 'P1') ? 'X' : 'O';
          lastX = px;
          lastY = py;
        }
      } else if (gameType === 'chomp') {
        if (py >= 0 && py < height && px >= 0 && px < width) {
          for (let r = py; r < height; r++) {
            for (let c = px; c < width; c++) {
              grid[r][c] = ' ';
            }
          }
          lastX = px;
          lastY = py;
        }
      }
    }

    return {
      ...session,
      grid: grid,
      lastMoveX: lastX,
      lastMoveY: lastY,
      status: (limit === history.length) ? session.status : 'InProgress',
      isAiTurn: false
    };
  }

  function scrubReplay(action) {
    if (!currentSession || !currentSession.moveHistory) return;
    const total = currentSession.moveHistory.length;
    if (total === 0) return;

    let target = replayIndex !== null ? replayIndex : total;

    if (action === 'start') {
      target = 0;
    } else if (action === 'prev') {
      target = Math.max(0, target - 1);
    } else if (action === 'next') {
      target = Math.min(total, target + 1);
    } else if (typeof action === 'number') {
      target = Math.max(0, Math.min(total, action));
    }

    if (target === total) {
      returnToLive();
      return;
    }

    replayIndex = target;
    if (btnReplayLive) btnReplayLive.classList.remove('hidden');
    if (replayStepText) replayStepText.textContent = `Replay: Move ${replayIndex} of ${total}`;

    const sim = simulateBoardAtStep(currentSession, replayIndex);
    renderBoard(sim);
    playSound('tap');
  }

  function returnToLive() {
    replayIndex = null;
    if (replayAutoPlayTimer) {
      clearInterval(replayAutoPlayTimer);
      replayAutoPlayTimer = null;
      if (btnReplayPlay) btnReplayPlay.textContent = '▶️';
    }
    if (btnReplayLive) btnReplayLive.classList.add('hidden');
    if (currentSession) {
      const total = currentSession.moveHistory ? currentSession.moveHistory.length : 0;
      if (replayStepText) replayStepText.textContent = `Live • ${total} moves`;
      renderBoard(currentSession);
    }
  }

  function toggleAutoPlay() {
    if (replayAutoPlayTimer) {
      clearInterval(replayAutoPlayTimer);
      replayAutoPlayTimer = null;
      if (btnReplayPlay) btnReplayPlay.textContent = '▶️';
      return;
    }

    if (!currentSession || !currentSession.moveHistory) return;
    const total = currentSession.moveHistory.length;
    if (total === 0) return;

    if (replayIndex === null || replayIndex >= total) {
      replayIndex = 0;
      scrubReplay(0);
    }

    if (btnReplayPlay) btnReplayPlay.textContent = '⏸️';

    replayAutoPlayTimer = setInterval(() => {
      if (!currentSession || !currentSession.moveHistory) {
        toggleAutoPlay();
        return;
      }
      const max = currentSession.moveHistory.length;
      if (replayIndex === null || replayIndex >= max) {
        returnToLive();
        return;
      }
      scrubReplay('next');
    }, 750);
  }

  function openSettingsModal() {
    if (!modalSettings) return;
    if (settingPlayerName) settingPlayerName.value = clientPlayerName;

    // Set theme active pill
    document.querySelectorAll('#group-setting-theme .pill-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.theme === clientTheme);
    });

    // Set board theme active pill
    document.querySelectorAll('#group-setting-board .pill-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.board === clientBoardTheme);
    });

    // Set piece theme active pill
    document.querySelectorAll('#group-setting-piece .pill-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.piece === clientPieceTheme);
    });

    // Set favorite game active pill
    document.querySelectorAll('#group-setting-fav .pill-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.fav === clientFavGame);
    });

    // Set default role active pill
    document.querySelectorAll('#group-setting-role .pill-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.role === clientDefaultRole);
    });

    // Set sound active pill
    document.querySelectorAll('#group-setting-sound .pill-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.sound === (soundEnabled ? 'on' : 'off'));
    });

    // Set sound pack active pill
    document.querySelectorAll('#group-setting-sound-pack .pill-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.pack === clientSoundPack);
    });

    renderStatsUI();

    modalSettings.classList.remove('hidden');
  }

  function saveSettings() {
    if (settingPlayerName) {
      const name = settingPlayerName.value.trim() || 'Player 1';
      clientPlayerName = name;
      localStorage.setItem('bg_player_name', name);
      if (inputP1) inputP1.value = name;
    }

    const activeThemeBtn = document.querySelector('#group-setting-theme .pill-btn.active');
    if (activeThemeBtn) {
      const th = activeThemeBtn.dataset.theme || 'nebula';
      clientTheme = th;
      localStorage.setItem('bg_theme', th);
    }

    const activeBoardBtn = document.querySelector('#group-setting-board .pill-btn.active');
    if (activeBoardBtn) {
      const bd = activeBoardBtn.dataset.board || 'auto';
      clientBoardTheme = bd;
      localStorage.setItem('bg_board_theme', bd);
    }

    const activePieceBtn = document.querySelector('#group-setting-piece .pill-btn.active');
    if (activePieceBtn) {
      const pc = activePieceBtn.dataset.piece || 'auto';
      clientPieceTheme = pc;
      localStorage.setItem('bg_piece_theme', pc);
    }

    applyTheme(clientTheme, clientBoardTheme, clientPieceTheme);

    const activeFavBtn = document.querySelector('#group-setting-fav .pill-btn.active');
    if (activeFavBtn) {
      const fav = activeFavBtn.dataset.fav || 'connect4';
      clientFavGame = fav;
      selectedGame = fav;
      localStorage.setItem('bg_fav_game', fav);
      document.querySelectorAll('#group-game .pill-btn').forEach(b => {
        b.classList.toggle('active', b.dataset.game === fav);
      });
      updateGridSizePills(fav);
    }

    const activeRoleBtn = document.querySelector('#group-setting-role .pill-btn.active');
    if (activeRoleBtn) {
      const role = activeRoleBtn.dataset.role || 'play';
      clientDefaultRole = role;
      localStorage.setItem('bg_default_role', role);
    }

    const activeSoundBtn = document.querySelector('#group-setting-sound .pill-btn.active');
    if (activeSoundBtn) {
      const enable = activeSoundBtn.dataset.sound === 'on';
      soundEnabled = enable;
      localStorage.setItem('bg_sound', enable);
      updateSoundUI();
    }

    const activePackBtn = document.querySelector('#group-setting-sound-pack .pill-btn.active');
    if (activePackBtn) {
      const pk = activePackBtn.dataset.pack || 'arcade';
      clientSoundPack = pk;
      localStorage.setItem('bg_sound_pack', pk);
    }

    playSound('tap');
    triggerHaptic(20);
    if (modalSettings) modalSettings.classList.add('hidden');
  }

  // Check URL parameters for direct room joining or spectating
  function parseUrlParams() {
    const params = new URLSearchParams(window.location.search);
    const spectateParam = params.get('spectate');
    if (spectateParam) {
      spectateSession(spectateParam.trim());
      return true;
    }
    const roomParam = params.get('room');
    if (roomParam) {
      joinRoomByCode(roomParam.trim().toUpperCase());
      return true;
    }
    return false;
  }

  // Create new session
  async function createSession(game, mode, diff, p1, p2, isPublic = true, customWidth = null, customHeight = null, timerSeconds = 0) {
    isSpectator = false;
    hasVotedPrediction = false;
    returnToLive();
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
          isPublic: Boolean(isPublic),
          customWidth: customWidth ? Number(customWidth) : null,
          customHeight: customHeight ? Number(customHeight) : null,
          timerSeconds: Number(timerSeconds || 0)
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
        player2Name: p2 || 'Player 2',
        width: customWidth,
        height: customHeight,
        timerSeconds: timerSeconds
      });
      onSessionUpdated(fallback);
    }
  }

  // Fetch available public Wi-Fi games
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
      lobbyRoomList.innerHTML = '<div class="lobby-empty">No active shared games found on Wi-Fi.<br><small style="color:var(--text-dim);display:inline-block;margin-top:4px;">Start a game with Share enabled or enter code below.</small></div>';
      return;
    }

    lobbyRoomList.innerHTML = '';
    const icons = { connect4: '🔴', tictactoe: '❌', chomp: '🍫' };
    const names = { connect4: 'Connect 4', tictactoe: 'Tic-Tac-Toe', chomp: 'Chomp' };
    const modeLabels = { 'pass-and-play': 'Pass & Play', 'vs-ai': 'vs AI', 'room': 'Room' };

    rooms.forEach(r => {
      const code = r.roomCode || r.RoomCode || '';
      const sid = r.sessionId || r.SessionId || '';
      const game = (r.gameType || r.GameType || 'connect4').toLowerCase();
      const mode = (r.mode || r.Mode || 'room').toLowerCase();
      const host = r.hostName || r.HostName || 'Host';
      const time = r.timeAgo || r.TimeAgo || 'just now';
      const status = r.status || r.Status || 'InProgress';
      const canJoin = Boolean(r.canJoin !== undefined ? r.canJoin : r.CanJoin);
      const canSpectate = Boolean(r.canSpectate !== undefined ? r.canSpectate : r.CanSpectate);

      const modeClass = mode === 'vs-ai' ? 'ai' : (mode === 'room' ? 'room' : 'local');
      const statusClass = status === 'WaitingForPlayer' ? 'waiting' : (status === 'Finished' || status === 'Draw' ? 'finished' : 'live');
      const statusLabel = status === 'WaitingForPlayer' ? 'Waiting' : (status === 'Finished' || status === 'Draw' ? 'Finished' : 'Live');

      const card = document.createElement('div');
      card.className = 'lobby-room-card';

      let codeBadgeHtml = '';
      if (mode === 'room' && code) {
        codeBadgeHtml = `<span class="lobby-code-badge">${code}</span>`;
      }

      card.innerHTML = `
        <div class="lobby-card-left">
          <span class="lobby-card-icon">${icons[game] || '🎲'}</span>
          <div class="lobby-card-info">
            <h4>${names[game] || game} <span class="lobby-mode-badge ${modeClass}">${modeLabels[mode] || mode}</span> ${codeBadgeHtml}</h4>
            <div class="lobby-card-meta">Host: ${host} • ${time} <span class="lobby-status-badge ${statusClass}">${statusLabel}</span></div>
          </div>
        </div>
      `;

      const actions = document.createElement('div');
      actions.className = 'lobby-actions';

      if (canJoin) {
        const joinBtn = document.createElement('button');
        joinBtn.className = 'btn btn-sm btn-primary';
        joinBtn.textContent = 'Join';
        joinBtn.type = 'button';
        joinBtn.addEventListener('click', (e) => {
          e.stopPropagation();
          modalMenu.classList.add('hidden');
          joinRoomByCode(code);
        });
        actions.appendChild(joinBtn);
      }

      if (canSpectate) {
        const specBtn = document.createElement('button');
        specBtn.className = 'btn btn-sm btn-spectate';
        specBtn.textContent = '👀 Watch';
        specBtn.type = 'button';
        specBtn.title = 'Spectate live match';
        specBtn.addEventListener('click', (e) => {
          e.stopPropagation();
          modalMenu.classList.add('hidden');
          spectateSession(sid);
        });
        actions.appendChild(specBtn);
      }

      card.appendChild(actions);
      lobbyRoomList.appendChild(card);
    });
  }

  // Spectate session live
  async function spectateSession(sessionId) {
    if (!sessionId) return;
    try {
      const res = await fetch(`/api/session/${sessionId}`);
      const text = await res.text();
      let data = null;
      try { data = text ? JSON.parse(text) : null; } catch (_) { }

      if (!res.ok || !data) {
        alert('Could not spectate session: ' + ((data && data.error) || text || 'Session not found'));
        return;
      }

      isSpectator = true;
      playerToken = 'spectator';
      modalMenu.classList.add('hidden');
      onSessionUpdated(data);
      connectStream(data.id || data.Id || sessionId);
    } catch (e) {
      alert('Network error connecting to session: ' + e.message);
    }
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
          playerName: clientPlayerName || 'Guest'
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

      isSpectator = false;
      playerToken = data.playerToken || data.PlayerToken;
      localStorage.setItem('bg_player_token', playerToken);
      const sessionObj = data.session || data.Session;
      onSessionUpdated(sessionObj);
      connectStream(sessionObj.id || sessionObj.Id);
    } catch (e) {
      alert('Network error joining room: ' + e.message);
    }
  }

  // Connection status updater
  function setConnectionStatus(status) {
    if (!connectionPill) return;
    connectionPill.className = 'connection-pill ' + status;
    connectionPill.title = status === 'connected' ? 'Connection: Live' : (status === 'reconnecting' ? 'Reconnecting to game...' : 'Disconnected');
  }

  // Floating reactions
  function showFloatingEmoji(emoji) {
    if (!reactionContainer || !emoji) return;
    const span = document.createElement('span');
    span.className = 'floating-emoji';
    span.textContent = emoji;
    const leftPercent = 15 + Math.floor(Math.random() * 70);
    const rotDeg = -15 + Math.floor(Math.random() * 30);
    span.style.left = `${leftPercent}%`;
    span.style.setProperty('--rot', `${rotDeg}deg`);
    reactionContainer.appendChild(span);
    setTimeout(() => {
      if (span.parentElement) span.parentElement.removeChild(span);
    }, 2500);
  }

  async function sendReaction(emoji) {
    if (!currentSession || !emoji) return;
    triggerHaptic(10);
    showFloatingEmoji(emoji);
    try {
      await fetch(`/api/session/${currentSession.id}/reaction`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ emoji: emoji })
      });
    } catch (e) { }
  }

  // Undo move
  async function undoMove() {
    if (!currentSession || !currentSession.canUndo || isSpectator) return;
    try {
      const res = await fetch(`/api/session/${currentSession.id}/undo`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerToken: playerToken })
      });
      if (res.ok) {
        playSound('tap');
        triggerHaptic(20);
        const data = await res.json();
        const sessionObj = data.session || data.Session;
        if (sessionObj) onSessionUpdated(sessionObj);
      }
    } catch (e) { }
  }

  // Reset match series
  async function resetSeries() {
    if (!currentSession) return;
    try {
      const res = await fetch(`/api/session/${currentSession.id}/reset-series`, { method: 'POST' });
      if (res.ok) {
        playSound('tap');
        const data = await res.json();
        const sessionObj = data.session || data.Session;
        if (sessionObj) onSessionUpdated(sessionObj);
        modalMenu.classList.add('hidden');
      }
    } catch (e) { }
  }

  // Connect Server-Sent Events stream for live real-time sync
  function connectStream(sessionId) {
    if (!sessionId) return;
    if (sseSource) {
      sseSource.close();
      sseSource = null;
    }
    setConnectionStatus('reconnecting');
    try {
      const url = `/api/session/${sessionId}/stream?spectator=${Boolean(isSpectator)}`;
      sseSource = new EventSource(url);
      sseSource.onopen = function () {
        setConnectionStatus('connected');
      };
      sseSource.onmessage = function (event) {
        setConnectionStatus('connected');
        try {
          const state = JSON.parse(event.data);
          onSessionUpdated(state);
        } catch (e) { }
      };
      sseSource.addEventListener('reaction', function (event) {
        try {
          const data = JSON.parse(event.data);
          if (data && data.emoji) showFloatingEmoji(data.emoji);
        } catch (e) { }
      });
      sseSource.addEventListener('chat', function (event) {
        try {
          const data = JSON.parse(event.data);
          if (data && data.text) showSpeechBubble(data.text, data.sender);
        } catch (e) { }
      });
      sseSource.addEventListener('prediction', function (event) {
        try {
          const data = JSON.parse(event.data);
          if (data) updatePredictionBar(data.p1, data.p2);
        } catch (e) { }
      });
      sseSource.onerror = function () {
        setConnectionStatus('reconnecting');
      };
    } catch (e) {
      setConnectionStatus('disconnected');
    }
  }

  // Submit move
  async function submitMove(x, y) {
    if (isSpectator) {
      triggerHaptic([20, 20]);
      return;
    }
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
  async function resetSession(isRematch = false) {
    if (!currentSession || isSpectator) return;
    try {
      const res = await fetch(`/api/session/${currentSession.id}/reset`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ isRematch: Boolean(isRematch) })
      });
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

    // Persist active session to localStorage for auto-reconnect
    try {
      localStorage.setItem('bg_session_id', session.id);
      localStorage.setItem('bg_is_spectator', String(Boolean(isSpectator)));
    } catch (e) { }

    // Header info
    const gameNames = { connect4: 'Connect Four', tictactoe: 'Tic-Tac-Toe', chomp: 'Chomp' };
    const modeNames = { 'pass-and-play': 'Pass & Play', 'vs-ai': 'vs AI', 'room': 'Wi-Fi Room' };
    gameTitle.textContent = gameNames[session.gameType] || 'Board Game';
    if (isSpectator) {
      modeBadge.textContent = `👀 Spectating (${modeNames[session.mode] || session.mode})`;
    } else {
      modeBadge.textContent = modeNames[session.mode] || session.mode;
    }

    // Series score tag
    if (seriesScore) {
      if (session.player1Score > 0 || session.player2Score > 0 || session.drawScore > 0 || session.gameNumber > 1) {
        seriesScore.classList.remove('hidden');
        seriesScore.textContent = `${session.player1Name} ${session.player1Score} - ${session.player2Score} ${session.player2Name} • Game ${session.gameNumber}`;
      } else {
        seriesScore.classList.add('hidden');
      }
    }

    // Spectator counter tag
    if (spectatorCounter) {
      if (session.spectatorCount > 0) {
        spectatorCounter.classList.remove('hidden');
        spectatorCounter.textContent = `👀 ${session.spectatorCount}`;
      } else {
        spectatorCounter.classList.add('hidden');
      }
    }

    // Undo button
    if (btnUndo) {
      btnUndo.classList.toggle('hidden', !session.canUndo || isSpectator);
    }

    // Room tag
    if (session.mode === 'room') {
      roomCodeTag.classList.remove('hidden');
      roomCodeVal.textContent = session.roomCode;
    } else {
      roomCodeTag.classList.add('hidden');
    }

    // Status Banner
    updateStatusBanner(session);

    // Turn Timer update
    updateTurnTimer(session);

    // Spectator Prediction Poll
    if (predictionWrap) {
      const showPrediction = isSpectator || session.mode === 'room' || session.spectatorCount > 0;
      predictionWrap.classList.toggle('hidden', !showPrediction);
      if (showPrediction) {
        updatePredictionBar(session.player1Predictions, session.player2Predictions);
        if (predictionActions) {
          predictionActions.classList.toggle('hidden', !isSpectator || session.status !== 'InProgress');
          if (btnPredictP1) {
            btnPredictP1.textContent = `Vote ${session.player1Name}`;
            btnPredictP1.disabled = hasVotedPrediction;
          }
          if (btnPredictP2) {
            btnPredictP2.textContent = `Vote ${session.player2Name}`;
            btnPredictP2.disabled = hasVotedPrediction;
          }
        }
      }
    }

    // Replay Scrubber Bar
    if (replayBar) {
      const hasHistory = session.moveHistory && session.moveHistory.length > 0;
      replayBar.classList.toggle('hidden', !hasHistory);
      if (hasHistory && replayIndex === null) {
        if (replayStepText) replayStepText.textContent = `Live • Move ${session.moveHistory.length} of ${session.moveHistory.length}`;
        if (btnReplayLive) btnReplayLive.classList.add('hidden');
      }
    }

    // Render Board (or preserve simulated replay view if currently reviewing)
    if (replayIndex !== null) {
      const sim = simulateBoardAtStep(session, replayIndex);
      renderBoard(sim);
    } else {
      renderBoard(session);
    }

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

    if (isSpectator) {
      if (session.status === 'WaitingForPlayer') {
        statusText.textContent = `👀 Spectating: Waiting for Player 2 to join...`;
      } else if (session.status === 'Finished') {
        statusText.textContent = `👀 Spectating: ${session.winner} Wins!`;
      } else if (session.status === 'Draw') {
        statusText.textContent = `👀 Spectating: Game ended in a Draw!`;
      } else {
        statusText.textContent = `👀 Spectating: ${session.currentTurnPlayer}'s Turn`;
      }
      return;
    }

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
    recordGameResult(session);

    if (session.status === 'Finished') {
      startConfetti();
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

    if (isSpectator) {
      btnPlayAgain.textContent = 'Browse Wi-Fi Lobby';
      btnPlayAgain.onclick = () => {
        gameOverModal.classList.add('hidden');
        openLobby();
      };
    } else {
      btnPlayAgain.textContent = `Next Round (Game ${session.gameNumber + 1})`;
      btnPlayAgain.onclick = () => {
        resetSession(true);
      };
    }

    gameOverModal.classList.remove('hidden');
  }

  // Board Renderers
  function renderBoard(session) {
    boardContainer.innerHTML = '';
    boardContainer.className = `board-container ${session.gameType}-mode` + (isSpectator ? ' spectator-mode' : '');

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
      btn.disabled = session.status !== 'InProgress' || session.isAiTurn || isSpectator;
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        if (!isSpectator) submitMove(col, 0);
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
          if (session.status === 'InProgress' && !session.isAiTurn && !isSpectator) {
            submitMove(x, 0);
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
    boardContainer.appendChild(wrapper);
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
            if (session.status === 'InProgress' && !session.isAiTurn && !isSpectator) {
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
        const isLastMove = (session.lastMoveX !== null && session.lastMoveY !== null &&
                            x === session.lastMoveX && y === session.lastMoveY);
        block.className = 'chomp-block' + (isLastMove ? ' last-move' : '');
        const piece = (session.grid && session.grid[y] && session.grid[y][x]) || ' ';

        if (piece === 'P') {
          block.classList.add('chomp-poison');
          block.innerHTML = '☠️';
          block.title = 'Poison Block! (Eating this loses the game)';
          block.addEventListener('click', () => {
            if (session.status === 'InProgress' && !session.isAiTurn && !isSpectator) {
              submitMove(x, y);
            }
          });
        } else if (piece === 'C') {
          block.title = `Chomp at (${x}, ${y})`;
          block.addEventListener('click', () => {
            if (session.status === 'InProgress' && !session.isAiTurn && !isSpectator) {
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

    // Open Lobby view
    function openLobby() {
      modalMenu.classList.remove('hidden');
      document.querySelectorAll('#group-mode .pill-btn').forEach(b => b.classList.remove('active'));
      const roomPill = document.querySelector('#group-mode .pill-btn[data-mode="room"]');
      if (roomPill) roomPill.classList.add('active');
      selectedMode = 'room';
      secAiDiff.classList.add('hidden');
      secRoom.classList.remove('hidden');

      tabJoin.classList.add('active');
      tabHost.classList.remove('active');
      if (roomJoinBox) roomJoinBox.classList.remove('hidden');
      roomAction = 'join';
      fetchAvailableRooms();
    }

    if (btnOpenLobby) {
      btnOpenLobby.addEventListener('click', () => openLobby());
    }

    // Reset button
    btnReset.addEventListener('click', () => {
      if (isSpectator) {
        alert('Spectators cannot restart the game.');
        return;
      }
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

      if (isSpectator && currentSession) {
        url += `?spectate=${currentSession.id}`;
      } else if (currentSession && currentSession.mode === 'room' && currentSession.status === 'WaitingForPlayer') {
        url += `?room=${currentSession.roomCode}`;
      } else if (currentSession) {
        url += `?spectate=${currentSession.id}`;
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

    // Undo button
    if (btnUndo) {
      btnUndo.addEventListener('click', () => undoMove());
    }

    // Reset Series button
    if (btnResetSeries) {
      btnResetSeries.addEventListener('click', () => {
        if (confirm('Reset match series scores to 0-0?')) {
          resetSeries();
        }
      });
    }

    // Toggle reaction picker
    if (btnToggleReactions && reactionPicker) {
      btnToggleReactions.addEventListener('click', (e) => {
        e.stopPropagation();
        const willShow = reactionPicker.classList.contains('hidden');
        reactionPicker.classList.toggle('hidden', !willShow);
        btnToggleReactions.classList.toggle('active', willShow);
      });
    }

    // Floating reaction buttons (send and autohide)
    document.querySelectorAll('.reaction-btn').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const emoji = btn.dataset.emoji;
        if (emoji) sendReaction(emoji);
        if (reactionPicker) reactionPicker.classList.add('hidden');
        if (btnToggleReactions) btnToggleReactions.classList.remove('active');
      });
    });

    // Close reaction picker when tapping anywhere else
    document.addEventListener('click', (e) => {
      if (reactionWidget && !reactionWidget.contains(e.target)) {
        if (reactionPicker) reactionPicker.classList.add('hidden');
        if (btnToggleReactions) btnToggleReactions.classList.remove('active');
      }
    });

    // Settings modal events
    if (btnSettings) {
      btnSettings.addEventListener('click', () => openSettingsModal());
    }
    if (btnCloseSettings) {
      btnCloseSettings.addEventListener('click', () => modalSettings.classList.add('hidden'));
    }
    if (btnOpenSettingsMenu) {
      btnOpenSettingsMenu.addEventListener('click', () => {
        modalMenu.classList.add('hidden');
        openSettingsModal();
      });
    }
    if (btnSaveSettings) {
      btnSaveSettings.addEventListener('click', () => saveSettings());
    }

    // Settings Theme pills
    document.querySelectorAll('#group-setting-theme .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-setting-theme .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        applyTheme(btn.dataset.theme, clientBoardTheme, clientPieceTheme); // Real-time preview
      });
    });

    // Settings Board pills
    document.querySelectorAll('#group-setting-board .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-setting-board .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        applyTheme(clientTheme, btn.dataset.board, clientPieceTheme); // Real-time preview
      });
    });

    // Settings Piece pills
    document.querySelectorAll('#group-setting-piece .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-setting-piece .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        applyTheme(clientTheme, clientBoardTheme, btn.dataset.piece); // Real-time preview
      });
    });

    // Settings Fav Game pills
    document.querySelectorAll('#group-setting-fav .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-setting-fav .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
      });
    });

    // Settings Default Role pills
    document.querySelectorAll('#group-setting-role .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-setting-role .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
      });
    });

    // Settings Sound pills
    document.querySelectorAll('#group-setting-sound .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-setting-sound .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        const enable = btn.dataset.sound === 'on';
        soundEnabled = enable;
        localStorage.setItem('bg_sound', enable);
        updateSoundUI();
        if (enable) playSound('tap');
      });
    });

    // Settings Sound Pack pills
    document.querySelectorAll('#group-setting-sound-pack .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-setting-sound-pack .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        clientSoundPack = btn.dataset.pack || 'arcade';
        playSound('tap');
      });
    });

    // Reset stats button
    if (btnResetStats) {
      btnResetStats.addEventListener('click', () => resetStats());
    }

    // Toggle quick chat picker
    if (btnToggleChat && chatPicker) {
      btnToggleChat.addEventListener('click', (e) => {
        e.stopPropagation();
        const willShow = chatPicker.classList.contains('hidden');
        chatPicker.classList.toggle('hidden', !willShow);
        btnToggleChat.classList.toggle('active', willShow);
      });
    }

    // Quick chat phrases
    document.querySelectorAll('.chat-phrase-btn').forEach(btn => {
      btn.addEventListener('click', (e) => {
        e.stopPropagation();
        const text = btn.dataset.text || btn.textContent.trim();
        if (text) sendQuickChat(text);
        if (chatPicker) chatPicker.classList.add('hidden');
        if (btnToggleChat) btnToggleChat.classList.remove('active');
      });
    });

    // Close chat picker when clicking outside
    document.addEventListener('click', (e) => {
      if (chatWidget && !chatWidget.contains(e.target)) {
        if (chatPicker) chatPicker.classList.add('hidden');
        if (btnToggleChat) btnToggleChat.classList.remove('active');
      }
    });

    // Spectator Predictions buttons
    if (btnPredictP1) {
      btnPredictP1.addEventListener('click', () => sendPrediction(1));
    }
    if (btnPredictP2) {
      btnPredictP2.addEventListener('click', () => sendPrediction(2));
    }

    // Replay Scrubber controls
    if (btnReplayStart) {
      btnReplayStart.addEventListener('click', () => scrubReplay('start'));
    }
    if (btnReplayPrev) {
      btnReplayPrev.addEventListener('click', () => scrubReplay('prev'));
    }
    if (btnReplayNext) {
      btnReplayNext.addEventListener('click', () => scrubReplay('next'));
    }
    if (btnReplayPlay) {
      btnReplayPlay.addEventListener('click', () => toggleAutoPlay());
    }
    if (btnReplayLive) {
      btnReplayLive.addEventListener('click', () => returnToLive());
    }

    // Turn timer pills
    document.querySelectorAll('#group-turn-timer .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-turn-timer .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        selectedTimerSeconds = Number(btn.dataset.timer || 0);
        playSound('tap');
      });
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
        updateGridSizePills(selectedGame);
        playSound('tap');
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
        isPublic,
        selectedGridWidth,
        selectedGridHeight,
        selectedTimerSeconds
      );
    });

    // Quick Rules button
    document.getElementById('btn-quick-rules').addEventListener('click', () => {
      const rules = {
        connect4: '🔴 CONNECT FOUR:\nTake turns dropping discs into columns.\nFirst player to connect 4 discs horizontally, vertically, or diagonally wins!',
        tictactoe: '❌ TIC-TAC-TOE:\nTake turns marking squares in the grid.\nAlign a full line in a row (or 4 in a row on 4x4/5x5) to win!',
        chomp: '🍫 CHOMP:\nTwo players eat blocks of chocolate. Chomping a block removes everything to its right and below.\nThe top-left block ☠️ is POISON!\nWhoever is forced to eat the poison block loses!'
      };
      alert(rules[currentSession?.gameType || 'connect4']);
    });
  }

  // App Initialization
  async function init() {
    applyTheme(clientTheme, clientBoardTheme, clientPieceTheme);
    if (inputP1) inputP1.value = clientPlayerName;
    updateSoundUI();
    setupEvents();
    updateGridSizePills(clientFavGame);
    renderStatsUI();

    // Set initial active game pill to favorite game
    document.querySelectorAll('#group-game .pill-btn').forEach(b => {
      b.classList.toggle('active', b.dataset.game === clientFavGame);
    });

    const joinedFromUrl = parseUrlParams();
    if (!joinedFromUrl) {
      let resumed = false;
      const savedId = localStorage.getItem('bg_session_id');
      if (savedId) {
        try {
          const res = await fetch(`/api/session/${savedId}`);
          if (res.ok) {
            const data = await res.json();
            isSpectator = localStorage.getItem('bg_is_spectator') === 'true';
            playerToken = localStorage.getItem('bg_player_token') || playerToken;
            onSessionUpdated(data);
            connectStream(data.id || data.Id || savedId);
            resumed = true;
          }
        } catch (e) { }
      }

      if (!resumed) {
        if (clientDefaultRole === 'spectate') {
          // Open Wi-Fi lobby directly when client prefers spectating by default
          await createSession(clientFavGame, 'pass-and-play', 'smart', clientPlayerName, 'Player 2', true, selectedGridWidth, selectedGridHeight, selectedTimerSeconds);
          openLobby();
        } else {
          // Start favorite game by default
          await createSession(clientFavGame, 'pass-and-play', 'smart', clientPlayerName, 'Player 2', true, selectedGridWidth, selectedGridHeight, selectedTimerSeconds);
        }
      }
    }
  }

  window.addEventListener('DOMContentLoaded', init);
})();
