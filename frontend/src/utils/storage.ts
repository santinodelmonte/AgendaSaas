import type { UsuarioRol } from '../types/auth';

export type AuthSession = {
  token: string;
  email: string;
  tenantId: string;
  rol: UsuarioRol;
};

const AUTH_KEY = 'agendasaas_auth';

export const authStorage = {
  get(): Partial<AuthSession> {
    try {
      const raw = localStorage.getItem(AUTH_KEY);
      return raw ? (JSON.parse(raw) as Partial<AuthSession>) : {};
    } catch {
      return {};
    }
  },
  set(session: AuthSession) {
    localStorage.setItem(AUTH_KEY, JSON.stringify(session));
  },
  clear() {
    localStorage.removeItem(AUTH_KEY);
  },
};
