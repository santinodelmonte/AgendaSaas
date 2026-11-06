import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { MessageCircle, CalendarDays } from 'lucide-react';
import {
  getPublicPerfil,
  getPublicServicios,
  getTurnosDisponibles,
  solicitarTurno,
} from '../api/public.service';
import type {
  DisponibilidadTurno,
  PublicPerfil,
  PublicServicio,
} from '../types/public';
import { Loading } from '../components/common/Loading';
import { EmptyState } from '../components/common/EmptyState';
import { HorarioSelector } from '../components/booking/HorarioSelector';
import { toDateInputValue } from '../utils/date';
import { useToast } from '../contexts/ToastContext';

export function PublicBookingPage() {
  const { slug = '' } = useParams();
  const navigate = useNavigate();
  const toast = useToast();

  const [perfil, setPerfil] = useState<PublicPerfil | null>(null);
  const [tenantId, setTenantId] = useState('');

  const hoy = toDateInputValue(new Date());
  const [fecha, setFecha] = useState(hoy);

  const [horarios, setHorarios] =
    useState<DisponibilidadTurno[]>([]);

  const [loading, setLoading] = useState(true);
  const [loadingHorarios, setLoadingHorarios] =
    useState(false);

  const [selectedSlot, setSelectedSlot] =
    useState('');

  const [saving, setSaving] = useState(false);

  // En el celular el formulario queda debajo de la grilla de horarios: al elegir
  // uno se baja hasta el formulario para que no tenga que buscarlo.
  const formRef = useRef<HTMLFormElement>(null);

  const elegirHorario = (value: string) => {
    setSelectedSlot(value);
    if (window.matchMedia('(max-width: 1023px)').matches) {
      setTimeout(() => formRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 50);
    }
  };

  const horarioElegidoTexto = selectedSlot
    ? new Date(selectedSlot).toLocaleString('es-UY', {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23',
      })
    : '';

  const [nombreCliente, setNombreCliente] =
    useState('');

  const [telefonoCliente, setTelefonoCliente] =
    useState('');

  const [servicio, setServicio] =
    useState('');

  const [serviciosDisponibles, setServiciosDisponibles] =
    useState<PublicServicio[]>([]);

  useEffect(() => {
    let mounted = true;

    async function loadAgenda() {
      try {
        setLoading(true);

        const perfilData =
          await getPublicPerfil(slug);

        if (!mounted) return;

        setPerfil(perfilData);

        getPublicServicios(slug).then(setServiciosDisponibles).catch(() => {});

        const currentTenantId =
          perfilData.tenantId;

        setTenantId(currentTenantId);

        const slots =
          await getTurnosDisponibles(
            fecha,
            currentTenantId
          );

        if (!mounted) return;

        setHorarios(slots);
      } catch (error) {
        console.error(error);

        toast.pushToast({
          type: 'error',
          message:
            'No fue posible cargar la agenda.',
        });
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    }

    loadAgenda();

    return () => {
      mounted = false;
    };
  }, [slug]);

  useEffect(() => {
    if (!tenantId) return;

    let mounted = true;

    async function loadHorarios() {
      try {
        setLoadingHorarios(true);

        const slots =
          await getTurnosDisponibles(
            fecha,
            tenantId
          );

        if (!mounted) return;

        setHorarios(slots);
      } catch (error) {
        console.error(error);

        toast.pushToast({
          type: 'error',
          message:
            'No fue posible obtener horarios.',
        });
      } finally {
        if (mounted) {
          setLoadingHorarios(false);
        }
      }
    }

    loadHorarios();

    return () => {
      mounted = false;
    };
  }, [fecha, tenantId]);

  const whatsappLink = useMemo(() => {
    const phone =
      perfil?.whatsApp ?? '';

    const text =
      encodeURIComponent(
        'Hola, quiero agendar un turno.'
      );

    return phone
      ? `https://wa.me/${phone}?text=${text}`
      : '#';
  }, [perfil?.whatsApp]);

  const TELEFONO_RE = /^[\d\s\+\-\(\)]{7,20}$/;

  const onSubmit = async (
    e: React.FormEvent
  ) => {
    e.preventDefault();

    if (!selectedSlot) {
      toast.pushToast({ type: 'error', message: 'Seleccioná un horario disponible.' });
      return;
    }

    const slotDate = new Date(selectedSlot);
    if (slotDate <= new Date()) {
      toast.pushToast({ type: 'error', message: 'No podés reservar un turno en el pasado.' });
      return;
    }

    const dosHoras = new Date(Date.now() + 2 * 60 * 60 * 1000);
    if (slotDate <= dosHoras) {
      toast.pushToast({ type: 'error', message: 'Debés reservar con al menos 2 horas de anticipación.' });
      return;
    }

    if (!nombreCliente.trim() || nombreCliente.length > 100) {
      toast.pushToast({ type: 'error', message: 'El nombre debe tener entre 1 y 100 caracteres.' });
      return;
    }

    if (!TELEFONO_RE.test(telefonoCliente)) {
      toast.pushToast({ type: 'error', message: 'Ingresá un teléfono válido (7-20 dígitos).' });
      return;
    }

    if (!servicio.trim() || servicio.length > 200) {
      toast.pushToast({ type: 'error', message: 'El servicio debe tener entre 1 y 200 caracteres.' });
      return;
    }

    try {
      setSaving(true);

      await solicitarTurno(
        {
          fechaHora: selectedSlot,
          nombreCliente,
          telefonoCliente,
          servicio,
        },
        tenantId
      );

      navigate(
        `/agenda/${slug}/exito`,
        {
          state: {
            nombreCliente,
            telefonoCliente,
            servicio,
            fechaHora: selectedSlot,
            nombre: perfil?.nombre,
            duracionMinutos: perfil?.duracionTurnoMinutos,
          },
        }
      );
    } catch (error) {
      const data = (error as { response?: { data?: unknown } }).response?.data;
      const detalle =
        typeof data === 'string'
          ? data
          : (data as { message?: string } | undefined)?.message;

      toast.pushToast({
        type: 'error',
        message: detalle || 'No fue posible solicitar el turno.',
      });

      // Si el horario se ocupó mientras completaba los datos, refrescamos la grilla.
      setSelectedSlot('');
      getTurnosDisponibles(fecha, tenantId).then(setHorarios).catch(() => {});
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Loading
        fullScreen
        label="Cargando agenda pública..."
      />
    );
  }

  if (!perfil) {
    return (
      <EmptyState
        title="Agenda no encontrada"
        description="Revisa el enlace e intenta nuevamente."
      />
    );
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[1.1fr_0.9fr]">
      <section className="space-y-6">
        <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-soft sm:p-6">
          <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">
            Agenda pública
          </p>

          <h1 className="mt-2 text-3xl font-semibold tracking-tight text-slate-900">
            {perfil.nombre}
          </h1>

          <p className="mt-3 text-sm text-slate-500">
            Reservá tu turno de forma rápida y sencilla.
          </p>

          <div className="mt-6 flex flex-wrap gap-3">
            <a
              href={whatsappLink}
              className="inline-flex min-h-11 items-center gap-2 rounded-2xl bg-emerald-500 px-5 py-3 text-sm font-semibold text-white transition hover:bg-emerald-600"
              target="_blank"
              rel="noreferrer"
            >
              <MessageCircle size={18} />
              WhatsApp
            </a>

            <div className="inline-flex items-center gap-2 rounded-2xl bg-slate-100 px-4 py-3 text-sm font-medium text-slate-700">
              <CalendarDays size={18} />
              Turnos de{' '}
              {perfil.duracionTurnoMinutos} min
            </div>
          </div>
        </div>

        <div className="rounded-3xl border border-slate-200 bg-white p-5 shadow-soft sm:p-6">
          <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="text-xl font-semibold text-slate-900">
                Horarios disponibles
              </h2>

              <p className="mt-1 text-sm text-slate-500">
                Elegí una fecha y un horario.
              </p>
            </div>

            <input
              type="date"
              value={fecha}
              min={hoy}
              onChange={(e) => {
                setFecha(e.target.value);
                setSelectedSlot('');
              }}
              className="h-12 w-full rounded-2xl border border-slate-300 bg-white px-4 text-base outline-none focus:border-brand-500 sm:h-11 sm:w-auto sm:text-sm"
            />
          </div>

          {loadingHorarios ? (
            <Loading label="Actualizando horarios..." />
          ) : horarios.length > 0 ? (
            <HorarioSelector
              horarios={horarios}
              value={selectedSlot}
              onChange={elegirHorario}
              onOcupado={() =>
                toast.pushToast({
                  type: 'error',
                  message: 'Ese horario ya está reservado. Elegí otro.',
                })
              }
            />
          ) : (
            <EmptyState
              title="No hay horarios disponibles"
              description="Probá con otra fecha."
            />
          )}
        </div>
      </section>

            <aside className="space-y-6">
        <form
          ref={formRef}
          onSubmit={onSubmit}
          className="scroll-mt-20 rounded-3xl border border-slate-200 bg-white p-5 shadow-soft sm:p-6"
        >
          <h2 className="text-xl font-semibold text-slate-900">
            Solicitar turno
          </h2>

          <p className="mt-1 text-sm text-slate-500">
            Completá tus datos y la manicurista te confirma por WhatsApp.
          </p>

          <div className="mt-6 space-y-4">
            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">
                Nombre
              </span>

              <input
                required
                autoComplete="name"
                value={nombreCliente}
                onChange={(e) =>
                  setNombreCliente(e.target.value)
                }
                className="h-12 w-full rounded-2xl border border-slate-300 px-4 text-base outline-none focus:border-brand-500 focus:ring-4 focus:ring-brand-100"
              />
            </label>

            <label className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">
                Teléfono
              </span>

              <input
                required
                type="tel"
                inputMode="tel"
                autoComplete="tel"
                placeholder="Ej: 099 123 456"
                value={telefonoCliente}
                onChange={(e) =>
                  setTelefonoCliente(e.target.value)
                }
                className="h-12 w-full rounded-2xl border border-slate-300 px-4 text-base outline-none focus:border-brand-500 focus:ring-4 focus:ring-brand-100"
              />
            </label>

            <div className="block">
              <span className="mb-2 block text-sm font-medium text-slate-700">
                Servicio
              </span>

              {serviciosDisponibles.length > 0 ? (
                <div role="radiogroup" aria-label="Servicio" className="grid gap-2">
                  {serviciosDisponibles.map((s) => {
                    const activo = servicio === s.nombre;
                    return (
                      <button
                        key={s.id}
                        type="button"
                        role="radio"
                        aria-checked={activo}
                        onClick={() => setServicio(s.nombre)}
                        className={`flex min-h-12 w-full items-center justify-between gap-3 rounded-2xl border px-4 py-3 text-left transition ${
                          activo
                            ? 'border-brand-500 bg-brand-50 ring-4 ring-brand-100'
                            : 'border-slate-300 bg-white hover:border-brand-500'
                        }`}
                      >
                        <span className="font-medium text-slate-900">{s.nombre}</span>
                        <span className="shrink-0 text-sm font-semibold text-slate-600">
                          ${s.precio.toLocaleString('es-UY', { minimumFractionDigits: 0 })}
                        </span>
                      </button>
                    );
                  })}
                </div>
              ) : (
                <input
                  required
                  value={servicio}
                  onChange={(e) => setServicio(e.target.value)}
                  placeholder="Ej: Kapping gel"
                  className="h-12 w-full rounded-2xl border border-slate-300 px-4 text-base outline-none focus:border-brand-500 focus:ring-4 focus:ring-brand-100"
                />
              )}
            </div>
          </div>

          <div className="mt-6 rounded-2xl bg-slate-50 p-4">
            <p className="text-sm font-medium text-slate-600">
              Horario elegido
            </p>

            <p className="mt-1 text-base font-semibold text-slate-900">
              {horarioElegidoTexto || 'Elegí un horario arriba'}
            </p>
          </div>

          <button
            type="submit"
            disabled={saving}
            className="mt-6 inline-flex h-12 w-full items-center justify-center rounded-2xl bg-brand-600 px-5 text-sm font-semibold text-white transition hover:bg-brand-700 disabled:cursor-not-allowed disabled:opacity-70"
          >
            {saving
              ? 'Enviando...'
              : 'Solicitar turno'}
          </button>

          <p className="mt-3 text-center text-xs leading-relaxed text-slate-500">
            Al solicitar el turno aceptás los{' '}
            <Link to="/terminos" className="underline">términos</Link> y que tus datos se usen para
            gestionar tu reserva y contactarte por WhatsApp, según la{' '}
            <Link to="/privacidad" className="underline">política de privacidad</Link>.
          </p>
        </form>
      </aside>
    </div>
  );
}