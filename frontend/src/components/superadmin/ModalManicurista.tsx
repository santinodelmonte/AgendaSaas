import { useState } from 'react';
import { actualizarManicurista, crearManicurista } from '../../api/superadmin.service';
import type { ManicuristaAdmin } from '../../types/superadmin';
import { mensajeApi } from '../schedule/timeline';
import { ModalBase } from '../schedule/ModalBase';
import { PasswordInput } from './PasswordInput';

type Props = {
  modo: 'crear' | 'editar';
  manicurista?: ManicuristaAdmin;
  onClose: () => void;
  onSaved: (mensaje: string) => void;
};

const DURACIONES = [15, 20, 30, 45, 60, 90];
const inputCls =
  'w-full rounded-xl border border-slate-300 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400';

export function ModalManicurista({ modo, manicurista, onClose, onSaved }: Props) {
  const [nombre, setNombre] = useState(manicurista?.nombre ?? '');
  const [email, setEmail] = useState(manicurista?.email ?? '');
  const [whatsApp, setWhatsApp] = useState(manicurista?.whatsApp ?? '');
  const [slug, setSlug] = useState(manicurista?.slug ?? '');
  const [duracion, setDuracion] = useState(manicurista?.duracionTurnoMinutos ?? 30);
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const esCrear = modo === 'crear';

  async function guardar() {
    if (!nombre.trim()) return setError('El nombre es obligatorio.');
    if (!whatsApp.trim()) return setError('El WhatsApp es obligatorio.');
    if (esCrear) {
      if (!email.trim()) return setError('El email es obligatorio.');
      if (password.length < 6) return setError('La contraseña debe tener al menos 6 caracteres.');
    }

    setSaving(true);
    setError(null);
    try {
      if (esCrear) {
        await crearManicurista({
          nombre: nombre.trim(),
          email: email.trim(),
          whatsApp: whatsApp.trim(),
          duracionTurnoMinutos: duracion,
          slug: slug.trim() || undefined,
          password,
        });
        onSaved('Manicurista creada.');
      } else {
        await actualizarManicurista(manicurista!.tenantId, {
          nombre: nombre.trim(),
          whatsApp: whatsApp.trim(),
          duracionTurnoMinutos: duracion,
          slug: slug.trim() || undefined,
        });
        onSaved('Manicurista actualizada.');
      }
    } catch (err) {
      setError(mensajeApi(err, 'No se pudo guardar.'));
      setSaving(false);
    }
  }

  return (
    <ModalBase title={esCrear ? 'Nueva manicurista' : 'Editar manicurista'} onClose={onClose}>
      <div className="space-y-4">
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-slate-700">Nombre</span>
          <input value={nombre} onChange={(e) => setNombre(e.target.value)} className={inputCls} placeholder="Nombre y apellido" />
        </label>

        <label className="block">
          <span className="mb-1 block text-sm font-medium text-slate-700">
            Email {esCrear ? '(login y contacto)' : '(login)'}
          </span>
          <input
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            disabled={!esCrear}
            className={`${inputCls} ${!esCrear ? 'bg-slate-100 text-slate-500' : ''}`}
            placeholder="email@ejemplo.com"
            type="email"
          />
          {!esCrear && <span className="mt-1 block text-xs text-slate-400">El email de login no se puede cambiar acá.</span>}
        </label>

        <label className="block">
          <span className="mb-1 block text-sm font-medium text-slate-700">WhatsApp</span>
          <input value={whatsApp} onChange={(e) => setWhatsApp(e.target.value)} className={inputCls} placeholder="+54 9 11 ..." />
        </label>

        <div className="grid grid-cols-2 gap-3">
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Duración turno</span>
            <select value={duracion} onChange={(e) => setDuracion(Number(e.target.value))} className={inputCls}>
              {DURACIONES.map((d) => (
                <option key={d} value={d}>{d} min</option>
              ))}
            </select>
          </label>
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Slug (opcional)</span>
            <input value={slug} onChange={(e) => setSlug(e.target.value)} className={inputCls} placeholder="auto" />
          </label>
        </div>

        {esCrear && (
          <div>
            <span className="mb-1 block text-sm font-medium text-slate-700">Contraseña inicial</span>
            <PasswordInput value={password} onChange={setPassword} />
            <span className="mt-1 block text-xs text-slate-400">Generala y pasásela a la manicurista.</span>
          </div>
        )}

        {error && (
          <p className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>
        )}

        <button
          type="button"
          onClick={guardar}
          disabled={saving}
          className="h-12 w-full rounded-2xl bg-slate-900 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:opacity-50"
        >
          {saving ? 'Guardando...' : esCrear ? 'Crear manicurista' : 'Guardar cambios'}
        </button>
      </div>
    </ModalBase>
  );
}
