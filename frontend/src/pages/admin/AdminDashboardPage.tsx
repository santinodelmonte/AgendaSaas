import { useEffect, useState } from 'react';
import { CalendarClock, CalendarCheck2, CalendarDays, Sparkles } from 'lucide-react';
import { getAdminDashboard } from '../../api/admin.service';
import type { DashboardStats } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { CardDashboard } from '../../components/dashboard/CardDashboard';
import { formatDateTime } from '../../utils/date';

export function AdminDashboardPage() {
  const [data, setData] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getAdminDashboard()
      .then(setData)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <Loading label="Cargando dashboard..." />;

  return (
    <div className="space-y-6">
      <div>
        <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Dashboard</p>
        <h2 className="mt-2 text-2xl font-semibold text-slate-900">Resumen general</h2>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <CardDashboard title="Turnos pendientes" value={data?.turnosPendientes ?? 0} icon={<CalendarClock size={20} />} />
        <CardDashboard title="Confirmados hoy" value={data?.confirmadosHoy ?? 0} icon={<CalendarCheck2 size={20} />} />
        <CardDashboard title="Confirmados mes" value={data?.confirmadosMes ?? 0} icon={<CalendarDays size={20} />} />
        <CardDashboard
          title="Próximo turno"
          value={data?.proximoTurno ? formatDateTime(data.proximoTurno.fechaHora) : 'Sin turnos'}
          description={data?.proximoTurno?.nombreCliente ?? 'Aún no hay próximos turnos'}
          icon={<Sparkles size={20} />}
        />
      </div>
    </div>
  );
}
