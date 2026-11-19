// ============================================================================
// Lifetime Stats & Trophies System
// ============================================================================

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

export const TROPHIES = [
  { id: 'first_win', icon: '🥉', name: 'First Victory', desc: 'Win your first game in any mode' },
  { id: 'streak_3', icon: '🔥', name: 'On Fire', desc: 'Achieve a 3-match win streak' },
  { id: 'streak_5', icon: '⚡', name: 'Unstoppable', desc: 'Achieve a 5-match win streak' },
  { id: 'master_c4', icon: '🔴', name: 'Connect King', desc: 'Win 3 Connect Four games' },
  { id: 'master_ttt', icon: '❌', name: 'Grid Master', desc: 'Win 3 Tic-Tac-Toe games' },
  { id: 'master_chomp', icon: '🍫', name: 'Sweet Tooth', desc: 'Win 3 Chomp games' },
  { id: 'blitz_win', icon: '⏱️', name: 'Blitz Buster', desc: 'Win a game with turn timer enabled' },
  { id: 'seer', icon: '🔮', name: 'Crowd Oracle', desc: 'Cast a prediction vote as a spectator' }
];

export function getStats() {
  try {
    const raw = localStorage.getItem('bg_stats');
    if (raw) return { ...DEFAULT_STATS, ...JSON.parse(raw) };
  } catch (e) { }
  return { ...DEFAULT_STATS };
}

export function saveStats(stats) {
  try {
    localStorage.setItem('bg_stats', JSON.stringify(stats));
  } catch (e) { }
}

export function unlockTrophy(stats, trophyId, onUnlock) {
  if (!stats.unlockedTrophies) stats.unlockedTrophies = [];
  if (!stats.unlockedTrophies.includes(trophyId)) {
    stats.unlockedTrophies.push(trophyId);
    const trophy = TROPHIES.find(t => t.id === trophyId);
    if (trophy && onUnlock) {
      onUnlock(trophy);
    }
  }
}

export function recordGameResult(session, { isSpectator, playerToken, playerName, onUnlock } = {}) {
  if (!session || isSpectator || session.mode === 'ai-vs-ai') return;
  const stats = getStats();
  stats.matches++;

  const isSoloOrDaily = session.mode === 'daily' || session.mode === 'solo';
  const storedName = (playerName || localStorage.getItem('bg_player_name') || '').trim();
  let isP1 = true;
  if (session.mode === 'room') {
    if (playerToken === 'p1') isP1 = true;
    else if (playerToken === 'p2') isP1 = false;
    else if (storedName && session.player2Name && storedName.toLowerCase() === session.player2Name.trim().toLowerCase() &&
             (!session.player1Name || storedName.toLowerCase() !== session.player1Name.trim().toLowerCase())) {
      isP1 = false;
    } else {
      isP1 = true;
    }
  } else {
    isP1 = (session.mode === 'vs-ai' || session.mode === 'pass-and-play' || isSoloOrDaily || !playerToken || playerToken === 'p1');
  }
  const myName = isP1 ? session.player1Name : session.player2Name;
  const isWinner = session.winner && (session.winner === myName || (session.status === 'Finished' && ((isP1 && session.winner === session.player1Name) || (!isP1 && session.winner === session.player2Name))));

  if (session.status === 'Draw' && isSoloOrDaily) {
    stats.losses++;
    stats.streak = 0;
  } else if (session.status === 'Draw') {
    stats.draws++;
    stats.streak = 0;
  } else if (isWinner) {
    stats.wins++;
    stats.streak++;
    if (stats.streak > stats.bestStreak) stats.bestStreak = stats.streak;
    if (!stats.gameWins) stats.gameWins = { connect4: 0, tictactoe: 0, chomp: 0 };
    stats.gameWins[session.gameType] = (stats.gameWins[session.gameType] || 0) + 1;

    unlockTrophy(stats, 'first_win', onUnlock);
    if (stats.streak >= 3) unlockTrophy(stats, 'streak_3', onUnlock);
    if (stats.streak >= 5) unlockTrophy(stats, 'streak_5', onUnlock);
    if ((stats.gameWins.connect4 || 0) >= 3) unlockTrophy(stats, 'master_c4', onUnlock);
    if ((stats.gameWins.tictactoe || 0) >= 3) unlockTrophy(stats, 'master_ttt', onUnlock);
    if ((stats.gameWins.chomp || 0) >= 3) unlockTrophy(stats, 'master_chomp', onUnlock);
    if (session.timerSeconds > 0) unlockTrophy(stats, 'blitz_win', onUnlock);
  } else {
    stats.losses++;
    stats.streak = 0;
  }

  saveStats(stats);
}

export function renderStatsUI({ statMatches, statWinrate, statStreak, trophyGrid }) {
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
