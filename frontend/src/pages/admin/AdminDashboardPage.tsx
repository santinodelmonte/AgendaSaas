import { useEffect, useState } from 'react';
import { CalendarClock, CalendarCheck2, CalendarDays, Sparkles, ChevronLeft, ChevronRight } from 'lucide-react';
import { addDays, parseISO } from 'date-fns';
import {
  getAdminDashboard,
  getAgendaSlots,
  confirmarTurno,
  rechazarTurno,
  cancelarTurno,
  crearTurnoManual,
} from '../../api/admin.service';
import type { AgendaSlot, DashboardStats } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { CardDashboard } from '../../components/dashboard/CardDashboard';
import { formatDateTime, formatDayLabel, formatTime, toDateInputValue } from '../../utils/date';

const ESTADO_CONFIG = {
  Disponible: { bg: 'bg-green-50', border: 'border-green-300', dot: 'bg-green-500', label: 'Libre' },
  Pendiente:  { bg: 'bg-amber-50',  border: 'border-amber-400',  dot: 'bg-amber-500',  label: 'Pendiente' },
  Confirmado: { bg: 'bg-red-50',    border: 'border-red-400',    dot: 'bg-red-500',    label: 'Reservado' },
  Rechazado:  { bg: 'bg-slate-100', border: 'border-slate-300',  dot: 'bg-slate-400',  label: 'Rechazado' },
} as const;

type EstadoKey = keyof typeof ESTADO_CONFIG;

function estadoCfg(estado: string) {
  return ESTADO_CONFIG[estado as EstadoKey] ?? ESTADO_CONFIG.Disponible;
}

