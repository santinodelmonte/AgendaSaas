import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { MessageCircle, CalendarDays } from 'lucide-react';

export function LayoutPublico({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-dvh bg-gradient-to-b from-slate-50 to-white">
      <header className="sticky top-0 z-30 border-b border-slate-200/70 bg-white/90 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 sm:px-6 lg:px-8">
          <Link to="/" className="flex items-center gap-2 font-semibold text-slate-900">
            <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-brand-600 text-white shadow-soft">
              <CalendarDays size={20} />
            </span>
            <span>AgendaSaaS</span>
          </Link>

          <a
            href="https://wa.me/"
            className="inline-flex items-center gap-2 rounded-2xl bg-slate-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-slate-800 active:scale-[0.99]"
          >
            <MessageCircle size={18} />
            WhatsApp
          </a>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-6 sm:px-6 lg:px-8">{children}</main>
    </div>
  );
}
