import { useEffect, useState } from 'react';

const QUERY = '(max-width: 767px)';

// Separa la capa de interacción (tap/swipe vs mouse) sin duplicar lógica de negocio.
export function useIsMobile() {
  const [isMobile, setIsMobile] = useState(() => window.matchMedia(QUERY).matches);

  useEffect(() => {
    const mql = window.matchMedia(QUERY);
    const onChange = (e: MediaQueryListEvent) => setIsMobile(e.matches);
    mql.addEventListener('change', onChange);
    return () => mql.removeEventListener('change', onChange);
  }, []);

  return isMobile;
}
