import { useEffect, useState } from 'react';
import {
  getAdminPerfil, updateAdminPerfil,
  getAdminHorarios, createAdminHorario, updateAdminHorario, deleteAdminHorario,
  getDiasBloqueados, bloquearDia, desbloquearDia,
  getAdminServicios, createAdminServicio, updateAdminServicio, deleteAdminServicio,
} from '../../api/admin.service';
import type { PerfilAdmin, Horario, DiaBloqueado, Servicio } from '../../types/admin';
import { useToast } from '../../contexts/ToastContext';

type Tab = 'perfil' | 'horarios' | 'dias' | 'servicios';

const DIAS_ORDEN = [
  { num: 1, label: 'Lunes' },
  { num: 2, label: 'Martes' },
  { num: 3, label: 'Miércoles' },
  { num: 4, label: 'Jueves' },
  { num: 5, label: 'Viernes' },
  { num: 6, label: 'Sábado' },
  { num: 0, label: 'Domingo' },
];

const DURACIONES = [15, 20, 30, 45, 60, 90, 120];

const toInput = (t: string | null) => (t ? t.slice(0, 5) : '');
const toApi = (t: string) => (t.length === 5 ? t + ':00' : t);

type DiaState = {
  id?: string;
  activo: boolean;
  horaInicio: string;
  horaFin: string;
  hasPausa: boolean;
  pausaInicio: string;
  pausaFin: string;
  saving: boolean;
};

function buildDias(horarios: Horario[]): Record<number, DiaState> {
  const map: Record<number, DiaState> = {};
  for (const d of DIAS_ORDEN) {
    const h = horarios.find((x) => x.diaSemana === d.num);
    map[d.num] = h
      ? {
          id: h.id,
          activo: true,
          horaInicio: toInput(h.horaInicio),
          horaFin: toInput(h.horaFin),
          hasPausa: !!h.pausaInicio,
          pausaInicio: toInput(h.pausaInicio),
          pausaFin: toInput(h.pausaFin),
          saving: false,
        }
      : { activo: false, horaInicio: '09:00', horaFin: '18:00', hasPausa: false, pausaInicio: '13:00', pausaFin: '14:00', saving: false };
  }
  return map;
}

