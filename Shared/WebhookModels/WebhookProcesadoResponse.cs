namespace Shared.WebhookModels;

public class WebhookProcesadoResponse
{
    public int UsuarioId { get; set; }
    public int EventoId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
