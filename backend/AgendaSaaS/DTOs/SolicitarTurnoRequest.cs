namespace AgendaSaaS.DTOs;

public class SolicitarTurnoRequest
{
    public DateTime FechaHora { get; set; }

    public string NombreCliente { get; set; } = string.Empty;

    public string TelefonoCliente { get; set; } = string.Empty;

    public string Servicio { get; set; } = string.Empty;
}