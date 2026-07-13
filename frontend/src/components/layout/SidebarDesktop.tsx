import { Link, NavLink } from 'react-router-dom';
import { CalendarRange, CalendarSearch, History, LayoutDashboard, Settings2 } from 'lucide-react';

const items = [
  { to: '/admin',                   label: 'Dashboard',      icon: LayoutDashboard, end: true },
  { to: '/admin/configuracion',     label: 'Configuración',  icon: Settings2 },
  { to: '/admin/turnos-pendientes', label: 'Turnos',         icon: CalendarSearch },
  { to: '/admin/agenda-diaria',     label: 'Agenda diaria',  icon: CalendarRange },
  { to: '/admin/agenda-semanal',    label: 'Agenda semanal', icon: CalendarRange },
  { to: '/admin/historial',         label: 'Historial',      icon: History },
];

export function SidebarDesktop() {
  return (
    <aside className="fixed left-0 top-0 hidden h-full w-56 flex-col border-r border-slate-200 bg-white lg:flex">
      <div className="flex h-16 items-center border-b border-slate-200 px-5">
        <Link to="/admin" className="text-lg font-semibold text-slate-900">
          AgendaSaaS
        </Link>
      </div>

      <nav className="flex-1 space-y-1 overflow-y-auto p-3">
        {items.map((item) => {
          const Icon = item.icon;
          return (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition ${
                  isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
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
  );
}
