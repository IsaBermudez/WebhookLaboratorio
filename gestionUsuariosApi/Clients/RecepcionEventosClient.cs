using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Shared.Constants;
using Shared.Helpers;
using Shared.WebhookModels;

namespace gestionUsuariosApi.Clients;

/// <summary>Cliente HTTP hacia recepcionEventosApi. Traduce los fallos de comunicación a ApiException.</summary>
public class RecepcionEventosClient(HttpClient http)
{
    public async Task<WebhookProcesadoResponse> EnviarUsuarioCreadoAsync(string payload, string firma, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookConstants.RutaRecibirEventoUsuarioCreado)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add(WebhookConstants.SignatureHeader, firma);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(503, ErrorCodes.ReceptorNoDisponible,
                "No fue posible entregar el webhook: el receptor no está disponible.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ApiException(503, ErrorCodes.ReceptorNoDisponible,
                "No fue posible entregar el webhook: tiempo de espera agotado.");
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var resultado = await response.Content.ReadFromJsonAsync<WebhookProcesadoResponse>(WebhookJson.Options, ct);
                    if (resultado is not null)
                        return resultado;
                }
                catch (JsonException)
                {
                    // se trata abajo como respuesta inválida
                }
                throw new ApiException(502, ErrorCodes.WebhookRechazado, "El receptor devolvió una respuesta inválida.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
                throw new ApiException(409, ErrorCodes.EventoDuplicado, "El receptor ya procesó este evento (usuario duplicado).");

            throw new ApiException(502, ErrorCodes.WebhookRechazado,
                $"El receptor rechazó el webhook (HTTP {(int)response.StatusCode}).");
        }
    }
}
