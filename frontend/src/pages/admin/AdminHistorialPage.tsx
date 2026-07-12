import { useEffect, useState } from 'react';
import { getHistorial } from '../../api/admin.service';
import type { HistorialItem } from '../../types/admin';
import { Loading } from '../../components/common/Loading';
import { formatDateTime } from '../../utils/date';

const ESTADO_BADGE: Record<string, string> = {
  Confirmado: 'bg-green-100 text-green-700',
  Rechazado:  'bg-red-100 text-red-700',
  Pendiente:  'bg-amber-100 text-amber-700',
};

export function AdminHistorialPage() {
  const [items, setItems] = useState<HistorialItem[]>([]);
  const [pagina, setPagina] = useState(1);
  const [loading, setLoading] = useState(true);
  const [hayMas, setHayMas] = useState(true);
  const TAMANO = 20;

  useEffect(() => {
    setLoading(true);
    getHistorial(1, TAMANO)
      .then((data) => {
        setItems(data);
        setHayMas(data.length === TAMANO);
      })
      .finally(() => setLoading(false));
  }, []);

  async function cargarMas() {
    const siguiente = pagina + 1;
    const data = await getHistorial(siguiente, TAMANO);
    setItems((prev) => [...prev, ...data]);
    setHayMas(data.length === TAMANO);
    setPagina(siguiente);
  }

  return (
    <div className="space-y-6">
      <div>
        <p className="text-sm font-medium uppercase tracking-[0.2em] text-brand-600">Historial</p>
        <h2 className="mt-2 text-2xl font-semibold text-slate-900">Turnos pasados</h2>
      </div>

      {loading ? (
        <Loading label="Cargando historial..." />
      ) : items.length === 0 ? (
        <p className="text-sm text-slate-400">No hay turnos en el historial.</p>
      ) : (
        <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-soft">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-100 bg-slate-50 text-left text-xs font-medium uppercase tracking-widest text-slate-500">
                <th className="px-5 py-3">Fecha</th>
                <th className="px-5 py-3">Cliente</th>
                <th className="px-5 py-3">Servicio</th>
                <th className="px-5 py-3">Estado</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {items.map((item) => (
                <tr key={item.id} className="hover:bg-slate-50">
                  <td className="px-5 py-3 font-medium text-slate-900 whitespace-nowrap">
                    {formatDateTime(item.fechaHora)}
                  </td>
                  <td className="px-5 py-3 text-slate-700">
                    <p>{item.nombreCliente ?? '-'}</p>
                    {item.telefonoCliente && (
                      <p className="text-xs text-slate-400">{item.telefonoCliente}</p>
                    )}
                  </td>
                  <td className="px-5 py-3 text-slate-600">{item.servicioSolicitado ?? '-'}</td>
                  <td className="px-5 py-3">
                    <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-medium ${ESTADO_BADGE[item.estado] ?? 'bg-slate-100 text-slate-600'}`}>
                      {item.estado}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {hayMas && (
            <div className="border-t border-slate-100 p-4 text-center">
              <button
                onClick={cargarMas}
                className="text-sm font-medium text-brand-600 hover:underline"
              >
                Cargar más
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
