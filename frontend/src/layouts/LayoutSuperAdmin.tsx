import { Outlet } from 'react-router-dom';
import { LogOut, ShieldCheck } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';

export function LayoutSuperAdmin() {
  const { email, signOut } = useAuth();

  return (
    <div className="min-h-dvh bg-slate-50">
      <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center justify-between gap-3 px-4 py-4 sm:px-6">
          <div className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-slate-900 text-white">
              <ShieldCheck size={20} />
            </span>
            <div>
              <p className="text-xs font-medium uppercase tracking-[0.2em] text-slate-400">Superadmin</p>
              <h1 className="text-lg font-semibold text-slate-900">AgendaSaaS</h1>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <div className="hidden text-right sm:block">
              <p className="text-sm font-medium text-slate-900">{email}</p>
              <p className="text-xs text-slate-500">Sesión de superadmin</p>
            </div>
            <button
              type="button"
              onClick={signOut}
              className="inline-flex h-11 items-center gap-2 rounded-2xl bg-slate-900 px-4 text-sm font-semibold text-white transition hover:bg-slate-800"
            >
              <LogOut size={16} />
              <span className="hidden sm:inline">Salir</span>
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-4 py-6 pb-24 sm:px-6">
        <Outlet />
      </main>
    </div>
  );
}
