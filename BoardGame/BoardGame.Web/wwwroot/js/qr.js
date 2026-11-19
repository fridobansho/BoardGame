// ============================================================================
// Lightweight QR Code Renderer
// ============================================================================

export function renderSimpleQR(text, container) {
  if (!container) return;
  container.innerHTML = '';
  const img = document.createElement('img');
  img.src = `https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${encodeURIComponent(text)}&margin=2`;
  img.alt = 'QR Code';
  img.width = 180;
  img.height = 180;
  img.style.display = 'block';
  img.style.borderRadius = '8px';
  img.addEventListener('error', () => {
    container.innerHTML = '';
    const fallback = document.createElement('div');
    fallback.style.padding = '20px';
    fallback.style.fontSize = '12px';
    fallback.style.color = '#1e293b';
    fallback.style.wordBreak = 'break-all';
    fallback.textContent = text;
    container.appendChild(fallback);
  });
  container.appendChild(img);
}
