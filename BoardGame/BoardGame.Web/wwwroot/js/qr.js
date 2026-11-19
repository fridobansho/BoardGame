// ============================================================================
// Lightweight QR Code Renderer
// ============================================================================

export function renderSimpleQR(text, container) {
  if (!container) return;
  const encoded = encodeURIComponent(text);
  container.innerHTML = `
    <img src="https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${encoded}&margin=2" 
         alt="QR Code" width="180" height="180" style="display:block;border-radius:8px;" 
         onerror="this.onerror=null;this.parentElement.innerHTML='<div style=\\'padding:20px;font-size:12px;color:#1e293b;word-break:break-all;\\'>${text}</div>'">
  `;
}
