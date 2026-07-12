import { Navigate, Route, Routes } from 'react-router-dom';
import { LayoutAdmin } from './layouts/LayoutAdmin';
import { LayoutSuperAdmin } from './layouts/LayoutSuperAdmin';
import { LayoutPublico } from './layouts/LayoutPublico';
import { ProtectedRoute } from './routes/ProtectedRoute';
import { PublicOnlyRoute } from './routes/PublicOnlyRoute';
import { ToastHost } from './components/common/ToastHost';
import { LoginPage } from './pages/LoginPage';
import { PublicBookingPage } from './pages/PublicBookingPage';
import { BookingSuccessPage } from './pages/BookingSuccessPage';
import { AdminDashboardPage } from './pages/admin/AdminDashboardPage';
import { AdminPerfilPage } from './pages/admin/AdminPerfilPage';
import { AdminHorariosPage } from './pages/admin/AdminHorariosPage';
import { AdminTurnosPendientesPage } from './pages/admin/AdminTurnosPendientesPage';
import { AdminAgendaDiariaPage } from './pages/admin/AdminAgendaDiariaPage';
import { AdminAgendaSemanalPage } from './pages/admin/AdminAgendaSemanalPage';
import { AdminHistorialPage } from './pages/admin/AdminHistorialPage';
import { AdminDiasBloqueadosPage } from './pages/admin/AdminDiasBloqueadosPage';
import { AdminConfiguracionPage } from './pages/admin/AdminConfiguracionPage';
import { SuperAdminManicuristasPage } from './pages/superadmin/SuperAdminManicuristasPage';

export default function App() {
  return (
    <>
      <Routes>
        <Route path="/login" element={<PublicOnlyRoute><LoginPage /></PublicOnlyRoute>} />
        <Route path="/agenda/:slug" element={<LayoutPublico><PublicBookingPage /></LayoutPublico>} />
        <Route path="/agenda/:slug/exito" element={<LayoutPublico><BookingSuccessPage /></LayoutPublico>} />

        <Route
          path="/superadmin"
          element={
            <ProtectedRoute role="SuperAdmin">
              <LayoutSuperAdmin />
            </ProtectedRoute>
          }
        >
          <Route index element={<SuperAdminManicuristasPage />} />
        </Route>

        <Route
          path="/admin"
          element={
            <ProtectedRoute role="Manicurista">
              <LayoutAdmin />
            </ProtectedRoute>
          }
        >
          <Route index element={<AdminDashboardPage />} />
          <Route path="perfil" element={<AdminPerfilPage />} />
          <Route path="horarios" element={<AdminHorariosPage />} />
          <Route path="turnos-pendientes" element={<AdminTurnosPendientesPage />} />
          <Route path="agenda-diaria" element={<AdminAgendaDiariaPage />} />
          <Route path="agenda-semanal" element={<AdminAgendaSemanalPage />} />
          <Route path="historial" element={<AdminHistorialPage />} />
          <Route path="dias-bloqueados" element={<AdminDiasBloqueadosPage />} />
          <Route path="configuracion" element={<AdminConfiguracionPage />} />
        </Route>

        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
      <ToastHost />
    </>
  );
}
