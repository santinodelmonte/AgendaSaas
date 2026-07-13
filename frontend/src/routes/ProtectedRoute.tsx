import { Navigate, useLocation } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { Loading } from '../components/common/Loading';
import { homeForRole } from '../utils/roles';
import type { UsuarioRol } from '../types/auth';

export function ProtectedRoute({ children, role }: { children: ReactNode; role?: UsuarioRol }) {
  const { isAuthenticated, isInitializing, rol } = useAuth();
  const location = useLocation();

  if (isInitializing) {
    return <Loading fullScreen label="Cargando sesión..." />;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  // Rol incorrecto para esta sección: mandar a su home.
  if (role && rol !== role) {
    return <Navigate to={homeForRole(rol)} replace />;
  }

  return <>{children}</>;
}
