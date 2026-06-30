import { useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Loading } from '../components/common/Loading';
import { useToast } from '../contexts/ToastContext';

export function LoginPage() {
  const { signIn, isAuthenticated, isInitializing } = useAuth();
  const toast = useToast();
  const location = useLocation();
  const from = (location.state as { from?: Location })?.from?.pathname ?? '/admin';

  const [email, setEmail] = useState('admin@maria.com');
  const [password, setPassword] = useState('123456');
  const [loading, setLoading] = useState(false);

  if (isInitializing) return <Loading fullScreen />;

  if (isAuthenticated) return <Navigate to={from} replace />;

  const onSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    try {
      setLoading(true);
      await signIn({ email, password });
      toast.pushToast({ type: 'success', message: 'Sesión iniciada correctamente.' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-dvh bg-slate-50">
      <div className="mx-auto grid min-h-dvh max-w-6xl lg:grid-cols-2">
        <div className="hidden flex-col justify-between bg-slate-950 p-10 text-white lg:flex">
          <div>
            <h1 className="text-4xl font-semibold tracking-tight">AgendaSaaS</h1>
            <p className="mt-4 max-w-md text-slate-300">
              Plataforma de reservas para manicuristas con agenda pública, panel administrativo y experiencia mobile first.
            </p>
          </div>
          <div className="rounded-3xl bg-white/5 p-6 backdrop-blur">
            <p className="text-sm text-slate-300">Diseño moderno. Flujos rápidos. Gestión clara.</p>
          </div>
        </div>

        <div className="flex items-center justify-center px-4 py-10 sm:px-6 lg:px-12">
          <form onSubmit={onSubmit} className="w-full max-w-md rounded-3xl border border-slate-200 bg-white p-6 shadow-soft sm:p-8">
            <div className="mb-8">
              <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Bienvenido</p>
              <h2 className="mt-2 text-2xl font-semibold text-slate-900">Iniciar sesión</h2>
              <p className="mt-2 text-sm text-slate-500">Accede al panel administrativo.</p>
            </div>

            <div className="space-y-5">
              <label className="block">
                <span className="mb-2 block text-sm font-medium text-slate-700">Email</span>
                <input
                  type="email"
                  className="h-12 w-full rounded-2xl border border-slate-300 bg-white px-4 outline-none transition focus:border-brand-500 focus:ring-4 focus:ring-brand-100"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  autoComplete="email"
                  required
                />
              </label>

              <label className="block">
                <span className="mb-2 block text-sm font-medium text-slate-700">Password</span>
                <input
                  type="password"
                  className="h-12 w-full rounded-2xl border border-slate-300 bg-white px-4 outline-none transition focus:border-brand-500 focus:ring-4 focus:ring-brand-100"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete="current-password"
                  required
                />
              </label>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="mt-8 inline-flex h-12 w-full items-center justify-center rounded-2xl bg-brand-600 px-5 text-sm font-semibold text-white transition hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {loading ? 'Ingresando...' : 'Ingresar'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
