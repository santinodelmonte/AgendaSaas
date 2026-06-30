import { formatDateTime } from '../../utils/date';
import type { AgendaItem } from '../../types/admin';

type Props = {
  items: AgendaItem[];
};

export function CalendarDay({ items }: Props) {
  return (
    <div className="space-y-3">
      {items.map((item) => (
        <div key={item.id} className="rounded-2xl border border-slate-200 bg-white p-4 shadow-soft">
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="font-semibold text-slate-900">{item.nombreCliente}</p>
              <p className="text-sm text-slate-500">{item.servicio}</p>
            </div>
            <div className="text-right">
              <p className="text-sm font-medium text-slate-900">{formatDateTime(item.fechaHora)}</p>
              <span className="mt-1 inline-flex rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600">
                {item.estado}
              </span>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
