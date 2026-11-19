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
