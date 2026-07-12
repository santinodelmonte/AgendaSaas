import { useEffect, useMemo, useRef, useState } from 'react';
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react';
import { addDays, parseISO } from 'date-fns';
import {
  getAdminPerfil,
  getAdminServicios,
  getAgendaDiaria,
  getAgendaSlots,
} from '../../api/admin.service';
import type { AgendaSlotsResponse, PerfilAdmin, Servicio, TurnoAgenda } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { AdminScheduleTimeline } from '../../components/schedule/AdminScheduleTimeline';
import { ModalCrearTurno } from '../../components/schedule/ModalCrearTurno';
import { ModalDetalleTurno } from '../../components/schedule/ModalDetalleTurno';
import { computarLayoutDia } from '../../components/schedule/timeline';
import { useIsMobile } from '../../hooks/useIsMobile';
import { useToast } from '../../contexts/ToastContext';
import { formatDayLabel, toDateInputValue } from '../../utils/date';

export function AdminAgendaDiariaPage() {
  const { pushToast } = useToast();
  const isMobile = useIsMobile();

  const [fecha, setFecha] = useState(toDateInputValue(new Date()));
  const [turnos, setTurnos] = useState<TurnoAgenda[]>([]);
  const [slotsResp, setSlotsResp] = useState<AgendaSlotsResponse | null>(null);
  const [perfil, setPerfil] = useState<PerfilAdmin | null>(null);
  const [servicios, setServicios] = useState<Servicio[]>([]);
  const [loading, setLoading] = useState(true);

  // undefined = cerrado; null = abierto sin hora (FAB); number = hora precargada por tap
  const [crearHora, setCrearHora] = useState<number | null | undefined>(undefined);
  const [detalle, setDetalle] = useState<TurnoAgenda | null>(null);

  const touchRef = useRef<{ x: number; y: number } | null>(null);

  useEffect(() => {
    getAdminPerfil().then(setPerfil).catch(() => undefined);
    getAdminServicios().then(setServicios).catch(() => setServicios([]));
  }, []);

  useEffect(() => {
    setLoading(true);
    Promise.all([getAgendaDiaria(fecha), getAgendaSlots(fecha)])
      .then(([agenda, slots]) => {
        setTurnos(agenda);
        setSlotsResp(slots);
      })
      .finally(() => setLoading(false));
  }, [fecha]);

  function refresh() {
    getAgendaDiaria(fecha).then(setTurnos);
    getAgendaSlots(fecha).then(setSlotsResp);
  }

  function navegarDia(dias: number) {
    setFecha((prev) => toDateInputValue(addDays(parseISO(prev), dias)));
  }

  const paso = perfil?.duracionTurnoMinutos ?? 30;
  const esHoy = fecha === toDateInputValue(new Date());

  const { ventana, pausas, huecos, ocupados, sinHorario } = useMemo(
    () => computarLayoutDia(turnos, slotsResp?.slots ?? [], paso),
    [turnos, slotsResp, paso],
  );

  function cerrarYRefrescar(mensaje: string) {
    setCrearHora(undefined);
    setDetalle(null);
    refresh();
    pushToast({ type: 'success', message: mensaje });
  }

  return (
    <div>
      {/* Header sticky: navegación por flechas, swipe y date picker */}
      <div
        className="sticky top-[77px] z-10 -mx-4 border-b border-slate-200/70 bg-slate-50/95 px-4 py-3 backdrop-blur sm:-mx-6 sm:px-6"
        onTouchStart={(e) => {
          touchRef.current = { x: e.touches[0].clientX, y: e.touches[0].clientY };
        }}
        onTouchEnd={(e) => {
          const t = touchRef.current;
          touchRef.current = null;
          if (!t) return;
          const dx = e.changedTouches[0].clientX - t.x;
          const dy = e.changedTouches[0].clientY - t.y;
          if (Math.abs(dx) > 60 && Math.abs(dy) < 50) navegarDia(dx < 0 ? 1 : -1);
        }}
      >
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => navegarDia(-1)}
            aria-label="Día anterior"
            className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl border border-slate-200 bg-white transition hover:bg-slate-50"
          >
            <ChevronLeft size={16} />
          </button>

          <div className="min-w-0 flex-1 text-center">
            <p className="truncate text-sm font-semibold capitalize text-slate-900">{formatDayLabel(fecha)}</p>
            {esHoy && <p className="text-[11px] font-medium uppercase tracking-widest text-brand-600">Hoy</p>}
          </div>

          <input
            type="date"
            value={fecha}
            onChange={(e) => e.target.value && setFecha(e.target.value)}
            aria-label="Elegir fecha"
            className="h-11 w-32 shrink-0 rounded-xl border border-slate-200 bg-white px-2 text-xs sm:w-40 sm:px-3 sm:text-sm"
          />

          <button
            type="button"
            onClick={() => navegarDia(1)}
            aria-label="Día siguiente"
            className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl border border-slate-200 bg-white transition hover:bg-slate-50"
          >
            <ChevronRight size={16} />
          </button>
        </div>
        {isMobile && (
          <p className="mt-1.5 text-center text-[11px] text-slate-400">
            Deslizá hacia los costados para cambiar de día · Tocá un hueco para crear un turno
          </p>
        )}
      </div>

      <div className="pt-4">
        {loading ? (
          <Loading label="Cargando agenda..." />
        ) : slotsResp?.bloqueado ? (
          <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
            Día bloqueado{slotsResp.motivo ? `: ${slotsResp.motivo}` : '.'}
          </div>
        ) : (
          <>
            {sinHorario && (
              <div className="mb-4 rounded-2xl border border-slate-200 bg-white p-4 text-sm text-slate-500">
                Sin horario configurado para este día. Igual podés crear turnos manuales tocando un hueco.
              </div>
            )}
            <AdminScheduleTimeline
              fecha={fecha}
              esHoy={esHoy}
              ventana={ventana}
              pausas={pausas}
              huecos={huecos}
              turnos={ocupados}
              duracionDefault={paso}
              onTapHueco={(min) => setCrearHora(min)}
              onTapTurno={setDetalle}
            />
          </>
        )}
      </div>

      {/* FAB: crear turno sin tocar un hueco exacto */}
      <button
        type="button"
        onClick={() => setCrearHora(null)}
        aria-label="Crear turno"
        className="fixed bottom-6 right-5 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-slate-900 text-white shadow-lg transition hover:bg-slate-800 active:scale-95 sm:bottom-8 sm:right-8"
      >
        <Plus size={24} />
      </button>

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
          onCreated={() => cerrarYRefrescar('Turno creado.')}
        />
      )}

      {detalle && (
        <ModalDetalleTurno
          turno={detalle}
          fecha={fecha}
          otrosTurnos={ocupados.filter((t) => t.id !== detalle.id)}
          duracionDefault={paso}
          onClose={() => setDetalle(null)}
          onChanged={() => cerrarYRefrescar('Cambios guardados.')}
        />
      )}
    </div>
  );
}
