import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { authStorage } from '../utils/storage';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '',
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const { token, tenantId } = authStorage.get();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  if (tenantId) {
    config.headers['X-Tenant-Id'] = tenantId;
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

    const message =
      (error.response?.data as { message?: string } | undefined)?.message ??
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
