// ============================================================================
// Audio & Visual Effects (Web Audio Synth + Confetti Canvas)
// ============================================================================

let audioCtx = null;
let confettiAnimId = null;

export function initAudio() {
  if (!audioCtx && (window.AudioContext || window.webkitAudioContext)) {
    audioCtx = new (window.AudioContext || window.webkitAudioContext)();
  }
  if (audioCtx && audioCtx.state === 'suspended') {
    audioCtx.resume();
  }
}

export function playTone(freq, duration, type = 'sine', gainVal = 0.15, soundEnabled = true) {
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

export function playSoundEffect(name, soundEnabled, pack = 'arcade') {
  if (!soundEnabled) return;
  const tone = (f, d, t = 'sine', g = 0.15) => playTone(f, d, t, g, soundEnabled);

  if (pack === 'arcade') {
    switch (name) {
      case 'tap':
        tone(587.33, 0.05, 'square', 0.16);
        break;
      case 'drop':
        tone(440, 0.06, 'square', 0.2);
        setTimeout(() => tone(220, 0.09, 'square', 0.18), 45);
        break;
      case 'chomp':
        tone(220, 0.05, 'square', 0.22);
        setTimeout(() => tone(146.83, 0.07, 'sawtooth', 0.22), 35);
        break;
      case 'win':
        [440, 554.37, 659.25, 880, 1108.73].forEach((f, i) => {
          setTimeout(() => tone(f, 0.16, 'square', 0.22), i * 85);
        });
        break;
      case 'draw':
        tone(293.66, 0.14, 'sawtooth', 0.2);
        setTimeout(() => tone(207.65, 0.24, 'sawtooth', 0.2), 120);
        break;
      case 'tick':
        tone(784, 0.03, 'square', 0.1);
        break;
      case 'warning':
        tone(880, 0.09, 'sawtooth', 0.25);
        break;
      case 'chat':
        tone(523.25, 0.05, 'square', 0.16);
        setTimeout(() => tone(783.99, 0.08, 'square', 0.18), 50);
        break;
    }
  } else if (pack === 'marimba') {
    switch (name) {
      case 'tap':
        tone(523.25, 0.12, 'sine', 0.25);
        break;
      case 'drop':
        tone(392, 0.16, 'sine', 0.28);
        setTimeout(() => tone(261.63, 0.2, 'sine', 0.22), 55);
        break;
      case 'chomp':
        tone(246.94, 0.1, 'sine', 0.26);
        setTimeout(() => tone(174.61, 0.16, 'triangle', 0.22), 40);
        break;
      case 'win':
        [523.25, 659.25, 783.99, 1046.5].forEach((f, i) => {
          setTimeout(() => tone(f, 0.32, 'sine', 0.26), i * 110);
        });
        break;
      case 'draw':
        tone(349.23, 0.2, 'triangle', 0.22);
        setTimeout(() => tone(261.63, 0.35, 'sine', 0.2), 140);
        break;
      case 'tick':
        tone(880, 0.04, 'sine', 0.12);
        break;
      case 'warning':
        tone(698.46, 0.12, 'sine', 0.28);
        break;
      case 'chat':
        tone(659.25, 0.1, 'sine', 0.2);
        setTimeout(() => tone(880, 0.18, 'sine', 0.22), 60);
        break;
    }
  } else {
    switch (name) {
      case 'tap':
        tone(950, 0.025, 'triangle', 0.28);
        break;
      case 'drop':
        tone(220, 0.04, 'triangle', 0.3);
        setTimeout(() => tone(110, 0.06, 'sine', 0.32), 30);
        break;
      case 'chomp':
        tone(130, 0.04, 'square', 0.28);
        setTimeout(() => tone(85, 0.05, 'triangle', 0.28), 25);
        break;
      case 'win':
        [440, 660, 880, 1320].forEach((f, i) => {
          setTimeout(() => tone(f, 0.1, 'triangle', 0.26), i * 75);
        });
        break;
      case 'draw':
        tone(165, 0.09, 'sawtooth', 0.26);
        setTimeout(() => tone(110, 0.14, 'square', 0.22), 90);
        break;
      case 'tick':
        tone(1200, 0.018, 'triangle', 0.15);
        break;
      case 'warning':
        tone(493.88, 0.06, 'sawtooth', 0.32);
        break;
      case 'chat':
        tone(1100, 0.025, 'triangle', 0.22);
        setTimeout(() => tone(1650, 0.035, 'triangle', 0.24), 40);
        break;
    }
  }
}

export function triggerHaptic(duration = 20) {
  if (navigator.vibrate) {
    try { navigator.vibrate(duration); } catch (e) { }
  }
}

export function startConfetti(confettiCanvas) {
  if (!confettiCanvas) return;
  const ctx = confettiCanvas.getContext('2d');
  if (!ctx) return;
  confettiCanvas.width = confettiCanvas.clientWidth || window.innerWidth;
  confettiCanvas.height = confettiCanvas.clientHeight || window.innerHeight;

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