export function AdminConfiguracionPage() {
  const [tab, setTab] = useState<Tab>('perfil');
  const { pushToast } = useToast();

  // --- Perfil ---
  const [perfil, setPerfil] = useState<PerfilAdmin>({ nombre: '', email: '', slug: '', whatsApp: '', duracionTurnoMinutos: 30 });
  const [savingPerfil, setSavingPerfil] = useState(false);

  useEffect(() => {
    getAdminPerfil().then(setPerfil).catch(() => {});
  }, []);

  async function handleSavePerfil(e: React.FormEvent) {
    e.preventDefault();
    setSavingPerfil(true);
    try {
      await updateAdminPerfil(perfil);
      pushToast({ message: 'Perfil actualizado', type: 'success' });
    } catch {
      pushToast({ message: 'Error al guardar perfil', type: 'error' });
    } finally {
      setSavingPerfil(false);
    }
  }

  // --- Horarios ---
  const [dias, setDias] = useState<Record<number, DiaState>>({});

  useEffect(() => {
    getAdminHorarios().then((h) => setDias(buildDias(h))).catch(() => {});
  }, []);

  function setDia(num: number, patch: Partial<DiaState>) {
    setDias((prev) => ({ ...prev, [num]: { ...prev[num], ...patch } }));
  }

  async function handleToggle(num: number) {
    const d = dias[num];
    if (!d) return;

    if (d.activo) {
      // Deactivate: delete
      if (!d.id) { setDia(num, { activo: false }); return; }
      setDia(num, { saving: true });
      try {
        await deleteAdminHorario(d.id);
        setDia(num, { activo: false, id: undefined, saving: false });
        pushToast({ message: 'Día desactivado', type: 'success' });
      } catch {
        pushToast({ message: 'Error al desactivar día', type: 'error' });
        setDia(num, { saving: false });
      }
    } else {
      // Activate: create
      setDia(num, { saving: true });
      try {
        const created = await createAdminHorario({
          diaSemana: num,
          horaInicio: toApi(d.horaInicio || '09:00'),
          horaFin: toApi(d.horaFin || '18:00'),
          pausaInicio: d.hasPausa ? toApi(d.pausaInicio) : null,
          pausaFin: d.hasPausa ? toApi(d.pausaFin) : null,
        });
        setDia(num, { activo: true, id: created.id, saving: false });
        pushToast({ message: 'Día activado', type: 'success' });
      } catch {
        pushToast({ message: 'Error al activar día', type: 'error' });
        setDia(num, { saving: false });
      }
    }
  }

  async function handleSaveDia(num: number) {
    const d = dias[num];
    if (!d?.id) return;
    setDia(num, { saving: true });
    try {
      await updateAdminHorario(d.id, {
        diaSemana: num,
        horaInicio: toApi(d.horaInicio),
        horaFin: toApi(d.horaFin),
        pausaInicio: d.hasPausa ? toApi(d.pausaInicio) : null,
        pausaFin: d.hasPausa ? toApi(d.pausaFin) : null,
      });
      pushToast({ message: 'Horario guardado', type: 'success' });
    } catch {
      pushToast({ message: 'Error al guardar horario', type: 'error' });
    } finally {
      setDia(num, { saving: false });
    }
  }

  // --- Días bloqueados ---
  const [bloqueados, setBloqueados] = useState<DiaBloqueado[]>([]);
  const [bloqueoFecha, setBloqueoFecha] = useState('');
  const [bloqueoMotivo, setBloqueoMotivo] = useState('');
  const [savingBloqueo, setSavingBloqueo] = useState(false);

  useEffect(() => {
    getDiasBloqueados().then(setBloqueados).catch(() => {});
  }, []);

  async function handleBloquear(e: React.FormEvent) {
    e.preventDefault();
    if (!bloqueoFecha) return;
    setSavingBloqueo(true);
    try {
      const nuevo = await bloquearDia(bloqueoFecha, bloqueoMotivo || undefined);
      setBloqueados((prev) => [...prev, nuevo]);
      setBloqueoFecha('');
      setBloqueoMotivo('');
      pushToast({ message: 'Día bloqueado', type: 'success' });
    } catch {
      pushToast({ message: 'Error al bloquear día', type: 'error' });
    } finally {
      setSavingBloqueo(false);
    }
  }

  async function handleDesbloquear(id: string) {
    try {
      await desbloquearDia(id);
      setBloqueados((prev) => prev.filter((b) => b.id !== id));
      pushToast({ message: 'Día desbloqueado', type: 'success' });
    } catch {
      pushToast({ message: 'Error al desbloquear', type: 'error' });
    }
  }

  // --- Servicios ---
  const [servicios, setServicios] = useState<Servicio[]>([]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [nuevoNombre, setNuevoNombre] = useState('');
  const [nuevoPrecio, setNuevoPrecio] = useState('');
  const [savingServicio, setSavingServicio] = useState(false);

  useEffect(() => {
    getAdminServicios().then(setServicios).catch(() => {});
  }, []);

  async function handleCrearServicio(e: React.FormEvent) {
    e.preventDefault();
    if (!nuevoNombre.trim()) return;
    setSavingServicio(true);
    try {
      const creado = await createAdminServicio({ nombre: nuevoNombre.trim(), precio: parseFloat(nuevoPrecio) || 0 });
      setServicios((prev) => [...prev, creado]);
      setNuevoNombre('');
      setNuevoPrecio('');
      pushToast({ message: 'Servicio creado', type: 'success' });
    } catch {
      pushToast({ message: 'Error al crear servicio', type: 'error' });
    } finally {
      setSavingServicio(false);
    }
  }

  async function handleGuardarServicio(s: Servicio) {
    try {
      const actualizado = await updateAdminServicio(s.id, { nombre: s.nombre, precio: s.precio, activo: s.activo });
      setServicios((prev) => prev.map((x) => x.id === actualizado.id ? actualizado : x));
      setEditingId(null);
      pushToast({ message: 'Servicio guardado', type: 'success' });
    } catch {
      pushToast({ message: 'Error al guardar servicio', type: 'error' });
    }
  }

  async function handleEliminarServicio(id: string) {
    try {
      await deleteAdminServicio(id);
      setServicios((prev) => prev.filter((s) => s.id !== id));
      pushToast({ message: 'Servicio eliminado', type: 'success' });
    } catch {
      pushToast({ message: 'Error al eliminar servicio', type: 'error' });
    }
  }

  const hoy = new Date().toISOString().split('T')[0];

  const tabs: { key: Tab; label: string }[] = [
    { key: 'perfil', label: 'Perfil' },
    { key: 'horarios', label: 'Horarios de trabajo' },
    { key: 'servicios', label: 'Servicios' },
    { key: 'dias', label: 'Días bloqueados' },
  ];

  return (
    <div className="mx-auto max-w-2xl space-y-6 p-4">
      <h1 className="text-2xl font-semibold text-slate-900">Configuración</h1>

      {/* Tabs */}
      <div className="flex gap-1 rounded-2xl bg-slate-100 p-1">
        {tabs.map((t) => (
          <button
            key={t.key}
            type="button"
            onClick={() => setTab(t.key)}
            className={`flex-1 rounded-xl py-2 text-sm font-medium transition ${
              tab === t.key ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Perfil tab */}
      {tab === 'perfil' && (
        <form onSubmit={handleSavePerfil} className="space-y-4 rounded-2xl border border-slate-200 bg-white p-6">
          <div className="space-y-1">
            <label className="text-sm font-medium text-slate-700">Nombre</label>
            <input
              className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              value={perfil.nombre}
              onChange={(e) => setPerfil({ ...perfil, nombre: e.target.value })}
              required
            />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium text-slate-700">Email</label>
            <input
              type="email"
              className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              value={perfil.email}
              onChange={(e) => setPerfil({ ...perfil, email: e.target.value })}
              required
            />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium text-slate-700">WhatsApp</label>
            <input
              className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              value={perfil.whatsApp}
              onChange={(e) => setPerfil({ ...perfil, whatsApp: e.target.value })}
            />
          </div>
          <div className="space-y-1">
            <label className="text-sm font-medium text-slate-700">Duración de turno</label>
            <select
              className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              value={perfil.duracionTurnoMinutos}
              onChange={(e) => setPerfil({ ...perfil, duracionTurnoMinutos: Number(e.target.value) })}
            >
              {DURACIONES.map((d) => (
                <option key={d} value={d}>{d} minutos</option>
              ))}
            </select>
          </div>
          <button
            type="submit"
            disabled={savingPerfil}
            className="w-full rounded-xl bg-brand-600 py-2 text-sm font-medium text-white hover:bg-brand-700 disabled:opacity-50"
          >
            {savingPerfil ? 'Guardando…' : 'Guardar perfil'}
          </button>
        </form>
      )}

      {/* Horarios tab */}
      {tab === 'horarios' && (
        <div className="space-y-3">
          {DIAS_ORDEN.map(({ num, label }) => {
            const d = dias[num];
            if (!d) return null;
            return (
              <div key={num} className="rounded-2xl border border-slate-200 bg-white p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <span className="font-medium text-slate-800">{label}</span>
                  <button
                    type="button"
                    disabled={d.saving}
                    onClick={() => handleToggle(num)}
                    className={`relative h-6 w-11 rounded-full transition-colors focus:outline-none disabled:opacity-50 ${
                      d.activo ? 'bg-brand-600' : 'bg-slate-200'
                    }`}
                    aria-label={d.activo ? 'Desactivar' : 'Activar'}
                  >
                    <span
                      className={`absolute top-0.5 h-5 w-5 rounded-full bg-white shadow transition-transform ${
                        d.activo ? 'translate-x-5' : 'translate-x-0.5'
                      }`}
                    />
                  </button>
                </div>

                {d.activo && (
                  <>
                    <div className="grid grid-cols-2 gap-3">
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-slate-500">Hora inicio</label>
                        <input
                          type="time"
                          className="w-full rounded-xl border border-slate-200 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                          value={d.horaInicio}
                          onChange={(e) => setDia(num, { horaInicio: e.target.value })}
                        />
                      </div>
                      <div className="space-y-1">
                        <label className="text-xs font-medium text-slate-500">Hora fin</label>
                        <input
                          type="time"
                          className="w-full rounded-xl border border-slate-200 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                          value={d.horaFin}
                          onChange={(e) => setDia(num, { horaFin: e.target.value })}
                        />
                      </div>
                    </div>

                    <label className="flex items-center gap-2 text-sm text-slate-600 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={d.hasPausa}
                        onChange={(e) => setDia(num, { hasPausa: e.target.checked })}
                        className="h-4 w-4 rounded border-slate-300 text-brand-600"
                      />
                      Pausa / almuerzo
                    </label>

                    {d.hasPausa && (
                      <div className="grid grid-cols-2 gap-3">
                        <div className="space-y-1">
                          <label className="text-xs font-medium text-slate-500">Pausa inicio</label>
                          <input
                            type="time"
                            className="w-full rounded-xl border border-slate-200 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                            value={d.pausaInicio}
                            onChange={(e) => setDia(num, { pausaInicio: e.target.value })}
                          />
                        </div>
                        <div className="space-y-1">
                          <label className="text-xs font-medium text-slate-500">Pausa fin</label>
                          <input
                            type="time"
                            className="w-full rounded-xl border border-slate-200 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                            value={d.pausaFin}
                            onChange={(e) => setDia(num, { pausaFin: e.target.value })}
                          />
                        </div>
                      </div>
                    )}

                    <button
                      type="button"
                      disabled={d.saving}
                      onClick={() => handleSaveDia(num)}
                      className="w-full rounded-xl bg-brand-600 py-1.5 text-sm font-medium text-white hover:bg-brand-700 disabled:opacity-50"
                    >
                      {d.saving ? 'Guardando…' : 'Guardar'}
                    </button>
                  </>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Servicios tab */}
      {tab === 'servicios' && (
        <div className="space-y-4">
          <form onSubmit={handleCrearServicio} className="rounded-2xl border border-slate-200 bg-white p-4 space-y-3">
            <h2 className="text-sm font-semibold text-slate-700">Nuevo servicio</h2>
            <div className="flex gap-2">
              <input
                placeholder="Ej: Kapping gel"
                value={nuevoNombre}
                onChange={(e) => setNuevoNombre(e.target.value)}
                required
                className="flex-1 rounded-xl border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
              <div className="relative">
                <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-sm text-slate-400">$</span>
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  placeholder="0"
                  value={nuevoPrecio}
                  onChange={(e) => setNuevoPrecio(e.target.value)}
                  className="w-28 rounded-xl border border-slate-200 pl-6 pr-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
              </div>
              <button
                type="submit"
                disabled={savingServicio}
                className="rounded-xl bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700 disabled:opacity-50"
              >
                {savingServicio ? '…' : 'Agregar'}
              </button>
            </div>
          </form>

          <div className="space-y-2">
            {servicios.length === 0 && (
              <p className="text-center text-sm text-slate-400">Sin servicios cargados</p>
            )}
            {servicios.map((s) => (
              <div key={s.id} className="rounded-2xl border border-slate-200 bg-white p-4">
                {editingId === s.id ? (
                  <div className="flex gap-2">
                    <input
                      className="flex-1 rounded-xl border border-slate-200 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                      value={s.nombre}
                      onChange={(e) => setServicios((prev) => prev.map((x) => x.id === s.id ? { ...x, nombre: e.target.value } : x))}
                    />
                    <div className="relative">
                      <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-sm text-slate-400">$</span>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        className="w-24 rounded-xl border border-slate-200 pl-6 pr-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                        value={s.precio}
                        onChange={(e) => setServicios((prev) => prev.map((x) => x.id === s.id ? { ...x, precio: parseFloat(e.target.value) || 0 } : x))}
                      />
                    </div>
                    <button
                      type="button"
                      onClick={() => handleGuardarServicio(s)}
                      className="rounded-xl bg-brand-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-brand-700"
                    >
                      Guardar
                    </button>
                    <button
                      type="button"
                      onClick={() => setEditingId(null)}
                      className="rounded-xl bg-slate-100 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-200"
                    >
                      Cancelar
                    </button>
                  </div>
                ) : (
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm font-medium text-slate-800">{s.nombre}</p>
                      <p className="text-xs text-slate-500">${s.precio.toLocaleString('es-AR', { minimumFractionDigits: 0 })}</p>
                    </div>
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => setEditingId(s.id)}
                        className="rounded-xl bg-slate-100 px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-200"
                      >
                        Editar
                      </button>
                      <button
                        type="button"
                        onClick={() => handleEliminarServicio(s.id)}
                        className="rounded-xl bg-red-50 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100"
                      >
                        Eliminar
                      </button>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Días bloqueados tab */}
      {tab === 'dias' && (
        <div className="space-y-4">
          <form onSubmit={handleBloquear} className="rounded-2xl border border-slate-200 bg-white p-4 space-y-3">
            <h2 className="text-sm font-semibold text-slate-700">Bloquear día</h2>
            <div className="space-y-1">
              <label className="text-xs font-medium text-slate-500">Fecha</label>
              <input
                type="date"
                min={hoy}
                required
                value={bloqueoFecha}
                onChange={(e) => setBloqueoFecha(e.target.value)}
                className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
            <div className="space-y-1">
              <label className="text-xs font-medium text-slate-500">Motivo (opcional)</label>
              <input
                value={bloqueoMotivo}
                onChange={(e) => setBloqueoMotivo(e.target.value)}
                placeholder="Ej: vacaciones, feriado…"
                className="w-full rounded-xl border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>
            <button
              type="submit"
              disabled={savingBloqueo}
              className="w-full rounded-xl bg-brand-600 py-2 text-sm font-medium text-white hover:bg-brand-700 disabled:opacity-50"
            >
              {savingBloqueo ? 'Bloqueando…' : 'Bloquear día'}
            </button>
          </form>

          <div className="space-y-2">
            {bloqueados.length === 0 && (
              <p className="text-center text-sm text-slate-400">Sin días bloqueados</p>
            )}
            {bloqueados.map((b) => (
              <div key={b.id} className="flex items-center justify-between rounded-2xl border border-slate-200 bg-white px-4 py-3">
                <div>
                  <p className="text-sm font-medium text-slate-800">
                    {new Date(b.fecha + 'T00:00:00').toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' })}
                  </p>
                  {b.motivo && <p className="text-xs text-slate-500">{b.motivo}</p>}
                </div>
                <button
                  type="button"
                  onClick={() => handleDesbloquear(b.id)}
                  className="rounded-xl bg-red-50 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100"
                >
                  Desbloquear
                </button>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
