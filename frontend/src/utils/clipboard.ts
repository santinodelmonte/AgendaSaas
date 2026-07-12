// Copia texto al portapapeles con fallback para navegadores/contextos sin
// Clipboard API. Devuelve true si se copió.
export async function copyToClipboard(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch {
    const ta = document.createElement('textarea');
    ta.value = text;
    ta.style.position = 'fixed';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.focus();
    ta.select();
    let ok = false;
    try {
      ok = document.execCommand('copy');
    } catch {
      ok = false;
    }
    document.body.removeChild(ta);
    return ok;
  }
}

// Contraseña aleatoria legible (sin caracteres ambiguos) para altas y reseteos.
export function generarPassword(largo = 12): string {
  const chars = 'ABCDEFGHJKMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789';
  const arr = new Uint32Array(largo);
  crypto.getRandomValues(arr);
  let out = '';
  for (const n of arr) out += chars[n % chars.length];
  return out;
}
