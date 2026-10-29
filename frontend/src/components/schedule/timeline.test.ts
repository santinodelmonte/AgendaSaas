import { describe, expect, it } from 'vitest';
import type { AgendaSlot, TurnoAgenda } from '../../types/admin';
import {
  aFechaHoraISO,
  computarLayoutDia,
  mensajeApi,
  minutosDe,
  redondearA15,
  restarIntervalos,
  sugerirHueco,
  unirIntervalos,
} from './timeline';

const h = (horas: number) => horas * 60;

function slots(desde: number, hasta: number, paso = 30): AgendaSlot[] {
  const resultado: AgendaSlot[] = [];
  for (let m = h(desde); m < h(hasta); m += paso) {
    resultado.push({ fechaHora: aFechaHoraISO('2030-01-14', m), estado: 'Disponible' });
  }
  return resultado;
}

function turno(inicio: number, duracion: number | null, estado = 'Confirmado'): TurnoAgenda {
  return {
    id: `${inicio}`,
    fechaHora: aFechaHoraISO('2030-01-14', inicio),
    estado,
    nombreCliente: 'Ana',
    telefonoCliente: null,
    servicioSolicitado: null,
    notaInterna: null,
    duracionMinutos: duracion,
  };
}

describe('fechas', () => {
  it('arma la fecha local sin zona horaria, como espera la API', () => {
    expect(aFechaHoraISO('2030-01-14', 615)).toBe('2030-01-14T10:15:00');
    expect(minutosDe('2030-01-14T10:15:00')).toBe(615);
  });

  it('redondea a 15 minutos', () => {
    expect(redondearA15(7)).toBe(0);
    expect(redondearA15(8)).toBe(15);
    expect(redondearA15(52)).toBe(45);
  });
});

describe('intervalos', () => {
  it('une intervalos solapados o contiguos', () => {
    expect(unirIntervalos([{ inicio: 60, fin: 90 }, { inicio: 0, fin: 30 }, { inicio: 30, fin: 45 }])).toEqual([
      { inicio: 0, fin: 45 },
      { inicio: 60, fin: 90 },
    ]);
  });

  it('resta los bloqueos de la base', () => {
    expect(restarIntervalos([{ inicio: 0, fin: 120 }], [{ inicio: 30, fin: 60 }])).toEqual([
      { inicio: 0, fin: 30 },
      { inicio: 60, fin: 120 },
    ]);
  });

  it('sugiere el primer hueco donde entra la duración', () => {
    const huecos = [{ inicio: h(9), fin: h(9) + 20 }, { inicio: h(10), fin: h(12) }];
    expect(sugerirHueco(huecos, 30, 0)).toBe(h(10));
    expect(sugerirHueco(huecos, 30, h(10) + 5)).toBe(h(10) + 15);
    expect(sugerirHueco(huecos, 180, 0)).toBeNull();
  });
});

describe('computarLayoutDia', () => {
  it('calcula pausa, huecos y turnos largos', () => {
    // 09-13 y 14-18 (pausa de 13 a 14); turno de 90' a las 10.
    const grilla = [...slots(9, 13), ...slots(14, 18)];
    const layout = computarLayoutDia([turno(h(10), 90), turno(h(16), null, 'Disponible')], grilla, 30);

    expect(layout.sinHorario).toBe(false);
    expect(layout.ventana).toEqual({ inicio: h(9), fin: h(18) });
    expect(layout.pausas).toEqual([{ inicio: h(13), fin: h(14) }]);
    expect(layout.ocupados).toHaveLength(1);
    expect(layout.huecos).toEqual([
      { inicio: h(9), fin: h(10) },
      { inicio: h(11.5), fin: h(13) },
      { inicio: h(14), fin: h(18) },
    ]);
  });

  it('sin horario usa 8 a 20 y amplía la ventana para turnos fuera de horario', () => {
    const layout = computarLayoutDia([turno(h(21), 60)], [], 30);

    expect(layout.sinHorario).toBe(true);
    expect(layout.ventana).toEqual({ inicio: h(8), fin: h(22) });
  });
});

describe('mensajeApi', () => {
  it('extrae el mensaje del error de axios o usa el fallback', () => {
    expect(mensajeApi({ response: { data: 'Ese horario ya está ocupado.' } }, 'x')).toBe('Ese horario ya está ocupado.');
    expect(mensajeApi({ response: { data: { error: 'Cuenta desactivada.' } } }, 'x')).toBe('Cuenta desactivada.');
    expect(mensajeApi(new Error('red'), 'fallback')).toBe('fallback');
  });
});
