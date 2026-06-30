import { useEffect, useState } from 'react';
import { getAdminPerfil, updateAdminPerfil } from '../../api/admin.service';
import type { PerfilAdmin } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { useToast } from '../../contexts/ToastContext';

export function AdminPerfilPage() {
  const toast = useToast();
  const [data, setData] = useState<PerfilAdmin | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    getAdminPerfil()
      .then(setData)
      .finally(() => setLoading(false));
  }, []);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!data) return;
    try {
      setSaving(true);
      const updated = await updateAdminPerfil(data);
      setData(updated);
      toast.pushToast({ type: 'success', message: 'Perfil actualizado correctamente.' });
    } finally {
      setSaving(false);
    }
  };

  if (loading || !data) return <Loading label="Cargando perfil..." />;

  return (
    <form onSubmit={onSubmit} className="mx-auto max-w-3xl rounded-3xl border border-slate-200 bg-white p-6 shadow-soft sm:p-8">
      <div>
        <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Perfil</p>
        <h2 className="mt-2 text-2xl font-semibold text-slate-900">Editar información</h2>
      </div>

      <div className="mt-6 grid gap-5 md:grid-cols-2">
        <Field label="Nombre">
          <input value={data.nombre} onChange={(e) => setData({ ...data, nombre: e.target.value })} className={inputClass} />
        </Field>
        <Field label="Email">
          <input type="email" value={data.email} onChange={(e) => setData({ ...data, email: e.target.value })} className={inputClass} />
        </Field>
        <Field label="WhatsApp">
          <input value={data.whatsApp} onChange={(e) => setData({ ...data, whatsApp: e.target.value })} className={inputClass} />
        </Field>
        <Field label="Duración de turno (min)">
          <input
            type="number"
            min={5}
            step={5}
            value={data.duracionTurnoMinutos}
            onChange={(e) => setData({ ...data, duracionTurnoMinutos: Number(e.target.value) })}
            className={inputClass}
          />
        </Field>
      </div>

      <button
        type="submit"
        disabled={saving}
        className="mt-8 inline-flex h-12 items-center justify-center rounded-2xl bg-brand-600 px-5 text-sm font-semibold text-white transition hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-70"
      >
        {saving ? 'Guardando...' : 'Guardar cambios'}
      </button>
    </form>
  );
}

const inputClass =
  'h-12 w-full rounded-2xl border border-slate-300 bg-white px-4 outline-none transition focus:border-brand-500 focus:ring-4 focus:ring-brand-100';

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-medium text-slate-700">{label}</span>
      {children}
    </label>
  );
}
