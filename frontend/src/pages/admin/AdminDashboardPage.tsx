import { useEffect, useMemo, useState } from 'react';
import { useToast } from '../../contexts/ToastContext';
import {
  CalendarClock, CalendarCheck2, CalendarDays, Sparkles,
  ChevronLeft, ChevronRight, Plus, Copy, Check,
} from 'lucide-react';
import { addDays, parseISO } from 'date-fns';
import {
  getAdminDashboard,
  getAdminPerfil,
  getAdminServicios,
  getAgendaDiaria,
  getAgendaSlots,
  confirmarTurno,
  rechazarTurno,
  cancelarTurno,
  crearTurnoManual,
  reprogramarTurno,
} from '../../api/admin.service';
import type {
  AgendaSlot, AgendaSlotsResponse, DashboardStats, Metricas,
  PerfilAdmin, Servicio, TurnoAgenda,
} from '../../types/admin';
import { getMetricas } from '../../api/admin.service';
import { Loading } from '../../components/common/Loading';
import { CardDashboard } from '../../components/dashboard/CardDashboard';
import { ModalCrearTurno } from '../../components/schedule/ModalCrearTurno';
import { StepperTiempo } from '../../components/schedule/StepperTiempo';
import { computarLayoutDia, mensajeApi } from '../../components/schedule/timeline';
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
  const [metricas, setMetricas] = useState<Metricas | null>(null);
  const [slotsResp, setSlotsResp] = useState<AgendaSlotsResponse | null>(null);
  const slots = slotsResp?.slots ?? [];
  const [turnos, setTurnos] = useState<TurnoAgenda[]>([]);
  const [perfil, setPerfil] = useState<PerfilAdmin | null>(null);
  const [servicios, setServicios] = useState<Servicio[]>([]);
  const [fecha, setFecha] = useState(toDateInputValue(new Date()));
  const [loadingDash, setLoadingDash] = useState(true);
  const [loadingSlots, setLoadingSlots] = useState(true);
  const [selected, setSelected] = useState<AgendaSlot | null>(null);
  const [duracion, setDuracion] = useState(30);
  const [form, setForm] = useState({ nombre: '', telefono: '', servicio: '', nota: '' });
  // undefined = cerrado; null = abierto sin hora precargada (botón Nuevo turno)
  const [crearHora, setCrearHora] = useState<number | null | undefined>(undefined);
  const [copiado, setCopiado] = useState(false);
  const { pushToast } = useToast();
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    Promise.all([getAdminDashboard(), getMetricas()])
      .then(([dash, met]) => { setData(dash); setMetricas(met); })
      .finally(() => setLoadingDash(false));
    getAdminPerfil().then(setPerfil).catch(() => undefined);
    getAdminServicios().then(setServicios).catch(() => setServicios([]));
  }, []);

  useEffect(() => {
    setLoadingSlots(true);
    Promise.all([getAgendaSlots(fecha), getAgendaDiaria(fecha)])
      .then(([slotsData, turnosData]) => { setSlotsResp(slotsData); setTurnos(turnosData); })
      .finally(() => setLoadingSlots(false));
  }, [fecha]);

  function refresh() {
    getAgendaSlots(fecha).then(setSlotsResp);
    getAgendaDiaria(fecha).then(setTurnos);
    getAdminDashboard().then(setData);
  }

  const paso = perfil?.duracionTurnoMinutos ?? 30;
  const esHoy = fecha === toDateInputValue(new Date());
  const { ventana, huecos } = useMemo(
    () => computarLayoutDia(turnos, slots, paso),
    [turnos, slots, paso],
  );
  const linkPublico = perfil?.slug ? `${window.location.origin}/agenda/${perfil.slug}` : '';

  async function copiarLink() {
    if (!linkPublico) return;

    let copiadoOk = false;
    try {
      await navigator.clipboard.writeText(linkPublico);
      copiadoOk = true;
    } catch {
      // Fallback para navegadores sin Clipboard API o contextos sin foco/no-seguros
      const ta = document.createElement('textarea');
      ta.value = linkPublico;
      ta.style.position = 'fixed';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.focus();
      ta.select();
      try {
        copiadoOk = document.execCommand('copy');
      } catch {
        copiadoOk = false;
      }
      document.body.removeChild(ta);
    }

    if (copiadoOk) {
      setCopiado(true);
      pushToast({ type: 'success', message: 'Link copiado al portapapeles.' });
      window.setTimeout(() => setCopiado(false), 2000);
    } else {
      pushToast({ type: 'error', message: 'No se pudo copiar el link.' });
    }
  }

  function navegarDia(dias: number) {
    setFecha(toDateInputValue(addDays(parseISO(fecha), dias)));
  }

  function abrirModal(slot: AgendaSlot) {
    setSelected(slot);
    setDuracion(slot.duracionMinutos ?? paso);
  }

  function cerrarModal() {
    setSelected(null);
    setForm({ nombre: '', telefono: '', servicio: '', nota: '' });
  }

  async function handleGuardarDuracion() {
    if (!selected?.id) return;
    setSaving(true);
    try {
      await reprogramarTurno(selected.id, {
        fechaHora: selected.fechaHora,
        duracionMinutos: duracion,
      });
      cerrarModal();
      refresh();
      pushToast({ type: 'success', message: 'Duración actualizada.' });
    } catch (err) {
      pushToast({ type: 'error', message: mensajeApi(err, 'No se pudo actualizar la duración.') });
    } finally {
      setSaving(false);
    }
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
    if (new Date(selected.fechaHora) < new Date()) {
      pushToast({ type: 'error', message: 'No se puede cancelar un turno que ya pasó.' });
      return;
    }
    setSaving(true);
    await cancelarTurno(selected.id);
    cerrarModal();
    refresh();
    setSaving(false);
  }

  async function handleCrearManual() {
    if (!selected) return;
    if (new Date(selected.fechaHora) <= new Date()) {
      pushToast({ type: 'error', message: 'No se puede crear un turno en el pasado.' });
      return;
    }
    setSaving(true);
    await crearTurnoManual({
      fechaHora: selected.fechaHora,
      nombreCliente: form.nombre,
      telefonoCliente: form.telefono,
      servicio: form.servicio,
      nota: form.nota,
      duracionMinutos: duracion,
    });
    cerrarModal();
    refresh();
    setSaving(false);
  }

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Dashboard</p>
          <h2 className="mt-2 text-2xl font-semibold text-slate-900">Resumen general</h2>
        </div>
        <button
          type="button"
          onClick={() => setCrearHora(null)}
          className="inline-flex h-11 shrink-0 items-center gap-2 rounded-2xl bg-slate-900 px-4 text-sm font-semibold text-white shadow-sm transition hover:bg-slate-800"
        >
          <Plus size={18} />
          <span className="hidden sm:inline">Nuevo turno</span>
          <span className="sm:hidden">Turno</span>
        </button>
      </div>

      {/* Link público de reservas */}
      {linkPublico && (
        <div className="rounded-3xl border border-brand-100 bg-brand-50/60 p-5 shadow-soft">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="min-w-0">
              <p className="text-xs font-medium uppercase tracking-widest text-brand-600">Tu link de reservas</p>
              <p className="mt-1 truncate font-medium text-slate-900">{linkPublico}</p>
              <p className="mt-0.5 text-xs text-slate-500">Compartilo con tus clientas para que reserven online.</p>
            </div>
            <button
              type="button"
              onClick={copiarLink}
              className={`inline-flex h-11 shrink-0 items-center justify-center gap-2 rounded-2xl px-4 text-sm font-semibold transition ${
                copiado
                  ? 'bg-green-600 text-white'
                  : 'bg-white text-slate-800 shadow-sm hover:bg-slate-50'
              }`}
            >
              {copiado ? <Check size={16} /> : <Copy size={16} />}
              {copiado ? 'Copiado' : 'Copiar link'}
            </button>
          </div>
        </div>
      )}

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
        ) : slotsResp?.bloqueado ? (
          <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
            Día bloqueado{slotsResp.motivo ? `: ${slotsResp.motivo}` : '.'}
          </div>
        ) : slots.length === 0 ? (
          <p className="text-sm text-slate-400">Sin horario configurado para este día.</p>
        ) : (
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
            {slots.map((slot) => {
              const cfg = estadoCfg(slot.estado);
              return (
                <button
                  key={slot.fechaHora}
                  onClick={() => abrirModal(slot)}
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

      {/* Métricas */}
      {metricas && (
        <div className="grid gap-6 lg:grid-cols-3">
          {/* Tasa de confirmación */}
          <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-soft">
            <p className="text-xs font-medium uppercase tracking-widest text-slate-500">Tasa de confirmación</p>
            <p className="mt-2 text-4xl font-bold text-slate-900">{metricas.tasaConfirmacion}%</p>
            <p className="mt-1 text-sm text-slate-400">
              {metricas.confirmados} confirmados / {metricas.totalPasados} totales
            </p>
          </div>

          {/* Horas más pedidas */}
          <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-soft">
            <p className="text-xs font-medium uppercase tracking-widest text-slate-500">Horas más solicitadas</p>
            <div className="mt-3 space-y-2">
              {metricas.horasMasPedidas.length === 0 ? (
                <p className="text-sm text-slate-400">Sin datos aún</p>
              ) : metricas.horasMasPedidas.map((h) => {
                const max = metricas.horasMasPedidas[0].total;
                const pct = Math.round((h.total / max) * 100);
                return (
                  <div key={h.hora} className="flex items-center gap-3">
                    <span className="w-12 text-right text-sm font-medium text-slate-700">
                      {String(h.hora).padStart(2, '0')}:00
                    </span>
                    <div className="flex-1 overflow-hidden rounded-full bg-slate-100 h-2">
                      <div className="h-2 rounded-full bg-brand-500" style={{ width: `${pct}%` }} />
                    </div>
                    <span className="w-6 text-xs text-slate-500">{h.total}</span>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Turnos por mes */}
          <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-soft">
            <p className="text-xs font-medium uppercase tracking-widest text-slate-500">Turnos últimos 6 meses</p>
            <div className="mt-3 space-y-2">
              {metricas.porMes.length === 0 ? (
                <p className="text-sm text-slate-400">Sin datos aún</p>
              ) : metricas.porMes.map((m) => {
                const max = Math.max(...metricas.porMes.map((x) => x.total));
                const pct = max > 0 ? Math.round((m.total / max) * 100) : 0;
                return (
                  <div key={m.mes} className="flex items-center gap-3">
                    <span className="w-14 text-right text-xs text-slate-500">{m.mes.slice(5)}</span>
                    <div className="flex-1 overflow-hidden rounded-full bg-slate-100 h-2">
                      <div className="h-2 rounded-full bg-emerald-500" style={{ width: `${pct}%` }} />
                    </div>
                    <span className="w-6 text-xs text-slate-500">{m.total}</span>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      )}

      {/* Modal de detalle/acción */}
      {selected && (
        <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center">
          <div className="max-h-[85dvh] w-full max-w-md overflow-y-auto rounded-3xl bg-white p-6 shadow-2xl">
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

            {/* Duración editable de un turno ya reservado */}
            {selected.estado !== 'Disponible' && selected.id && (
              <div className="mb-4 space-y-3 rounded-2xl bg-slate-50 p-4">
                <StepperTiempo
                  label="Duración"
                  value={duracion}
                  modo="duracion"
                  min={15}
                  max={480}
                  onChange={setDuracion}
                />
                {duracion !== (selected.duracionMinutos ?? paso) && (
                  <button
                    onClick={handleGuardarDuracion}
                    disabled={saving}
                    className="w-full rounded-2xl bg-slate-900 py-2.5 text-sm font-medium text-white disabled:opacity-50"
                  >
                    {saving ? 'Guardando...' : `Guardar duración (${duracion} min)`}
                  </button>
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
                <div className="rounded-xl bg-slate-50 p-3">
                  <StepperTiempo
                    label="Duración"
                    value={duracion}
                    modo="duracion"
                    min={15}
                    max={480}
                    onChange={setDuracion}
                  />
                </div>
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

      {/* Modal de creación rápida (mismo que la agenda diaria) */}
      {crearHora !== undefined && (
        <ModalCrearTurno
          fecha={fecha}
          esHoy={esHoy}
          horaInicial={crearHora}
          duracionDefault={paso}
          servicios={servicios}
          huecos={huecos}
          ventana={ventana}
          onClose={() => setCrearHora(undefined)}
          onCreated={() => {
            setCrearHora(undefined);
            refresh();
            pushToast({ type: 'success', message: 'Turno creado.' });
          }}
        />
      )}
    </div>
  );
}
