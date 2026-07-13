import { useState } from 'react';
import { ArrowLeftRight, ChevronLeft, Trash2 } from 'lucide-react';
import {
  cancelarTurno,
  confirmarTurno,
  intercambiarTurnos,
  rechazarTurno,
  reprogramarTurno,
} from '../../api/admin.service';
import type { TurnoAgenda } from '../../types/admin';
import { formatDayLabel } from '../../utils/date';
import { ModalBase } from './ModalBase';
import { StepperTiempo } from './StepperTiempo';
import {
  aFechaHoraISO,
  estadoTurnoCfg,
  formatMin,
  mensajeApi,
  minutosDe,
} from './timeline';

type Props = {
  turno: TurnoAgenda;
  fecha: string;
  /** Demás turnos ocupados del día, candidatos para el intercambio. */
  otrosTurnos: TurnoAgenda[];
  duracionDefault: number;
  onClose: () => void;
  onChanged: () => void;
};

export function ModalDetalleTurno({
  turno,
  fecha,
  otrosTurnos,
  duracionDefault,
  onClose,
  onChanged,
}: Props) {
  const horaOriginal = minutosDe(turno.fechaHora);
  const duracionOriginal = turno.duracionMinutos ?? duracionDefault;

  const [vista, setVista] = useState<'detalle' | 'swap'>('detalle');
  const [hora, setHora] = useState(horaOriginal);
  const [duracion, setDuracion] = useState(duracionOriginal);
  const [swapSeleccion, setSwapSeleccion] = useState<TurnoAgenda | null>(null);
  const [swapDuraciones, setSwapDuraciones] = useState(false);
  const [confirmandoEliminar, setConfirmandoEliminar] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const cfg = estadoTurnoCfg(turno.estado);
  const hayCambios = hora !== horaOriginal || duracion !== duracionOriginal;

  async function ejecutar(fn: () => Promise<unknown>, fallback: string) {
    setSaving(true);
    setError(null);
    try {
      await fn();
      onChanged();
    } catch (err) {
      setError(mensajeApi(err, fallback));
      setSaving(false);
    }
  }

  const guardarCambios = () =>
    ejecutar(
      () => reprogramarTurno(turno.id, { fechaHora: aFechaHoraISO(fecha, hora), duracionMinutos: duracion }),
      'No se pudo reprogramar el turno.',
    );

  const eliminar = () =>
    ejecutar(
      () => (turno.estado === 'Pendiente' ? rechazarTurno(turno.id) : cancelarTurno(turno.id)),
      'No se pudo eliminar el turno.',
    );

  const intercambiar = () => {
    if (!swapSeleccion) return;
    return ejecutar(
      () =>
        intercambiarTurnos({
          turnoAId: turno.id,
          turnoBId: swapSeleccion.id,
          intercambiarDuraciones: swapDuraciones,
        }),
      'No se pudo intercambiar los turnos.',
    );
  };

  if (vista === 'swap') {
    return (
      <ModalBase
        title="Intercambiar turno"
        subtitle={`${formatMin(horaOriginal)} · ${turno.nombreCliente ?? '-'}`}
        onClose={onClose}
      >
        <button
          type="button"
          onClick={() => { setVista('detalle'); setSwapSeleccion(null); setError(null); }}
          className="mb-3 flex h-11 items-center gap-1 rounded-xl px-2 text-sm font-medium text-slate-500 transition hover:bg-slate-100"
        >
          <ChevronLeft size={16} /> Volver al detalle
        </button>

        {otrosTurnos.length === 0 ? (
          <p className="rounded-2xl bg-slate-50 p-4 text-sm text-slate-500">
            No hay otros turnos en este día para intercambiar.
          </p>
        ) : (
          <div className="space-y-2">
            {otrosTurnos.map((t) => {
              const c = estadoTurnoCfg(t.estado);
              const seleccionado = swapSeleccion?.id === t.id;
              return (
                <button
                  key={t.id}
                  type="button"
                  onClick={() => setSwapSeleccion(seleccionado ? null : t)}
                  className={`w-full rounded-2xl border-2 p-3 text-left transition ${c.bg} ${
                    seleccionado ? 'border-slate-900' : c.border
                  }`}
                >
                  <div className="flex items-center gap-1.5">
                    <span className={`h-2 w-2 rounded-full ${c.dot}`} />
                    <span className="text-xs font-bold tabular-nums text-slate-900">
                      {formatMin(minutosDe(t.fechaHora))}
                    </span>
                    <span className="ml-auto text-xs text-slate-500">
                      {t.duracionMinutos ?? duracionDefault} min
                    </span>
                  </div>
                  <p className="mt-0.5 truncate text-sm font-semibold text-slate-900">{t.nombreCliente ?? '-'}</p>
                  {t.servicioSolicitado && (
                    <p className="truncate text-xs text-slate-500">{t.servicioSolicitado}</p>
                  )}
                </button>
              );
            })}
          </div>
        )}

        {swapSeleccion && (
          <div className="mt-4 space-y-3 rounded-2xl bg-slate-50 p-4">
            <p className="text-sm text-slate-700">
              <span className="font-semibold">{turno.nombreCliente ?? '-'}</span> pasa a las{' '}
              <span className="font-semibold tabular-nums">{formatMin(minutosDe(swapSeleccion.fechaHora))}</span> y{' '}
              <span className="font-semibold">{swapSeleccion.nombreCliente ?? '-'}</span> a las{' '}
              <span className="font-semibold tabular-nums">{formatMin(horaOriginal)}</span>.
            </p>
            <label className="flex min-h-11 cursor-pointer items-center gap-2.5 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={swapDuraciones}
                onChange={(e) => setSwapDuraciones(e.target.checked)}
                className="h-5 w-5 rounded border-slate-300 accent-slate-900"
              />
              Intercambiar también las duraciones
            </label>
            {error && (
              <p className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>
            )}
            <button
              type="button"
              onClick={intercambiar}
              disabled={saving}
              className="h-12 w-full rounded-2xl bg-slate-900 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:opacity-50"
            >
              {saving ? 'Intercambiando...' : 'Confirmar intercambio'}
            </button>
          </div>
        )}
      </ModalBase>
    );
  }

  return (
    <ModalBase
      title={`${formatMin(horaOriginal)} – ${formatMin(horaOriginal + duracionOriginal)}`}
      subtitle={<span className="capitalize">{formatDayLabel(fecha)}</span>}
      onClose={onClose}
    >
      <span
        className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium ${cfg.bg} ${cfg.border}`}
      >
        <span className={`h-1.5 w-1.5 rounded-full ${cfg.dot}`} />
        {cfg.label}
      </span>

      <div className="mt-4 space-y-1.5 rounded-2xl bg-slate-50 p-4">
        <p className="text-sm text-slate-700">
          <span className="font-medium">Cliente:</span> {turno.nombreCliente ?? '-'}
        </p>
        {turno.telefonoCliente && (
          <p className="text-sm text-slate-700"><span className="font-medium">Teléfono:</span> {turno.telefonoCliente}</p>
        )}
        {turno.servicioSolicitado && (
          <p className="text-sm text-slate-700"><span className="font-medium">Servicio:</span> {turno.servicioSolicitado}</p>
        )}
        {turno.notaInterna && (
          <p className="text-sm text-slate-700"><span className="font-medium">Nota:</span> {turno.notaInterna}</p>
        )}
      </div>

      <div className="mt-4 space-y-3 rounded-2xl bg-slate-50 p-4">
        <StepperTiempo label="Hora de inicio" value={hora} modo="hora" min={0} max={24 * 60 - 15} onChange={setHora} />
        <StepperTiempo label="Duración" value={duracion} modo="duracion" min={15} max={480} onChange={setDuracion} />
        {hayCambios && (
          <button
            type="button"
            onClick={guardarCambios}
            disabled={saving}
            className="h-12 w-full rounded-2xl bg-slate-900 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:opacity-50"
          >
            {saving ? 'Guardando...' : `Guardar cambios (${formatMin(hora)} · ${duracion} min)`}
          </button>
        )}
      </div>

      {error && (
        <p className="mt-4 rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>
      )}

      <div className="mt-4 space-y-2.5">
        {turno.estado === 'Pendiente' && (
          <div className="flex gap-2.5">
            <button
              type="button"
              onClick={() => ejecutar(() => confirmarTurno(turno.id), 'No se pudo confirmar el turno.')}
              disabled={saving}
              className="h-12 flex-1 rounded-2xl bg-green-600 text-sm font-semibold text-white transition hover:bg-green-700 disabled:opacity-50"
            >
              Confirmar
            </button>
            <button
              type="button"
              onClick={() => ejecutar(() => rechazarTurno(turno.id), 'No se pudo rechazar el turno.')}
              disabled={saving}
              className="h-12 flex-1 rounded-2xl bg-red-500 text-sm font-semibold text-white transition hover:bg-red-600 disabled:opacity-50"
            >
              Rechazar
            </button>
          </div>
        )}

        <button
          type="button"
          onClick={() => setVista('swap')}
          disabled={saving}
          className="flex h-12 w-full items-center justify-center gap-2 rounded-2xl border border-slate-300 bg-white text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:opacity-50"
        >
          <ArrowLeftRight size={16} />
          Intercambiar con otro turno
        </button>

        {confirmandoEliminar ? (
          <div className="flex items-center gap-2.5 rounded-2xl border border-red-200 bg-red-50 p-2.5">
            <span className="flex-1 pl-1.5 text-sm font-medium text-red-700">¿Eliminar este turno?</span>
            <button
              type="button"
              onClick={eliminar}
              disabled={saving}
              className="h-11 rounded-xl bg-red-600 px-4 text-sm font-semibold text-white transition hover:bg-red-700 disabled:opacity-50"
            >
              {saving ? '...' : 'Sí, eliminar'}
            </button>
            <button
              type="button"
              onClick={() => setConfirmandoEliminar(false)}
              disabled={saving}
              className="h-11 rounded-xl px-3 text-sm font-medium text-slate-600 transition hover:bg-white"
            >
              No
            </button>
          </div>
        ) : (
          <button
            type="button"
            onClick={() => setConfirmandoEliminar(true)}
            disabled={saving}
            className="flex h-12 w-full items-center justify-center gap-2 rounded-2xl border border-red-200 bg-white text-sm font-semibold text-red-600 transition hover:bg-red-50 disabled:opacity-50"
          >
            <Trash2 size={16} />
            Eliminar turno
          </button>
        )}
      </div>
    </ModalBase>
  );
}
