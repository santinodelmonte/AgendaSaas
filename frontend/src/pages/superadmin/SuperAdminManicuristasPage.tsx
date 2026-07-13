import { useEffect, useState } from 'react';
import {
  Plus, Pencil, KeyRound, Power, Trash2, ExternalLink, Copy, CalendarDays,
} from 'lucide-react';
import { getManicuristas, setManicuristaActivo, eliminarManicurista } from '../../api/superadmin.service';
import type { ManicuristaAdmin } from '../../types/superadmin';
import { Loading } from '../../components/common/Loading';
import { EmptyState } from '../../components/common/EmptyState';
import { ModalManicurista } from '../../components/superadmin/ModalManicurista';
import { ModalResetPassword } from '../../components/superadmin/ModalResetPassword';
import { ConfirmDialog } from '../../components/superadmin/ConfirmDialog';
import { useToast } from '../../contexts/ToastContext';
import { copyToClipboard } from '../../utils/clipboard';

type Dialogo =
  | { tipo: 'crear' }
  | { tipo: 'editar'; m: ManicuristaAdmin }
  | { tipo: 'password'; m: ManicuristaAdmin }
  | { tipo: 'activo'; m: ManicuristaAdmin }
  | { tipo: 'eliminar'; m: ManicuristaAdmin }
  | null;

