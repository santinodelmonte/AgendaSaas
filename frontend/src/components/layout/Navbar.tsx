import { Menu, LogOut } from 'lucide-react';
import { useAuth } from '../../contexts/AuthContext';

export function Navbar() {
  const { email, signOut } = useAuth();

  return (
    <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/90 backdrop-blur">
      <div className="mx-auto flex max-w-7xl items-center justify-between gap-3 px-4 py-4 sm:px-6 lg:px-8">
        <div>
          <p className="text-xs font-medium uppercase tracking-[0.2em] text-slate-400">Admin</p>
          <h1 className="text-lg font-semibold text-slate-900">AgendaSaaS</h1>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            className="inline-flex h-11 w-11 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-700 shadow-sm transition hover:bg-slate-50 lg:hidden"
            aria-label="Abrir menú"
          >
            <Menu size={20} />
          </button>

          <div className="hidden text-right sm:block">
            <p className="text-sm font-medium text-slate-900">{email}</p>
            <p className="text-xs text-slate-500">Sesión activa</p>
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
  );
}
