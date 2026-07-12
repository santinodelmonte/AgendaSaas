import { useState } from 'react';
import { resetPasswordManicurista } from '../../api/superadmin.service';
import type { ManicuristaAdmin } from '../../types/superadmin';
import { mensajeApi } from '../schedule/timeline';
import { ModalBase } from '../schedule/ModalBase';
import { PasswordInput } from './PasswordInput';

type Props = {
  manicurista: ManicuristaAdmin;
  onClose: () => void;
  onSaved: (mensaje: string) => void;
};

export function ModalResetPassword({ manicurista, onClose, onSaved }: Props) {
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function guardar() {
    if (password.length < 6) return setError('La contraseña debe tener al menos 6 caracteres.');

    setSaving(true);
    setError(null);
    try {
      await resetPasswordManicurista(manicurista.tenantId, password);
      onSaved('Contraseña actualizada.');
    } catch (err) {
      setError(mensajeApi(err, 'No se pudo actualizar la contraseña.'));
      setSaving(false);
    }
  }

  return (
    <ModalBase title="Restablecer contraseña" subtitle={manicurista.nombre} onClose={onClose}>
      <div className="space-y-4">
        <p className="text-sm text-slate-600">
          Generá una contraseña nueva, copiala y pasásela a la manicurista. La anterior deja de funcionar.
        </p>
        <PasswordInput value={password} onChange={setPassword} />

        {error && (
          <p className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>
        )}

        <button
          type="button"
          onClick={guardar}
          disabled={saving}
          className="h-12 w-full rounded-2xl bg-slate-900 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:opacity-50"
        >
          {saving ? 'Guardando...' : 'Guardar contraseña'}
        </button>
      </div>
    </ModalBase>
  );
}
