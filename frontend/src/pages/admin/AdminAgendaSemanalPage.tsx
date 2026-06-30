import { useEffect, useState } from 'react';
import { getAgendaSemanal } from '../../api/admin.service';
import type { AgendaItem } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { EmptyState } from '../../components/common/EmptyState';
import { CalendarWeek } from '../../components/calendar/CalendarWeek';
import { toDateInputValue } from '../../utils/date';

export function AdminAgendaSemanalPage() {
  const [items, setItems] = useState<AgendaItem[]>([]);
  const [fechaInicio, setFechaInicio] = useState(toDateInputValue(new Date()));
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    getAgendaSemanal(fechaInicio)
      .then(setItems)
      .finally(() => setLoading(false));
  }, [fechaInicio]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Agenda semanal</p>
          <h2 className="mt-2 text-2xl font-semibold text-slate-900">Vista calendario</h2>
        </div>
        <input type="date" value={fechaInicio} onChange={(e) => setFechaInicio(e.target.value)} className="h-11 rounded-2xl border border-slate-300 bg-white px-4 text-sm" />
      </div>

      {loading ? <Loading label="Cargando agenda semanal..." /> : items.length ? <CalendarWeek items={items} /> : <EmptyState title="Sin turnos para la semana" />}
    </div>
  );
}
