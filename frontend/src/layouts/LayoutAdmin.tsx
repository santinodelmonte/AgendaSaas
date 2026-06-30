import { Outlet } from 'react-router-dom';
import { Navbar } from '../components/layout/Navbar';
import { SidebarMobile } from '../components/layout/SidebarMobile';

export function LayoutAdmin() {
  return (
    <div className="min-h-dvh bg-slate-50">
      <SidebarMobile />
      <Navbar />
      <main className="mx-auto max-w-7xl px-4 py-6 pb-24 sm:px-6 lg:px-8">
        <Outlet />
      </main>
    </div>
  );
}
