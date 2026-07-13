import { Copy, RefreshCw } from 'lucide-react';
import { copyToClipboard, generarPassword } from '../../utils/clipboard';
import { useToast } from '../../contexts/ToastContext';

type Props = {
  value: string;
  onChange: (value: string) => void;
};

// La contraseña se muestra en claro a propósito: el superadmin la fija y se la
// pasa a la manicurista (no hay flujo de email todavía).
export function PasswordInput({ value, onChange }: Props) {
  const { pushToast } = useToast();

  async function copiar() {
    if (!value) return;
    const ok = await copyToClipboard(value);
    pushToast(
      ok
        ? { type: 'success', message: 'Contraseña copiada.' }
        : { type: 'error', message: 'No se pudo copiar.' },
    );
  }

  return (
    <div className="flex gap-2">
      <input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder="Contraseña"
        className="h-11 flex-1 rounded-xl border border-slate-300 px-3 font-mono text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
      />
      <button
        type="button"
        onClick={() => onChange(generarPassword())}
        aria-label="Generar contraseña"
        title="Generar"
        className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl border border-slate-300 bg-white text-slate-700 transition hover:bg-slate-50"
      >
        <RefreshCw size={16} />
      </button>
      <button
        type="button"
        onClick={copiar}
        aria-label="Copiar contraseña"
        title="Copiar"
        className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl border border-slate-300 bg-white text-slate-700 transition hover:bg-slate-50"
      >
        <Copy size={16} />
      </button>
    </div>
  );
}
