import { format, parseISO } from 'date-fns';
import { es } from 'date-fns/locale';

export function formatDateTime(value?: string) {
  if (!value) return '-';
  return format(parseISO(value), 'dd/MM/yyyy HH:mm', { locale: es });
}

export function formatDate(value?: string) {
  if (!value) return '-';
  return format(parseISO(value), 'yyyy-MM-dd');
}

export function formatDayLabel(value?: string) {
  if (!value) return '-';
  return format(parseISO(value), "EEEE d 'de' MMMM", { locale: es });
}

export function formatTime(value?: string) {
  if (!value) return '-';
  return format(parseISO(value), 'HH:mm', { locale: es });
}

export function toDateInputValue(value: Date) {
  return format(value, 'yyyy-MM-dd');
}
