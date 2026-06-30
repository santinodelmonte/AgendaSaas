import { Navigate, Route, Routes } from 'react-router-dom';
import { LayoutAdmin } from './layouts/LayoutAdmin';
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

export default function App() {
  return (
    <>
      <Routes>
        <Route path="/login" element={<PublicOnlyRoute><LoginPage /></PublicOnlyRoute>} />
        <Route path="/agenda/:slug" element={<LayoutPublico><PublicBookingPage /></LayoutPublico>} />
        <Route path="/agenda/:slug/exito" element={<LayoutPublico><BookingSuccessPage /></LayoutPublico>} />

        <Route
          path="/admin"
          element={
            <ProtectedRoute>
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
        </Route>

        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
      <ToastHost />
    </>
  );
}
