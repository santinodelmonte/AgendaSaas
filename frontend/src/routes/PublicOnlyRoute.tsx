import { Navigate } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { Loading } from '../components/common/Loading';
import { homeForRole } from '../utils/roles';

export function PublicOnlyRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, isInitializing, rol } = useAuth();

  if (isInitializing) {
    return <Loading fullScreen label="Cargando..." />;
  }

  if (isAuthenticated) {
    return <Navigate to={homeForRole(rol)} replace />;
  }

  return <>{children}</>;
}
