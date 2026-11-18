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
  let clientDefaultMode = localStorage.getItem('bg_default_mode') || 'pass-and-play';
  let clientDefaultRole = localStorage.getItem('bg_default_role') || 'play';
  let clientSoundPack = localStorage.getItem('bg_sound_pack') || 'arcade';
  let clientDefaultVariant = localStorage.getItem('bg_default_variant') || 'standard';
  let clientDefaultDiff = localStorage.getItem('bg_default_diff') || 'smart';
  let clientDefaultSize = localStorage.getItem('bg_default_size') || 'standard';
  let clientDefaultTimer = Number(localStorage.getItem('bg_default_timer') || 0);

  // Selected config in modal
  let selectedGame = clientFavGame;
  let selectedMode = clientDefaultMode;
  let selectedDiff = clientDefaultDiff;
  let selectedVariant = 'standard';
  let selectedGridWidth = 7;
  let selectedGridHeight = 6;
  let selectedTimerSeconds = clientDefaultTimer;
  let roomAction = 'host';

  // Runtime features state
  let timerInterval = null;
  let remainingTimerSeconds = 0;
  let replayIndex = null;
  let replayAutoPlayTimer = null;
  let hasVotedPrediction = false;
  let confettiAnimId = null;
  let selectedChecker = null;
  let selectedBreakthroughPiece = null;
  let wordleCurrentGuess = '';
  let minesweeperFlagMode = false;
  let aiExhibitionTimer = null;
  let aiExhibitionPaused = false;
  let selectedScrabbleTileIndex = null;
  let pendingScrabblePlacements = [];
  let selectedSudokuCell = null;
  let highlightedSudokuDigit = null;
  let lbFilterGame = 'all';
  let lbFilterMode = 'all';
  let lbActiveTab = 'scores';

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
  const groupVariant = document.getElementById('group-variant');
  const variantDescHint = document.getElementById('variant-desc-hint');

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
  const shareModalTitle = document.getElementById('share-modal-title');
  const shareModalSubtitle = document.getElementById('share-modal-subtitle');
  const tabShareShortcut = document.getElementById('tab-share-shortcut');
  const tabShareSession = document.getElementById('tab-share-session');
  const shareUrlText = document.getElementById('share-url-text');
  const btnCopyUrl = document.getElementById('btn-copy-url');
  const copyFeedback = document.getElementById('copy-feedback');
  const qrContainer = document.getElementById('qr-container');

  const btnSound = document.getElementById('btn-sound');
  const iconSoundOn = document.getElementById('icon-sound-on');
  const iconSoundOff = document.getElementById('icon-sound-off');

  const btnUndo = document.getElementById('btn-undo');
  const btnLeaderboard = document.getElementById('btn-leaderboard');
  const modalLeaderboard = document.getElementById('modal-leaderboard');
  const btnCloseLeaderboard = document.getElementById('btn-close-leaderboard');
  const btnPlayDailySudoku = document.getElementById('btn-play-daily-sudoku');
  const btnRefreshLeaderboard = document.getElementById('btn-refresh-leaderboard');
  const tabLbScores = document.getElementById('tab-lb-scores');
  const tabLbPlayers = document.getElementById('tab-lb-players');
  const leaderboardList = document.getElementById('leaderboard-list');
  const lbDailyDate = document.getElementById('lb-daily-date');
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
    const gameType = resolveGameId(rawGame) || 'connect4';
    const meta = GAME_REGISTRY[gameType] || GAME_REGISTRY.connect4;

    const width = Number(s.width || s.Width || meta.defaultWidth);
    const height = Number(s.height || s.Height || meta.defaultHeight);
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
      } else if (gameType === 'checkers') {
        for (let y = 0; y < 3 && y < height; y++) {
          for (let x = 0; x < width; x++) {
            if ((x + y) % 2 === 1) grid[y][x] = 'B';
          }
        }
        for (let y = Math.max(0, height - 3); y < height; y++) {
          for (let x = 0; x < width; x++) {
            if ((x + y) % 2 === 1) grid[y][x] = 'R';
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
      moveHistory: s.moveHistory || s.MoveHistory || [],
      player1GamePoints: Number(s.player1GamePoints !== undefined ? s.player1GamePoints : (s.Player1GamePoints || 0)),
      player2GamePoints: Number(s.player2GamePoints !== undefined ? s.player2GamePoints : (s.Player2GamePoints || 0)),
      currentPlayerRack: s.currentPlayerRack || s.CurrentPlayerRack || [],
      player1Rack: s.player1Rack || s.Player1Rack || [],
      player2Rack: s.player2Rack || s.Player2Rack || [],
      tileBagCount: Number(s.tileBagCount !== undefined ? s.tileBagCount : (s.TileBagCount || 0)),
      bonusGrid: s.bonusGrid || s.BonusGrid || null,
      variant: (s.variant || s.Variant || 'standard').toLowerCase()
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

    const isP1 = (session.mode === 'vs-ai' || session.mode === 'pass-and-play' || session.mode === 'daily' || session.mode === 'solo' || !playerToken || playerToken === 'p1');
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

  // Unified Game Metadata Registry (single source of truth for categories, modes, variants, grids & rules)
  const GAME_REGISTRY = {
    connect4: {
      id: 'connect4',
      name: 'Connect Four',
      shortName: 'Connect 4',
      icon: '🔴',
      category: 'alignment',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 7,
      defaultHeight: 6,
      aliases: ['connectfour', 'connect-four', 'connect-4', 'c4'],
      variants: [
        { id: 'standard', icon: '🔴', label: 'Standard (4)', desc: 'Connect 4 discs horizontally, vertically, or diagonally.' },
        { id: 'connect5', icon: '5️⃣', label: 'Connect 5', desc: 'Align 5 discs in a row to win (great on 7×6 or 8×7).' },
        { id: 'misere', icon: '🔄', label: 'Misère', desc: 'Avoid 4-in-a-row! First player to align 4 loses.' }
      ],
      gridOptions: [
        { label: '7×6 (Standard)', width: 7, height: 6 },
        { label: '6×5 (Quick)', width: 6, height: 5 },
        { label: '8×7 (Expanded)', width: 8, height: 7 }
      ],
      rules: '🔴 CONNECT FOUR:\nTake turns dropping discs into columns.\nFirst player to connect 4 discs horizontally, vertically, or diagonally wins!'
    },
    tictactoe: {
      id: 'tictactoe',
      name: 'Tic-Tac-Toe',
      shortName: 'Tic-Tac-Toe',
      icon: '❌',
      category: 'alignment',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 3,
      defaultHeight: 3,
      aliases: ['ttt', 'tic-tac-toe', 'tic'],
      variants: [
        { id: 'standard', icon: '❌', label: 'Classic', desc: 'First player to complete a line wins.' },
        { id: 'misere', icon: '🔄', label: 'Misère', desc: 'Reverse rules: completing a full line loses!' }
      ],
      gridOptions: [
        { label: '3×3 (Classic)', width: 3, height: 3 },
        { label: '4×4 (4-in-a-row)', width: 4, height: 4 },
        { label: '5×5 (Grand)', width: 5, height: 5 }
      ],
      rules: '❌ TIC-TAC-TOE:\nTake turns marking squares in the grid.\nAlign a full line in a row (or 4 in a row on 4x4/5x5) to win!'
    },
    chomp: {
      id: 'chomp',
      name: 'Chomp',
      shortName: 'Chomp',
      icon: '🍫',
      category: 'strategy',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 5,
      defaultHeight: 4,
      aliases: [],
      variants: [
        { id: 'standard', icon: '☠️', label: 'Misère (Poison)', desc: 'Standard Chomp: eating the top-left poison block loses.' },
        { id: 'normal', icon: '🍫', label: 'Normal (Last)', desc: 'Alternative rules: eating the top-left block wins!' }
      ],
      gridOptions: [
        { label: '5×4 (Standard)', width: 5, height: 4 },
        { label: '4×3 (Bite-sized)', width: 4, height: 3 },
        { label: '6×5 (Large)', width: 6, height: 5 },
        { label: '7×4 (Wide)', width: 7, height: 4 }
      ],
      rules: '🍫 CHOMP:\nTwo players eat blocks of chocolate. Chomping a block removes everything to its right and below.\nThe top-left block ☠️ is POISON!\nWhoever is forced to eat the poison block loses!'
    },
    gomoku: {
      id: 'gomoku',
      name: 'Gomoku',
      shortName: 'Gomoku',
      icon: '⚫',
      category: 'alignment',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 15,
      defaultHeight: 15,
      aliases: ['five-in-a-row'],
      variants: [
        { id: 'standard', icon: '⚫', label: 'Freestyle (5+)', desc: 'Five or more stones in a row wins.' },
        { id: 'exact5', icon: '🎯', label: 'Exact 5 Only', desc: 'Exactly 5 in a row wins; 6+ overlines do not count.' },
        { id: 'misere', icon: '🔄', label: 'Misère', desc: 'Avoid 5-in-a-row! Forming 5 stones loses.' }
      ],
      gridOptions: [
        { label: '15×15 (Standard)', width: 15, height: 15 },
        { label: '9×9 (Quick)', width: 9, height: 9 },
        { label: '19×19 (Pro)', width: 19, height: 19 }
      ],
      rules: '⚫ GOMOKU (5-in-a-Row):\nTwo players alternate placing black and white stones on a 15×15 board.\nFirst player to form an unbroken line of 5 stones horizontally, vertically, or diagonally wins!'
    },
    othello: {
      id: 'othello',
      name: 'Othello',
      shortName: 'Othello',
      icon: '⚪',
      category: 'strategy',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 8,
      defaultHeight: 8,
      aliases: ['reversi'],
      variants: [
        { id: 'standard', icon: '⚪', label: 'Classic (Most)', desc: 'Player with the most discs when no moves remain wins.' },
        { id: 'reversed', icon: '🔄', label: 'Anti-Reversi', desc: 'Reverse Othello: fewest discs at the end wins!' }
      ],
      gridOptions: null,
      rules: '⚪ OTHELLO (Reversi):\nTwo players place black and white discs on an 8×8 board.\nOutflank opponent discs in any direction to flip them to your color. Most discs when no moves remain wins!'
    },
    nim: {
      id: 'nim',
      name: 'Nim',
      shortName: 'Nim',
      icon: '🪨',
      category: 'strategy',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 7,
      defaultHeight: 4,
      aliases: [],
      variants: [
        { id: 'standard', icon: '🪨', label: 'Normal Play', desc: 'Player who takes the LAST stone wins.' },
        { id: 'misere', icon: '🔄', label: 'Misère Nim', desc: 'Classic Misère: player forced to take the LAST stone loses!' }
      ],
      gridOptions: null,
      rules: '🪨 NIM:\nFour rows of stones (1, 3, 5, 7).\nOn your turn, click any stone to remove it and all stones to its right in that row.\nThe player who takes the LAST stone wins!'
    },
    hex: {
      id: 'hex',
      name: 'Hex',
      shortName: 'Hex',
      icon: '⬡',
      category: 'alignment',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 7,
      defaultHeight: 7,
      aliases: [],
      variants: [
        { id: 'standard', icon: '⬡', label: 'Standard', desc: 'First player to connect their two opposite sides wins.' },
        { id: 'misere', icon: '🔄', label: 'Misère', desc: 'Avoid connecting your sides—completing a path loses!' }
      ],
      gridOptions: [
        { label: '7×7 (Standard)', width: 7, height: 7 },
        { label: '5×5 (Quick)', width: 5, height: 5 },
        { label: '11×11 (Pro)', width: 11, height: 11 }
      ],
      rules: '⬡ HEX:\nBlack connects Top-to-Bottom; White connects Left-to-Right.\nTake turns placing stones on the skewed hex grid. First player to form an unbroken chain wins!'
    },
    checkers: {
      id: 'checkers',
      name: 'Checkers',
      shortName: 'Checkers',
      icon: '👑',
      category: 'strategy',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 8,
      defaultHeight: 8,
      aliases: ['draughts', 'checker'],
      variants: [
        { id: 'standard', icon: '👑', label: 'Standard', desc: 'Mandatory captures with multi-jump chains and King promotion.' },
        { id: 'casual', icon: '⚡', label: 'Casual', desc: 'Optional captures—choose any legal diagonal move.' },
        { id: 'giveaway', icon: '🔄', label: 'Giveaway', desc: 'Lose all your pieces or get blocked to win!' }
      ],
      gridOptions: null,
      rules: '👑 CHECKERS (Draughts):\nMove diagonally forward on dark squares. Captures by jumping over opponent pieces are mandatory (multi-jumps chain automatically).\nReach the opposite row to promote to a King 👑, which can move & jump in all 4 diagonal directions!'
    },
    scrabble: {
      id: 'scrabble',
      name: 'Scrabble',
      shortName: 'Scrabble',
      icon: '🔤',
      category: 'puzzles',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 15,
      defaultHeight: 15,
      aliases: [],
      variants: [
        { id: 'standard', icon: '🔤', label: 'Standard', desc: 'Classic 98-tile bag and standard bonus multipliers.' },
        { id: 'blitz', icon: '⚡', label: 'Blitz (36 Tiles)', desc: 'Shortened tile bag for fast, quick-finish matches.' },
        { id: 'double_bonus', icon: '💎', label: 'Super Bonus', desc: 'Boosted DL/TL/DW/TW multipliers (×3/×4/×5)!' }
      ],
      gridOptions: null,
      rules: '🔤 SCRABBLE:\nSelect tiles from your 7-tile rack and place them in a line on the 15×15 board.\nThe first word must cover the center ★ (DW). All subsequent words must connect to existing tiles and form valid dictionary words.\nScore bonus squares (DL, TL, DW, TW) to outscore your opponent!'
    },
    sudoku: {
      id: 'sudoku',
      name: 'Sudoku',
      shortName: 'Sudoku',
      icon: '🔢',
      category: 'puzzles',
      supportsDaily: true,
      supportsSolo: true,
      supportsAiVsAi: false,
      defaultWidth: 9,
      defaultHeight: 9,
      aliases: [],
      variants: [
        { id: 'standard', icon: '🔢', label: 'Classic (40)', desc: 'Balanced 9×9 Sudoku puzzle (40 blank cells).' },
        { id: 'easy_puzzle', icon: '⚡', label: 'Casual (28)', desc: 'More starting clues for a faster, lighter solve.' },
        { id: 'hard_puzzle', icon: '🔥', label: 'Master (50)', desc: 'Fewer starting clues for a tougher challenge.' }
      ],
      gridOptions: null,
      rules: '🔢 SUDOKU:\nFill every row, column, and 3×3 box with digits 1–9 without repeats.\nTap an empty cell, then tap a digit on the number pad (or press 1–9 on your keyboard).\nEarn +10 pts per digit and +20 bonus pts for completing a row, column, or 3×3 box!\nPlay 👤 Solo or 📅 Daily Puzzle to compete on the leaderboard.'
    },
    breakthrough: {
      id: 'breakthrough',
      name: 'Breakthrough',
      shortName: 'Breakthrough',
      icon: '🛡️',
      category: 'strategy',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 8,
      defaultHeight: 8,
      aliases: ['bt'],
      variants: [
        { id: 'standard', icon: '🛡️', label: 'Standard', desc: 'First pawn to reach the opponent\'s back rank wins!' },
        { id: 'misere', icon: '🔄', label: 'Misère', desc: 'Avoid reaching the back rank or capturing all enemies!' }
      ],
      gridOptions: [
        { label: '8×8 (Standard)', width: 8, height: 8 },
        { label: '6×6 (Quick)', width: 6, height: 6 }
      ],
      rules: '🛡️ BREAKTHROUGH:\nPawns move 1 square straight forward (to empty) or diagonally forward (empty or capture).\nFirst player to reach the opposite home row—or capture all enemy pawns—wins!'
    },
    mancala: {
      id: 'mancala',
      name: 'Mancala',
      shortName: 'Mancala',
      icon: '🫘',
      category: 'strategy',
      supportsDaily: false,
      supportsSolo: false,
      defaultWidth: 6,
      defaultHeight: 2,
      aliases: ['kalah'],
      variants: [
        { id: 'standard', icon: '🫘', label: 'Classic (4)', desc: '4 stones per pit. Land in your store for an extra turn; empty own pit captures opposite!' },
        { id: 'quick', icon: '⚡', label: 'Quick (3)', desc: '3 stones per pit for faster rounds.' },
        { id: 'misere', icon: '🔄', label: 'Anti-Mancala', desc: 'Reverse rules: fewest stones in your store wins!' }
      ],
      gridOptions: null,
      rules: '🫘 MANCALA (Kalah):\nTap any non-empty pit on your bottom row to sow stones counter-clockwise.\nLand your last stone in your Store for a FREE EXTRA TURN!\nLand in an empty pit on your side to capture the opposite pit\'s stones.'
    },
    wordle: {
      id: 'wordle',
      name: 'Wordle',
      shortName: 'Wordle',
      icon: '🟩',
      category: 'puzzles',
      supportsDaily: true,
      supportsSolo: true,
      supportsAiVsAi: false,
      defaultWidth: 5,
      defaultHeight: 6,
      aliases: ['wordguess'],
      variants: [
        { id: 'standard', icon: '🟩', label: 'Dictionary', desc: 'Valid 5-letter English dictionary words only (6 guesses).' },
        { id: 'any_word', icon: '⚡', label: 'Free Guess', desc: 'Allow any 5-letter combination to probe letters.' }
      ],
      gridOptions: null,
      rules: '🟩 WORDLE:\nGuess the secret 5-letter word in 6 tries!\n🟩 Green = right letter & spot • 🟨 Yellow = right letter, wrong spot • ⬛ Gray = not in word.\nPlay 📅 Daily Puzzle to compete on today\'s shared word!'
    },
    minesweeper: {
      id: 'minesweeper',
      name: 'Minesweeper',
      shortName: 'Minesweeper',
      icon: '💣',
      category: 'puzzles',
      supportsDaily: true,
      supportsSolo: true,
      supportsAiVsAi: false,
      defaultWidth: 9,
      defaultHeight: 9,
      aliases: ['mines'],
      variants: [
        { id: 'standard', icon: '💣', label: 'Standard', desc: 'Balanced minefield density.' },
        { id: 'easy_puzzle', icon: '⚡', label: 'Light Mines', desc: 'Half as many mines for a relaxed clear.' },
        { id: 'hard_puzzle', icon: '🔥', label: 'Dense Mines', desc: '50% more mines for an intense challenge!' }
      ],
      gridOptions: [
        { label: '9×9 (Standard)', width: 9, height: 9 },
        { label: '6×6 (Quick)', width: 6, height: 6 },
        { label: '12×12 (Large)', width: 12, height: 12 }
      ],
      rules: '💣 MINESWEEPER:\nTap squares to reveal safe cells and clear the board without hitting a mine.\nNumbers show how many mines touch that square. Use Flag Mode (or right-click) to mark mines!'
    }
  };

  function resolveGameId(raw) {
    if (!raw) return null;
    const key = String(raw).trim().toLowerCase();
    if (GAME_REGISTRY[key]) return key;
    for (const g of Object.values(GAME_REGISTRY)) {
      if (g.aliases && g.aliases.includes(key)) return g.id;
    }
    for (const g of Object.values(GAME_REGISTRY)) {
      if (key.includes(g.id) || (g.aliases && g.aliases.some(a => key.includes(a)))) return g.id;
    }
    return null;
  }

  function getGameMeta(game) {
    return GAME_REGISTRY[resolveGameId(game) || 'connect4'] || GAME_REGISTRY.connect4;
  }

  const VARIANT_OPTIONS = Object.fromEntries(Object.values(GAME_REGISTRY).map(g => [g.id, g.variants]));
  const GAME_CATEGORY_MAP = Object.fromEntries(Object.values(GAME_REGISTRY).map(g => [g.id, g.category]));
  const DAILY_PUZZLE_GAMES = new Set(Object.values(GAME_REGISTRY).filter(g => g.supportsDaily).map(g => g.id));
  const SOLO_GAMES = new Set(Object.values(GAME_REGISTRY).filter(g => g.supportsSolo).map(g => g.id));
  const AI_VS_AI_GAMES = new Set(Object.values(GAME_REGISTRY).filter(g => g.supportsAiVsAi !== false).map(g => g.id));
  const GRID_OPTIONS = Object.fromEntries(Object.values(GAME_REGISTRY).filter(g => g.gridOptions).map(g => [g.id, g.gridOptions]));

  function resolvePreferredVariant(opts) {
    if (!opts || opts.length === 0) return 'standard';
    if (clientDefaultVariant === 'misere') {
      const m = opts.find(o => o.id === 'misere' || o.id === 'reversed' || o.id === 'giveaway' || o.id === 'normal');
      if (m) return m.id;
    } else if (clientDefaultVariant === 'casual') {
      const c = opts.find(o => o.id === 'casual' || o.id === 'blitz' || o.id === 'easy_puzzle' || o.id === 'connect5' || o.id === 'exact5');
      if (c) return c.id;
    }
    return opts[0].id;
  }

  function updateVariantPills(game, preserveCurrent = false) {
    if (!groupVariant) return;
    const opts = VARIANT_OPTIONS[game] || VARIANT_OPTIONS.connect4;
    if (!preserveCurrent || !opts.some(o => o.id === selectedVariant)) {
      selectedVariant = resolvePreferredVariant(opts);
    }

    groupVariant.innerHTML = '';
    opts.forEach(opt => {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = `pill-btn ${opt.id === selectedVariant ? 'active' : ''}`;
      btn.dataset.variant = opt.id;
      btn.innerHTML = `<span class="pill-icon">${opt.icon}</span><span>${opt.label}</span>`;
      btn.addEventListener('click', () => {
        groupVariant.querySelectorAll('.pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        selectedVariant = opt.id;
        if (variantDescHint) variantDescHint.textContent = opt.desc;
        syncPillGroupSelect(groupVariant);
        playSound('tap');
      });
      groupVariant.appendChild(btn);
    });

    const activeOpt = opts.find(o => o.id === selectedVariant) || opts[0];
    if (variantDescHint && activeOpt) {
      variantDescHint.textContent = activeOpt.desc;
    }
    syncPillGroupSelect(groupVariant);
  }

  // Game Category Filtering
  let selectedCategory = GAME_CATEGORY_MAP[selectedGame] || 'alignment';

  function getCategoryForGame(game) {
    return GAME_CATEGORY_MAP[resolveGameId(game) || 'connect4'] || 'alignment';
  }

  function applyGameCategoryFilter(category, selectFirstIfOutside = true) {
    selectedCategory = category || 'alignment';
    const catGroup = document.getElementById('group-game-category');
    if (catGroup) {
      catGroup.querySelectorAll('.pill-btn').forEach(b => {
        b.classList.toggle('active', b.dataset.category === selectedCategory);
      });
      syncPillGroupSelect(catGroup);
    }

    const gameGroup = document.getElementById('group-game');
    if (!gameGroup) return;

    const buttons = Array.from(gameGroup.querySelectorAll('.pill-btn'));
    buttons.forEach(btn => {
      const cat = btn.dataset.category || getCategoryForGame(btn.dataset.game);
      btn.classList.toggle('hidden', cat !== selectedCategory);
    });

    if (selectFirstIfOutside && getCategoryForGame(selectedGame) !== selectedCategory) {
      const favInCat = buttons.find(b => !b.classList.contains('hidden') && b.dataset.game === clientFavGame);
      const nextBtn = favInCat || buttons.find(b => !b.classList.contains('hidden'));
      if (nextBtn) {
        selectedGame = nextBtn.dataset.game;
        buttons.forEach(b => b.classList.toggle('active', b === nextBtn));
        updateModeOptionsForGame(selectedGame);
        updateVariantPills(selectedGame);
        updateGridSizePills(selectedGame);
      }
    } else {
      buttons.forEach(b => b.classList.toggle('active', b.dataset.game === selectedGame));
    }

    syncPillGroupSelect(gameGroup);
  }

  function applySettingFavCategoryFilter(category, selectFirstIfOutside = false) {
    const cat = category || 'alignment';
    const catGroup = document.getElementById('group-setting-fav-category');
    if (catGroup) {
      catGroup.querySelectorAll('.pill-btn').forEach(b => {
        b.classList.toggle('active', b.dataset.category === cat);
      });
      syncPillGroupSelect(catGroup);
    }

    const favGroup = document.getElementById('group-setting-fav');
    if (!favGroup) return;

    const buttons = Array.from(favGroup.querySelectorAll('.pill-btn'));
    buttons.forEach(btn => {
      const btnCat = btn.dataset.category || getCategoryForGame(btn.dataset.fav);
      btn.classList.toggle('hidden', btnCat !== cat);
    });

    if (selectFirstIfOutside) {
      const activeVisible = buttons.find(b => b.classList.contains('active') && !b.classList.contains('hidden'));
      if (!activeVisible) {
        const firstVisible = buttons.find(b => !b.classList.contains('hidden'));
        if (firstVisible) {
          buttons.forEach(b => b.classList.toggle('active', b === firstVisible));
          updateSettingModeOptionsForGame(firstVisible.dataset.fav);
        }
      }
    }

    syncPillGroupSelect(favGroup);
  }

  // Mode capabilities derived from GAME_REGISTRY
  function supportsDailyMode(game) {
    return DAILY_PUZZLE_GAMES.has(game);
  }

  function supportsSoloMode(game) {
    return SOLO_GAMES.has(game);
  }

  function supportsAiVsAiMode(game) {
    return AI_VS_AI_GAMES.has(game);
  }

  function updateModeOptionsForGame(game) {
    const modeGroup = document.getElementById('group-mode');
    if (!modeGroup) return;
    const dailyAllowed = supportsDailyMode(game);
    const soloAllowed = supportsSoloMode(game);
    const aiVsAiAllowed = supportsAiVsAiMode(game);
    const dailyBtn = modeGroup.querySelector('.pill-btn[data-mode="daily"]');
    const soloBtn = modeGroup.querySelector('.pill-btn[data-mode="solo"]');
    const aiBtn = modeGroup.querySelector('.pill-btn[data-mode="vs-ai"]');
    const aiVsAiBtn = modeGroup.querySelector('.pill-btn[data-mode="ai-vs-ai"]');
    if (dailyBtn) dailyBtn.classList.toggle('hidden', !dailyAllowed);
    if (soloBtn) soloBtn.classList.toggle('hidden', !soloAllowed);
    if (aiBtn) aiBtn.classList.toggle('hidden', !aiVsAiAllowed);
    if (aiVsAiBtn) aiVsAiBtn.classList.toggle('hidden', !aiVsAiAllowed);

    const invalidMode = (!dailyAllowed && selectedMode === 'daily') ||
                        (!soloAllowed && selectedMode === 'solo') ||
                        (!aiVsAiAllowed && (selectedMode === 'vs-ai' || selectedMode === 'ai-vs-ai'));
    if (invalidMode) {
      const fallback = (clientDefaultMode === 'daily' && !dailyAllowed) ||
                       (clientDefaultMode === 'solo' && !soloAllowed) ||
                       ((clientDefaultMode === 'vs-ai' || clientDefaultMode === 'ai-vs-ai') && !aiVsAiAllowed)
        ? 'pass-and-play'
        : clientDefaultMode;
      selectedMode = fallback || 'pass-and-play';
      modeGroup.querySelectorAll('.pill-btn').forEach(b => {
        b.classList.toggle('active', b.dataset.mode === selectedMode);
      });
    }
    if (secAiDiff) secAiDiff.classList.toggle('hidden', selectedMode !== 'vs-ai' && selectedMode !== 'ai-vs-ai');
    if (secRoom) secRoom.classList.toggle('hidden', selectedMode !== 'room');
    syncPillGroupSelect(modeGroup);
  }

  function updateSettingModeOptionsForGame(favGame) {
    const settingModeGroup = document.getElementById('group-setting-mode');
    if (!settingModeGroup) return;
    const dailyAllowed = supportsDailyMode(favGame);
    const soloAllowed = supportsSoloMode(favGame);
    const aiVsAiAllowed = supportsAiVsAiMode(favGame);
    const dailyBtn = settingModeGroup.querySelector('.pill-btn[data-mode="daily"]');
    const soloBtn = settingModeGroup.querySelector('.pill-btn[data-mode="solo"]');
    const aiBtn = settingModeGroup.querySelector('.pill-btn[data-mode="vs-ai"]');
    const aiVsAiBtn = settingModeGroup.querySelector('.pill-btn[data-mode="ai-vs-ai"]');
    if (dailyBtn) dailyBtn.classList.toggle('hidden', !dailyAllowed);
    if (soloBtn) soloBtn.classList.toggle('hidden', !soloAllowed);
    if (aiBtn) aiBtn.classList.toggle('hidden', !aiVsAiAllowed);
    if (aiVsAiBtn) aiVsAiBtn.classList.toggle('hidden', !aiVsAiAllowed);

    const activeHidden = (dailyBtn && !dailyAllowed && dailyBtn.classList.contains('active')) ||
                         (soloBtn && !soloAllowed && soloBtn.classList.contains('active')) ||
                         (aiBtn && !aiVsAiAllowed && aiBtn.classList.contains('active')) ||
                         (aiVsAiBtn && !aiVsAiAllowed && aiVsAiBtn.classList.contains('active'));
    if (activeHidden) {
      settingModeGroup.querySelectorAll('.pill-btn').forEach(b => {
        b.classList.toggle('active', b.dataset.mode === 'pass-and-play');
      });
    }
    syncPillGroupSelect(settingModeGroup);
  }

  // Responsive Dropdown Synchronization (<select> on small screens, icon pills on larger screens)
  function syncPillGroupSelect(groupEl) {
    if (!groupEl) return;
    const buttons = Array.from(groupEl.querySelectorAll('.pill-btn:not(.hidden)'));
    let select = groupEl.nextElementSibling;
    if (!select || !select.classList.contains('responsive-select')) {
      select = document.createElement('select');
      select.className = 'responsive-select';
      const prevLabel = groupEl.previousElementSibling;
      if (prevLabel && prevLabel.classList.contains('section-label')) {
        select.setAttribute('aria-label', prevLabel.textContent.trim());
      }
      select.addEventListener('change', () => {
        const idx = Number(select.value);
        const currentButtons = Array.from(groupEl.querySelectorAll('.pill-btn:not(.hidden)'));
        if (currentButtons[idx]) {
          currentButtons[idx].click();
        }
      });
      groupEl.insertAdjacentElement('afterend', select);
    }

    groupEl.classList.add('has-responsive-select');
    select.innerHTML = '';
    let activeIdx = 0;
    buttons.forEach((btn, idx) => {
      const option = document.createElement('option');
      option.value = String(idx);
      option.textContent = btn.textContent.replace(/\s+/g, ' ').trim();
      if (btn.classList.contains('active')) {
        activeIdx = idx;
      }
      select.appendChild(option);
    });
    select.selectedIndex = activeIdx;
    select.style.display = buttons.length === 0 ? 'none' : '';
  }

  function syncAllResponsiveSelects() {
    document.querySelectorAll('.pill-group').forEach(groupEl => {
      syncPillGroupSelect(groupEl);
    });
  }

  // Dynamic Grid Size Pills (options derived from GAME_REGISTRY)

  function updateGridSizePills(game) {
    const secGridSize = document.getElementById('sec-grid-size');
    if (!groupGridSize) return;
    const opts = GRID_OPTIONS[game];
    if (!opts) {
      if (secGridSize) secGridSize.style.display = 'none';
      selectedGridWidth = null;
      selectedGridHeight = null;
      return;
    }
    if (secGridSize) secGridSize.style.display = '';
    groupGridSize.innerHTML = '';

    let match = opts.find(o => o.width === selectedGridWidth && o.height === selectedGridHeight);
    if (!match) {
      if (clientDefaultSize === 'compact' && opts.length > 1) {
        match = opts[1];
      } else if (clientDefaultSize === 'large' && opts.length > 1) {
        match = opts[opts.length - 1];
      } else {
        match = opts[0];
      }
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
        syncPillGroupSelect(groupGridSize);
        playSound('tap');
      });
      groupGridSize.appendChild(btn);
    });
    syncPillGroupSelect(groupGridSize);
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

  async function sendPrediction(playerIndex) {
    if (!currentSession) return;
    try {
      const res = await fetch(`/api/session/${currentSession.id}/predict`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          playerIndex: Number(playerIndex)
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
  function simulateBoardAtStep(session, stepIndex) {
    const history = session.moveHistory || [];
    if (stepIndex >= 0 && stepIndex < history.length) {
      const step = history[stepIndex];
      const stepGrid = step.grid || step.Grid;
      if (Array.isArray(stepGrid) && stepGrid.length >= session.height) {
        const lx = step.x !== undefined ? step.x : step.X;
        const ly = step.y !== undefined ? step.y : step.Y;
        return {
          ...session,
          grid: stepGrid,
          lastMoveX: lx !== undefined ? lx : null,
          lastMoveY: ly !== undefined ? ly : null,
          status: (stepIndex === history.length - 1) ? session.status : 'InProgress',
          isAiTurn: false
        };
      }
    }

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

    const limit = Math.min(stepIndex, history.length - 1);
    let lastX = null;
    let lastY = null;

    for (let i = 0; i <= limit; i++) {
      const step = history[i];
      const rawX = step.x !== undefined ? step.x : step.X;
      const rawY = step.y !== undefined ? step.y : step.Y;
      const moveNum = step.moveNumber !== undefined ? step.moveNumber : step.MoveNumber;
      if (rawX === null || rawX === undefined || moveNum === 0) continue;

      const px = Number(rawX);
      const py = Number(rawY);
      const piece = step.piece || step.Piece || (moveNum % 2 === 1 ? 'R' : 'Y');

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
      status: (limit === history.length - 1) ? session.status : 'InProgress',
      isAiTurn: false
    };
  }

  function scrubReplay(action) {
    if (!currentSession || !currentSession.moveHistory) return;
    const maxStep = Math.max(0, currentSession.moveHistory.length - 1);
    if (maxStep === 0) return;

    let target = replayIndex !== null ? replayIndex : maxStep;

    if (action === 'start') {
      target = 0;
    } else if (action === 'prev') {
      target = Math.max(0, target - 1);
    } else if (action === 'next') {
      target = Math.min(maxStep, target + 1);
    } else if (typeof action === 'number') {
      target = Math.max(0, Math.min(maxStep, action));
    }

    if (target === maxStep) {
      returnToLive();
      return;
    }

    replayIndex = target;
    if (btnReplayLive) btnReplayLive.classList.remove('hidden');
    if (replayStepText) replayStepText.textContent = `Replay: Move ${replayIndex} of ${maxStep}`;

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
      const maxStep = currentSession.moveHistory ? Math.max(0, currentSession.moveHistory.length - 1) : 0;
      if (replayStepText) replayStepText.textContent = `Live • Move ${maxStep} of ${maxStep}`;
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
    const maxStep = Math.max(0, currentSession.moveHistory.length - 1);
    if (maxStep === 0) return;

    if (replayIndex === null || replayIndex >= maxStep) {
      replayIndex = 0;
      scrubReplay(0);
    }

    if (btnReplayPlay) btnReplayPlay.textContent = '⏸️';

    replayAutoPlayTimer = setInterval(() => {
      if (!currentSession || !currentSession.moveHistory) {
        toggleAutoPlay();
        return;
      }
      const max = Math.max(0, currentSession.moveHistory.length - 1);
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
    applySettingFavCategoryFilter(getCategoryForGame(clientFavGame), false);

    // Set default play mode active pill
    document.querySelectorAll('#group-setting-mode .pill-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.mode === clientDefaultMode);
    });
    updateSettingModeOptionsForGame(clientFavGame);

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

    // Set default variant / difficulty / board size / turn timer active pills
    document.querySelectorAll('#group-setting-variant .pill-btn').forEach(btn => {
      btn.classList.toggle('active', (btn.dataset.variant || 'standard') === clientDefaultVariant);
    });
    document.querySelectorAll('#group-setting-diff .pill-btn').forEach(btn => {
      btn.classList.toggle('active', (btn.dataset.diff || 'smart') === clientDefaultDiff);
    });
    document.querySelectorAll('#group-setting-size .pill-btn').forEach(btn => {
      btn.classList.toggle('active', (btn.dataset.size || 'standard') === clientDefaultSize);
    });
    document.querySelectorAll('#group-setting-timer .pill-btn').forEach(btn => {
      btn.classList.toggle('active', String(btn.dataset.timer || '0') === String(clientDefaultTimer));
    });

    renderStatsUI();
    syncAllResponsiveSelects();

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
      applyGameCategoryFilter(getCategoryForGame(fav), false);
      updateGridSizePills(fav);
    }

    const activeModeBtn = document.querySelector('#group-setting-mode .pill-btn.active');
    if (activeModeBtn) {
      const mode = activeModeBtn.dataset.mode || 'pass-and-play';
      clientDefaultMode = mode;
      selectedMode = mode;
      localStorage.setItem('bg_default_mode', mode);
      document.querySelectorAll('#group-mode .pill-btn').forEach(b => {
        b.classList.toggle('active', b.dataset.mode === mode);
      });
      if (secAiDiff) secAiDiff.classList.toggle('hidden', mode !== 'vs-ai');
      if (secRoom) secRoom.classList.toggle('hidden', mode !== 'room');
    }
    updateSettingModeOptionsForGame(selectedGame);
    updateModeOptionsForGame(selectedGame);

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

    const activeVariantBtn = document.querySelector('#group-setting-variant .pill-btn.active');
    if (activeVariantBtn) {
      const v = activeVariantBtn.dataset.variant || 'standard';
      clientDefaultVariant = v;
      localStorage.setItem('bg_default_variant', v);
    }

    const activeDiffBtn = document.querySelector('#group-setting-diff .pill-btn.active');
    if (activeDiffBtn) {
      const d = activeDiffBtn.dataset.diff || 'smart';
      clientDefaultDiff = d;
      localStorage.setItem('bg_default_diff', d);
      selectedDiff = d;
      document.querySelectorAll('#group-diff .pill-btn').forEach(btn => {
        btn.classList.toggle('active', (btn.dataset.diff || btn.dataset.difficulty) === d);
      });
    }

    const activeSizeBtn = document.querySelector('#group-setting-size .pill-btn.active');
    if (activeSizeBtn) {
      const s = activeSizeBtn.dataset.size || 'standard';
      clientDefaultSize = s;
      localStorage.setItem('bg_default_size', s);
      selectedGridWidth = null;
      selectedGridHeight = null;
    }

    const activeTimerBtn = document.querySelector('#group-setting-timer .pill-btn.active');
    if (activeTimerBtn) {
      const t = Number(activeTimerBtn.dataset.timer || 0);
      clientDefaultTimer = t;
      localStorage.setItem('bg_default_timer', String(t));
      selectedTimerSeconds = t;
      document.querySelectorAll('#group-turn-timer .pill-btn').forEach(btn => {
        btn.classList.toggle('active', Number(btn.dataset.timer || 0) === t);
      });
    }

    updateVariantPills(selectedGame);
    updateGridSizePills(selectedGame);
    syncAllResponsiveSelects();

    playSound('tap');
    triggerHaptic(20);
    if (modalSettings) modalSettings.classList.add('hidden');
  }

  // Build a direct shareable URL that immediately starts a specific game & mode
  function buildDirectGameUrl(baseOrigin, config = {}) {
    const origin = (baseOrigin || window.location.origin).replace(/\/$/, '');
    const game = resolveGameId(config.game || selectedGame) || 'connect4';
    let mode = config.mode || selectedMode || 'pass-and-play';
    if (mode === 'daily' && !supportsDailyMode(game)) {
      mode = 'pass-and-play';
    }
    if (mode === 'solo' && !supportsSoloMode(game)) {
      mode = 'pass-and-play';
    }
    if ((mode === 'vs-ai' || mode === 'ai-vs-ai') && !supportsAiVsAiMode(game)) {
      mode = 'pass-and-play';
    }
    const urlParams = new URLSearchParams();
    urlParams.set('game', game);
    urlParams.set('mode', mode);
    if (config.variant && config.variant !== 'standard') {
      urlParams.set('variant', config.variant);
    }
    if ((mode === 'vs-ai' || mode === 'ai-vs-ai') && config.diff) {
      urlParams.set('diff', config.diff);
    }
    if (config.width && config.height && GRID_OPTIONS[game]) {
      urlParams.set('size', `${config.width}x${config.height}`);
    }
    if (Number(config.timer || 0) > 0) {
      urlParams.set('timer', String(Number(config.timer)));
    }
    return `${origin}/?${urlParams.toString()}`;
  }

  // Check URL parameters for direct room joining, spectating, or game shortcut links
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

    const rawGame = params.get('game');
    const rawMode = params.get('mode');
    if (!rawGame && !rawMode) {
      return false;
    }

    let g = resolveGameId(rawGame);
    const normalizedRawMode = rawMode ? rawMode.trim().toLowerCase() : '';
    if (!g && (normalizedRawMode === 'daily' || normalizedRawMode === 'solo')) {
      g = 'sudoku';
    }
    if (!g || !VARIANT_OPTIONS[g]) {
      return false;
    }

    const modeAliases = {
      ai: 'vs-ai',
      bot: 'vs-ai',
      aivsai: 'ai-vs-ai',
      bots: 'ai-vs-ai',
      local: 'pass-and-play',
      pass: 'pass-and-play',
      wifi: 'room'
    };
    let m = (rawMode || 'pass-and-play').trim().toLowerCase();
    m = modeAliases[m] || m;
    const validModes = new Set(['pass-and-play', 'vs-ai', 'room', 'ai-vs-ai', 'solo', 'daily']);
    if (!validModes.has(m)) {
      m = 'pass-and-play';
    }
    if (m === 'daily' && !supportsDailyMode(g)) {
      m = 'pass-and-play';
    }
    if (m === 'solo' && !supportsSoloMode(g)) {
      m = 'pass-and-play';
    }
    if ((m === 'vs-ai' || m === 'ai-vs-ai') && !supportsAiVsAiMode(g)) {
      m = 'pass-and-play';
    }

    const varOpts = VARIANT_OPTIONS[g] || [];
    const rawVariant = (params.get('variant') || '').trim().toLowerCase();
    const validVar = varOpts.find(v => v.id === rawVariant);
    selectedGame = g;
    selectedMode = m;
    selectedVariant = validVar ? validVar.id : 'standard';

    const rawDiff = (params.get('diff') || '').trim().toLowerCase();
    if (rawDiff === 'easy' || rawDiff === 'smart') {
      selectedDiff = rawDiff;
    }

    selectedGridWidth = null;
    selectedGridHeight = null;
    const rawSize = (params.get('size') || '').trim().toLowerCase();
    const sizeMatch = rawSize.match(/^(\d+)\s*[x×]\s*(\d+)$/);
    if (sizeMatch) {
      selectedGridWidth = Number(sizeMatch[1]);
      selectedGridHeight = Number(sizeMatch[2]);
    } else {
      const rawW = Number(params.get('width'));
      const rawH = Number(params.get('height'));
      if (rawW > 0 && rawH > 0) {
        selectedGridWidth = rawW;
        selectedGridHeight = rawH;
      }
    }

    const rawTimer = params.get('timer');
    if (rawTimer !== null && rawTimer !== '') {
      const t = Number(rawTimer);
      if ([0, 15, 30, 60].includes(t)) {
        selectedTimerSeconds = t;
      }
    }

    document.querySelectorAll('#group-game .pill-btn').forEach(b => {
      b.classList.toggle('active', b.dataset.game === selectedGame);
    });
    applyGameCategoryFilter(getCategoryForGame(selectedGame), false);
    document.querySelectorAll('#group-mode .pill-btn').forEach(b => {
      b.classList.toggle('active', b.dataset.mode === selectedMode);
    });
    updateModeOptionsForGame(selectedGame);
    if (secAiDiff) secAiDiff.classList.toggle('hidden', selectedMode !== 'vs-ai' && selectedMode !== 'ai-vs-ai');
    if (secRoom) secRoom.classList.toggle('hidden', selectedMode !== 'room');
    updateVariantPills(selectedGame, true);
    updateGridSizePills(selectedGame);
    document.querySelectorAll('#group-diff .pill-btn').forEach(b => {
      b.classList.toggle('active', (b.dataset.diff || b.dataset.difficulty) === selectedDiff);
    });
    document.querySelectorAll('#group-turn-timer .pill-btn').forEach(b => {
      b.classList.toggle('active', Number(b.dataset.timer || 0) === Number(selectedTimerSeconds));
    });
    syncAllResponsiveSelects();

    const p2Name = selectedMode === 'daily'
      ? 'Daily Challenge'
      : (selectedMode === 'solo' ? 'Solo' : (selectedMode === 'ai-vs-ai' ? 'AI Bot 2' : 'Player 2'));
    createSession(
      selectedGame,
      selectedMode,
      selectedDiff,
      clientPlayerName || 'Player 1',
      p2Name,
      true,
      selectedGridWidth,
      selectedGridHeight,
      selectedTimerSeconds,
      selectedVariant
    );
    return true;
  }

  // Create new session
  async function createSession(game, mode, diff, p1, p2, isPublic = true, customWidth = null, customHeight = null, timerSeconds = 0, variant = selectedVariant) {
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
          timerSeconds: Number(timerSeconds || 0),
          variant: variant || 'standard'
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
        timerSeconds: timerSeconds,
        variant: variant || 'standard'
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
    const modeLabels = { 'pass-and-play': 'Pass & Play', 'vs-ai': 'vs AI', 'room': 'Room', 'solo': 'Solo', 'daily': 'Daily' };

    rooms.forEach(r => {
      const code = r.roomCode || r.RoomCode || '';
      const sid = r.sessionId || r.SessionId || '';
      const game = (r.gameType || r.GameType || 'connect4').toLowerCase();
      const meta = getGameMeta(game);
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
        codeBadgeHtml = `<span class="lobby-code-badge">${escapeHtml(code)}</span>`;
      }

      card.innerHTML = `
        <div class="lobby-card-left">
          <span class="lobby-card-icon">${meta.icon || '🎲'}</span>
          <div class="lobby-card-info">
            <h4>${escapeHtml(meta.shortName || game)} <span class="lobby-mode-badge ${modeClass}">${escapeHtml(modeLabels[mode] || mode)}</span> ${codeBadgeHtml}</h4>
            <div class="lobby-card-meta">Host: ${escapeHtml(host)} • ${escapeHtml(time)} <span class="lobby-status-badge ${statusClass}">${statusLabel}</span></div>
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
  async function postMovePayload(payload) {
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
        body: JSON.stringify(payload)
      });
      const data = await res.json();
      if (!res.ok) {
        triggerHaptic([30, 50, 30]);
        if (data && data.error) {
          showSpeechBubble(data.error, 'Rules');
        }
        return;
      }
      const sessionObj = data.session || data.Session;
      if (sessionObj) onSessionUpdated(sessionObj);
    } catch (e) { }
  }

  async function submitMove(x, y) {
    return postMovePayload({
      x: Number(x),
      y: Number(y),
      playerToken: playerToken
    });
  }

  async function submitPieceMove(fromX, fromY, toX, toY) {
    return postMovePayload({
      x: Number(fromX),
      y: Number(fromY),
      toX: Number(toX),
      toY: Number(toY),
      playerToken: playerToken
    });
  }

  async function submitWordMove(placements, isPass) {
    const first = placements && placements.length > 0 ? placements[0] : { x: 0, y: 0 };
    return postMovePayload({
      x: Number(first.x || 0),
      y: Number(first.y || 0),
      placements: placements || [],
      isPass: Boolean(isPass),
      playerToken: playerToken
    });
  }

  async function submitSudokuMove(x, y, digit) {
    return postMovePayload({
      x: Number(x),
      y: Number(y),
      digit: Number(digit),
      playerToken: playerToken
    });
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

  let aiExhibitionInFlight = false;
  async function stepAiExhibition(sessionId) {
    if (!sessionId || aiExhibitionInFlight) return;
    aiExhibitionInFlight = true;
    try {
      const res = await fetch(`/api/session/${sessionId}/ai-step`, { method: 'POST' });
      if (res.ok) {
        const data = await res.json();
        const sessionObj = data.session || data.Session;
        if (sessionObj) onSessionUpdated(sessionObj);
      }
    } catch (e) { } finally {
      aiExhibitionInFlight = false;
    }
  }

  function syncAiExhibitionLoop(session) {
    if (aiExhibitionTimer) {
      clearInterval(aiExhibitionTimer);
      aiExhibitionTimer = null;
    }
    if (!session || session.mode !== 'ai-vs-ai' || session.status !== 'InProgress' || isSpectator || aiExhibitionPaused) {
      return;
    }
    aiExhibitionTimer = setInterval(() => {
      if (!currentSession || currentSession.mode !== 'ai-vs-ai' || currentSession.status !== 'InProgress' || aiExhibitionPaused) {
        clearInterval(aiExhibitionTimer);
        aiExhibitionTimer = null;
        return;
      }
      stepAiExhibition(currentSession.id);
    }, 550);
  }

  // Handle session updates
  function onSessionUpdated(rawSession) {
    const session = normalizeSession(rawSession);
    if (!session) return;

    const prevStatus = currentSession ? currentSession.status : null;
    const prevMoveCount = currentSession ? currentSession.moveCount : 0;
    const prevId = currentSession ? currentSession.id : null;
    if (prevId !== session.id) {
      aiExhibitionPaused = false;
    }
    if (prevId !== session.id || prevMoveCount !== session.moveCount) {
      selectedChecker = null;
      selectedBreakthroughPiece = null;
      wordleCurrentGuess = '';
      selectedScrabbleTileIndex = null;
      pendingScrabblePlacements = [];
      selectedSudokuCell = null;
      highlightedSudokuDigit = null;
    }
    currentSession = session;

    if (session.moveCount === 0 && session.player1Predictions === 0 && session.player2Predictions === 0) {
      hasVotedPrediction = false;
    }

    // Persist active session to localStorage for auto-reconnect
    try {
      localStorage.setItem('bg_session_id', session.id);
      localStorage.setItem('bg_is_spectator', String(Boolean(isSpectator)));
    } catch (e) { }

    // Header info
    const modeNames = { 'pass-and-play': 'Pass & Play', 'vs-ai': 'vs AI', 'room': 'Wi-Fi Room', 'ai-vs-ai': '⚔️ AI vs AI', 'daily': '📅 Daily Puzzle', 'solo': '👤 Solo' };
    gameTitle.textContent = getGameMeta(session.gameType).name || 'Board Game';
    const vOpts = VARIANT_OPTIONS[session.gameType] || [];
    const vOpt = vOpts.find(v => v.id === session.variant);
    const vLabel = vOpt ? vOpt.label : session.variant;
    const baseMode = modeNames[session.mode] || session.mode;
    const modeLabel = (session.variant && session.variant !== 'standard')
      ? `${baseMode} • ${vLabel}`
      : baseMode;
    if (isSpectator) {
      modeBadge.textContent = `👀 Spectating (${modeLabel})`;
    } else {
      modeBadge.textContent = modeLabel;
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
      const totalVotes = (session.player1Predictions || 0) + (session.player2Predictions || 0);
      const showPrediction = isSpectator || totalVotes > 0;
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
      const maxStep = session.moveHistory ? Math.max(0, session.moveHistory.length - 1) : 0;
      const hasHistory = maxStep > 0;
      replayBar.classList.toggle('hidden', !hasHistory);
      if (hasHistory && replayIndex === null) {
        if (replayStepText) replayStepText.textContent = `Live • Move ${maxStep} of ${maxStep}`;
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

    syncAiExhibitionLoop(session);
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

    if (session.mode === 'ai-vs-ai' && session.status === 'InProgress' && !isSpectator && replayIndex === null) {
      const aiBar = document.createElement('div');
      aiBar.className = 'ai-exhibition-bar';

      const btnPause = document.createElement('button');
      btnPause.type = 'button';
      btnPause.className = 'btn btn-xs btn-secondary';
      btnPause.textContent = aiExhibitionPaused ? '▶ Resume AI' : '⏸ Pause AI';
      btnPause.addEventListener('click', () => {
        aiExhibitionPaused = !aiExhibitionPaused;
        syncAiExhibitionLoop(session);
        renderBoard(session);
      });

      const btnStep = document.createElement('button');
      btnStep.type = 'button';
      btnStep.className = 'btn btn-xs btn-primary';
      btnStep.textContent = '⏭ Step Move';
      btnStep.addEventListener('click', () => stepAiExhibition(session.id));

      aiBar.appendChild(btnPause);
      aiBar.appendChild(btnStep);
      boardContainer.appendChild(aiBar);
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
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !isSpectator && replayIndex === null;
    if (!canPlay) {
      hint.textContent = session.lastMoveText || 'Breakthrough';
    } else if (selectedBreakthroughPiece) {
      hint.textContent = `Pawn (${selectedBreakthroughPiece.x}, ${selectedBreakthroughPiece.y}) selected — tap forward square`;
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
        const isSelected = selectedBreakthroughPiece && selectedBreakthroughPiece.x === x && selectedBreakthroughPiece.y === y;
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
              selectedBreakthroughPiece = (selectedBreakthroughPiece && selectedBreakthroughPiece.x === x && selectedBreakthroughPiece.y === y)
                ? null
                : { x, y };
              playSound('tap');
              renderBoard(session);
            } else if (selectedBreakthroughPiece) {
              const fromX = selectedBreakthroughPiece.x;
              const fromY = selectedBreakthroughPiece.y;
              selectedBreakthroughPiece = null;
              submitPieceMove(fromX, fromY, x, y);
            }
          });
        } else {
          cell.disabled = true;
        }

        grid.appendChild(cell);
      }
    }

    wrapper.appendChild(grid);
    boardContainer.appendChild(wrapper);
  }

  function renderMancala(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'checkers-wrapper';
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !isSpectator && replayIndex === null;

    const hint = document.createElement('div');
    hint.className = 'checkers-hint';
    hint.textContent = session.lastMoveText || 'Tap a pit on your row to sow stones counter-clockwise';
    wrapper.appendChild(hint);

    const board = document.createElement('div');
    board.className = 'mancala-board';

    // Left Store = Player 2
    const p2Store = document.createElement('div');
    p2Store.className = 'mancala-store';
    p2Store.innerHTML = `<span class="mancala-store-name">${escapeHtml(session.player2Name)}</span><span class="mancala-store-val">${session.player2GamePoints || 0}</span>`;
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
        btn.addEventListener('click', () => submitMove(x, y));
        pitsWrap.appendChild(btn);
      }
    }
    board.appendChild(pitsWrap);

    // Right Store = Player 1
    const p1Store = document.createElement('div');
    p1Store.className = 'mancala-store';
    p1Store.innerHTML = `<span class="mancala-store-name">${escapeHtml(session.player1Name)}</span><span class="mancala-store-val">${session.player1GamePoints || 0}</span>`;
    board.appendChild(p1Store);

    wrapper.appendChild(board);
    boardContainer.appendChild(wrapper);
  }

  function renderWordle(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'wordle-wrapper';
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !isSpectator && replayIndex === null;

    // HUD
    const hud = document.createElement('div');
    hud.className = 'sudoku-hud';
    hud.innerHTML = `
      <span class="scrabble-score-pill active">${escapeHtml(session.player1Name)}: <strong>${session.player1GamePoints || 0} pts</strong></span>
      <button type="button" class="btn btn-xs btn-secondary wordle-lb-btn">🏆 Leaderboard</button>
    `;
    const lbBtn = hud.querySelector('.wordle-lb-btn');
    if (lbBtn) {
      lbBtn.addEventListener('click', () => openLeaderboard('wordle', session.mode === 'daily' ? 'daily' : 'all'));
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
        } else if (canPlay && y === activeRow && x < wordleCurrentGuess.length) {
          cell.classList.add('filled');
          cell.textContent = wordleCurrentGuess[x];
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
      if (!canPlay || activeRow >= session.height || wordleCurrentGuess.length !== 5) return;
      const placements = Array.from(wordleCurrentGuess).map((ch, x) => ({ x, y: activeRow, letter: ch }));
      submitWordMove(placements, false);
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
            if (wordleCurrentGuess.length > 0) {
              wordleCurrentGuess = wordleCurrentGuess.slice(0, -1);
              playSound('tap');
              renderBoard(session);
            }
          } else if (wordleCurrentGuess.length < 5) {
            wordleCurrentGuess += k;
            playSound('tap');
            renderBoard(session);
          }
        });
        rowEl.appendChild(btn);
      });
      kb.appendChild(rowEl);
    });
    wrapper.appendChild(kb);
    boardContainer.appendChild(wrapper);
  }

  function renderMinesweeper(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'ms-wrapper';
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !isSpectator && replayIndex === null;

    const toolbar = document.createElement('div');
    toolbar.className = 'ms-toolbar';

    const scorePill = document.createElement('span');
    scorePill.className = 'scrabble-score-pill active';
    scorePill.innerHTML = `Score: <strong>${session.player1GamePoints || 0}</strong>`;

    const btnFlag = document.createElement('button');
    btnFlag.type = 'button';
    btnFlag.className = `btn btn-xs ${minesweeperFlagMode ? 'btn-primary' : 'btn-secondary'}`;
    btnFlag.textContent = minesweeperFlagMode ? '🚩 Flag Mode: ON' : '⛏️ Reveal Mode';
    btnFlag.disabled = !canPlay;
    btnFlag.addEventListener('click', () => {
      minesweeperFlagMode = !minesweeperFlagMode;
      playSound('tap');
      renderBoard(session);
    });

    const btnLb = document.createElement('button');
    btnLb.type = 'button';
    btnLb.className = 'btn btn-xs btn-secondary';
    btnLb.textContent = '🏆 Ranks';
    btnLb.addEventListener('click', () => openLeaderboard('minesweeper', session.mode === 'daily' ? 'daily' : 'all'));

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
            if (minesweeperFlagMode) {
              submitSudokuMove(x, y, 1);
            } else if (val === 'H') {
              submitMove(x, y);
            }
          });
          cell.addEventListener('contextmenu', (e) => {
            e.preventDefault();
            submitSudokuMove(x, y, 1);
          });
        } else {
          cell.disabled = true;
        }

        grid.appendChild(cell);
      }
    }

    wrapper.appendChild(grid);
    boardContainer.appendChild(wrapper);
  }

  function renderSudoku(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'sudoku-wrapper';

    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !isSpectator && replayIndex === null;

    // HUD
    const hud = document.createElement('div');
    hud.className = 'sudoku-hud';
    const isSoloDaily = session.mode === 'daily' || session.mode === 'solo';
    if (isSoloDaily) {
      hud.innerHTML = `
        <span class="scrabble-score-pill active">${escapeHtml(session.player1Name)}: <strong>${session.player1GamePoints || 0} pts</strong></span>
        <button type="button" class="btn btn-xs btn-secondary sudoku-lb-btn">🏆 Leaderboard</button>
      `;
    } else {
      hud.innerHTML = `
        <span class="scrabble-score-pill ${session.currentTurnIndex === 0 ? 'active' : ''}">${escapeHtml(session.player1Name)}: <strong>${session.player1GamePoints || 0}</strong></span>
        <button type="button" class="btn btn-xs btn-secondary sudoku-lb-btn">🏆 Ranks</button>
        <span class="scrabble-score-pill ${session.currentTurnIndex === 1 ? 'active' : ''}">${escapeHtml(session.player2Name)}: <strong>${session.player2GamePoints || 0}</strong></span>
      `;
    }
    const lbShortcut = hud.querySelector('.sudoku-lb-btn');
    if (lbShortcut) {
      lbShortcut.addEventListener('click', () => {
        openLeaderboard('sudoku', session.mode === 'daily' ? 'daily' : 'all');
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
        const isSelected = selectedSudokuCell && selectedSudokuCell.x === x && selectedSudokuCell.y === y;
        const isSameDigit = highlightedSudokuDigit && digit === highlightedSudokuDigit;
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
            highlightedSudokuDigit = highlightedSudokuDigit === digit ? null : digit;
            selectedSudokuCell = null;
            playSound('tap');
            renderBoard(session);
          } else if (canPlay) {
            selectedSudokuCell = { x, y };
            highlightedSudokuDigit = null;
            playSound('tap');
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
      btn.disabled = !canPlay || !selectedSudokuCell;
      btn.addEventListener('click', () => {
        if (!canPlay || !selectedSudokuCell) return;
        const { x, y } = selectedSudokuCell;
        submitSudokuMove(x, y, d);
      });
      numpad.appendChild(btn);
    }
    wrapper.appendChild(numpad);

    boardContainer.appendChild(wrapper);
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
    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !isSpectator && replayIndex === null;
    if (!canPlay) {
      hint.textContent = session.lastMoveText || 'Checkers';
    } else if (selectedChecker) {
      hint.textContent = `Piece (${selectedChecker.x}, ${selectedChecker.y}) selected — tap destination square`;
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
        const isSelected = selectedChecker && selectedChecker.x === x && selectedChecker.y === y;
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
              if (selectedChecker && selectedChecker.x === x && selectedChecker.y === y) {
                selectedChecker = null;
              } else {
                selectedChecker = { x, y };
              }
              playSound('tap');
              renderBoard(session);
            } else if (selectedChecker && (val === '' || val === ' ')) {
              const fromX = selectedChecker.x;
              const fromY = selectedChecker.y;
              selectedChecker = null;
              submitPieceMove(fromX, fromY, x, y);
            }
          });
        } else {
          cell.disabled = true;
        }

        grid.appendChild(cell);
      }
    }

    wrapper.appendChild(grid);
    boardContainer.appendChild(wrapper);
  }

  const SCRABBLE_LETTER_POINTS = {
    A: 1, B: 3, C: 3, D: 2, E: 1, F: 4, G: 2, H: 4, I: 1, J: 8, K: 5, L: 1, M: 3,
    N: 1, O: 1, P: 3, Q: 10, R: 1, S: 1, T: 1, U: 1, V: 4, W: 4, X: 8, Y: 4, Z: 10
  };

  function renderScrabble(session) {
    const wrapper = document.createElement('div');
    wrapper.className = 'scrabble-wrapper';

    const canPlay = session.status === 'InProgress' && !session.isAiTurn && !isSpectator && replayIndex === null;
    const rack = Array.isArray(session.currentPlayerRack) ? session.currentPlayerRack : [];
    const usedIndices = new Set(pendingScrabblePlacements.map(p => p.rackIndex));

    // Score & Bag HUD
    const hud = document.createElement('div');
    hud.className = 'scrabble-hud';
    hud.innerHTML = `
      <span class="scrabble-score-pill ${session.currentTurnIndex === 0 ? 'active' : ''}">${escapeHtml(session.player1Name)}: <strong>${session.player1GamePoints || 0}</strong></span>
      <span class="scrabble-bag-pill">Bag: <strong>${session.tileBagCount || 0}</strong></span>
      <span class="scrabble-score-pill ${session.currentTurnIndex === 1 ? 'active' : ''}">${escapeHtml(session.player2Name)}: <strong>${session.player2GamePoints || 0}</strong></span>
    `;
    wrapper.appendChild(hud);

    // 15x15 Board
    const grid = document.createElement('div');
    grid.className = 'scrabble-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const boardVal = (session.grid[y] && session.grid[y][x]) ? session.grid[y][x] : ' ';
        const pending = pendingScrabblePlacements.find(p => p.x === x && p.y === y);
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
          tile.innerHTML = `${escapeHtml(boardVal)}<sub>${pts}</sub>`;
          cell.appendChild(tile);
          cell.disabled = true;
        } else if (pending) {
          const tile = document.createElement('span');
          tile.className = 'scrabble-tile pending-tile';
          const pts = SCRABBLE_LETTER_POINTS[pending.letter.toUpperCase()] || 0;
          tile.innerHTML = `${escapeHtml(pending.letter)}<sub>${pts}</sub>`;
          cell.appendChild(tile);
          if (canPlay) {
            cell.addEventListener('click', () => {
              pendingScrabblePlacements = pendingScrabblePlacements.filter(p => !(p.x === x && p.y === y));
              playSound('tap');
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
              if (selectedScrabbleTileIndex === null || usedIndices.has(selectedScrabbleTileIndex)) {
                // Auto-select first unused rack tile if user taps the board first
                const nextIdx = rack.findIndex((_, idx) => !usedIndices.has(idx));
                if (nextIdx === -1) return;
                selectedScrabbleTileIndex = nextIdx;
              }
              const letter = rack[selectedScrabbleTileIndex];
              if (!letter) return;
              pendingScrabblePlacements.push({ x, y, letter, rackIndex: selectedScrabbleTileIndex });
              selectedScrabbleTileIndex = null;
              playSound('tap');
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
          (idx === selectedScrabbleTileIndex ? ' selected' : '') +
          (isUsed ? ' used' : '');
        btn.disabled = !canPlay || isUsed;
        const pts = SCRABBLE_LETTER_POINTS[letter.toUpperCase()] || 0;
        btn.innerHTML = `${escapeHtml(letter)}<sub>${pts}</sub>`;
        btn.addEventListener('click', () => {
          selectedScrabbleTileIndex = (selectedScrabbleTileIndex === idx) ? null : idx;
          playSound('tap');
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
    btnSubmit.disabled = !canPlay || pendingScrabblePlacements.length === 0;
    btnSubmit.addEventListener('click', () => {
      const payload = pendingScrabblePlacements.map(p => ({ x: p.x, y: p.y, letter: p.letter }));
      submitWordMove(payload, false);
    });

    const btnClear = document.createElement('button');
    btnClear.type = 'button';
    btnClear.className = 'btn btn-sm btn-secondary';
    btnClear.textContent = 'Clear';
    btnClear.disabled = !canPlay || pendingScrabblePlacements.length === 0;
    btnClear.addEventListener('click', () => {
      pendingScrabblePlacements = [];
      selectedScrabbleTileIndex = null;
      playSound('tap');
      renderBoard(session);
    });

    const btnPass = document.createElement('button');
    btnPass.type = 'button';
    btnPass.className = 'btn btn-sm btn-secondary';
    btnPass.textContent = 'Pass Turn';
    btnPass.disabled = !canPlay;
    btnPass.addEventListener('click', () => {
      pendingScrabblePlacements = [];
      selectedScrabbleTileIndex = null;
      submitWordMove([], true);
    });

    actions.appendChild(btnSubmit);
    actions.appendChild(btnClear);
    actions.appendChild(btnPass);
    wrapper.appendChild(actions);

    boardContainer.appendChild(wrapper);
  }

  function renderGomoku(session) {
    const grid = document.createElement('div');
    grid.className = 'gomoku-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    const lastMove = session.lastMove;
    const canPlay = !isSpectator;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const val = session.grid[y] && session.grid[y][x] ? session.grid[y][x] : '';
        const isLastMove = lastMove && lastMove.x === x && lastMove.y === y;
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
          cell.addEventListener('click', () => submitMove(x, y));
        }

        grid.appendChild(cell);
      }
    }

    boardContainer.appendChild(grid);
  }

  function renderOthello(session) {
    const grid = document.createElement('div');
    grid.className = 'othello-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    const lastMove = session.lastMove;
    const canPlay = !isSpectator;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const val = session.grid[y] && session.grid[y][x] ? session.grid[y][x] : '';
        const isLastMove = lastMove && lastMove.x === x && lastMove.y === y;
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
          cell.addEventListener('click', () => submitMove(x, y));
        }

        grid.appendChild(cell);
      }
    }

    boardContainer.appendChild(grid);
  }

  function renderNim(session) {
    const grid = document.createElement('div');
    grid.className = 'nim-grid';
    grid.style.gridTemplateColumns = `repeat(${session.width}, 1fr)`;

    const canPlay = !isSpectator;

    for (let y = 0; y < session.height; y++) {
      for (let x = 0; x < session.width; x++) {
        const val = session.grid[y] && session.grid[y][x] ? session.grid[y][x] : '';
        const hasStone = val === 'S';
        const cell = document.createElement('button');
        cell.className = 'nim-cell' + (hasStone ? '' : ' nim-empty');
        cell.disabled = !canPlay || !hasStone;
        cell.setAttribute('aria-label', `Row ${y + 1}, Stone ${x + 1}`);

        if (canPlay && hasStone) {
          cell.addEventListener('click', () => submitMove(x, y));
        }

        grid.appendChild(cell);
      }
    }

    boardContainer.appendChild(grid);
  }

  function renderHex(session) {
    const grid = document.createElement('div');
    grid.className = 'hex-grid';

    const lastMove = session.lastMove;
    const canPlay = !isSpectator;

    for (let y = 0; y < session.height; y++) {
      const row = document.createElement('div');
      row.className = 'hex-row';
      row.style.marginLeft = `${y * 16}px`;

      for (let x = 0; x < session.width; x++) {
        const val = session.grid[y] && session.grid[y][x] ? session.grid[y][x] : '';
        const isLastMove = lastMove && lastMove.x === x && lastMove.y === y;
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
          cell.addEventListener('click', () => submitMove(x, y));
        }

        row.appendChild(cell);
      }
      grid.appendChild(row);
    }

    boardContainer.appendChild(grid);
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
    function openGameSetupModal() {
      if (currentSession && currentSession.gameType) {
        selectedGame = currentSession.gameType;
        if (currentSession.variant) {
          selectedVariant = currentSession.variant;
        }
        applyGameCategoryFilter(getCategoryForGame(selectedGame), false);
        updateModeOptionsForGame(selectedGame);
        updateVariantPills(selectedGame, true);
        updateGridSizePills(selectedGame);
        syncAllResponsiveSelects();
      } else {
        applyGameCategoryFilter(selectedCategory || getCategoryForGame(selectedGame), false);
      }
      modalMenu.classList.remove('hidden');
    }

    // Menu modal
    btnOpenMenu.addEventListener('click', () => openGameSetupModal());
    btnCloseMenu.addEventListener('click', () => modalMenu.classList.add('hidden'));
    btnNewGame.addEventListener('click', () => openGameSetupModal());

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

    // Share modal (Game Shortcut & Live Match)
    let shareLinkMode = 'shortcut';
    let shareConfigSource = 'current';
    let cachedShareBaseOrigin = null;

    async function resolveShareBaseOrigin() {
      if (cachedShareBaseOrigin) return cachedShareBaseOrigin;
      let origin = window.location.origin;
      const host = window.location.hostname;
      if (host === 'localhost' || host === '127.0.0.1' || host === '::1' || host === '[::1]') {
        try {
          const infoRes = await fetch('/api/info');
          if (infoRes.ok) {
            const info = await infoRes.json();
            const ips = info.localIps || info.LocalIps;
            if (ips && ips.length > 0) {
              const port = window.location.port ? `:${window.location.port}` : '';
              origin = `${window.location.protocol}//${ips[0]}${port}`;
            }
          }
        } catch (e) { }
      }
      cachedShareBaseOrigin = origin;
      return origin;
    }

    async function openShareModal(linkMode = 'shortcut', source = 'current') {
      shareLinkMode = linkMode;
      shareConfigSource = source;
      if (modalShare) modalShare.classList.remove('hidden');
      if (copyFeedback) copyFeedback.classList.add('hidden');
      if (tabShareShortcut) tabShareShortcut.classList.toggle('active', shareLinkMode === 'shortcut');
      if (tabShareSession) tabShareSession.classList.toggle('active', shareLinkMode === 'session');

      const baseOrigin = await resolveShareBaseOrigin();
      let url = baseOrigin;

      if (shareLinkMode === 'shortcut') {
        const useMenu = shareConfigSource === 'menu' || !currentSession;
        const cfg = useMenu
          ? {
              game: selectedGame,
              mode: selectedMode,
              variant: selectedVariant,
              diff: selectedDiff,
              width: selectedGridWidth,
              height: selectedGridHeight,
              timer: selectedTimerSeconds
            }
          : {
              game: currentSession.gameType,
              mode: currentSession.mode,
              variant: currentSession.variant || 'standard',
              diff: selectedDiff || clientDefaultDiff,
              width: currentSession.width,
              height: currentSession.height,
              timer: currentSession.timerSeconds || 0
            };

        const gameLabel = getGameMeta(cfg.game).name || cfg.game;
        if (shareModalTitle) shareModalTitle.textContent = 'Share Game Shortcut';
        if (shareModalSubtitle) {
          shareModalSubtitle.textContent = `Scan or copy this link to immediately start a new ${gameLabel} (${cfg.mode}) game:`;
        }
        url = buildDirectGameUrl(baseOrigin, cfg);
      } else {
        if (shareModalTitle) shareModalTitle.textContent = 'Play on Phone';
        if (isSpectator && currentSession) {
          url = `${baseOrigin}/?spectate=${currentSession.id}`;
          if (shareModalSubtitle) shareModalSubtitle.textContent = 'Scan or copy this link to spectate this live match:';
        } else if (currentSession && currentSession.mode === 'room' && currentSession.status === 'WaitingForPlayer') {
          url = `${baseOrigin}/?room=${currentSession.roomCode}`;
          if (shareModalSubtitle) shareModalSubtitle.textContent = 'Scan or copy this link to join this live Wi-Fi match:';
        } else if (currentSession) {
          url = `${baseOrigin}/?spectate=${currentSession.id}`;
          if (shareModalSubtitle) shareModalSubtitle.textContent = 'Scan or copy this link to spectate this live match on another device:';
        } else {
          if (shareModalSubtitle) shareModalSubtitle.textContent = 'Scan this code with any phone connected to the same Wi-Fi network:';
        }
      }

      shareUrlText.textContent = url;
      renderSimpleQR(url, qrContainer);
    }

    if (tabShareShortcut) {
      tabShareShortcut.addEventListener('click', () => {
        openShareModal('shortcut', shareConfigSource);
        playSound('tap');
      });
    }

    if (tabShareSession) {
      tabShareSession.addEventListener('click', () => {
        openShareModal('session', shareConfigSource);
        playSound('tap');
      });
    }

    btnShare.addEventListener('click', () => {
      const preferLive = currentSession && (isSpectator || (currentSession.mode === 'room' && currentSession.status === 'WaitingForPlayer'));
      openShareModal(preferLive ? 'session' : 'shortcut', 'current');
    });

    btnCloseShare.addEventListener('click', () => modalShare.classList.add('hidden'));

    const btnShareShortcut = document.getElementById('btn-share-shortcut');
    if (btnShareShortcut) {
      btnShareShortcut.addEventListener('click', () => {
        modalMenu.classList.add('hidden');
        openShareModal('shortcut', 'menu');
        playSound('tap');
      });
    }

    const btnShareP2 = document.getElementById('btn-share-p2');
    if (btnShareP2) {
      btnShareP2.addEventListener('click', () => {
        modalMenu.classList.add('hidden');
        openShareModal('shortcut', 'menu');
        playSound('tap');
      });
    }

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

    // Settings Fav Game Category pills
    document.querySelectorAll('#group-setting-fav-category .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        applySettingFavCategoryFilter(btn.dataset.category, true);
        playSound('tap');
      });
    });

    // Settings Fav Game pills
    document.querySelectorAll('#group-setting-fav .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-setting-fav .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        updateSettingModeOptionsForGame(btn.dataset.fav);
      });
    });

    // Settings Default Play Mode pills
    document.querySelectorAll('#group-setting-mode .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-setting-mode .pill-btn').forEach(b => b.classList.remove('active'));
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

    // Settings Variant, AI Difficulty, Board Size & Turn Timer pills
    ['#group-setting-variant', '#group-setting-diff', '#group-setting-size', '#group-setting-timer'].forEach(sel => {
      const grp = document.querySelector(sel);
      if (!grp) return;
      grp.querySelectorAll('.pill-btn').forEach(btn => {
        btn.addEventListener('click', () => {
          grp.querySelectorAll('.pill-btn').forEach(b => b.classList.remove('active'));
          btn.classList.add('active');
          syncPillGroupSelect(grp);
          playSound('tap');
        });
      });
    });

    // Keep responsive <select> dropdowns in sync with pill clicks
    document.addEventListener('click', (e) => {
      const btn = e.target.closest('.pill-group .pill-btn');
      if (!btn) return;
      const groupEl = btn.closest('.pill-group');
      if (groupEl) syncPillGroupSelect(groupEl);
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
      btnPredictP1.addEventListener('click', () => sendPrediction(0));
    }
    if (btnPredictP2) {
      btnPredictP2.addEventListener('click', () => sendPrediction(1));
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
        syncPillGroupSelect(document.getElementById('group-turn-timer'));
        playSound('tap');
      });
    });

    btnChangeGame.addEventListener('click', () => {
      gameOverModal.classList.add('hidden');
      openGameSetupModal();
    });

    // Category selections in menu
    document.querySelectorAll('#group-game-category .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        applyGameCategoryFilter(btn.dataset.category, true);
        playSound('tap');
      });
    });

    // Pill selections in menu
    document.querySelectorAll('#group-game .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-game .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        selectedGame = btn.dataset.game;
        updateModeOptionsForGame(selectedGame);
        updateVariantPills(selectedGame);
        updateGridSizePills(selectedGame);
        syncPillGroupSelect(document.getElementById('group-game'));
        playSound('tap');
      });
    });

    document.querySelectorAll('#group-mode .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('#group-mode .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        selectedMode = btn.dataset.mode;
        if (selectedMode === 'daily' && selectedGame !== 'sudoku') {
          selectedGame = 'sudoku';
          document.querySelectorAll('#group-game .pill-btn').forEach(b => {
            b.classList.toggle('active', b.dataset.game === 'sudoku');
          });
          updateGridSizePills('sudoku');
        }
        secAiDiff.classList.toggle('hidden', selectedMode !== 'vs-ai' && selectedMode !== 'ai-vs-ai');
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
      const p2Name = selectedMode === 'daily'
        ? 'Daily Challenge'
        : (selectedMode === 'solo'
          ? 'Solo'
          : (selectedMode === 'ai-vs-ai' ? 'AI Bot 2' : (inputP2.value.trim() || 'Player 2')));
      modalMenu.classList.add('hidden');
      createSession(
        selectedGame,
        selectedMode,
        selectedDiff,
        inputP1.value.trim() || 'Player 1',
        p2Name,
        isPublic,
        selectedGridWidth,
        selectedGridHeight,
        selectedTimerSeconds,
        selectedVariant
      );
    });

    // Quick Rules button
    document.getElementById('btn-quick-rules').addEventListener('click', () => {
      alert(getGameMeta(currentSession?.gameType).rules);
    });

    // Keyboard input 1-9 for Sudoku & A-Z/Backspace/Enter for Wordle
    document.addEventListener('keydown', (e) => {
      if (document.activeElement && (document.activeElement.tagName === 'INPUT' || document.activeElement.tagName === 'TEXTAREA')) {
        return;
      }
      if (!currentSession || currentSession.status !== 'InProgress' || currentSession.isAiTurn || isSpectator) {
        return;
      }
      if (currentSession.gameType === 'sudoku' && selectedSudokuCell && e.key >= '1' && e.key <= '9') {
        submitSudokuMove(selectedSudokuCell.x, selectedSudokuCell.y, Number(e.key));
      } else if (currentSession.gameType === 'wordle') {
        if (e.key === 'Backspace') {
          if (wordleCurrentGuess.length > 0) {
            wordleCurrentGuess = wordleCurrentGuess.slice(0, -1);
            renderBoard(currentSession);
          }
        } else if (e.key === 'Enter' && wordleCurrentGuess.length === 5) {
          let activeRow = currentSession.height;
          for (let y = 0; y < currentSession.height; y++) {
            const first = (currentSession.grid[y] && currentSession.grid[y][0]) ? currentSession.grid[y][0].trim() : '';
            if (!first) { activeRow = y; break; }
          }
          if (activeRow < currentSession.height) {
            const placements = Array.from(wordleCurrentGuess).map((ch, x) => ({ x, y: activeRow, letter: ch }));
            submitWordMove(placements, false);
          }
        } else if (/^[a-zA-Z]$/.test(e.key) && wordleCurrentGuess.length < 5) {
          wordleCurrentGuess += e.key.toUpperCase();
          renderBoard(currentSession);
        }
      }
    });

    // Leaderboard modal events
    if (btnLeaderboard) {
      btnLeaderboard.addEventListener('click', () => {
        const g = currentSession?.gameType || 'all';
        const m = (currentSession?.gameType === 'sudoku' && currentSession?.mode === 'daily') ? 'daily' : 'all';
        openLeaderboard(g, m);
      });
    }
    if (btnCloseLeaderboard && modalLeaderboard) {
      btnCloseLeaderboard.addEventListener('click', () => modalLeaderboard.classList.add('hidden'));
    }
    if (btnRefreshLeaderboard) {
      btnRefreshLeaderboard.addEventListener('click', () => fetchLeaderboard());
    }
    if (btnPlayDailySudoku) {
      btnPlayDailySudoku.addEventListener('click', () => {
        if (modalLeaderboard) modalLeaderboard.classList.add('hidden');
        createSession('sudoku', 'daily', 'smart', clientPlayerName || 'Player 1', 'Daily Challenge', true, null, null, 0);
      });
    }
    document.querySelectorAll('#group-lb-filter .pill-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        lbFilterGame = btn.dataset.lbGame || 'all';
        lbFilterMode = btn.dataset.lbMode || 'all';
        document.querySelectorAll('#group-lb-filter .pill-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        playSound('tap');
        fetchLeaderboard();
      });
    });
    if (tabLbScores && tabLbPlayers) {
      tabLbScores.addEventListener('click', () => {
        lbActiveTab = 'scores';
        tabLbScores.classList.add('active');
        tabLbPlayers.classList.remove('active');
        fetchLeaderboard();
      });
      tabLbPlayers.addEventListener('click', () => {
        lbActiveTab = 'players';
        tabLbPlayers.classList.add('active');
        tabLbScores.classList.remove('active');
        fetchLeaderboard();
      });
    }
  }

  function openLeaderboard(gameFilter = 'all', modeFilter = 'all') {
    if (!modalLeaderboard) return;
    lbFilterGame = gameFilter || 'all';
    lbFilterMode = modeFilter || 'all';
    document.querySelectorAll('#group-lb-filter .pill-btn').forEach(b => {
      const match = (b.dataset.lbGame || 'all') === lbFilterGame && (b.dataset.lbMode || 'all') === lbFilterMode;
      b.classList.toggle('active', match);
    });
    syncPillGroupSelect(document.getElementById('group-lb-filter'));
    syncAllResponsiveSelects();
    modalLeaderboard.classList.remove('hidden');
    fetchLeaderboard();
  }

  function formatDurationShort(sec) {
    const s = Math.max(0, Number(sec || 0));
    const mins = Math.floor(s / 60);
    const rem = s % 60;
    return mins > 0 ? `${mins}m ${String(rem).padStart(2, '0')}s` : `${rem}s`;
  }

  async function fetchLeaderboard() {
    if (!leaderboardList) return;
    leaderboardList.innerHTML = '<div class="lobby-empty">Loading leaderboard...</div>';
    try {
      const params = new URLSearchParams({ gameType: lbFilterGame, mode: lbFilterMode });
      const res = await fetch(`/api/leaderboard?${params.toString()}`);
      if (!res.ok) throw new Error('Failed to load');
      const data = await res.json();
      if (lbDailyDate && data.dailyDate) {
        lbDailyDate.textContent = `Daily Puzzle • ${data.dailyDate}`;
      }
      renderLeaderboardEntries(data.entries || [], data.standings || []);
    } catch (e) {
      leaderboardList.innerHTML = '<div class="lobby-empty">Could not load leaderboard right now.</div>';
    }
  }

  function renderLeaderboardEntries(entries, standings) {
    if (!leaderboardList) return;
    leaderboardList.innerHTML = '';

    const medal = (rank) => rank === 1 ? '🥇' : (rank === 2 ? '🥈' : (rank === 3 ? '🥉' : `#${rank}`));

    if (lbActiveTab === 'players') {
      if (!standings || standings.length === 0) {
        leaderboardList.innerHTML = '<div class="lobby-empty">No completed games recorded yet.<br><small style="color:var(--text-dim);">Finish a match or Daily Sudoku to appear here!</small></div>';
        return;
      }
      standings.forEach(p => {
        const row = document.createElement('div');
        row.className = 'lb-row';
        row.innerHTML = `
          <div class="lb-left">
            <span class="lb-rank">${medal(p.rank)}</span>
            <div>
              <div class="lb-player">${escapeHtml(p.playerName)}</div>
              <div class="lb-meta">${p.wins} win${p.wins === 1 ? '' : 's'} • Best ${p.bestScore} pts • Fastest ${formatDurationShort(p.fastestWinSeconds)}</div>
            </div>
          </div>
          <div class="lb-score">${p.totalScore} <span>total</span></div>
        `;
        leaderboardList.appendChild(row);
      });
      return;
    }

    if (!entries || entries.length === 0) {
      leaderboardList.innerHTML = '<div class="lobby-empty">No scores recorded for this filter yet.<br><small style="color:var(--text-dim);">Complete a game to claim the #1 spot!</small></div>';
      return;
    }

    entries.forEach(e => {
      const row = document.createElement('div');
      row.className = 'lb-row';
      const gameLabel = getGameMeta(e.gameType).shortName || e.gameType;
      const modeTag = e.mode === 'daily' ? ' • Daily' : (e.mode === 'solo' ? ' • Solo' : '');
      row.innerHTML = `
        <div class="lb-left">
          <span class="lb-rank">${medal(e.rank)}</span>
          <div>
            <div class="lb-player">${escapeHtml(e.playerName)}</div>
            <div class="lb-meta">${escapeHtml(gameLabel + modeTag)} • ${formatDurationShort(e.durationSeconds)} • ${e.moves} moves • ${escapeHtml(e.timeAgo)}</div>
          </div>
        </div>
        <div class="lb-score">${e.score} <span>pts</span></div>
      `;
      leaderboardList.appendChild(row);
    });
  }

  // App Initialization
  async function init() {
    applyTheme(clientTheme, clientBoardTheme, clientPieceTheme);
    if (inputP1) inputP1.value = clientPlayerName;
    updateSoundUI();
    setupEvents();
    selectedDiff = clientDefaultDiff;
    selectedTimerSeconds = clientDefaultTimer;
    updateGridSizePills(clientFavGame);
    renderStatsUI();

    // Set initial active game pill to favorite game
    document.querySelectorAll('#group-game .pill-btn').forEach(b => {
      b.classList.toggle('active', b.dataset.game === clientFavGame);
    });

    // Set initial active mode pill to default play mode
    document.querySelectorAll('#group-mode .pill-btn').forEach(b => {
      b.classList.toggle('active', b.dataset.mode === clientDefaultMode);
    });
    if (secAiDiff) secAiDiff.classList.toggle('hidden', clientDefaultMode !== 'vs-ai');
    if (secRoom) secRoom.classList.toggle('hidden', clientDefaultMode !== 'room');

    // Set initial active difficulty, variant & turn timer from preferences
    document.querySelectorAll('#group-diff .pill-btn').forEach(b => {
      b.classList.toggle('active', (b.dataset.diff || b.dataset.difficulty) === clientDefaultDiff);
    });
    document.querySelectorAll('#group-turn-timer .pill-btn').forEach(b => {
      b.classList.toggle('active', Number(b.dataset.timer || 0) === Number(clientDefaultTimer));
    });
    updateModeOptionsForGame(clientFavGame);
    updateSettingModeOptionsForGame(clientFavGame);
    applyGameCategoryFilter(getCategoryForGame(clientFavGame), false);
    applySettingFavCategoryFilter(getCategoryForGame(clientFavGame), false);
    updateVariantPills(clientFavGame);
    syncAllResponsiveSelects();

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
          await createSession(clientFavGame, 'pass-and-play', selectedDiff, clientPlayerName, 'Player 2', true, selectedGridWidth, selectedGridHeight, selectedTimerSeconds, selectedVariant);
          openLobby();
        } else {
          // Start favorite game in default play mode
          await createSession(clientFavGame, selectedMode, selectedDiff, clientPlayerName, 'Player 2', true, selectedGridWidth, selectedGridHeight, selectedTimerSeconds, selectedVariant);
        }
      }
    }
  }

  window.addEventListener('DOMContentLoaded', init);
})();
