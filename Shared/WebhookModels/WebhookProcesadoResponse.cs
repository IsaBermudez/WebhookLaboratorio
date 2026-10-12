namespace Shared.WebhookModels;

/// <summary>Respuesta 200 del receptor: devuelve los IDs generados por la base de datos.</summary>
public class WebhookProcesadoResponse
{
    public int UsuarioId { get; set; }
    public int EventoId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