export function SuperAdminManicuristasPage() {
  const { pushToast } = useToast();
  const [manicuristas, setManicuristas] = useState<ManicuristaAdmin[]>([]);
  const [loading, setLoading] = useState(true);
  const [dialogo, setDialogo] = useState<Dialogo>(null);
  const [procesando, setProcesando] = useState(false);

  function cargar() {
    setLoading(true);
    getManicuristas()
      .then(setManicuristas)
      .finally(() => setLoading(false));
  }

  useEffect(cargar, []);

  function cerrarYRefrescar(mensaje: string) {
    setDialogo(null);
    cargar();
    pushToast({ type: 'success', message: mensaje });
  }

  async function copiarLink(slug: string) {
    const url = `${window.location.origin}/agenda/${slug}`;
    const ok = await copyToClipboard(url);
    pushToast(ok ? { type: 'success', message: 'Link copiado.' } : { type: 'error', message: 'No se pudo copiar.' });
  }

  async function confirmarActivo() {
    if (dialogo?.tipo !== 'activo') return;
    setProcesando(true);
    try {
      await setManicuristaActivo(dialogo.m.tenantId, !dialogo.m.activo);
      cerrarYRefrescar(dialogo.m.activo ? 'Manicurista desactivada.' : 'Manicurista activada.');
    } catch {
      pushToast({ type: 'error', message: 'No se pudo cambiar el estado.' });
    } finally {
      setProcesando(false);
    }
  }

  async function confirmarEliminar() {
    if (dialogo?.tipo !== 'eliminar') return;
    setProcesando(true);
    try {
      await eliminarManicurista(dialogo.m.tenantId);
      cerrarYRefrescar('Manicurista eliminada.');
    } catch {
      pushToast({ type: 'error', message: 'No se pudo eliminar.' });
    } finally {
      setProcesando(false);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-medium uppercase tracking-[0.2em] text-slate-400">Gestión</p>
          <h2 className="mt-2 text-2xl font-semibold text-slate-900">Manicuristas</h2>
        </div>
        <button
          type="button"
          onClick={() => setDialogo({ tipo: 'crear' })}
          className="inline-flex h-11 shrink-0 items-center gap-2 rounded-2xl bg-slate-900 px-4 text-sm font-semibold text-white shadow-sm transition hover:bg-slate-800"
        >
          <Plus size={18} />
          <span className="hidden sm:inline">Nueva manicurista</span>
          <span className="sm:hidden">Nueva</span>
        </button>
      </div>

      {loading ? (
        <Loading label="Cargando manicuristas..." />
      ) : manicuristas.length === 0 ? (
        <EmptyState title="Todavía no hay manicuristas" description="Creá la primera con el botón de arriba." />
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {manicuristas.map((m) => (
            <article key={m.tenantId} className="rounded-3xl border border-slate-200 bg-white p-5 shadow-soft">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <h3 className="truncate text-lg font-semibold text-slate-900">{m.nombre}</h3>
                    <span
                      className={`shrink-0 rounded-full px-2 py-0.5 text-xs font-medium ${
                        m.activo ? 'bg-green-100 text-green-700' : 'bg-slate-200 text-slate-600'
                      }`}
                    >
                      {m.activo ? 'Activa' : 'Inactiva'}
                    </span>
                  </div>
                  <p className="mt-0.5 truncate text-sm text-slate-500">{m.email}</p>
                </div>
              </div>

              <div className="mt-3 space-y-1.5 text-sm text-slate-600">
                <div className="flex items-center gap-2">
                  <a
                    href={`/agenda/${m.slug}`}
                    target="_blank"
                    rel="noreferrer"
                    className="inline-flex items-center gap-1 truncate font-medium text-brand-600 hover:underline"
                  >
                    /agenda/{m.slug}
                    <ExternalLink size={13} className="shrink-0" />
                  </a>
                  <button
                    type="button"
                    onClick={() => copiarLink(m.slug)}
                    aria-label="Copiar link público"
                    className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
                  >
                    <Copy size={14} />
                  </button>
                </div>
                <p>WhatsApp: {m.whatsApp}</p>
                <div className="flex items-center gap-3 text-xs text-slate-400">
                  <span>{m.duracionTurnoMinutos} min/turno</span>
                  <span className="inline-flex items-center gap-1">
                    <CalendarDays size={12} /> {m.cantidadTurnos} turnos
                  </span>
                </div>
              </div>

              <div className="mt-4 flex flex-wrap gap-2">
                <BotonAccion icon={<Pencil size={15} />} label="Editar" onClick={() => setDialogo({ tipo: 'editar', m })} />
                <BotonAccion icon={<KeyRound size={15} />} label="Contraseña" onClick={() => setDialogo({ tipo: 'password', m })} />
                <BotonAccion icon={<Power size={15} />} label={m.activo ? 'Desactivar' : 'Activar'} onClick={() => setDialogo({ tipo: 'activo', m })} />
                <BotonAccion icon={<Trash2 size={15} />} label="Eliminar" danger onClick={() => setDialogo({ tipo: 'eliminar', m })} />
              </div>
            </article>
          ))}
        </div>
      )}

      {dialogo?.tipo === 'crear' && (
        <ModalManicurista modo="crear" onClose={() => setDialogo(null)} onSaved={cerrarYRefrescar} />
      )}
      {dialogo?.tipo === 'editar' && (
        <ModalManicurista modo="editar" manicurista={dialogo.m} onClose={() => setDialogo(null)} onSaved={cerrarYRefrescar} />
      )}
      {dialogo?.tipo === 'password' && (
        <ModalResetPassword manicurista={dialogo.m} onClose={() => setDialogo(null)} onSaved={cerrarYRefrescar} />
      )}
      {dialogo?.tipo === 'activo' && (
        <ConfirmDialog
          title={dialogo.m.activo ? 'Desactivar manicurista' : 'Activar manicurista'}
          message={
            dialogo.m.activo
              ? <>Se va a bloquear el login de <b>{dialogo.m.nombre}</b> y ocultar su agenda pública. Los turnos se conservan.</>
              : <>Se va a reactivar el login y la agenda pública de <b>{dialogo.m.nombre}</b>.</>
          }
          confirmLabel={dialogo.m.activo ? 'Desactivar' : 'Activar'}
          loading={procesando}
          onConfirm={confirmarActivo}
          onClose={() => setDialogo(null)}
        />
      )}
      {dialogo?.tipo === 'eliminar' && (
        <ConfirmDialog
          title="Eliminar manicurista"
          danger
          message={
            <>
              Se eliminará <b>{dialogo.m.nombre}</b> junto con su usuario, horarios y{' '}
              <b>{dialogo.m.cantidadTurnos} turnos</b>. Esta acción no se puede deshacer.
            </>
          }
          confirmLabel="Eliminar definitivamente"
          loading={procesando}
          onConfirm={confirmarEliminar}
          onClose={() => setDialogo(null)}
        />
      )}
    </div>
  );
}

function BotonAccion({ icon, label, danger, onClick }: { icon: React.ReactNode; label: string; danger?: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`inline-flex h-10 items-center gap-1.5 rounded-xl border px-3 text-sm font-medium transition ${
        danger
          ? 'border-red-200 bg-white text-red-600 hover:bg-red-50'
          : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
      }`}
    >
      {icon}
      {label}
    </button>
  );
}
