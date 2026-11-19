// ============================================================================
// Unified Game Metadata Registry
// ============================================================================

// Unified Game Metadata Registry (single source of truth for categories, modes, variants, grids & rules)
export const GAME_REGISTRY = {
  connect4: {
    id: 'connect4',
    name: 'Connect Four',
    shortName: 'Connect 4',
    icon: '🔴',
    category: 'alignment',
    supportsDaily: false,
    supportsSolo: false,
    supportsHeadToHead: true,
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
    name: 'Tic-Tac-Toe & Gomoku',
    shortName: 'Tic-Tac-Toe',
    icon: '❌',
    category: 'alignment',
    supportsDaily: false,
    supportsSolo: false,
    supportsHeadToHead: true,
    defaultWidth: 3,
    defaultHeight: 3,
    aliases: ['ttt', 'tic-tac-toe', 'tic', 'gomoku', 'five-in-a-row'],
    variants: [
      { id: 'standard', icon: '❌', label: 'Classic', desc: 'Align 3 (on 3×3), 4 (on 4×4/5×5), or 5 (on 9×9+) in a row to win.' },
      { id: 'gomoku', icon: '⚫', label: 'Gomoku (5-in-a-Row)', desc: 'Classic 5-in-a-row stone alignment (best on 9×9, 15×15, or 19×19).' },
      { id: 'exact5', icon: '🎯', label: 'Exact Line Only', desc: 'Exact target length wins; overlines of 6+ do not count.' },
      { id: 'misere', icon: '🔄', label: 'Misère', desc: 'Reverse rules: completing a line loses!' }
    ],
    gridOptions: [
      { label: '3×3 (Classic 3)', width: 3, height: 3 },
      { label: '4×4 (4-in-a-row)', width: 4, height: 4 },
      { label: '5×5 (4-in-a-row)', width: 5, height: 5 },
      { label: '9×9 (Gomoku 5)', width: 9, height: 9 },
      { label: '15×15 (Gomoku 5)', width: 15, height: 15 },
      { label: '19×19 (Pro Gomoku 5)', width: 19, height: 19 }
    ],
    rules: '❌ TIC-TAC-TOE & GOMOKU:\nTake turns placing marks or stones on the grid.\nAlign 3 on 3×3, 4 on 4×4/5×5, or 5 on 9×9+ Gomoku boards to win!'
  },
  hex: {
    id: 'hex',
    name: 'Hex',
    shortName: 'Hex',
    icon: '⬡',
    category: 'alignment',
    supportsDaily: false,
    supportsSolo: false,
    supportsHeadToHead: true,
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
  chomp: {
    id: 'chomp',
    name: 'Nim & Chomp',
    shortName: 'Chomp / Nim',
    icon: '🍫',
    category: 'strategy',
    supportsDaily: false,
    supportsSolo: false,
    supportsHeadToHead: true,
    defaultWidth: 5,
    defaultHeight: 4,
    aliases: ['nim', 'takeaway'],
    variants: [
      { id: 'standard', icon: '☠️', label: 'Chomp (Misère)', desc: '2D chocolate grid: eating the top-left poison block loses.' },
      { id: 'normal', icon: '🍫', label: 'Chomp (Normal)', desc: '2D chocolate grid: eating the last block wins!' },
      { id: 'nim', icon: '🪨', label: 'Nim Heaps (Normal)', desc: '1D stone rows (1-3-5-7): taking the last stone wins!' },
      { id: 'nim_misere', icon: '🔄', label: 'Misère Nim', desc: '1D stone rows (1-3-5-7): player forced to take the last stone loses!' }
    ],
    gridOptions: [
      { label: '5×4 (Standard)', width: 5, height: 4 },
      { label: '4×3 (Bite-sized)', width: 4, height: 3 },
      { label: '6×5 (Large)', width: 6, height: 5 },
      { label: '7×4 (Wide)', width: 7, height: 4 }
    ],
    rules: '🍫 NIM & CHOMP:\nIn Chomp, eating a block removes everything down & right—avoid the top-left poison block ☠️!\nIn Nim Heaps, click any stone to remove it and all stones to its right in that row.'
  },
  othello: {
    id: 'othello',
    name: 'Othello',
    shortName: 'Othello',
    icon: '⚪',
    category: 'strategy',
    supportsDaily: false,
    supportsSolo: false,
    supportsHeadToHead: true,
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
  checkers: {
    id: 'checkers',
    name: 'Checkers',
    shortName: 'Checkers',
    icon: '👑',
    category: 'strategy',
    supportsDaily: false,
    supportsSolo: false,
    supportsHeadToHead: true,
    defaultWidth: 8,
    defaultHeight: 8,
    aliases: ['draughts', 'checker', 'breakthrough', 'bt'],
    variants: [
      { id: 'standard', icon: '👑', label: 'Standard Draughts', desc: 'Mandatory jump captures with multi-jump chains and King promotion.' },
      { id: 'casual', icon: '⚡', label: 'Casual Draughts', desc: 'Optional captures—choose any legal diagonal move.' },
      { id: 'giveaway', icon: '🔄', label: 'Giveaway (Misère)', desc: 'Lose all your pieces or get blocked to win!' },
      { id: 'breakthrough', icon: '🛡️', label: 'Breakthrough', desc: 'Pawn race! Move 1 step forward or diagonally forward; first to the back rank wins.' },
      { id: 'breakthrough_misere', icon: '🔄', label: 'Misère Breakthrough', desc: 'Pawn race reverse rules: avoid reaching the opponent back rank!' }
    ],
    gridOptions: null,
    rules: '👑 CHECKERS & BREAKTHROUGH:\nIn Draughts, move diagonally on dark squares (captures mandatory in Standard) and reach the opposite row to promote to King 👑.\nIn Breakthrough, pawns move 1 square straight or diagonally forward—first to the opposite home row wins!'
  },
  mancala: {
    id: 'mancala',
    name: 'Mancala',
    shortName: 'Mancala',
    icon: '🫘',
    category: 'strategy',
    supportsDaily: false,
    supportsSolo: false,
    supportsHeadToHead: true,
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
  scrabble: {
    id: 'scrabble',
    name: 'Scrabble',
    shortName: 'Scrabble',
    icon: '🔤',
    category: 'puzzles',
    supportsDaily: false,
    supportsSolo: false,
    supportsHeadToHead: true,
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
    supportsHeadToHead: false,
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
  wordle: {
    id: 'wordle',
    name: 'Wordle',
    shortName: 'Wordle',
    icon: '🟩',
    category: 'puzzles',
    supportsDaily: true,
    supportsSolo: true,
    supportsAiVsAi: false,
    supportsHeadToHead: false,
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
    supportsHeadToHead: false,
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

export function resolveGameId(raw) {
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

export function getGameMeta(game) {
  return GAME_REGISTRY[resolveGameId(game) || 'connect4'] || GAME_REGISTRY.connect4;
}

export const VARIANT_OPTIONS = Object.fromEntries(Object.values(GAME_REGISTRY).map(g => [g.id, g.variants]));
export const GAME_CATEGORY_MAP = Object.fromEntries(Object.values(GAME_REGISTRY).map(g => [g.id, g.category]));
export const DAILY_PUZZLE_GAMES = new Set(Object.values(GAME_REGISTRY).filter(g => g.supportsDaily).map(g => g.id));
export const SOLO_GAMES = new Set(Object.values(GAME_REGISTRY).filter(g => g.supportsSolo).map(g => g.id));
export const AI_VS_AI_GAMES = new Set(Object.values(GAME_REGISTRY).filter(g => g.supportsAiVsAi !== false).map(g => g.id));
export const HEAD_TO_HEAD_GAMES = new Set(Object.values(GAME_REGISTRY).filter(g => g.supportsHeadToHead !== false).map(g => g.id));
export const GRID_OPTIONS = Object.fromEntries(Object.values(GAME_REGISTRY).filter(g => g.gridOptions).map(g => [g.id, g.gridOptions]));

export function getCategoryForGame(game) {
  return GAME_CATEGORY_MAP[resolveGameId(game) || 'connect4'] || 'alignment';
}

export function supportsDailyMode(game) {
  return DAILY_PUZZLE_GAMES.has(game);
}

export function supportsSoloMode(game) {
  return SOLO_GAMES.has(game);
}

export function supportsAiVsAiMode(game) {
  return AI_VS_AI_GAMES.has(game);
}

export function supportsHeadToHeadMode(game) {
  return HEAD_TO_HEAD_GAMES.has(game);
}