export function AdminDashboardPage() {
  const [data, setData] = useState<DashboardStats | null>(null);
  const [slots, setSlots] = useState<AgendaSlot[]>([]);
  const [fecha, setFecha] = useState(toDateInputValue(new Date()));
  const [loadingDash, setLoadingDash] = useState(true);
  const [loadingSlots, setLoadingSlots] = useState(true);
  const [selected, setSelected] = useState<AgendaSlot | null>(null);
  const [form, setForm] = useState({ nombre: '', telefono: '', servicio: '', nota: '' });
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    getAdminDashboard()
      .then(setData)
      .finally(() => setLoadingDash(false));
  }, []);

  useEffect(() => {
    setLoadingSlots(true);
    getAgendaSlots(fecha)
      .then(setSlots)
      .finally(() => setLoadingSlots(false));
  }, [fecha]);

  function refresh() {
    getAgendaSlots(fecha).then(setSlots);
    getAdminDashboard().then(setData);
  }

  function navegarDia(dias: number) {
    setFecha(toDateInputValue(addDays(parseISO(fecha), dias)));
  }

  function cerrarModal() {
    setSelected(null);
    setForm({ nombre: '', telefono: '', servicio: '', nota: '' });
  }

  async function handleConfirmar() {
    if (!selected?.id) return;
    setSaving(true);
    await confirmarTurno(selected.id);
    cerrarModal();
    refresh();
    setSaving(false);
  }

  async function handleRechazar() {
    if (!selected?.id) return;
    setSaving(true);
    await rechazarTurno(selected.id);
    cerrarModal();
    refresh();
    setSaving(false);
  }

  async function handleCancelar() {
    if (!selected?.id) return;
    setSaving(true);
    await cancelarTurno(selected.id);
    cerrarModal();
    refresh();
    setSaving(false);
  }

  async function handleCrearManual() {
    if (!selected) return;
    setSaving(true);
    await crearTurnoManual({
      fechaHora: selected.fechaHora,
      nombreCliente: form.nombre,
      telefonoCliente: form.telefono,
      servicio: form.servicio,
      nota: form.nota,
    });
    cerrarModal();
    refresh();
    setSaving(false);
  }

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Dashboard</p>
        <h2 className="mt-2 text-2xl font-semibold text-slate-900">Resumen general</h2>
      </div>

      {/* Cards de resumen */}
      {loadingDash ? (
        <Loading label="Cargando dashboard..." />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <CardDashboard title="Turnos pendientes" value={data?.turnosPendientes ?? 0} icon={<CalendarClock size={20} />} />
          <CardDashboard title="Confirmados hoy"   value={data?.confirmadosHoy ?? 0}    icon={<CalendarCheck2 size={20} />} />
          <CardDashboard title="Confirmados mes"   value={data?.confirmadosMes ?? 0}    icon={<CalendarDays size={20} />} />
          <CardDashboard
            title="Próximo turno"
            value={data?.proximoTurno ? formatDateTime(data.proximoTurno.fechaHora) : 'Sin turnos'}
            description={data?.proximoTurno?.nombreCliente ?? 'Aún no hay próximos turnos'}
            icon={<Sparkles size={20} />}
          />
        </div>
      )}

      {/* Agenda del día */}
      <div>
        {/* Navegador de fecha */}
        <div className="mb-4 flex items-center justify-between">
          <div>
            <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Agenda</p>
            <h3 className="mt-1 text-xl font-semibold text-slate-900">Turnos del día</h3>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => navegarDia(-1)}
              className="flex h-9 w-9 items-center justify-center rounded-xl border border-slate-200 bg-white hover:bg-slate-50"
            >
              <ChevronLeft size={16} />
            </button>
            <span className="min-w-[200px] text-center text-sm font-medium capitalize text-slate-700">
              {formatDayLabel(fecha)}
            </span>
            <button
              onClick={() => navegarDia(1)}
              className="flex h-9 w-9 items-center justify-center rounded-xl border border-slate-200 bg-white hover:bg-slate-50"
            >
              <ChevronRight size={16} />
            </button>
          </div>
        </div>

        {/* Leyenda */}
        <div className="mb-4 flex flex-wrap gap-4">
          {Object.entries(ESTADO_CONFIG).map(([estado, cfg]) => (
            <div key={estado} className="flex items-center gap-1.5">
              <span className={`h-2.5 w-2.5 rounded-full ${cfg.dot}`} />
              <span className="text-xs text-slate-500">{cfg.label}</span>
            </div>
          ))}
        </div>

        {/* Grid de slots */}
        {loadingSlots ? (
          <Loading label="Cargando agenda..." />
        ) : slots.length === 0 ? (
          <p className="text-sm text-slate-400">Sin horario configurado para este día.</p>
        ) : (
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
            {slots.map((slot) => {
              const cfg = estadoCfg(slot.estado);
              return (
                <button
                  key={slot.fechaHora}
                  onClick={() => setSelected(slot)}
                  className={`rounded-2xl border-2 ${cfg.bg} ${cfg.border} p-3 text-left transition hover:opacity-75`}
                >
                  <div className="mb-1 flex items-center gap-1.5">
                    <span className={`h-2 w-2 rounded-full ${cfg.dot}`} />
                    <span className="text-xs font-medium text-slate-500">{cfg.label}</span>
                  </div>
                  <p className="text-base font-bold text-slate-900">{formatTime(slot.fechaHora)}</p>
                  {slot.nombreCliente && (
                    <p className="mt-0.5 truncate text-xs text-slate-600">{slot.nombreCliente}</p>
                  )}
                </button>
              );
            })}
          </div>
        )}
      </div>

      {/* Modal de detalle/acción */}
      {selected && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center">
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-2xl">
            {/* Cabecera modal */}
            <div className="mb-4 flex items-start justify-between">
              <div>
                <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium
                  ${estadoCfg(selected.estado).bg} ${estadoCfg(selected.estado).border} border`}>
                  <span className={`h-1.5 w-1.5 rounded-full ${estadoCfg(selected.estado).dot}`} />
                  {estadoCfg(selected.estado).label}
                </span>
                <h4 className="mt-2 text-2xl font-bold text-slate-900">{formatTime(selected.fechaHora)}</h4>
                <p className="text-sm text-slate-400">{formatDayLabel(fecha)}</p>
              </div>
              <button onClick={cerrarModal} className="text-2xl leading-none text-slate-400 hover:text-slate-600">
                &times;
              </button>
            </div>

            {/* Datos del cliente (si tiene) */}
            {selected.estado !== 'Disponible' && (
              <div className="mb-4 space-y-1.5 rounded-2xl bg-slate-50 p-4">
                {selected.nombreCliente && (
                  <p className="text-sm text-slate-700"><span className="font-medium">Cliente:</span> {selected.nombreCliente}</p>
                )}
                {selected.telefonoCliente && (
                  <p className="text-sm text-slate-700"><span className="font-medium">Teléfono:</span> {selected.telefonoCliente}</p>
                )}
                {selected.servicioSolicitado && (
                  <p className="text-sm text-slate-700"><span className="font-medium">Servicio:</span> {selected.servicioSolicitado}</p>
                )}
                {selected.notaInterna && (
                  <p className="text-sm text-slate-700"><span className="font-medium">Nota:</span> {selected.notaInterna}</p>
                )}
              </div>
            )}

            {/* Acciones según estado */}
            {selected.estado === 'Disponible' && (
              <div className="space-y-3">
                <p className="text-sm text-slate-500">Reservar manualmente este turno:</p>
                <input
                  placeholder="Nombre del cliente *"
                  value={form.nombre}
                  onChange={(e) => setForm((f) => ({ ...f, nombre: e.target.value }))}
                  className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                />
                <input
                  placeholder="Teléfono"
                  value={form.telefono}
                  onChange={(e) => setForm((f) => ({ ...f, telefono: e.target.value }))}
                  className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                />
                <input
                  placeholder="Servicio"
                  value={form.servicio}
                  onChange={(e) => setForm((f) => ({ ...f, servicio: e.target.value }))}
                  className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                />
                <input
                  placeholder="Nota interna (opcional)"
                  value={form.nota}
                  onChange={(e) => setForm((f) => ({ ...f, nota: e.target.value }))}
                  className="w-full rounded-xl border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                />
                <button
                  onClick={handleCrearManual}
                  disabled={saving || !form.nombre.trim()}
                  className="w-full rounded-2xl bg-slate-900 py-2.5 text-sm font-medium text-white disabled:opacity-50"
                >
                  {saving ? 'Guardando...' : 'Confirmar reserva'}
                </button>
              </div>
            )}

            {selected.estado === 'Pendiente' && (
              <div className="flex gap-3">
                <button
                  onClick={handleConfirmar}
                  disabled={saving}
                  className="flex-1 rounded-2xl bg-green-600 py-2.5 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50"
                >
                  {saving ? '...' : 'Confirmar'}
                </button>
                <button
                  onClick={handleRechazar}
                  disabled={saving}
                  className="flex-1 rounded-2xl bg-red-500 py-2.5 text-sm font-medium text-white hover:bg-red-600 disabled:opacity-50"
                >
                  {saving ? '...' : 'Rechazar'}
                </button>
              </div>
            )}

            {selected.estado === 'Confirmado' && (
              <button
                onClick={handleCancelar}
                disabled={saving}
                className="w-full rounded-2xl bg-slate-200 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-300 disabled:opacity-50"
              >
                {saving ? '...' : 'Cancelar turno'}
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
