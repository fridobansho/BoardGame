import {
  GAME_REGISTRY,
  VARIANT_OPTIONS,
  GAME_CATEGORY_MAP,
  DAILY_PUZZLE_GAMES,
  SOLO_GAMES,
  AI_VS_AI_GAMES,
  GRID_OPTIONS,
  resolveGameId,
  getGameMeta,
  getCategoryForGame,
  supportsDailyMode,
  supportsSoloMode,
  supportsAiVsAiMode,
  supportsHeadToHeadMode
} from './game-registry.js';
import { initAudio, playSoundEffect, triggerHaptic, startConfetti as runConfetti } from './audio-fx.js';
import { renderSimpleQR } from './qr.js';
import { getStats, saveStats, unlockTrophy, recordGameResult as recordStatsResult, renderStatsUI as renderStatsPanel } from './stats.js';
import { createBoardRenderer } from './renderers.js?v=3';

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
  let playerSseSource = null;
  let soundEnabled = localStorage.getItem('bg_sound') !== 'false';

  // Client Preferences (Persisted in localStorage)
  let clientPlayerName = localStorage.getItem('bg_player_name') || 'Player 1';
  let clientTheme = localStorage.getItem('bg_theme') || 'nebula';
  let clientBoardTheme = localStorage.getItem('bg_board_theme') || 'auto';
  let clientPieceTheme = localStorage.getItem('bg_piece_theme') || 'auto';
  let clientFavGame = resolveGameId(localStorage.getItem('bg_fav_game')) || 'connect4';
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
  let selectedChecker = null;
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
  let moveInFlight = false;
  let lastGameOverKey = null;

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


  function playSound(name) {
    playSoundEffect(name, soundEnabled, clientSoundPack || 'arcade');
  }

  function startConfetti() {
    runConfetti(confettiCanvas);
  }

  function recordGameResult(session) {
    recordStatsResult(session, {
      isSpectator,
      playerToken,
      playerName: clientPlayerName,
      onUnlock: (trophy) => showSpeechBubble(`🏆 Trophy Unlocked: ${trophy.name}!`, 'System')
    });
  }

  function renderStatsUI() {
    renderStatsPanel({ statMatches, statWinrate, statStreak, trophyGrid });
  }

  function resetStats() {
    if (confirm('Reset all lifetime stats and unlocked trophies?')) {
      localStorage.removeItem('bg_stats');
      renderStatsUI();
      playSound('tap');
    }
  }

  const { renderBoard } = createBoardRenderer({
    get boardContainer() { return boardContainer; },
    get isSpectator() { return isSpectator; },
    get replayIndex() { return replayIndex; },
    get aiExhibitionPaused() { return aiExhibitionPaused; },
    set aiExhibitionPaused(v) { aiExhibitionPaused = v; },
    get selectedChecker() { return selectedChecker; },
    set selectedChecker(v) { selectedChecker = v; },
    get wordleCurrentGuess() { return wordleCurrentGuess; },
    set wordleCurrentGuess(v) { wordleCurrentGuess = v; },
    get minesweeperFlagMode() { return minesweeperFlagMode; },
    set minesweeperFlagMode(v) { minesweeperFlagMode = v; },
    get selectedScrabbleTileIndex() { return selectedScrabbleTileIndex; },
    set selectedScrabbleTileIndex(v) { selectedScrabbleTileIndex = v; },
    get pendingScrabblePlacements() { return pendingScrabblePlacements; },
    set pendingScrabblePlacements(v) { pendingScrabblePlacements = v; },
    get selectedSudokuCell() { return selectedSudokuCell; },
    set selectedSudokuCell(v) { selectedSudokuCell = v; },
    get highlightedSudokuDigit() { return highlightedSudokuDigit; },
    set highlightedSudokuDigit(v) { highlightedSudokuDigit = v; },
    get moveInFlight() { return moveInFlight; },
    syncAiExhibitionLoop: (...args) => syncAiExhibitionLoop(...args),
    stepAiExhibition: (...args) => stepAiExhibition(...args),
    submitMove: (...args) => submitMove(...args),
    submitPieceMove: (...args) => submitPieceMove(...args),
    submitWordMove: (...args) => submitWordMove(...args),
    submitSudokuMove: (...args) => submitSudokuMove(...args),
    openLeaderboard: (...args) => openLeaderboard(...args),
    showSpeechBubble: (...args) => showSpeechBubble(...args),
    triggerHaptic: (...args) => triggerHaptic(...args),
    playSound: (name) => playSound(name),
    escapeHtml: (str) => escapeHtml(str)
  });

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



  function resolvePreferredVariant(opts) {
    if (!opts || opts.length === 0) return 'standard';
    if (clientDefaultVariant === 'misere') {
      const m = opts.find(o => o.id === 'misere' || o.id === 'reversed' || o.id === 'giveaway' || o.id === 'nim_misere' || o.id === 'breakthrough_misere' || o.id === 'normal');
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
        if (game === 'tictactoe' && (opt.id === 'gomoku' || opt.id === 'exact5') && (selectedGridWidth || 3) < 9) {
          selectedGridWidth = 15;
          selectedGridHeight = 15;
          updateGridSizePills(game);
        } else if (game === 'chomp') {
          updateGridSizePills(game);
        }
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


  function updatePlayer2InputVisibility(mode = selectedMode) {
    if (!inputP2) return;
    const isPassAndPlay = mode === 'pass-and-play';
    inputP2.classList.toggle('hidden', !isPassAndPlay);
    inputP2.disabled = !isPassAndPlay;
  }

  function updateModeSectionsVisibility(mode = selectedMode) {
    if (secAiDiff) secAiDiff.classList.toggle('hidden', mode !== 'vs-ai' && mode !== 'ai-vs-ai');
    if (secRoom) secRoom.classList.toggle('hidden', mode !== 'room');
    const secTurnTimer = document.getElementById('sec-turn-timer');
    if (secTurnTimer) secTurnTimer.classList.toggle('hidden', mode === 'solo' || mode === 'daily');
    updatePlayer2InputVisibility(mode);
  }

  function updateModeOptionsForGame(game) {
    const modeGroup = document.getElementById('group-mode');
    if (!modeGroup) return;
    const dailyAllowed = supportsDailyMode(game);
    const soloAllowed = supportsSoloMode(game);
    const aiVsAiAllowed = supportsAiVsAiMode(game);
    const headToHeadAllowed = supportsHeadToHeadMode(game);
    const passBtn = modeGroup.querySelector('.pill-btn[data-mode="pass-and-play"]');
    const roomBtn = modeGroup.querySelector('.pill-btn[data-mode="room"]');
    const dailyBtn = modeGroup.querySelector('.pill-btn[data-mode="daily"]');
    const soloBtn = modeGroup.querySelector('.pill-btn[data-mode="solo"]');
    const aiBtn = modeGroup.querySelector('.pill-btn[data-mode="vs-ai"]');
    const aiVsAiBtn = modeGroup.querySelector('.pill-btn[data-mode="ai-vs-ai"]');
    if (passBtn) passBtn.classList.toggle('hidden', !headToHeadAllowed);
    if (roomBtn) roomBtn.classList.toggle('hidden', !headToHeadAllowed);
    if (dailyBtn) dailyBtn.classList.toggle('hidden', !dailyAllowed);
    if (soloBtn) soloBtn.classList.toggle('hidden', !soloAllowed);
    if (aiBtn) aiBtn.classList.toggle('hidden', !aiVsAiAllowed);
    if (aiVsAiBtn) aiVsAiBtn.classList.toggle('hidden', !aiVsAiAllowed);

    const modeAllowed = {
      'pass-and-play': headToHeadAllowed,
      'room': headToHeadAllowed,
      'vs-ai': aiVsAiAllowed,
      'ai-vs-ai': aiVsAiAllowed,
      'solo': soloAllowed,
      'daily': dailyAllowed
    };
    if (!modeAllowed[selectedMode]) {
      let fallback = headToHeadAllowed ? 'pass-and-play' : (soloAllowed ? 'solo' : 'daily');
      if (modeAllowed[clientDefaultMode]) {
        fallback = clientDefaultMode;
      } else if (!headToHeadAllowed && clientDefaultMode === 'daily' && dailyAllowed) {
        fallback = 'daily';
      }
      selectedMode = fallback;
      modeGroup.querySelectorAll('.pill-btn').forEach(b => {
        b.classList.toggle('active', b.dataset.mode === selectedMode);
      });
    }
    updateModeSectionsVisibility(selectedMode);
    syncPillGroupSelect(modeGroup);
  }

  function updateSettingModeOptionsForGame(favGame) {
    const settingModeGroup = document.getElementById('group-setting-mode');
    if (!settingModeGroup) return;
    const dailyAllowed = supportsDailyMode(favGame);
    const soloAllowed = supportsSoloMode(favGame);
    const aiVsAiAllowed = supportsAiVsAiMode(favGame);
    const headToHeadAllowed = supportsHeadToHeadMode(favGame);
    const passBtn = settingModeGroup.querySelector('.pill-btn[data-mode="pass-and-play"]');
    const roomBtn = settingModeGroup.querySelector('.pill-btn[data-mode="room"]');
    const dailyBtn = settingModeGroup.querySelector('.pill-btn[data-mode="daily"]');
    const soloBtn = settingModeGroup.querySelector('.pill-btn[data-mode="solo"]');
    const aiBtn = settingModeGroup.querySelector('.pill-btn[data-mode="vs-ai"]');
    const aiVsAiBtn = settingModeGroup.querySelector('.pill-btn[data-mode="ai-vs-ai"]');
    if (passBtn) passBtn.classList.toggle('hidden', !headToHeadAllowed);
    if (roomBtn) roomBtn.classList.toggle('hidden', !headToHeadAllowed);
    if (dailyBtn) dailyBtn.classList.toggle('hidden', !dailyAllowed);
    if (soloBtn) soloBtn.classList.toggle('hidden', !soloAllowed);
    if (aiBtn) aiBtn.classList.toggle('hidden', !aiVsAiAllowed);
    if (aiVsAiBtn) aiVsAiBtn.classList.toggle('hidden', !aiVsAiAllowed);

    const activeHidden = (passBtn && !headToHeadAllowed && passBtn.classList.contains('active')) ||
                         (roomBtn && !headToHeadAllowed && roomBtn.classList.contains('active')) ||
                         (dailyBtn && !dailyAllowed && dailyBtn.classList.contains('active')) ||
                         (soloBtn && !soloAllowed && soloBtn.classList.contains('active')) ||
                         (aiBtn && !aiVsAiAllowed && aiBtn.classList.contains('active')) ||
                         (aiVsAiBtn && !aiVsAiAllowed && aiVsAiBtn.classList.contains('active'));
    if (activeHidden) {
      const fallbackMode = headToHeadAllowed ? 'pass-and-play' : (soloAllowed ? 'solo' : 'daily');
      settingModeGroup.querySelectorAll('.pill-btn').forEach(b => {
        b.classList.toggle('active', b.dataset.mode === fallbackMode);
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
    if (!opts || (game === 'chomp' && (selectedVariant === 'nim' || selectedVariant === 'nim_misere'))) {
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
        unlockTrophy(stats, 'seer', (trophy) => showSpeechBubble(`🏆 Trophy Unlocked: ${trophy.name}!`, 'System'));
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
        fetch(`/api/session/${session.id}`)
          .then(r => r.ok ? r.json() : null)
          .then(s => { if (s) onSessionUpdated(s); })
          .catch(() => {});
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
    if (gameType === 'chomp' && session.variant !== 'nim' && session.variant !== 'nim_misere') {
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
      const prevName = clientPlayerName;
      const name = settingPlayerName.value.trim() || 'Player 1';
      clientPlayerName = name;
      localStorage.setItem('bg_player_name', name);
      if (inputP1) inputP1.value = name;
      if (name.toLowerCase() !== prevName.trim().toLowerCase()) {
        connectPlayerStream(name);
        syncSessionByPlayerName(name);
      }
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
      updateModeSectionsVisibility(mode);
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
    const headToHeadAllowed = supportsHeadToHeadMode(game);
    const defaultFallback = headToHeadAllowed ? 'pass-and-play' : 'solo';
    let mode = config.mode || selectedMode || defaultFallback;
    if (!headToHeadAllowed && (mode === 'pass-and-play' || mode === 'room')) {
      mode = defaultFallback;
    }
    if (mode === 'daily' && !supportsDailyMode(game)) {
      mode = defaultFallback;
    }
    if (mode === 'solo' && !supportsSoloMode(game)) {
      mode = defaultFallback;
    }
    if ((mode === 'vs-ai' || mode === 'ai-vs-ai') && !supportsAiVsAiMode(game)) {
      mode = defaultFallback;
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
    const headToHeadAllowed = supportsHeadToHeadMode(g);
    const defaultFallback = headToHeadAllowed ? 'pass-and-play' : 'solo';
    let m = (rawMode || defaultFallback).trim().toLowerCase();
    m = modeAliases[m] || m;
    const validModes = new Set(['pass-and-play', 'vs-ai', 'room', 'ai-vs-ai', 'solo', 'daily']);
    if (!validModes.has(m)) {
      m = defaultFallback;
    }
    if (!headToHeadAllowed && (m === 'pass-and-play' || m === 'room')) {
      m = defaultFallback;
    }
    if (m === 'daily' && !supportsDailyMode(g)) {
      m = defaultFallback;
    }
    if (m === 'solo' && !supportsSoloMode(g)) {
      m = defaultFallback;
    }
    if ((m === 'vs-ai' || m === 'ai-vs-ai') && !supportsAiVsAiMode(g)) {
      m = defaultFallback;
    }

    const varOpts = VARIANT_OPTIONS[g] || [];
    const rawGameKey = (rawGame || '').trim().toLowerCase();
    let rawVariant = (params.get('variant') || '').trim().toLowerCase();
    if (!rawVariant) {
      if (rawGameKey === 'gomoku' || rawGameKey === 'five-in-a-row') rawVariant = 'gomoku';
      else if (rawGameKey === 'nim') rawVariant = 'nim';
      else if (rawGameKey === 'breakthrough' || rawGameKey === 'bt') rawVariant = 'breakthrough';
    } else if (rawGameKey === 'nim' && rawVariant === 'misere') {
      rawVariant = 'nim_misere';
    } else if ((rawGameKey === 'breakthrough' || rawGameKey === 'bt') && rawVariant === 'misere') {
      rawVariant = 'breakthrough_misere';
    }
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
      } else if (selectedGame === 'tictactoe' && (selectedVariant === 'gomoku' || selectedVariant === 'exact5')) {
        selectedGridWidth = 15;
        selectedGridHeight = 15;
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
    updateModeSectionsVisibility(selectedMode);
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
      : (selectedMode === 'solo'
        ? 'Solo'
        : (selectedMode === 'ai-vs-ai'
          ? 'AI Bot 2'
          : (selectedMode === 'vs-ai' ? null : 'Player 2')));
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
        lastGameOverKey = null;
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
        lastGameOverKey = null;
        playSound('tap');
        const data = await res.json();
        const sessionObj = data.session || data.Session;
        if (sessionObj) onSessionUpdated(sessionObj);
        modalMenu.classList.add('hidden');
      }
    } catch (e) { }
  }

  // Sync active session by player name (shared state across devices with same name)
  async function syncSessionByPlayerName(playerName) {
    if (!playerName || !playerName.trim()) return false;
    try {
      const res = await fetch(`/api/player/session?name=${encodeURIComponent(playerName.trim())}`);
      if (!res.ok) return false;
      const data = await res.json();
      if (data && data.found && data.session) {
        const sessionObj = data.session;
        const newId = sessionObj.id || sessionObj.Id;
        const newToken = data.playerToken || data.PlayerToken;
        isSpectator = false;
        hasVotedPrediction = false;
        returnToLive();
        if (newToken) {
          playerToken = newToken;
          localStorage.setItem('bg_player_token', playerToken);
        }
        onSessionUpdated(sessionObj);
        connectStream(newId);
        return true;
      }
    } catch (e) { }
    return false;
  }

  // Subscribe to session changes for this player name across devices
  function connectPlayerStream(playerName) {
    if (playerSseSource) {
      playerSseSource.close();
      playerSseSource = null;
    }
    if (!playerName || !playerName.trim()) return;
    try {
      const url = `/api/player/stream?name=${encodeURIComponent(playerName.trim())}`;
      playerSseSource = new EventSource(url);
      playerSseSource.addEventListener('session', function (event) {
        try {
          const data = JSON.parse(event.data);
          if (!data || !data.session) return;
          const sessionObj = data.session;
          const newId = sessionObj.id || sessionObj.Id;
          const newToken = data.playerToken || data.PlayerToken;
          if (!currentSession || currentSession.id !== newId || (newToken && playerToken !== newToken)) {
            isSpectator = false;
            hasVotedPrediction = false;
            returnToLive();
            if (newToken) {
              playerToken = newToken;
              localStorage.setItem('bg_player_token', playerToken);
            }
            onSessionUpdated(sessionObj);
            connectStream(newId);
          }
        } catch (e) { }
      });
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
    if (!currentSession || currentSession.status !== 'InProgress' || moveInFlight) return;
    moveInFlight = true;
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
        if (currentSession) {
          renderBoard(currentSession);
        }
        return;
      }
      const sessionObj = data.session || data.Session;
      if (sessionObj) onSessionUpdated(sessionObj);
    } catch (e) {
      if (currentSession) {
        renderBoard(currentSession);
      }
    } finally {
      moveInFlight = false;
    }
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
        lastGameOverKey = null;
        wordleCurrentGuess = '';
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

    if (currentSession && currentSession.id === session.id && currentSession.gameNumber === session.gameNumber) {
      if ((currentSession.status === 'Finished' || currentSession.status === 'Draw') &&
          session.status === 'InProgress' &&
          session.moveCount >= currentSession.moveCount) {
        return;
      }
      if (currentSession.status === 'InProgress' &&
          session.status === 'InProgress' &&
          session.moveCount < currentSession.moveCount &&
          lastGameOverKey === `${session.id}:${session.gameNumber}`) {
        lastGameOverKey = null;
      }
    }

    const prevStatus = currentSession ? currentSession.status : null;
    const prevMoveCount = currentSession ? currentSession.moveCount : 0;
    const prevId = currentSession ? currentSession.id : null;
    if (prevId !== session.id) {
      aiExhibitionPaused = false;
      lastGameOverKey = null;
    } else if (session.status === 'InProgress' && session.moveCount === 0) {
      lastGameOverKey = null;
    }
    if (prevId !== session.id || prevMoveCount !== session.moveCount) {
      selectedChecker = null;
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
      if (session.gameType === 'chomp' && session.variant !== 'nim' && session.variant !== 'nim_misere') playSound('chomp');
      else if (session.gameType === 'connect4') playSound('drop');
      else playSound('tap');
    }

    // Check game over transition
    if (prevStatus === 'InProgress' && (session.status === 'Finished' || session.status === 'Draw')) {
      const gameOverKey = `${session.id}:${session.gameNumber}`;
      if (lastGameOverKey !== gameOverKey) {
        lastGameOverKey = gameOverKey;
        handleGameOver(session);
      }
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
        if (session.gameType === 'wordle' || session.gameType === 'minesweeper') {
          statusText.textContent = `👀 Spectating: ${session.lastMoveText || 'Game Over'}`;
        } else {
          statusText.textContent = `👀 Spectating: Game ended in a Draw!`;
        }
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
      if (session.gameType === 'wordle' || session.gameType === 'minesweeper') {
        statusText.textContent = `${session.gameType === 'wordle' ? '📖' : '💥'} ${session.lastMoveText || 'Game Over'}`;
      } else {
        statusText.textContent = `🤝 Game Draw!`;
      }
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
      gameOverMsg.textContent = (session.gameType === 'chomp' && session.variant === 'standard')
        ? 'Ate the poison block! Better luck next time.'
        : 'Congratulations on an amazing match!';
    } else if (session.gameType === 'minesweeper' || session.gameType === 'wordle') {
      playSound('draw');
      gameOverIcon.textContent = session.gameType === 'minesweeper' ? '💥' : '📖';
      gameOverTitle.textContent = 'Game Over';
      gameOverMsg.textContent = session.lastMoveText || 'Better luck next time!';
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
    } else if (session.gameType === 'wordle' && session.mode === 'daily') {
      btnPlayAgain.textContent = 'Play Solo Wordle';
      btnPlayAgain.onclick = () => {
        gameOverModal.classList.add('hidden');
        createSession(
          'wordle',
          'solo',
          'smart',
          session.player1Name || clientPlayerName || 'Player 1',
          'Solo',
          true,
          null,
          null,
          session.timerSeconds || 0,
          session.variant || 'standard'
        );
      };
    } else {
      btnPlayAgain.textContent = `Next Round (Game ${session.gameNumber + 1})`;
      btnPlayAgain.onclick = () => {
        resetSession(true);
      };
    }

    gameOverModal.classList.remove('hidden');
  }



  // Setup Event Listeners
  function setupEvents() {
    function openGameSetupModal() {
      if (currentSession && currentSession.gameType) {
        selectedGame = currentSession.gameType;
        if (currentSession.variant) {
          selectedVariant = currentSession.variant;
        }
        selectedGridWidth = currentSession.width || null;
        selectedGridHeight = currentSession.height || null;
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

    // Close any modal when clicking outside its content (backdrop)
    document.querySelectorAll('.modal').forEach(modal => {
      modal.addEventListener('click', (e) => {
        if (e.target === modal || e.target.classList.contains('modal-backdrop')) {
          modal.classList.add('hidden');
        }
      });
    });

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
      if (!supportsHeadToHeadMode(selectedGame)) {
        selectedGame = 'connect4';
        applyGameCategoryFilter(getCategoryForGame(selectedGame), false);
        updateModeOptionsForGame(selectedGame);
        updateVariantPills(selectedGame);
        updateGridSizePills(selectedGame);
      }
      document.querySelectorAll('#group-mode .pill-btn').forEach(b => b.classList.remove('active'));
      const roomPill = document.querySelector('#group-mode .pill-btn[data-mode="room"]');
      if (roomPill) roomPill.classList.add('active');
      selectedMode = 'room';
      updateModeSectionsVisibility('room');
      syncPillGroupSelect(document.getElementById('group-mode'));

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
        if (selectedMode === 'daily' && !supportsDailyMode(selectedGame)) {
          selectedGame = 'sudoku';
          document.querySelectorAll('#group-game .pill-btn').forEach(b => {
            b.classList.toggle('active', b.dataset.game === 'sudoku');
          });
          applyGameCategoryFilter(getCategoryForGame('sudoku'), false);
          updateModeOptionsForGame('sudoku');
          updateVariantPills('sudoku');
          updateGridSizePills('sudoku');
          syncAllResponsiveSelects();
        }
        updateModeSectionsVisibility(selectedMode);
        syncPillGroupSelect(document.getElementById('group-mode'));
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
      const p2Name = selectedMode === 'pass-and-play'
        ? (inputP2.value.trim() || 'Player 2')
        : (selectedMode === 'daily'
          ? 'Daily Challenge'
          : (selectedMode === 'solo'
            ? 'Solo'
            : (selectedMode === 'ai-vs-ai' ? 'AI Bot 2' : (selectedMode === 'vs-ai' ? null : 'Player 2'))));
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
      if (e.ctrlKey || e.metaKey || e.altKey) {
        return;
      }
      if (document.activeElement && (document.activeElement.tagName === 'INPUT' || document.activeElement.tagName === 'TEXTAREA')) {
        return;
      }
      if (document.querySelector('.modal:not(.hidden)')) {
        return;
      }
      if (!currentSession || currentSession.status !== 'InProgress' || currentSession.isAiTurn || isSpectator || replayIndex !== null || moveInFlight) {
        return;
      }
      if (currentSession.gameType === 'sudoku' && selectedSudokuCell && e.key >= '1' && e.key <= '9') {
        submitSudokuMove(selectedSudokuCell.x, selectedSudokuCell.y, Number(e.key));
      } else if (currentSession.gameType === 'wordle') {
        if (e.key === 'Backspace') {
          e.preventDefault();
          if (wordleCurrentGuess.length > 0) {
            wordleCurrentGuess = wordleCurrentGuess.slice(0, -1);
            renderBoard(currentSession);
          }
        } else if (e.key === 'Enter') {
          e.preventDefault();
          if (wordleCurrentGuess.length < 5) {
            showSpeechBubble('Not enough letters (need 5).', 'Rules');
            triggerHaptic([20, 20]);
            return;
          }
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
          e.preventDefault();
          wordleCurrentGuess += e.key.toUpperCase();
          renderBoard(currentSession);
        }
      }
    });

    // Leaderboard modal events
    if (btnLeaderboard) {
      btnLeaderboard.addEventListener('click', () => {
        const g = currentSession?.gameType || 'all';
        const m = (currentSession?.mode === 'daily') ? 'daily' : 'all';
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
    updateModeSectionsVisibility(clientDefaultMode);

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
    updateGridSizePills(clientFavGame);
    syncAllResponsiveSelects();

    connectPlayerStream(clientPlayerName);

    const joinedFromUrl = parseUrlParams();
    if (!joinedFromUrl) {
      let resumed = await syncSessionByPlayerName(clientPlayerName);
      if (!resumed) {
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
      }

      if (!resumed) {
        if (clientDefaultRole === 'spectate') {
          // Open Wi-Fi lobby directly when client prefers spectating by default
          await createSession(clientFavGame, 'pass-and-play', selectedDiff, clientPlayerName, 'Player 2', true, selectedGridWidth, selectedGridHeight, selectedTimerSeconds, selectedVariant);
          openLobby();
        } else {
          // Start favorite game in default play mode
          const defaultP2 = selectedMode === 'vs-ai' ? null : (selectedMode === 'daily' ? 'Daily Challenge' : (selectedMode === 'solo' ? 'Solo' : (selectedMode === 'ai-vs-ai' ? 'AI Bot 2' : 'Player 2')));
          await createSession(clientFavGame, selectedMode, selectedDiff, clientPlayerName, defaultP2, true, selectedGridWidth, selectedGridHeight, selectedTimerSeconds, selectedVariant);
        }
      }
    }
  }

  window.addEventListener('DOMContentLoaded', init);
})();
