import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { authStorage } from '../utils/storage';
import { mensajeDeError } from './errors';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '',
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const { token } = authStorage.get();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  // El tenant de los endpoints autenticados lo deriva el backend del JWT, no del
  // cliente. La reserva pública (anónima) manda X-Tenant-Id explícito por request.
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const status = error.response?.status;
    const esLogin = error.config?.url?.includes('/api/auth/login') ?? false;

    if (esLogin && status === 401) {
      window.dispatchEvent(
        new CustomEvent('app:toast', {
          detail: { type: 'error', message: 'Email o contraseña incorrectos.' },
        }),
      );
      return Promise.reject(error);
    }

    if (!esLogin && (status === 401 || status === 403)) {
      authStorage.clear();
      window.dispatchEvent(
        new CustomEvent('auth:expired', {
          detail: { message: 'Tu sesión expiró. Inicia sesión nuevamente.' },
        }),
      );
      if (window.location.pathname !== '/login') {
        window.location.assign('/login');
      }
    }

    const message =
      mensajeDeError(error.response?.data) ??
      (error.response ? 'Ocurrió un error inesperado.' : 'No se pudo conectar con el servidor.');

    window.dispatchEvent(
      new CustomEvent('app:toast', {
        detail: { type: 'error', message },
      }),
    );

    return Promise.reject(error);
  },
);

export default api;
