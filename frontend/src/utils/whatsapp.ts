import { format, parseISO } from 'date-fns';
import { es } from 'date-fns/locale';

export type TipoAviso = 'confirmado' | 'rechazado' | 'cancelado';

export type DatosAviso = {
  tipo: TipoAviso;
  nombreCliente?: string | null;
  telefonoCliente?: string | null;
  fechaHora: string;
  servicio?: string | null;
};

/**
 * Convierte un teléfono cargado a mano por la clienta al formato de wa.me
 * (internacional, sin "+"). Asume Argentina salvo que venga con otro "+código".
 *   "011 15-2233-4455" -> "5491122334455"
 *   "+54 9 11 2233 4455" -> "5491122334455"
 * Devuelve null si no parece un teléfono.
 */
export function normalizarTelefono(telefono?: string | null): string | null {
  if (!telefono) return null;

  let digitos = telefono.replace(/\D/g, '');

  // "+34..." o "0034...": viene con código de país.
  const internacional = telefono.trim().startsWith('+') || digitos.startsWith('00');
  if (digitos.startsWith('00')) digitos = digitos.slice(2);
  if (digitos.length < 8) return null;

  if (internacional || digitos.startsWith('54')) {
    if (!digitos.startsWith('54')) return digitos; // otro país: tal cual

    // Celulares argentinos en WhatsApp: 54 + 9 + área + número (sin el 0 ni el 15).
    let nacional = digitos.slice(2);
    if (nacional.startsWith('9')) nacional = nacional.slice(1);
    return `549${quitar15(nacional.replace(/^0/, ''))}`;
  }

  return `549${quitar15(digitos.replace(/^0/, ''))}`;
}

// "11 15 2233 4455" -> "11 2233 4455": el 15 va después del código de área
// (2 a 4 dígitos) y el número nacional queda en 10 dígitos.
function quitar15(nacional: string) {
  if (nacional.length !== 12) return nacional;
  for (const largoArea of [2, 3, 4]) {
    if (nacional.slice(largoArea, largoArea + 2) === '15') {
      return nacional.slice(0, largoArea) + nacional.slice(largoArea + 2);
    }
  }
  return nacional;
}

export function linkWhatsApp(telefono: string | null | undefined, mensaje: string) {
  const numero = normalizarTelefono(telefono) ?? '';
  return `https://wa.me/${numero}?text=${encodeURIComponent(mensaje)}`;
}

function cuando(fechaHora: string) {
  // "el lunes 14/1 a las 10:00"
  return format(parseISO(fechaHora), "'el' EEEE d/M 'a las' HH:mm", { locale: es });
}

export function mensajeAviso(datos: DatosAviso, negocio: { nombre?: string; linkAgenda?: string }) {
  const nombre = datos.nombreCliente?.trim().split(/\s+/)[0];
  const saludo = nombre ? `¡Hola ${nombre}!` : '¡Hola!';
  const servicio = datos.servicio ? ` (${datos.servicio})` : '';
  const firma = negocio.nombre ? `\n\n${negocio.nombre}` : '';
  const otroHorario = negocio.linkAgenda ? `\n\nPodés elegir otro horario acá: ${negocio.linkAgenda}` : '';

  switch (datos.tipo) {
    case 'confirmado':
      return `${saludo} Te confirmo tu turno ${cuando(datos.fechaHora)}${servicio}. ¡Te espero! 💅${firma}`;
    case 'rechazado':
      return `${saludo} Lamentablemente no puedo tomar tu turno ${cuando(datos.fechaHora)}${servicio}.${otroHorario}${firma}`;
    case 'cancelado':
      return `${saludo} Te aviso que tuve que cancelar tu turno ${cuando(datos.fechaHora)}${servicio}. Disculpá las molestias.${otroHorario}${firma}`;
  }
}
