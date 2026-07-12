namespace AgendaSaaS.DTOs;

public class ReprogramarTurnoRequest
{
    public DateTime FechaHora { get; set; }

    public int? DuracionMinutos { get; set; }
}
