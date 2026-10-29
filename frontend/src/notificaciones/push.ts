import api from '../api/axios';

// Estado de las notificaciones en este dispositivo.
export type EstadoPush =
  | 'cargando'
  | 'no-soportado'
  // iPhone/iPad en Safari: solo funcionan con el panel agregado a la pantalla de inicio.
  | 'ios-instalar'
  | 'bloqueadas'
  | 'inactivas'
  | 'activas';

type Entorno = {
  userAgent: string;
  plataforma: string;
  maxTouchPoints: number;
  standalone: boolean;
  soportaPush: boolean;
  permiso: NotificationPermission | null;
};

export function esIOS(userAgent: string, plataforma: string, maxTouchPoints: number) {
  // Los iPad recientes se presentan como "MacIntel" con pantalla táctil.
  return /iPad|iPhone|iPod/.test(userAgent) || (plataforma === 'MacIntel' && maxTouchPoints > 1);
}

/** Estado sin contar la suscripción (que requiere consultar al service worker). */
export function estadoBase(entorno: Entorno): Exclude<EstadoPush, 'cargando' | 'activas'> {
  if (esIOS(entorno.userAgent, entorno.plataforma, entorno.maxTouchPoints) && !entorno.standalone) {
    return 'ios-instalar';
  }
  if (!entorno.soportaPush) return 'no-soportado';
  if (entorno.permiso === 'denied') return 'bloqueadas';
  return 'inactivas';
}

function entornoActual(): Entorno {
  const nav = navigator as Navigator & { standalone?: boolean };
  return {
    userAgent: nav.userAgent,
    plataforma: nav.platform,
    maxTouchPoints: nav.maxTouchPoints ?? 0,
    standalone: window.matchMedia?.('(display-mode: standalone)').matches || nav.standalone === true,
    soportaPush: 'serviceWorker' in nav && 'PushManager' in window && 'Notification' in window,
    permiso: 'Notification' in window ? Notification.permission : null,
  };
}

async function registro() {
  await navigator.serviceWorker.register('/sw.js');
  return navigator.serviceWorker.ready;
}

export async function obtenerEstado(): Promise<EstadoPush> {
  const base = estadoBase(entornoActual());
  if (base !== 'inactivas') return base;

  const reg = await registro();
  const suscripcion = await reg.pushManager.getSubscription();
  return suscripcion && Notification.permission === 'granted' ? 'activas' : 'inactivas';
}

export function claveABytes(base64Url: string) {
  const relleno = '='.repeat((4 - (base64Url.length % 4)) % 4);
  const base64 = (base64Url + relleno).replace(/-/g, '+').replace(/_/g, '/');
  const binario = atob(base64);
  return Uint8Array.from(binario, (c) => c.charCodeAt(0));
}

async function enviarAlServidor(suscripcion: PushSubscription) {
  await api.post('/api/admin/notificaciones/suscripciones', suscripcion.toJSON());
}

/** Pide permiso, suscribe el dispositivo y lo registra en el backend. */
export async function activar(): Promise<EstadoPush> {
  const permiso = await Notification.requestPermission();
  if (permiso !== 'granted') return permiso === 'denied' ? 'bloqueadas' : 'inactivas';

  const reg = await registro();
  const { data } = await api.get<{ publicKey: string }>('/api/admin/notificaciones/clave-publica');
  const clave = claveABytes(data.publicKey);

  let suscripcion = await reg.pushManager.getSubscription();

  // Una suscripción hecha con otra clave del servidor no sirve: se rehace.
  const claveActual = suscripcion?.options.applicationServerKey;
  if (suscripcion && claveActual && !mismosBytes(new Uint8Array(claveActual), clave)) {
    await suscripcion.unsubscribe();
    suscripcion = null;
  }

  suscripcion ??= await reg.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: clave,
  });

  await enviarAlServidor(suscripcion);
  return 'activas';
}

export async function desactivar(): Promise<EstadoPush> {
  const reg = await registro();
  const suscripcion = await reg.pushManager.getSubscription();

  if (suscripcion) {
    await api.post('/api/admin/notificaciones/suscripciones/eliminar', { endpoint: suscripcion.endpoint });
    await suscripcion.unsubscribe();
  }

  return 'inactivas';
}

export async function enviarPrueba() {
  const { data } = await api.post<{ enviados: number }>('/api/admin/notificaciones/prueba');
  return data.enviados;
}

/**
 * Al abrir el panel, vuelve a informar la suscripción existente al backend: el
 * navegador puede renovarla, o el backend puede haberla borrado por vencida.
 */
export async function sincronizar() {
  try {
    if (estadoBase(entornoActual()) !== 'inactivas' || Notification.permission !== 'granted') return;
    const reg = await registro();
    const suscripcion = await reg.pushManager.getSubscription();
    if (suscripcion) await enviarAlServidor(suscripcion);
  } catch {
    // Silencioso: es una sincronización de fondo.
  }
}

function mismosBytes(a: Uint8Array, b: Uint8Array) {
  return a.length === b.length && a.every((v, i) => v === b[i]);
}
