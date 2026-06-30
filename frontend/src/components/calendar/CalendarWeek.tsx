import { formatDateTime } from '../../utils/date';
import type { AgendaItem } from '../../types/admin';

type Props = {
  items: AgendaItem[];
};

export function CalendarWeek({ items }: Props) {
  const grouped = items.reduce<Record<string, AgendaItem[]>>((acc, item) => {
    const day = item.fechaHora.slice(0, 10);
    acc[day] = acc[day] ?? [];
    acc[day].push(item);
    return acc;
  }, {});

  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      {Object.entries(grouped).map(([day, list]) => (
        <section key={day} className="rounded-3xl border border-slate-200 bg-white p-4 shadow-soft">
          <h3 className="mb-4 text-sm font-semibold uppercase tracking-[0.2em] text-slate-500">{day}</h3>
          <div className="space-y-3">
            {list.map((item) => (
              <div key={item.id} className="rounded-2xl bg-slate-50 p-4">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="font-semibold text-slate-900">{item.nombreCliente}</p>
                    <p className="text-sm text-slate-500">{item.servicio}</p>
                  </div>
                  <span className="text-sm font-medium text-slate-700">{formatDateTime(item.fechaHora)}</span>
                </div>
              </div>
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}
