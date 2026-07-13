import { Minus, Plus } from 'lucide-react';
import { formatMin } from './timeline';

type Props = {
  label: string;
  /** Valor en minutos: hora del día o duración según `modo`. */
  value: number;
  modo: 'hora' | 'duracion';
  min: number;
  max: number;
  onChange: (value: number) => void;
};

// Stepper táctil de ±15 min; en modo hora incluye un time picker nativo como alternativa.
export function StepperTiempo({ label, value, modo, min, max, onChange }: Props) {
  const paso = 15;

  function step(delta: number) {
    onChange(Math.max(min, Math.min(max, value + delta)));
  }

  return (
    <div className="flex items-center justify-between gap-3">
      <span className="text-sm font-medium text-slate-700">{label}</span>
      <div className="flex items-center gap-1.5">
        <button
          type="button"
          onClick={() => step(-paso)}
          disabled={value - paso < min}
          aria-label={`Restar 15 minutos a ${label.toLowerCase()}`}
          className="flex h-11 w-11 items-center justify-center rounded-xl border border-slate-300 bg-white text-slate-700 transition hover:bg-slate-50 disabled:opacity-40"
        >
          <Minus size={16} />
        </button>

        {modo === 'hora' ? (
          <input
            type="time"
            step={300}
            value={formatMin(value)}
            onChange={(e) => {
              if (!e.target.value) return;
              const [h, m] = e.target.value.split(':').map(Number);
              onChange(Math.max(min, Math.min(max, h * 60 + m)));
            }}
            className="h-11 w-24 rounded-xl border border-slate-300 bg-white px-2 text-center text-sm font-semibold tabular-nums"
          />
        ) : (
          <span className="inline-flex h-11 w-24 items-center justify-center rounded-xl border border-slate-200 bg-slate-50 text-sm font-semibold tabular-nums text-slate-900">
            {value} min
          </span>
        )}

        <button
          type="button"
          onClick={() => step(paso)}
          disabled={value + paso > max}
          aria-label={`Sumar 15 minutos a ${label.toLowerCase()}`}
          className="flex h-11 w-11 items-center justify-center rounded-xl border border-slate-300 bg-white text-slate-700 transition hover:bg-slate-50 disabled:opacity-40"
        >
          <Plus size={16} />
        </button>
      </div>
    </div>
  );
}
