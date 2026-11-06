import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { CalendarDays } from 'lucide-react';

export function LayoutPublico({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-dvh flex-col bg-gradient-to-b from-slate-50 to-white">
      <header className="sticky top-0 z-30 border-b border-slate-200/70 bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center px-4 py-3 sm:px-6 lg:px-8">
          <div className="flex items-center gap-2 font-semibold text-slate-900">
            <span className="flex h-9 w-9 items-center justify-center rounded-2xl bg-brand-600 text-white shadow-soft">
              <CalendarDays size={18} />
            </span>
            <span>AgendaSaaS</span>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6 sm:px-6 lg:px-8">{children}</main>

      <footer className="border-t border-slate-200/70 py-5 text-center text-xs text-slate-500">
        <Link to="/terminos" className="underline-offset-2 hover:underline">Términos</Link>
        <span className="mx-2">·</span>
        <Link to="/privacidad" className="underline-offset-2 hover:underline">Privacidad</Link>
      </footer>
    </div>
  );
}
