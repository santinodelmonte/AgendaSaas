import { useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { sincronizar } from '../notificaciones/push';
import { AvisoWhatsAppProvider } from '../contexts/AvisoWhatsAppContext';
import { Navbar } from '../components/layout/Navbar';
import { SidebarMobile } from '../components/layout/SidebarMobile';
import { SidebarDesktop } from '../components/layout/SidebarDesktop';

export function LayoutAdmin() {
  useEffect(() => {
    sincronizar();
  }, []);

  return (
    <AvisoWhatsAppProvider>
      <div className="min-h-dvh bg-slate-50">
        <SidebarDesktop />
        <SidebarMobile />

        <div className="lg:pl-56">
          <Navbar />
          <main className="mx-auto max-w-5xl px-4 py-6 pb-24 sm:px-6">
            <Outlet />
          </main>
        </div>
      </div>
    </AvisoWhatsAppProvider>
  );
}
