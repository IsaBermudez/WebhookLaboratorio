namespace Shared.DTOs;

public class UsuarioCreadoWebhookDto
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public UsuarioWebhookDataDto Data { get; set; } = null!;
}
