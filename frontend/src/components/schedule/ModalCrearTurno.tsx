import { useState } from 'react';
import { Sparkles } from 'lucide-react';
import { crearTurnoManual } from '../../api/admin.service';
import type { Servicio } from '../../types/admin';
import { formatDayLabel } from '../../utils/date';
import { ModalBase } from './ModalBase';
import { StepperTiempo } from './StepperTiempo';
import {
  aFechaHoraISO,
  mensajeApi,
  minutosAhora,
  sugerirHueco,
  type Intervalo,
} from './timeline';

type Props = {
  fecha: string;
  esHoy: boolean;
  /** Hora precargada por tap en un hueco; null cuando se abre desde el FAB. */
  horaInicial: number | null;
  duracionDefault: number;
  servicios: Servicio[];
  huecos: Intervalo[];
  ventana: Intervalo;
  onClose: () => void;
  onCreated: () => void;
};

const inputCls =
  'w-full rounded-xl border border-slate-300 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400';

export function ModalCrearTurno({
  fecha,
  esHoy,
  horaInicial,
  duracionDefault,
  servicios,
  huecos,
  ventana,
  onClose,
  onCreated,
}: Props) {
  const desde = esHoy ? Math.max(ventana.inicio, minutosAhora() + 15) : ventana.inicio;

  const [hora, setHora] = useState(
    () => horaInicial ?? sugerirHueco(huecos, duracionDefault, desde) ?? ventana.inicio,
  );
  const [duracion, setDuracion] = useState(duracionDefault);
  const [form, setForm] = useState({ nombre: '', telefono: '', servicio: '', nota: '' });
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  function sugerir() {
    const sugerido = sugerirHueco(huecos, duracion, desde);
    if (sugerido === null) {
      setError(`No hay huecos libres de ${duracion} minutos en este día.`);
      return;
    }
    setError(null);
    setHora(sugerido);
  }

  async function crear() {
    setSaving(true);
    setError(null);
    try {
      await crearTurnoManual({
        fechaHora: aFechaHoraISO(fecha, hora),
        nombreCliente: form.nombre.trim(),
        telefonoCliente: form.telefono.trim(),
        servicio: form.servicio.trim(),
        nota: form.nota.trim(),
        duracionMinutos: duracion,
      });
      onCreated();
    } catch (err) {
      setError(mensajeApi(err, 'No se pudo crear el turno.'));
      setSaving(false);
    }
  }

  return (
    <ModalBase title="Nuevo turno" subtitle={<span className="capitalize">{formatDayLabel(fecha)}</span>} onClose={onClose}>
      <div className="space-y-4">
        <div className="space-y-3 rounded-2xl bg-slate-50 p-4">
          <StepperTiempo
            label="Hora de inicio"
            value={hora}
            modo="hora"
            min={0}
            max={24 * 60 - 15}
            onChange={setHora}
          />
          <StepperTiempo
            label="Duración"
            value={duracion}
            modo="duracion"
            min={15}
            max={480}
            onChange={setDuracion}
          />
          <button
            type="button"
            onClick={sugerir}
            className="flex h-11 w-full items-center justify-center gap-2 rounded-xl border border-brand-100 bg-brand-50 text-sm font-medium text-brand-700 transition hover:bg-brand-100"
          >
            <Sparkles size={15} />
            Sugerir próximo hueco disponible
          </button>
        </div>

        <input
          placeholder="Nombre del cliente *"
          value={form.nombre}
          onChange={(e) => setForm((f) => ({ ...f, nombre: e.target.value }))}
          className={inputCls}
        />
        <input
          placeholder="Teléfono"
          value={form.telefono}
          onChange={(e) => setForm((f) => ({ ...f, telefono: e.target.value }))}
          className={inputCls}
        />
        <input
          placeholder="Servicio"
          list="servicios-activos"
          value={form.servicio}
          onChange={(e) => setForm((f) => ({ ...f, servicio: e.target.value }))}
          className={inputCls}
        />
        <datalist id="servicios-activos">
          {servicios.filter((s) => s.activo).map((s) => (
            <option key={s.id} value={s.nombre} />
          ))}
        </datalist>
        <input
          placeholder="Nota interna (opcional)"
          value={form.nota}
          onChange={(e) => setForm((f) => ({ ...f, nota: e.target.value }))}
          className={inputCls}
        />

        {error && (
          <p className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>
        )}

        <button
          type="button"
          onClick={crear}
          disabled={saving || !form.nombre.trim()}
          className="h-12 w-full rounded-2xl bg-slate-900 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:opacity-50"
        >
          {saving ? 'Guardando...' : 'Crear turno'}
        </button>
      </div>
    </ModalBase>
  );
}
