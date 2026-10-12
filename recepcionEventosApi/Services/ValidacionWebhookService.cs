using System.Text.Json;
using recepcionEventosApi.Security;
using Shared.Constants;
using Shared.DTOs;
using Shared.Helpers;

namespace recepcionEventosApi.Services;

/// <summary>Orden de validación: 1) firma HMAC (401)  2) estructura (400).</summary>
public class ValidacionWebhookService(HmacAuthenticationService hmac)
{
    public UsuarioCreadoWebhookDto Validar(string body, string? firma)
    {
        if (!hmac.ValidarFirma(body, firma))
            throw new ApiException(401, ErrorCodes.FirmaInvalida, "La firma HMAC (X-Signature) falta o no es válida.");

        UsuarioCreadoWebhookDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<UsuarioCreadoWebhookDto>(body, WebhookJson.Options);
        }
        catch (JsonException)
        {
            throw Invalido("El cuerpo no es un JSON válido con la estructura esperada.");
        }

        if (dto is null)
            throw Invalido("El cuerpo del webhook está vacío.");
        if (dto.EventType != WebhookConstants.EventTypeUsuarioCreado)
            throw Invalido($"eventType debe ser '{WebhookConstants.EventTypeUsuarioCreado}'.");
        if (dto.Timestamp == default)
            throw Invalido("timestamp es obligatorio.");
        if (dto.Data is null)
            throw Invalido("data es obligatorio.");

        var error = UsuarioValidator.ObtenerError(dto.Data.Nombre, dto.Data.Email);
        if (error is not null)
            throw Invalido(error);

        return dto;
    }

    private static ApiException Invalido(string mensaje) =>
        new(400, ErrorCodes.SolicitudInvalida, mensaje);
}
