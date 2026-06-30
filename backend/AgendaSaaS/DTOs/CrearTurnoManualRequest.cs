namespace AgendaSaaS.DTOs;

public class CrearTurnoManualRequest
{
    public DateTime FechaHora { get; set; }

    public string Nota { get; set; } = string.Empty;
}