import { describe, expect, it } from 'vitest';
import { linkWhatsApp, mensajeAviso, normalizarTelefono } from './whatsapp';

describe('normalizarTelefono', () => {
  it.each([
    ['11 2233-4455', '5491122334455'],
    ['011 15 2233-4455', '5491122334455'],
    ['1122334455', '5491122334455'],
    ['+54 9 11 2233 4455', '5491122334455'],
    ['+54 11 2233 4455', '5491122334455'],
    ['54 9 351 15 512 3456', '5493515123456'],
    ['0351 15-512-3456', '5493515123456'],
    ['(0221) 15 456-7890', '5492214567890'],
    ['+34 612 345 678', '34612345678'],
    ['0034 612 345 678', '34612345678'],
  ])('%s -> %s', (entrada, esperado) => {
    expect(normalizarTelefono(entrada)).toBe(esperado);
  });

  it('descarta valores que no son teléfonos', () => {
    expect(normalizarTelefono('')).toBeNull();
    expect(normalizarTelefono(null)).toBeNull();
    expect(normalizarTelefono('123')).toBeNull();
  });
});

describe('mensajeAviso', () => {
  const turno = {
    nombreCliente: 'Ana María López',
    telefonoCliente: '11 2233 4455',
    fechaHora: '2030-01-14T10:00:00',
    servicio: 'Semipermanente',
  };
  const negocio = { nombre: 'Uñas de Caro', linkAgenda: 'https://agenda.test/agenda/caro' };

  it('confirmación con día, hora, servicio y firma', () => {
    expect(mensajeAviso({ ...turno, tipo: 'confirmado' }, negocio)).toBe(
      '¡Hola Ana! Te confirmo tu turno el lunes 14/1 a las 10:00 (Semipermanente). ¡Te espero! 💅\n\nUñas de Caro',
    );
  });

  it('rechazo y cancelación ofrecen el link para elegir otro horario', () => {
    expect(mensajeAviso({ ...turno, tipo: 'rechazado' }, negocio)).toContain(
      'no puedo tomar tu turno el lunes 14/1 a las 10:00 (Semipermanente).\n\nPodés elegir otro horario acá: https://agenda.test/agenda/caro',
    );
    expect(mensajeAviso({ ...turno, tipo: 'cancelado' }, negocio)).toContain('tuve que cancelar tu turno');
  });

  it('funciona sin nombre ni servicio', () => {
    expect(mensajeAviso({ tipo: 'confirmado', fechaHora: '2030-01-14T10:00:00' }, {})).toBe(
      '¡Hola! Te confirmo tu turno el lunes 14/1 a las 10:00. ¡Te espero! 💅',
    );
  });
});

describe('linkWhatsApp', () => {
  it('arma el link con el número y el texto codificado', () => {
    expect(linkWhatsApp('11 2233 4455', 'Hola & chau')).toBe('https://wa.me/5491122334455?text=Hola%20%26%20chau');
  });

  it('sin teléfono válido deja elegir el contacto en WhatsApp', () => {
    expect(linkWhatsApp(null, 'Hola')).toBe('https://wa.me/?text=Hola');
  });
});
