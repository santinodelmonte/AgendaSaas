import { CheckCircle2, CalendarPlus, ArrowLeft } from 'lucide-react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { formatDateTime } from '../utils/date';

type State = {
  nombreCliente?: string;
  telefonoCliente?: string;
  servicio?: string;
  fechaHora?: string;
  nombre?: string;
  duracionMinutos?: number;
};

function googleCalendarUrl(state: State) {
  if (!state.fechaHora) return '#';

  const inicio = new Date(state.fechaHora);
  const fin = new Date(inicio.getTime() + (state.duracionMinutos ?? 30) * 60000);

  const fmt = (d: Date) =>
    d.toISOString().replace(/[-:]/g, '').split('.')[0] + 'Z';

  const params = new URLSearchParams({
    action: 'TEMPLATE',
    text: `Turno en ${state.nombre ?? 'AgendaSaaS'}`,
    dates: `${fmt(inicio)}/${fmt(fin)}`,
    details: `Servicio: ${state.servicio ?? ''}`,
  });

  return `https://calendar.google.com/calendar/render?${params.toString()}`;
}

export function BookingSuccessPage() {
  const { slug = '' } = useParams();
  const location = useLocation();
  const state = (location.state ?? {}) as State;

  return (
    <div className="mx-auto max-w-lg space-y-4">
      {/* Card principal */}
      <div className="rounded-3xl border border-slate-200 bg-white p-8 text-center shadow-soft">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100 text-emerald-600">
          <CheckCircle2 size={32} />
        </div>

        <h1 className="mt-5 text-2xl font-semibold tracking-tight text-slate-900">
          ¡Turno solicitado!
        </h1>

        <p className="mt-2 text-sm text-slate-500">
          Tu solicitud fue enviada correctamente.{' '}
          <span className="font-medium text-amber-600">
            Está pendiente de confirmación.
          </span>
        </p>

        {/* Resumen */}
        <div className="mt-6 space-y-2 rounded-2xl bg-slate-50 p-5 text-left">
          <Detail label="Nombre"    value={state.nombreCliente ?? '-'} />
          <Detail label="Teléfono"  value={state.telefonoCliente ?? '-'} />
          <Detail label="Servicio"  value={state.servicio ?? '-'} />
          <Detail label="Horario"   value={formatDateTime(state.fechaHora)} />
          {state.nombre && <Detail label="Manicurista" value={state.nombre} />}
        </div>

        {/* Acciones */}
        <div className="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-center">
          <a
            href={googleCalendarUrl(state)}
            target="_blank"
            rel="noreferrer"
            className="inline-flex h-11 items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
          >
            <CalendarPlus size={16} />
            Agregar al calendario
          </a>

          <Link
            to={`/agenda/${slug}`}
            className="inline-flex h-11 items-center justify-center gap-2 rounded-2xl bg-slate-900 px-5 text-sm font-semibold text-white transition hover:bg-slate-800"
          >
            <ArrowLeft size={16} />
            Volver a la agenda
          </Link>
        </div>
      </div>

      <p className="text-center text-xs text-slate-400">
        La manicurista confirmará o rechazará el turno a la brevedad.
      </p>
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
