import { useEffect, useState } from 'react';
import { Trash2 } from 'lucide-react';
import { getDiasBloqueados, bloquearDia, desbloquearDia } from '../../api/admin.service';
import type { DiaBloqueado } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { useToast } from '../../contexts/ToastContext';
import { toDateInputValue } from '../../utils/date';
import { formatDayLabel } from '../../utils/date';

export function AdminDiasBloqueadosPage() {
  const { pushToast } = useToast();
  const [dias, setDias] = useState<DiaBloqueado[]>([]);
  const [loading, setLoading] = useState(true);
  const [fecha, setFecha] = useState(toDateInputValue(new Date()));
  const [motivo, setMotivo] = useState('');
  const [saving, setSaving] = useState(false);

  const hoy = toDateInputValue(new Date());

  useEffect(() => {
    getDiasBloqueados()
      .then(setDias)
      .finally(() => setLoading(false));
  }, []);

  async function handleBloquear(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      const nuevo = await bloquearDia(fecha, motivo || undefined);
      setDias((prev) => [...prev, nuevo].sort((a, b) => a.fecha.localeCompare(b.fecha)));
      setMotivo('');
      pushToast({ type: 'success', message: 'Día bloqueado correctamente.' });
    } catch {
      pushToast({ type: 'error', message: 'No se pudo bloquear el día.' });
    } finally {
      setSaving(false);
    }
  }

  async function handleDesbloquear(id: string) {
    await desbloquearDia(id);
    setDias((prev) => prev.filter((d) => d.id !== id));
    pushToast({ type: 'success', message: 'Día desbloqueado.' });
  }

  return (
    <div className="space-y-6">
      <div>
        <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Configuración</p>
        <h2 className="mt-2 text-2xl font-semibold text-slate-900">Días bloqueados</h2>
        <p className="mt-1 text-sm text-slate-500">
          Marcá feriados o días de vacaciones — esos días no aparecerán como disponibles para las clientas.
        </p>
      </div>

      {/* Formulario */}
      <form
        onSubmit={handleBloquear}
        className="rounded-3xl border border-slate-200 bg-white p-6 shadow-soft"
      >
        <h3 className="mb-4 text-base font-semibold text-slate-900">Bloquear un día</h3>
        <div className="flex flex-col gap-3 sm:flex-row">
          <input
            type="date"
            value={fecha}
            min={hoy}
            onChange={(e) => setFecha(e.target.value)}
            className="h-11 rounded-2xl border border-slate-300 bg-white px-4 text-sm outline-none focus:border-brand-500"
          />
          <input
            placeholder="Motivo (opcional)"
            value={motivo}
            onChange={(e) => setMotivo(e.target.value)}
            className="h-11 flex-1 rounded-2xl border border-slate-300 px-4 text-sm outline-none focus:border-brand-500"
          />
          <button
            type="submit"
            disabled={saving}
            className="h-11 rounded-2xl bg-slate-900 px-6 text-sm font-semibold text-white disabled:opacity-50"
          >
            {saving ? 'Bloqueando...' : 'Bloquear'}
          </button>
        </div>
      </form>

      {/* Lista */}
      {loading ? (
        <Loading label="Cargando días bloqueados..." />
      ) : dias.length === 0 ? (
        <p className="text-sm text-slate-400">No hay días bloqueados.</p>
      ) : (
        <div className="space-y-2">
          {dias.map((dia) => (
            <div
              key={dia.id}
              className="flex items-center justify-between rounded-2xl border border-slate-200 bg-white px-5 py-4"
            >
              <div>
                <p className="font-medium capitalize text-slate-900">{formatDayLabel(dia.fecha)}</p>
                {dia.motivo && <p className="text-sm text-slate-500">{dia.motivo}</p>}
              </div>
              <button
                onClick={() => handleDesbloquear(dia.id)}
                className="flex h-9 w-9 items-center justify-center rounded-xl text-slate-400 hover:bg-red-50 hover:text-red-500"
                title="Desbloquear"
              >
                <Trash2 size={16} />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
