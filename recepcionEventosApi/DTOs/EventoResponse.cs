namespace recepcionEventosApi.DTOs;

public class EventoResponse
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaRecepcion { get; set; }
}
