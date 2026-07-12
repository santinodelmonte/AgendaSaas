import { parseISO } from 'date-fns';
import type { AgendaSlot, TurnoAgenda } from '../../types/admin';

// Escala vertical del timeline: 30 min = 60px, así un turno estándar
// supera el área táctil mínima de 44px.
export const PX_POR_MINUTO = 2;

// Intervalo en minutos desde la medianoche, fin exclusivo.
export type Intervalo = { inicio: number; fin: number };

export const ESTADO_TURNO_CFG = {
  Pendiente:  { bg: 'bg-amber-50',  border: 'border-amber-400', dot: 'bg-amber-500', label: 'Pendiente' },
  Confirmado: { bg: 'bg-red-50',    border: 'border-red-400',   dot: 'bg-red-500',   label: 'Reservado' },
  Rechazado:  { bg: 'bg-slate-100', border: 'border-slate-300', dot: 'bg-slate-400', label: 'Rechazado' },
} as const;

export function estadoTurnoCfg(estado: string) {
  return ESTADO_TURNO_CFG[estado as keyof typeof ESTADO_TURNO_CFG] ?? ESTADO_TURNO_CFG.Rechazado;
}

export function minutosDe(iso: string) {
  const d = parseISO(iso);
  return d.getHours() * 60 + d.getMinutes();
}

export function minutosAhora() {
  const d = new Date();
  return d.getHours() * 60 + d.getMinutes();
}

// La API devuelve los BadRequest como string plano en el body.
export function mensajeApi(err: unknown, fallback: string) {
  const data = (err as { response?: { data?: unknown } })?.response?.data;
  return typeof data === 'string' && data.length > 0 ? data : fallback;
}

export function formatMin(min: number) {
  const h = Math.floor(min / 60);
  const m = min % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
}

// "2025-07-05" + 615 -> "2025-07-05T10:15:00" (hora local, como espera la API)
export function aFechaHoraISO(fecha: string, min: number) {
  return `${fecha}T${formatMin(min)}:00`;
}

export function redondearA15(min: number) {
  return Math.round(min / 15) * 15;
}

function ordenar(intervalos: Intervalo[]) {
  return [...intervalos].sort((a, b) => a.inicio - b.inicio);
}

export function unirIntervalos(intervalos: Intervalo[]): Intervalo[] {
  const resultado: Intervalo[] = [];
  for (const actual of ordenar(intervalos)) {
    const ultimo = resultado[resultado.length - 1];
    if (ultimo && actual.inicio <= ultimo.fin) {
      ultimo.fin = Math.max(ultimo.fin, actual.fin);
    } else {
      resultado.push({ ...actual });
    }
  }
  return resultado;
}

// Resta a cada intervalo de `base` los intervalos de `bloqueos`.
export function restarIntervalos(base: Intervalo[], bloqueos: Intervalo[]): Intervalo[] {
  const ocupado = unirIntervalos(bloqueos);
  const resultado: Intervalo[] = [];

  for (const b of unirIntervalos(base)) {
    let cursor = b.inicio;
    for (const o of ocupado) {
      if (o.fin <= cursor || o.inicio >= b.fin) continue;
      if (o.inicio > cursor) resultado.push({ inicio: cursor, fin: o.inicio });
      cursor = Math.max(cursor, o.fin);
    }
    if (cursor < b.fin) resultado.push({ inicio: cursor, fin: b.fin });
  }

  return resultado;
}

// Primer inicio (redondeado a 15') donde entra un turno de `duracion` minutos.
export function sugerirHueco(huecos: Intervalo[], duracion: number, desde: number): number | null {
  for (const h of ordenar(huecos)) {
    const inicio = Math.ceil(Math.max(h.inicio, desde) / 15) * 15;
    if (h.fin - inicio >= duracion) return inicio;
  }
  return null;
}

export type LayoutDia = {
  ventana: Intervalo;
  pausas: Intervalo[];
  huecos: Intervalo[];
  ocupados: TurnoAgenda[];
  sinHorario: boolean;
};

// Deriva la geometría del día (ventana visible, pausas, huecos libres y turnos
// ocupados) a partir de la grilla de slots y los turnos con duración variable.
// Compartida por la agenda diaria y el dashboard para no duplicar la lógica.
export function computarLayoutDia(
  turnos: TurnoAgenda[],
  slots: AgendaSlot[],
  paso: number,
): LayoutDia {
  const ocupados = turnos.filter((t) => t.estado === 'Pendiente' || t.estado === 'Confirmado');
  const slotTimes = slots.map((s) => minutosDe(s.fechaHora));

  // Tiempo laborable del día según la grilla de slots; 8-20 como fallback
  const laborable: Intervalo[] = slotTimes.length
    ? unirIntervalos(slotTimes.map((t) => ({ inicio: t, fin: t + paso })))
    : [{ inicio: 8 * 60, fin: 20 * 60 }];

  const bloquesOcupados = ocupados.map((t) => {
    const inicio = minutosDe(t.fechaHora);
    return { inicio, fin: inicio + (t.duracionMinutos ?? paso) };
  });

  // La ventana visible cubre el horario laboral y cualquier turno fuera de él
  let inicio = laborable[0].inicio;
  let fin = laborable[laborable.length - 1].fin;
  for (const b of bloquesOcupados) {
    inicio = Math.min(inicio, b.inicio);
    fin = Math.max(fin, b.fin);
  }
  const ventana = { inicio: Math.floor(inicio / 60) * 60, fin: Math.ceil(fin / 60) * 60 };

  return {
    ventana,
    pausas: restarIntervalos([ventana], laborable),
    huecos: restarIntervalos(laborable, bloquesOcupados).filter((h) => h.fin - h.inicio >= 15),
    ocupados,
    sinHorario: slotTimes.length === 0,
  };
}
