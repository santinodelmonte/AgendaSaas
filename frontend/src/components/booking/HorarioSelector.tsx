import type { DisponibilidadTurno } from '../../types/public';
import { formatTime } from '../../utils/date';

type Props = {
  horarios: DisponibilidadTurno[];
  value?: string;
  onChange: (value: string) => void;
  onOcupado?: () => void;
};

export function HorarioSelector({ horarios, value, onChange, onOcupado }: Props) {
  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
      {horarios.map((item) => {
        const disabled = item.estado !== 'Disponible';
        const active = value === item.fechaHora;

        return (
          <button
            key={item.fechaHora}
            type="button"
            onClick={() => {
              if (disabled) {
                onOcupado?.();
              } else {
                onChange(item.fechaHora);
              }
            }}
            className={`rounded-2xl border px-4 py-4 text-sm font-semibold transition ${
              active
                ? 'border-brand-600 bg-brand-600 text-white shadow-soft'
                : disabled
                  ? 'cursor-not-allowed border-slate-200 bg-slate-100 text-slate-400'
                  : 'border-slate-200 bg-white text-slate-900 hover:border-brand-300 hover:bg-brand-50'
            }`}
          >
            {formatTime(item.fechaHora)}
            <span className="mt-1 block text-xs font-normal opacity-80">{item.estado}</span>
          </button>
        );
      })}
    </div>
  );
}
