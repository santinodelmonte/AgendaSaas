import type { ReactNode } from 'react';
import { X } from 'lucide-react';

type Props = {
  title: ReactNode;
  subtitle?: ReactNode;
  onClose: () => void;
  children: ReactNode;
};

// Bottom-sheet en mobile, centrado en desktop — mismo patrón que el modal del dashboard.
export function ModalBase({ title, subtitle, onClose, children }: Props) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
      onClick={onClose}
    >
      <div
        className="max-h-[85dvh] w-full max-w-md overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mb-4 flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h4 className="text-xl font-bold text-slate-900">{title}</h4>
            {subtitle && <p className="mt-0.5 text-sm text-slate-400">{subtitle}</p>}
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Cerrar"
            className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
          >
            <X size={20} />
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
