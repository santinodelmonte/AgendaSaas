// La API responde los errores de distintas formas: BadRequest con un string plano,
// el middleware/rate limiter con { error }, y la validación de ASP.NET con
// ProblemDetails ({ title, errors }).
export function mensajeDeError(data: unknown): string | undefined {
  if (typeof data === 'string') {
    return data.trim().length > 0 ? data : undefined;
  }

  if (data && typeof data === 'object') {
    const { error, message, title } = data as { error?: unknown; message?: unknown; title?: unknown };
    for (const valor of [error, message, title]) {
      if (typeof valor === 'string' && valor.trim().length > 0) return valor;
    }
  }

  return undefined;
}
