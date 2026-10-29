namespace AgendaSaaS.Services;

/// <summary>
/// Hora "de pared" del negocio. Los turnos se guardan en hora local (sin zona),
/// así que toda comparación contra "ahora" tiene que usar la misma zona horaria
/// que las manicuristas, no la del servidor (que en la nube suele ser UTC).
/// </summary>
public interface IClock
{
    DateTime Now { get; }

    DateTime Today => Now.Date;
}
