namespace AgendaSaaS.DTOs;

public class CrearManicuristaRequest
{
    public string Nombre { get; set; } = string.Empty;

    // Email de contacto y también de login de la manicurista.
    public string Email { get; set; } = string.Empty;

    public string WhatsApp { get; set; } = string.Empty;

    public int DuracionTurnoMinutos { get; set; }

    // Opcional: si viene vacío se genera a partir del nombre.
    public string? Slug { get; set; }

    public string Password { get; set; } = string.Empty;
}