import { useEffect, useMemo, useState } from 'react';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import { createAdminHorario, deleteAdminHorario, getAdminHorarios, updateAdminHorario } from '../../api/admin.service';
import type { Horario } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { EmptyState } from '../../components/common/EmptyState';
import { useToast } from '../../contexts/ToastContext';

const days = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];

export function AdminHorariosPage() {
  const toast = useToast();
  const [items, setItems] = useState<Horario[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<Horario | null>(null);
  const [creating, setCreating] = useState(false);

  const emptyModel = useMemo(
    () => ({ diaSemana: 'Lunes', horaInicio: '09:00', horaFin: '18:00', activo: true }),
    [],
  );

  useEffect(() => {
    load();
  }, []);

  const load = async () => {
    setLoading(true);
    try {
      const data = await getAdminHorarios();
      setItems(data);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setCreating(true);
      await createAdminHorario(emptyModel);
      toast.pushToast({ type: 'success', message: 'Horario creado.' });
      await load();
    } finally {
      setCreating(false);
    }
  };

  const handleUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editing) return;
    try {
      await updateAdminHorario(editing.id, editing);
      toast.pushToast({ type: 'success', message: 'Horario actualizado.' });
      setEditing(null);
      await load();
    } finally {
      setCreating(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('¿Eliminar este horario?')) return;
    await deleteAdminHorario(id);
    toast.pushToast({ type: 'success', message: 'Horario eliminado.' });
    await load();
  };

  if (loading) return <Loading label="Cargando horarios..." />;

  return (
    <div className="space-y-6">
      <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-soft">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Horarios</p>
            <h2 className="mt-2 text-2xl font-semibold text-slate-900">Agenda semanal</h2>
          </div>
        </div>

        <form onSubmit={handleCreate} className="mt-6 grid gap-3 md:grid-cols-5">
          <select className={inputClass}>
            {days.map((d) => <option key={d}>{d}</option>)}
          </select>
          <input type="time" defaultValue="09:00" className={inputClass} />
          <input type="time" defaultValue="18:00" className={inputClass} />
          <select className={inputClass} defaultValue="true">
            <option value="true">Activo</option>
            <option value="false">Inactivo</option>
          </select>
          <button type="submit" disabled={creating} className="inline-flex h-12 items-center justify-center rounded-2xl bg-brand-600 px-5 text-sm font-semibold text-white">
            <Plus size={16} className="mr-2" />
            Crear
          </button>
        </form>
      </div>

      {items.length ? (
        <div className="grid gap-4 lg:grid-cols-2">
          {items.map((item) => (
            <article key={item.id} className="rounded-3xl border border-slate-200 bg-white p-5 shadow-soft">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="font-semibold text-slate-900">{item.diaSemana}</p>
                  <p className="mt-1 text-sm text-slate-500">
                    {item.horaInicio} - {item.horaFin}
                  </p>
                </div>
                <span className={`rounded-full px-3 py-1 text-xs font-medium ${item.activo ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500'}`}>
                  {item.activo ? 'Activo' : 'Inactivo'}
                </span>
              </div>

              <div className="mt-5 flex gap-2">
                <button onClick={() => setEditing(item)} className="inline-flex h-11 items-center gap-2 rounded-2xl border border-slate-200 px-4 text-sm font-medium text-slate-700">
                  <Pencil size={16} />
                  Editar
                </button>
                <button onClick={() => handleDelete(item.id)} className="inline-flex h-11 items-center gap-2 rounded-2xl border border-rose-200 px-4 text-sm font-medium text-rose-600">
                  <Trash2 size={16} />
                  Eliminar
                </button>
              </div>
            </article>
          ))}
        </div>
      ) : (
        <EmptyState title="No hay horarios cargados" description="Crea el primer horario para comenzar." />
      )}

      {editing ? (
        <div className="fixed inset-0 z-50 bg-slate-950/40 p-4">
          <form onSubmit={handleUpdate} className="mx-auto mt-16 max-w-xl rounded-3xl bg-white p-6 shadow-2xl">
            <h3 className="text-xl font-semibold text-slate-900">Editar horario</h3>
            <div className="mt-5 grid gap-3 md:grid-cols-2">
              <select value={editing.diaSemana} onChange={(e) => setEditing({ ...editing, diaSemana: e.target.value })} className={inputClass}>
                {days.map((d) => <option key={d}>{d}</option>)}
              </select>
              <input type="time" value={editing.horaInicio} onChange={(e) => setEditing({ ...editing, horaInicio: e.target.value })} className={inputClass} />
              <input type="time" value={editing.horaFin} onChange={(e) => setEditing({ ...editing, horaFin: e.target.value })} className={inputClass} />
              <select value={String(editing.activo)} onChange={(e) => setEditing({ ...editing, activo: e.target.value === 'true' })} className={inputClass}>
                <option value="true">Activo</option>
                <option value="false">Inactivo</option>
              </select>
            </div>
            <div className="mt-6 flex justify-end gap-2">
              <button type="button" onClick={() => setEditing(null)} className="h-11 rounded-2xl border border-slate-200 px-4 text-sm font-medium">
                Cancelar
              </button>
              <button type="submit" className="h-11 rounded-2xl bg-brand-600 px-4 text-sm font-semibold text-white">
                Guardar
              </button>
            </div>
          </form>
        </div>
      ) : null}
    </div>
  );
}

const inputClass = 'h-12 w-full rounded-2xl border border-slate-300 bg-white px-4 outline-none focus:border-brand-500';
