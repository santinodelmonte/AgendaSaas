import { useEffect, useState } from 'react';
import { getAgendaDiaria } from '../../api/admin.service';
import type { AgendaItem } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { EmptyState } from '../../components/common/EmptyState';
import { CalendarDay } from '../../components/calendar/CalendarDay';
import { toDateInputValue } from '../../utils/date';

export function AdminAgendaDiariaPage() {
  const [items, setItems] = useState<AgendaItem[]>([]);
  const [fecha, setFecha] = useState(toDateInputValue(new Date()));
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    getAgendaDiaria(fecha)
      .then(setItems)
      .finally(() => setLoading(false));
  }, [fecha]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Agenda diaria</p>
          <h2 className="mt-2 text-2xl font-semibold text-slate-900">Turnos del día</h2>
        </div>
        <input type="date" value={fecha} onChange={(e) => setFecha(e.target.value)} className="h-11 rounded-2xl border border-slate-300 bg-white px-4 text-sm" />
      </div>

      {loading ? <Loading label="Cargando agenda..." /> : items.length ? <CalendarDay items={items} /> : <EmptyState title="Sin turnos para esta fecha" />}
    </div>
  );
}
