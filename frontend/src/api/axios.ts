import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { authStorage } from '../utils/storage';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '',
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const { token } = authStorage.get();

  // El tenant de los endpoints autenticados lo deriva el backend del JWT, no del
  // cliente. La reserva pública (anónima) manda X-Tenant-Id explícito por request
  // y va SIN token: si una manicurista logueada abre la página de otra, el
  // backend tomaría el tenant de su token y le mostraría su propia agenda.
  if (token && !config.headers['X-Tenant-Id']) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const status = error.response?.status;

    if (status === 401 || status === 403) {
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

    // El backend responde { message } (errores globales, rate limit) o texto
    // plano (validaciones con BadRequest("...")).
    const data = error.response?.data;
    const message =
      (typeof data === 'string' && data.trim() ? data : undefined) ??
      (data as { message?: string } | undefined)?.message ??
      error.message ??
      'Ocurrió un error inesperado.';

    window.dispatchEvent(
      new CustomEvent('app:toast', {
        detail: { type: 'error', message },
      }),
    );

    return Promise.reject(error);
  },
);

export default api;
