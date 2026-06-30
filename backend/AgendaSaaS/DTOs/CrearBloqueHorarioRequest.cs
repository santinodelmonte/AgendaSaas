namespace AgendaSaaS.DTOs;

public class CrearBloqueHorarioRequest
{
    public DayOfWeek DiaSemana { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }

    public TimeSpan? PausaInicio { get; set; }

    public TimeSpan? PausaFin { get; set; }
}