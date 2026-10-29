using AgendaSaaS.Services;

namespace AgendaSaaS.Tests.Infra;

public sealed class FakeClock : IClock
{
    // Lunes 7 de enero de 2030, 08:00 (hora del negocio).
    public static readonly DateTime Inicial = new(2030, 1, 7, 8, 0, 0);

    public DateTime Now { get; set; } = Inicial;
}
