using System.Collections.Concurrent;

namespace AgendaSaaS.Services;

/// <summary>
/// Serializa las operaciones que ocupan horarios dentro de un mismo tenant, para
/// que dos reservas simultáneas no puedan quedarse con el mismo turno (el chequeo
/// de solapamiento y el insert no son atómicos).
/// Es un lock en memoria: vale mientras la API corra en una sola instancia.
/// </summary>
public class TenantLocks
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AdquirirAsync(
        Guid tenantId)
    {
        var semaforo =
            _locks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));

        await semaforo.WaitAsync();

        return new Liberador(semaforo);
    }

    private sealed class Liberador : IDisposable
    {
        private SemaphoreSlim? _semaforo;

        public Liberador(SemaphoreSlim semaforo)
        {
            _semaforo = semaforo;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _semaforo, null)?.Release();
        }
    }
}
