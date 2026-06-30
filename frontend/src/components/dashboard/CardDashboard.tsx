import type { ReactNode } from 'react';

type Props = {
  title: string;
  value: string | number;
  description?: string;
  icon?: ReactNode;
};

export function CardDashboard({ title, value, description, icon }: Props) {
  return (
    <article className="rounded-3xl border border-slate-200 bg-white p-5 shadow-soft">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium text-slate-500">{title}</p>
          <p className="mt-2 text-2xl font-semibold tracking-tight text-slate-900">{value}</p>
          {description ? <p className="mt-2 text-sm text-slate-500">{description}</p> : null}
        </div>
        {icon ? <div className="rounded-2xl bg-slate-100 p-3 text-slate-700">{icon}</div> : null}
      </div>
    </article>
  );
}
