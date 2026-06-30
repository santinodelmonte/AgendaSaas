import { Loader2 } from 'lucide-react';

type Props = {
  label?: string;
  fullScreen?: boolean;
};

export function Loading({ label = 'Cargando...', fullScreen = false }: Props) {
  const content = (
    <div className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-white px-4 py-3 shadow-soft">
      <Loader2 className="h-5 w-5 animate-spin text-brand-600" />
      <span className="text-sm font-medium text-slate-700">{label}</span>
    </div>
  );

  if (fullScreen) {
    return <div className="flex min-h-dvh items-center justify-center p-6">{content}</div>;
  }

  return content;
}
