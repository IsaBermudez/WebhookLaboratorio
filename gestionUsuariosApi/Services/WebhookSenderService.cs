using System.Text.Json;
using gestionUsuariosApi.Clients;
using Microsoft.Extensions.Options;
using Shared.Constants;
using Shared.DTOs;
using Shared.Helpers;
using Shared.WebhookModels;

namespace gestionUsuariosApi.Services;

/// <summary>Construye el payload webhook, lo firma con HMAC-SHA256 y lo envía.</summary>
public class WebhookSenderService(RecepcionEventosClient client, IOptions<WebhookOptions> options)
{
    public Task<WebhookProcesadoResponse> EnviarUsuarioCreadoAsync(CrearUsuarioRequest usuario, CancellationToken ct)
    {
        var evento = new UsuarioCreadoWebhookDto
        {
            EventType = WebhookConstants.EventTypeUsuarioCreado,
            Timestamp = DateTime.UtcNow,
            Data = new UsuarioWebhookDataDto { Nombre = usuario.Nombre, Email = usuario.Email }
        };

        var payload = JsonSerializer.Serialize(evento, WebhookJson.Options);
        var firma = HmacHelper.CalcularFirma(payload, options.Value.Secret);

        return client.EnviarUsuarioCreadoAsync(payload, firma, ct);
    }
}
