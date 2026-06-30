namespace AgendaSaaS.DTOs;

public class CrearTurnoManualRequest
{
    public DateTime FechaHora { get; set; }

    public string? NombreCliente { get; set; }

    public string? TelefonoCliente { get; set; }

    public string? Servicio { get; set; }

    public string Nota { get; set; } = string.Empty;
}