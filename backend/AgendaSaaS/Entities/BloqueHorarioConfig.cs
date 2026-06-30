namespace AgendaSaaS.Entities;

public class BloqueHorarioConfig
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public DayOfWeek DiaSemana { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }

    public TimeSpan? PausaInicio { get; set; }

    public TimeSpan? PausaFin { get; set; }
}