import { useEffect, useRef, useState } from 'react';
import { Plus } from 'lucide-react';
import type { TurnoAgenda } from '../../types/admin';
import {
  PX_POR_MINUTO,
  estadoTurnoCfg,
  formatMin,
  minutosAhora,
  minutosDe,
  redondearA15,
  type Intervalo,
} from './timeline';

type Props = {
  fecha: string;
  esHoy: boolean;
  ventana: Intervalo;
  pausas: Intervalo[];
  huecos: Intervalo[];
  turnos: TurnoAgenda[];
  duracionDefault: number;
  onTapHueco: (minutos: number) => void;
  onTapTurno: (turno: TurnoAgenda) => void;
};

export function AdminScheduleTimeline({
  fecha,
  esHoy,
  ventana,
  pausas,
  huecos,
  turnos,
  duracionDefault,
  onTapHueco,
  onTapTurno,
}: Props) {
  const nowRef = useRef<HTMLDivElement>(null);
  const [ahora, setAhora] = useState(minutosAhora);

  useEffect(() => {
    const id = window.setInterval(() => setAhora(minutosAhora()), 60_000);
    return () => window.clearInterval(id);
  }, []);

  // Auto-scroll al horario actual al montar o cambiar de día
  useEffect(() => {
    if (esHoy) {
      nowRef.current?.scrollIntoView({ block: 'center', behavior: 'smooth' });
    }
  }, [fecha, esHoy]);

  const top = (min: number) => (min - ventana.inicio) * PX_POR_MINUTO;
  const alto = (i: Intervalo) => (i.fin - i.inicio) * PX_POR_MINUTO;

  // Marcas del eje cada 30 minutos
  const marcas: number[] = [];
  for (let m = ventana.inicio; m <= ventana.fin; m += 30) marcas.push(m);

  function handleTapHueco(e: React.MouseEvent<HTMLButtonElement>, hueco: Intervalo) {
    const rect = e.currentTarget.getBoundingClientRect();
    const relativo = (e.clientY - rect.top) / PX_POR_MINUTO;
    const redondeado = redondearA15(hueco.inicio + relativo);
    const min = Math.max(hueco.inicio, Math.min(redondeado, hueco.fin - 15));
    onTapHueco(min);
  }

  const mostrarAhora = esHoy && ahora >= ventana.inicio && ahora <= ventana.fin;

  return (
    <div className="relative" style={{ height: alto(ventana) + 24 }}>
      {/* Eje de horas + líneas guía */}
      {marcas.map((m) => (
        <div key={m} className="pointer-events-none absolute left-0 right-0" style={{ top: top(m) }}>
          <div className="flex items-center gap-2">
            <span
              className={`w-11 -translate-y-1/2 text-right text-[11px] tabular-nums ${
                m % 60 === 0 ? 'font-semibold text-slate-500' : 'text-slate-300'
              }`}
            >
              {formatMin(m)}
            </span>
            <div className={`flex-1 border-t ${m % 60 === 0 ? 'border-slate-200' : 'border-slate-100'}`} />
          </div>
        </div>
      ))}

      {/* Pausas / fuera de horario */}
      {pausas.map((p) => (
        <div
          key={`pausa-${p.inicio}`}
          className="absolute left-14 right-1 flex items-center justify-center rounded-xl"
          style={{
            top: top(p.inicio),
            height: alto(p),
            backgroundImage:
              'repeating-linear-gradient(-45deg, rgba(100,116,139,0.08) 0 6px, transparent 6px 12px)',
          }}
        >
          {alto(p) >= 28 && (
            <span className="text-[11px] font-medium uppercase tracking-widest text-slate-300">
              No disponible
            </span>
          )}
        </div>
      ))}

      {/* Huecos libres: tocables para crear un turno */}
      {huecos.map((h) => (
        <button
          key={`hueco-${h.inicio}`}
          type="button"
          onClick={(e) => handleTapHueco(e, h)}
          aria-label={`Crear turno entre ${formatMin(h.inicio)} y ${formatMin(h.fin)}`}
          className="group absolute left-14 right-1 z-10 rounded-xl border border-dashed border-green-300/80 bg-green-50/50 transition hover:bg-green-100/70 active:bg-green-100"
          style={{ top: top(h.inicio) + 1, height: alto(h) - 2, minHeight: 20 }}
        >
          <span className="pointer-events-none flex h-full items-center justify-center gap-1 text-green-600/0 transition group-hover:text-green-700">
            {alto(h) >= 34 && (
              <>
                <Plus size={14} className="text-green-500/70" />
                <span className="text-xs font-medium text-green-600/70">
                  {formatMin(h.inicio)} – {formatMin(h.fin)}
                </span>
              </>
            )}
          </span>
        </button>
      ))}

      {/* Bloques de turno: altura proporcional a su duración */}
      {turnos.map((t) => {
        const inicio = minutosDe(t.fechaHora);
        const dur = t.duracionMinutos ?? duracionDefault;
        const cfg = estadoTurnoCfg(t.estado);
        const altoPx = Math.max(dur * PX_POR_MINUTO, 30);
        const compacto = altoPx < 52;

        return (
          <button
            key={t.id}
            type="button"
            onClick={() => onTapTurno(t)}
            className={`absolute left-14 right-1 z-20 overflow-hidden rounded-xl border-2 px-2.5 text-left shadow-sm transition hover:opacity-90 active:scale-[0.99] ${cfg.bg} ${cfg.border} ${
              compacto ? 'py-0.5' : 'py-1.5'
            }`}
            style={{ top: top(inicio) + 1, height: altoPx - 2 }}
          >
            <div className="flex items-center gap-1.5">
              <span className={`h-2 w-2 shrink-0 rounded-full ${cfg.dot}`} />
              <span className="text-xs font-bold tabular-nums text-slate-900">
                {formatMin(inicio)} – {formatMin(inicio + dur)}
              </span>
              <span className="ml-auto truncate text-[11px] text-slate-500">
                {compacto ? t.nombreCliente : cfg.label}
              </span>
            </div>
            {!compacto && (
              <>
                <p className="mt-0.5 truncate text-sm font-semibold text-slate-900">
                  {t.nombreCliente ?? '-'}
                </p>
                {t.servicioSolicitado && altoPx >= 76 && (
                  <p className="truncate text-xs text-slate-500">{t.servicioSolicitado}</p>
                )}
              </>
            )}
          </button>
        );
      })}

      {/* Línea de hora actual */}
      {mostrarAhora && (
        <div ref={nowRef} className="pointer-events-none absolute left-11 right-0 z-30" style={{ top: top(ahora) }}>
          <div className="flex items-center">
            <span className="h-2.5 w-2.5 -translate-y-1/2 rounded-full bg-brand-600 ring-2 ring-white" />
            <div className="h-px flex-1 -translate-y-1/2 bg-brand-600" />
          </div>
        </div>
      )}
    </div>
  );
}
