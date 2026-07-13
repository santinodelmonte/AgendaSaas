import { useState } from 'react';
import { Link, NavLink } from 'react-router-dom';
import { CalendarRange, CalendarSearch, History, LayoutDashboard, Menu, Settings2, X } from 'lucide-react';

const items = [
  { to: '/admin',                  label: 'Dashboard',      icon: LayoutDashboard, end: true },
  { to: '/admin/configuracion',    label: 'Configuración',  icon: Settings2 },
  { to: '/admin/turnos-pendientes',label: 'Turnos',         icon: CalendarSearch },
  { to: '/admin/agenda-diaria',    label: 'Agenda diaria',  icon: CalendarRange },
  { to: '/admin/agenda-semanal',   label: 'Agenda semanal', icon: CalendarRange },
  { to: '/admin/historial',        label: 'Historial',      icon: History },
];

export function SidebarMobile() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="fixed left-4 top-4 z-40 inline-flex h-12 w-12 items-center justify-center rounded-2xl border border-slate-200 bg-white shadow-soft lg:hidden"
        aria-label="Abrir menú lateral"
      >
        <Menu size={20} />
      </button>

      {open ? (
        <div className="fixed inset-0 z-40 bg-slate-950/40 lg:hidden" onClick={() => setOpen(false)}>
          <aside
            className="absolute left-0 top-0 h-full w-[84%] max-w-xs bg-white p-4 shadow-2xl"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="mb-6 flex items-center justify-between">
              <Link to="/admin" className="text-lg font-semibold text-slate-900">
                AgendaSaaS
              </Link>
              <button
                type="button"
                onClick={() => setOpen(false)}
                className="inline-flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-100 text-slate-700"
                aria-label="Cerrar menú"
              >
                <X size={18} />
              </button>
            </div>

            <nav className="space-y-2">
              {items.map((item) => {
                const Icon = item.icon;
                return (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    end={item.end}
                    onClick={() => setOpen(false)}
                    className={({ isActive }) =>
                      `flex items-center gap-3 rounded-2xl px-4 py-3 text-sm font-medium transition ${
                        isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-700 hover:bg-slate-100'
                      }`
                    }
                  >
                    <Icon size={18} />
                    {item.label}
                  </NavLink>
                );
              })}
            </nav>
          </aside>
        </div>
      ) : null}
    </>
  );
}
