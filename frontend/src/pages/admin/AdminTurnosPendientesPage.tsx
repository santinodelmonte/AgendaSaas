import { useEffect, useState } from 'react';
import { Check, X } from 'lucide-react';
import { confirmarTurno, getTurnosPendientes, rechazarTurno } from '../../api/admin.service';
import type { TurnoPendiente } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { EmptyState } from '../../components/common/EmptyState';
import { formatDateTime } from '../../utils/date';
import { useToast } from '../../contexts/ToastContext';

export function AdminTurnosPendientesPage() {
  const toast = useToast();
  const [items, setItems] = useState<TurnoPendiente[]>([]);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    try {
      setItems(await getTurnosPendientes());
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const onConfirm = async (id: string) => {
    await confirmarTurno(id);
    toast.pushToast({ type: 'success', message: 'Turno confirmado.' });
    await load();
  };

  const onReject = async (id: string) => {
    await rechazarTurno(id);
    toast.pushToast({ type: 'success', message: 'Turno rechazado.' });
    await load();
  };

  if (loading) return <Loading label="Cargando turnos pendientes..." />;

  return (
    <div className="space-y-6">
      <div>
        <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Pendientes</p>
        <h2 className="mt-2 text-2xl font-semibold text-slate-900">Turnos por revisar</h2>
      </div>

      {items.length ? (
        <div className="grid gap-4">
          {items.map((item) => (
            <article key={item.id} className="rounded-3xl border border-slate-200 bg-white p-5 shadow-soft">
              <div className="grid gap-4 md:grid-cols-[1fr_auto] md:items-center">
                <div>
                  <p className="text-lg font-semibold text-slate-900">{item.nombreCliente}</p>
                  <p className="text-sm text-slate-500">{item.telefonoCliente}</p>
                  <p className="mt-2 text-sm text-slate-700">{item.servicio}</p>
                  <p className="mt-1 text-sm font-medium text-slate-900">{formatDateTime(item.fechaHora)}</p>
                </div>

                <div className="flex gap-2">
                  <button onClick={() => onConfirm(item.id)} className="inline-flex h-11 items-center gap-2 rounded-2xl bg-emerald-500 px-4 text-sm font-semibold text-white">
                    <Check size={16} />
                    Confirmar
                  </button>
                  <button onClick={() => onReject(item.id)} className="inline-flex h-11 items-center gap-2 rounded-2xl bg-rose-500 px-4 text-sm font-semibold text-white">
                    <X size={16} />
                    Rechazar
                  </button>
                </div>
              </div>
            </article>
          ))}
        </div>
      ) : (
        <EmptyState title="No hay turnos pendientes" description="Cuando lleguen solicitudes aparecerán aquí." />
      )}
    </div>
  );
}
