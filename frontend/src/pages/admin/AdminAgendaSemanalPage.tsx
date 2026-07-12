import { useEffect, useState } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { addDays, parseISO, startOfWeek } from 'date-fns';
import { getAgendaSemanal, confirmarTurno, rechazarTurno, cancelarTurno } from '../../api/admin.service';
import type { AgendaItem, SemanaDia } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { formatDayLabel, formatTime, toDateInputValue } from '../../utils/date';
import { useToast } from '../../contexts/ToastContext';

const ESTADO_CFG = {
  Pendiente:  { bg: 'bg-amber-50',  border: 'border-amber-300',  dot: 'bg-amber-500'  },
  Confirmado: { bg: 'bg-green-50',  border: 'border-green-300',  dot: 'bg-green-500'  },
  Rechazado:  { bg: 'bg-slate-100', border: 'border-slate-200',  dot: 'bg-slate-400'  },
} as const;

function cfg(estado: string) {
  return ESTADO_CFG[estado as keyof typeof ESTADO_CFG] ?? ESTADO_CFG.Rechazado;
}

function inicioSemana(fecha: Date) {
  return startOfWeek(fecha, { weekStartsOn: 1 });
}

export function AdminAgendaSemanalPage() {
  const { pushToast } = useToast();
  const [semana, setSemana] = useState<SemanaDia[]>([]);
  const [lunes, setLunes] = useState(() => toDateInputValue(inicioSemana(new Date())));
  const [loading, setLoading] = useState(true);
  const [actionId, setActionId] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    getAgendaSemanal(lunes)
      .then(setSemana)
      .finally(() => setLoading(false));
  }, [lunes]);

  function navegar(semanas: number) {
    setLunes((prev) => toDateInputValue(addDays(parseISO(prev), semanas * 7)));
  }

  function refresh() {
    getAgendaSemanal(lunes).then(setSemana);
  }

  async function accion(fn: () => Promise<unknown>, id: string) {
    setActionId(id);
    try {
      await fn();
      refresh();
    } catch {
      pushToast({ type: 'error', message: 'No se pudo realizar la acción.' });
    } finally {
      setActionId(null);
    }
  }

  const lunesLabel = formatDayLabel(lunes);
  const domingoLabel = formatDayLabel(toDateInputValue(addDays(parseISO(lunes), 6)));

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Agenda semanal</p>
          <h2 className="mt-2 text-2xl font-semibold text-slate-900">Vista calendario</h2>
        </div>
        <div className="flex items-center gap-2">
          <button onClick={() => navegar(-1)} className="flex h-9 w-9 items-center justify-center rounded-xl border border-slate-200 bg-white hover:bg-slate-50">
            <ChevronLeft size={16} />
          </button>
          <span className="min-w-[240px] text-center text-sm font-medium capitalize text-slate-700">
            {lunesLabel} — {domingoLabel}
          </span>
          <button onClick={() => navegar(1)} className="flex h-9 w-9 items-center justify-center rounded-xl border border-slate-200 bg-white hover:bg-slate-50">
            <ChevronRight size={16} />
          </button>
        </div>
      </div>

      {loading ? (
        <Loading label="Cargando agenda semanal..." />
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4">
          {semana.map((dia) => (
            <section key={dia.fecha} className="rounded-3xl border border-slate-200 bg-white p-4 shadow-soft">
              <h3 className="mb-3 text-sm font-semibold capitalize text-slate-700">
                {formatDayLabel(dia.fecha)}
              </h3>

              {dia.turnos.length === 0 ? (
                <p className="text-xs text-slate-400">Sin turnos</p>
              ) : (
                <div className="space-y-2">
                  {dia.turnos.map((turno) => (
                    <TurnoCard
                      key={turno.id}
                      turno={turno}
                      loading={actionId === turno.id}
                      onConfirmar={() => accion(() => confirmarTurno(turno.id), turno.id)}
                      onRechazar={() => accion(() => rechazarTurno(turno.id), turno.id)}
                      onCancelar={() => accion(() => cancelarTurno(turno.id), turno.id)}
                    />
                  ))}
                </div>
              )}
            </section>
          ))}
        </div>
      )}
    </div>
  );
}

function TurnoCard({
  turno,
  loading,
  onConfirmar,
  onRechazar,
  onCancelar,
}: {
  turno: AgendaItem;
  loading: boolean;
  onConfirmar: () => void;
  onRechazar: () => void;
  onCancelar: () => void;
}) {
  const c = cfg(turno.estado);

  return (
    <div className={`rounded-2xl border-2 ${c.bg} ${c.border} p-3`}>
      <div className="mb-2 flex items-center gap-1.5">
        <span className={`h-2 w-2 rounded-full ${c.dot}`} />
        <span className="text-xs font-bold text-slate-900">{formatTime(turno.fechaHora)}</span>
        <span className="ml-auto text-xs text-slate-500">{turno.estado}</span>
      </div>
      <p className="text-sm font-semibold text-slate-900">{turno.nombreCliente ?? '-'}</p>
      {turno.servicio && <p className="text-xs text-slate-500">{turno.servicio}</p>}

      {!loading && turno.estado === 'Pendiente' && (
        <div className="mt-2 flex gap-1.5">
          <button onClick={onConfirmar} className="flex-1 rounded-xl bg-green-600 py-1 text-xs font-semibold text-white hover:bg-green-700">
            Confirmar
          </button>
          <button onClick={onRechazar} className="flex-1 rounded-xl bg-red-500 py-1 text-xs font-semibold text-white hover:bg-red-600">
            Rechazar
          </button>
        </div>
      )}
      {!loading && turno.estado === 'Confirmado' && new Date(turno.fechaHora) > new Date() && (
        <button onClick={onCancelar} className="mt-2 w-full rounded-xl bg-slate-200 py-1 text-xs font-medium text-slate-700 hover:bg-slate-300">
          Cancelar
        </button>
      )}
      {loading && <p className="mt-2 text-center text-xs text-slate-400">...</p>}
    </div>
  );
}
