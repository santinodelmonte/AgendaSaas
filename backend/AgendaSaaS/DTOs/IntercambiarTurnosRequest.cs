namespace AgendaSaaS.DTOs;

public class IntercambiarTurnosRequest
{
    public Guid TurnoAId { get; set; }

    public Guid TurnoBId { get; set; }

    // true: cada turno se lleva la duración del otro;
    // false: cada turno conserva su duración original.
    public bool IntercambiarDuraciones { get; set; }
}
