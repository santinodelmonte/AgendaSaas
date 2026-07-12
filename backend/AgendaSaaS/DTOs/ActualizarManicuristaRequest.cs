namespace AgendaSaaS.DTOs;

public class ActualizarManicuristaRequest
{
    public string Nombre { get; set; } = string.Empty;

    public string WhatsApp { get; set; } = string.Empty;

    public int DuracionTurnoMinutos { get; set; }

    public string? Slug { get; set; }
}
