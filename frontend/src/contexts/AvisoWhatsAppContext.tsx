import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';
import { MessageCircle } from 'lucide-react';
import { getAdminPerfil } from '../api/admin.service';
import { ModalBase } from '../components/schedule/ModalBase';
import { linkWhatsApp, mensajeAviso, normalizarTelefono, type DatosAviso } from '../utils/whatsapp';

type AvisoWhatsAppContextValue = {
  /** Después de confirmar/rechazar/cancelar: ofrece avisarle a la clienta por WhatsApp. */
  ofrecerAviso: (datos: DatosAviso) => void;
};

const AvisoWhatsAppContext = createContext<AvisoWhatsAppContextValue>({ ofrecerAviso: () => undefined });

const TITULOS: Record<DatosAviso['tipo'], string> = {
  confirmado: 'Turno confirmado ✓',
  rechazado: 'Turno rechazado',
  cancelado: 'Turno cancelado',
};

export function AvisoWhatsAppProvider({ children }: { children: ReactNode }) {
  const [negocio, setNegocio] = useState<{ nombre?: string; linkAgenda?: string }>({});
  const [aviso, setAviso] = useState<DatosAviso | null>(null);
  const [mensaje, setMensaje] = useState('');

  useEffect(() => {
    getAdminPerfil()
      .then((p) => setNegocio({ nombre: p.nombre, linkAgenda: `${window.location.origin}/agenda/${p.slug}` }))
      .catch(() => undefined);
  }, []);

  const ofrecerAviso = useCallback(
    (datos: DatosAviso) => {
      setMensaje(mensajeAviso(datos, negocio));
      setAviso(datos);
    },
    [negocio],
  );

  const cerrar = () => setAviso(null);
  const tieneTelefono = !!normalizarTelefono(aviso?.telefonoCliente);
  const nombre = aviso?.nombreCliente?.trim() || 'la clienta';

  return (
    <AvisoWhatsAppContext.Provider value={{ ofrecerAviso }}>
      {children}

      {aviso && (
        <ModalBase title={TITULOS[aviso.tipo]} subtitle={`¿Le avisás a ${nombre} por WhatsApp?`} onClose={cerrar}>
          <label className="block space-y-1">
            <span className="text-sm font-medium text-slate-700">Mensaje</span>
            <textarea
              className="h-40 w-full rounded-2xl border border-slate-200 p-3 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              value={mensaje}
              onChange={(e) => setMensaje(e.target.value)}
            />
          </label>

          {!tieneTelefono && (
            <p className="mt-2 text-xs text-amber-700">
              El teléfono de la clienta no parece válido: WhatsApp te va a pedir que elijas el contacto.
            </p>
          )}

          <div className="mt-4 flex flex-col gap-2">
            <a
              href={linkWhatsApp(aviso.telefonoCliente, mensaje)}
              target="_blank"
              rel="noopener noreferrer"
              onClick={cerrar}
              className="inline-flex h-12 items-center justify-center gap-2 rounded-2xl bg-emerald-500 px-4 text-sm font-semibold text-white"
            >
              <MessageCircle size={18} />
              Enviar por WhatsApp
            </a>
            <button type="button" onClick={cerrar} className="h-11 rounded-2xl text-sm font-medium text-slate-500">
              Ahora no
            </button>
          </div>
        </ModalBase>
      )}
    </AvisoWhatsAppContext.Provider>
  );
}

export function useAvisoWhatsApp() {
  return useContext(AvisoWhatsAppContext);
}
