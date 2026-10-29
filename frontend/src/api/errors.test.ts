import { describe, expect, it } from 'vitest';
import { mensajeDeError } from './errors';

describe('mensajeDeError', () => {
  it('usa el string plano de un BadRequest', () => {
    expect(mensajeDeError('El turno ya no está disponible.')).toBe('El turno ya no está disponible.');
  });

  it('usa { error } del middleware y del rate limiter', () => {
    expect(mensajeDeError({ error: 'Demasiados intentos.' })).toBe('Demasiados intentos.');
  });

  it('usa el title de un ProblemDetails de validación', () => {
    expect(mensajeDeError({ title: 'One or more validation errors occurred.', errors: {} })).toBe(
      'One or more validation errors occurred.',
    );
  });

  it('devuelve undefined si no hay mensaje útil', () => {
    expect(mensajeDeError('')).toBeUndefined();
    expect(mensajeDeError(undefined)).toBeUndefined();
    expect(mensajeDeError({ otra: 'cosa' })).toBeUndefined();
  });
});
