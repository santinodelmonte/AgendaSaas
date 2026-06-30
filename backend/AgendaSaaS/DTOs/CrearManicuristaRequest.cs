namespace AgendaSaaS.DTOs;

public class CrearManicuristaRequest
{
    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string WhatsApp { get; set; } = string.Empty;

    public int DuracionTurnoMinutos { get; set; }
}