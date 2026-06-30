import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { X } from 'lucide-react';

export type ToastType = 'success' | 'error' | 'info';

export type ToastItem = {
  id: string;
  type: ToastType;
  message: string;
};

type ToastContextValue = {
  pushToast: (toast: Omit<ToastItem, 'id'>) => void;
};

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const pushToast = (toast: Omit<ToastItem, 'id'>) => {
    const id = crypto.randomUUID();
    setToasts((current) => [...current, { ...toast, id }]);
    window.setTimeout(() => {
      setToasts((current) => current.filter((item) => item.id !== id));
    }, 3500);
  };

  useEffect(() => {
    const listener = (event: Event) => {
      const custom = event as CustomEvent<{ type: ToastType; message: string }>;
      pushToast(custom.detail);
    };
    window.addEventListener('app:toast', listener);
    return () => window.removeEventListener('app:toast', listener);
  }, []);

  const value = useMemo(() => ({ pushToast }), []);

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div className="pointer-events-none fixed right-4 top-4 z-50 flex w-[calc(100%-2rem)] max-w-sm flex-col gap-2 sm:right-6 sm:top-6">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className="pointer-events-auto flex items-start gap-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-soft"
          >
            <div
              className={`mt-0.5 h-2.5 w-2.5 rounded-full ${
                toast.type === 'success'
                  ? 'bg-emerald-500'
                  : toast.type === 'error'
                    ? 'bg-rose-500'
                    : 'bg-sky-500'
              }`}
            />
            <p className="flex-1 text-sm text-slate-700">{toast.message}</p>
            <button
              type="button"
              onClick={() => setToasts((current) => current.filter((item) => item.id !== toast.id))}
              className="rounded-lg p-1 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600"
              aria-label="Cerrar notificación"
            >
              <X size={16} />
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) throw new Error('useToast debe usarse dentro de ToastProvider');
  return context;
}
