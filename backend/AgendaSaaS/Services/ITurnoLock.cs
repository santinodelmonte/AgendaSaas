namespace AgendaSaaS.Services;

public interface ITurnoLock
{
    /// <summary>
    /// Serializa las operaciones que ocupan horarios de una manicurista, para que
    /// dos reservas simultáneas no pasen ambas el chequeo de solapamiento.
    /// </summary>
    Task<IAsyncDisposable> AdquirirAsync(Guid tenantId);
}
