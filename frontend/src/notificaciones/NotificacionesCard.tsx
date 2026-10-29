import { useEffect, useState } from 'react';
import { Bell, BellOff, BellRing, Share, SquarePlus, X } from 'lucide-react';
import { useToast } from '../contexts/ToastContext';
import { activar, desactivar, enviarPrueba, obtenerEstado, type EstadoPush } from './push';

const CLAVE_OCULTO = 'agendasaas_aviso_notificaciones_oculto';

type Props = {
  // Versión aviso (dashboard): solo aparece si falta activarlas y se puede cerrar.
  compacto?: boolean;
};

export function NotificacionesCard({ compacto = false }: Props) {
  const { pushToast } = useToast();
  const [estado, setEstado] = useState<EstadoPush>('cargando');
  const [trabajando, setTrabajando] = useState(false);
  const [oculto, setOculto] = useState(() => {
    try {
      return compacto && localStorage.getItem(CLAVE_OCULTO) === '1';
    } catch {
      return false;
    }
  });

  useEffect(() => {
    obtenerEstado().then(setEstado).catch(() => setEstado('no-soportado'));
  }, []);

  const ejecutar = async (accion: () => Promise<void>) => {
    setTrabajando(true);
    try {
      await accion();
    } catch {
      pushToast({ type: 'error', message: 'No se pudieron configurar las notificaciones.' });
    } finally {
      setTrabajando(false);
    }
  };

  const onActivar = () =>
    ejecutar(async () => {
      const nuevo = await activar();
      setEstado(nuevo);
      if (nuevo === 'activas') {
        const enviados = await enviarPrueba();
        pushToast({
          type: 'success',
          message: enviados > 0 ? 'Listo: te mandamos una notificación de prueba.' : 'Notificaciones activadas.',
        });
      }
    });

  const onProbar = () =>
    ejecutar(async () => {
      const enviados = await enviarPrueba();
      pushToast(
        enviados > 0
          ? { type: 'success', message: `Notificación enviada a ${enviados} dispositivo${enviados === 1 ? '' : 's'}.` }
          : { type: 'error', message: 'No hay dispositivos activos. Volvé a activar las notificaciones.' },
      );
    });

  const onDesactivar = () => ejecutar(async () => setEstado(await desactivar()));

  const ocultar = () => {
    setOculto(true);
    try {
      localStorage.setItem(CLAVE_OCULTO, '1');
    } catch {
      // Sin almacenamiento: se oculta solo por esta visita.
    }
  };

  if (estado === 'cargando') return null;
  if (compacto && (oculto || estado === 'activas' || estado === 'no-soportado')) return null;

  return (
    <section className="relative rounded-3xl border border-slate-200 bg-white p-5 shadow-soft">
      {compacto && (
        <button
          type="button"
          onClick={ocultar}
          aria-label="Cerrar aviso"
          className="absolute right-2 top-2 flex h-11 w-11 items-center justify-center rounded-full text-slate-400 hover:text-slate-600"
        >
          <X size={18} />
        </button>
      )}

      <div className="flex items-start gap-3 pr-8">
        <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-brand-50 text-brand-600">
          {estado === 'activas' ? <BellRing size={20} /> : estado === 'bloqueadas' ? <BellOff size={20} /> : <Bell size={20} />}
        </div>
        <div className="space-y-1">
          <h3 className="font-semibold text-slate-900">Avisos de reservas nuevas</h3>
          <p className="text-sm text-slate-500">
            {estado === 'activas'
              ? 'Activadas en este dispositivo. Te avisamos al instante cuando una clienta reserva.'
              : 'Recibí una notificación en el celular cada vez que una clienta reserva un turno.'}
          </p>
        </div>
      </div>

      <div className="mt-4">
        {estado === 'ios-instalar' && (
          <ol className="space-y-3 rounded-2xl bg-slate-50 p-4 text-sm text-slate-700">
            <li className="flex items-center gap-3">
              <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-white font-semibold">1</span>
              <span>
                Abrí esta página en <b>Safari</b> y tocá <Share size={16} className="inline align-text-bottom" /> <b>Compartir</b>.
              </span>
            </li>
            <li className="flex items-center gap-3">
              <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-white font-semibold">2</span>
              <span>
                Elegí <SquarePlus size={16} className="inline align-text-bottom" /> <b>Agregar a pantalla de inicio</b>.
              </span>
            </li>
            <li className="flex items-center gap-3">
              <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-white font-semibold">3</span>
              <span>
                Abrí <b>Agenda</b> desde el ícono nuevo, iniciá sesión y activá los avisos acá.
              </span>
            </li>
            <li className="pl-10 text-xs text-slate-500">Requiere iOS 16.4 o posterior.</li>
          </ol>
        )}

        {estado === 'no-soportado' && (
          <p className="rounded-2xl bg-slate-50 p-4 text-sm text-slate-600">
            Este navegador no permite notificaciones. Probá desde Chrome en Android, o instalando el panel en tu iPhone.
          </p>
        )}

        {estado === 'bloqueadas' && (
          <p className="rounded-2xl bg-amber-50 p-4 text-sm text-amber-800">
            Las notificaciones están bloqueadas para este sitio. Habilitalas desde los ajustes del navegador
            (en iPhone: Ajustes → Notificaciones → Agenda) y volvé a esta pantalla.
          </p>
        )}

        {estado === 'inactivas' && (
          <button
            type="button"
            onClick={onActivar}
            disabled={trabajando}
            className="inline-flex h-11 w-full items-center justify-center gap-2 rounded-2xl bg-brand-600 px-4 text-sm font-semibold text-white disabled:opacity-60 sm:w-auto"
          >
            <Bell size={16} />
            {trabajando ? 'Activando...' : 'Activar notificaciones'}
          </button>
        )}

        {estado === 'activas' && (
          <div className="flex flex-col gap-2 sm:flex-row">
            <button
              type="button"
              onClick={onProbar}
              disabled={trabajando}
              className="inline-flex h-11 items-center justify-center gap-2 rounded-2xl border border-slate-200 px-4 text-sm font-semibold text-slate-700 disabled:opacity-60"
            >
              Enviar prueba
            </button>
            <button
              type="button"
              onClick={onDesactivar}
              disabled={trabajando}
              className="inline-flex h-11 items-center justify-center gap-2 rounded-2xl px-4 text-sm font-medium text-slate-500 disabled:opacity-60"
            >
              Desactivar en este dispositivo
            </button>
          </div>
        )}
      </div>
    </section>
  );
}
