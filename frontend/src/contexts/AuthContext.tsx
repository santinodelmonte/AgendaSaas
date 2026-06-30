import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import type { LoginRequest } from '../types/auth';
import { login } from '../api/auth.service';
import { authStorage } from '../utils/storage';

type AuthContextValue = {
  token: string | null;
  email: string | null;
  tenantId: string | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
  signIn: (credentials: LoginRequest) => Promise<void>;
  signOut: () => void;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const [isInitializing, setIsInitializing] = useState(true);
  const [token, setToken] = useState<string | null>(null);
  const [email, setEmail] = useState<string | null>(null);
  const [tenantId, setTenantId] = useState<string | null>(null);

  useEffect(() => {
    const session = authStorage.get();
    setToken(session.token ?? null);
    setEmail(session.email ?? null);
    setTenantId(session.tenantId ?? null);
    setIsInitializing(false);
  }, []);

  useEffect(() => {
    const onExpired = () => {
      setToken(null);
      setEmail(null);
      setTenantId(null);
    };
    window.addEventListener('auth:expired', onExpired as EventListener);
    return () => window.removeEventListener('auth:expired', onExpired as EventListener);
  }, []);

  const signIn = async (credentials: LoginRequest) => {
    const data = await login(credentials);
    authStorage.set(data);
    setToken(data.token);
    setEmail(data.email);
    setTenantId(data.tenantId);
    navigate('/admin', { replace: true });
  };

  const signOut = () => {
    authStorage.clear();
    setToken(null);
    setEmail(null);
    setTenantId(null);
    navigate('/login', { replace: true });
  };

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      email,
      tenantId,
      isAuthenticated: Boolean(token),
      isInitializing,
      signIn,
      signOut,
    }),
    [token, email, tenantId, isInitializing],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth debe usarse dentro de AuthProvider');
  return context;
}
