import { describe, expect, it } from 'vitest';
import { claveABytes, esIOS, estadoBase } from './push';

const IPHONE = 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1';
const ANDROID = 'Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Mobile Safari/537.36';
const MAC = 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15';

const base = { seguro: true, plataforma: 'Linux armv8l', maxTouchPoints: 5, standalone: false, soportaPush: true, permiso: 'default' as const };

describe('esIOS', () => {
  it('detecta iPhone y iPad (que se presenta como Mac táctil)', () => {
    expect(esIOS(IPHONE, 'iPhone', 5)).toBe(true);
    expect(esIOS(MAC, 'MacIntel', 5)).toBe(true);
    expect(esIOS(MAC, 'MacIntel', 0)).toBe(false);
    expect(esIOS(ANDROID, 'Linux armv8l', 5)).toBe(false);
  });
});

describe('estadoBase', () => {
  it('en iPhone desde Safari pide instalar el panel primero', () => {
    expect(estadoBase({ ...base, userAgent: IPHONE, plataforma: 'iPhone', soportaPush: false })).toBe('ios-instalar');
  });

  it('en iPhone con el panel instalado permite activar', () => {
    expect(estadoBase({ ...base, userAgent: IPHONE, plataforma: 'iPhone', standalone: true })).toBe('inactivas');
  });

  it('en Android permite activar directo desde el navegador', () => {
    expect(estadoBase({ ...base, userAgent: ANDROID })).toBe('inactivas');
  });

  it('sin HTTPS avisa que hace falta, aun en iPhone', () => {
    expect(estadoBase({ ...base, userAgent: IPHONE, plataforma: 'iPhone', seguro: false })).toBe('requiere-https');
  });

  it('informa si el permiso está bloqueado o el navegador no soporta push', () => {
    expect(estadoBase({ ...base, userAgent: ANDROID, permiso: 'denied' })).toBe('bloqueadas');
    expect(estadoBase({ ...base, userAgent: ANDROID, soportaPush: false })).toBe('no-soportado');
  });
});

describe('claveABytes', () => {
  it('decodifica base64url sin relleno', () => {
    expect(Array.from(claveABytes('BAEC_-8'))).toEqual([4, 1, 2, 255, 239]);
  });
});
