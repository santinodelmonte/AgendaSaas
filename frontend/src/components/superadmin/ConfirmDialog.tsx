import type { ReactNode } from 'react';
import { ModalBase } from '../schedule/ModalBase';

type Props = {
  title: string;
  message: ReactNode;
  confirmLabel: string;
  danger?: boolean;
  loading?: boolean;
  onConfirm: () => void;
  onClose: () => void;
};

export function ConfirmDialog({ title, message, confirmLabel, danger, loading, onConfirm, onClose }: Props) {
  return (
    <ModalBase title={title} onClose={onClose}>
      <div className="space-y-4">
        <div className="text-sm text-slate-600">{message}</div>
        <div className="flex gap-2.5">
          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="h-12 flex-1 rounded-2xl border border-slate-300 bg-white text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
          >
            Cancelar
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={loading}
            className={`h-12 flex-1 rounded-2xl text-sm font-semibold text-white transition disabled:opacity-50 ${
              danger ? 'bg-red-600 hover:bg-red-700' : 'bg-slate-900 hover:bg-slate-800'
            }`}
          >
            {loading ? '...' : confirmLabel}
          </button>
        </div>
      </div>
    </ModalBase>
  );
}
