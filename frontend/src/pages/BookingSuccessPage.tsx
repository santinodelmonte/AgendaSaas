import { CheckCircle2 } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';
import { formatDateTime } from '../utils/date';

type State = {
  nombreCliente?: string;
  telefonoCliente?: string;
  servicio?: string;
  fechaHora?: string;
  nombre?: string;
};

export function BookingSuccessPage() {
  const location = useLocation();
  const state = (location.state ?? {}) as State;

  return (
    <div className="mx-auto max-w-2xl rounded-3xl border border-slate-200 bg-white p-8 text-center shadow-soft">
      <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100 text-emerald-600">
        <CheckCircle2 size={30} />
      </div>
      <h1 className="mt-5 text-3xl font-semibold tracking-tight text-slate-900">Turno solicitado</h1>
      <p className="mt-3 text-sm text-slate-500">Recibimos tu solicitud y quedó registrada correctamente.</p>

      <div className="mt-8 grid gap-3 rounded-3xl bg-slate-50 p-5 text-left">
        <Detail label="Cliente" value={state.nombreCliente ?? '-'} />
        <Detail label="Teléfono" value={state.telefonoCliente ?? '-'} />
        <Detail label="Servicio" value={state.servicio ?? '-'} />
        <Detail label="Horario" value={formatDateTime(state.fechaHora)} />
      </div>

      <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:justify-center">
        <Link
          to="/login"
          className="inline-flex h-12 items-center justify-center rounded-2xl bg-slate-900 px-5 text-sm font-semibold text-white transition hover:bg-slate-800"
        >
          Ir al login
        </Link>
      </div>
    </div>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-4">
      <span className="text-sm text-slate-500">{label}</span>
      <span className="text-sm font-semibold text-slate-900">{value}</span>
    </div>
  );
}
