using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using gestionUsuariosApi.Clients;
using gestionUsuariosApi.Services;
using Microsoft.Extensions.Options;
using Shared.Constants;
using Shared.DTOs;
using Shared.Helpers;
using Shared.WebhookModels;

namespace Tests.Unit;

public class WebhookSenderServiceTests
{
    private const string Secret = "secreto-emisor";

    private static WebhookSenderService CrearServicio(HttpMessageHandler handler) =>
        new(new RecepcionEventosClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }),
            Options.Create(new WebhookOptions { Secret = Secret }));

    private static readonly CrearUsuarioRequest Usuario = new() { Nombre = "Carolina", Email = "carolina@gmail.com" };

    [Fact]
    public async Task EnviaPayloadFirmadoConHmacSobreElBodyExacto()
    {
        var handler = new CapturaHandler(HttpStatusCode.OK,
            JsonSerializer.Serialize(new WebhookProcesadoResponse { UsuarioId = 7, EventoId = 9, Mensaje = "ok" }, WebhookJson.Options));

        var resultado = await CrearServicio(handler).EnviarUsuarioCreadoAsync(Usuario, CancellationToken.None);

        resultado.UsuarioId.Should().Be(7);
        handler.Metodo.Should().Be(HttpMethod.Post);
        handler.Ruta.Should().Be("/" + WebhookConstants.RutaRecibirEventoUsuarioCreado);
        handler.Firma.Should().Be(HmacHelper.CalcularFirma(handler.Body!, Secret));

        var payload = JsonSerializer.Deserialize<JsonElement>(handler.Body!);
        payload.GetProperty("eventType").GetString().Should().Be("USUARIO_CREADO");
        payload.GetProperty("data").GetProperty("email").GetString().Should().Be("carolina@gmail.com");
    }

    [Fact]
    public async Task ReceptorNoDisponible_Lanza503()
    {
        var handler = new CapturaHandler(error: new HttpRequestException("conexión rechazada"));

        var act = () => CrearServicio(handler).EnviarUsuarioCreadoAsync(Usuario, CancellationToken.None);

        (await act.Should().ThrowAsync<ApiException>())
            .Where(e => e.StatusCode == 503 && e.Codigo == ErrorCodes.ReceptorNoDisponible);
    }

    [Fact]
    public async Task ReceptorResponde409_SePropagaComoConflicto()
    {
        var handler = new CapturaHandler(HttpStatusCode.Conflict, "{}");

        var act = () => CrearServicio(handler).EnviarUsuarioCreadoAsync(Usuario, CancellationToken.None);

        (await act.Should().ThrowAsync<ApiException>()).Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task ReceptorResponde401_SeGestionaComoWebhookRechazado502()
    {
        var handler = new CapturaHandler(HttpStatusCode.Unauthorized, "{}");

        var act = () => CrearServicio(handler).EnviarUsuarioCreadoAsync(Usuario, CancellationToken.None);

        (await act.Should().ThrowAsync<ApiException>())
            .Where(e => e.StatusCode == 502 && e.Codigo == ErrorCodes.WebhookRechazado);
    }

    private sealed class CapturaHandler(HttpStatusCode status = HttpStatusCode.OK, string? json = null, Exception? error = null)
        : HttpMessageHandler
    {
        public HttpMethod? Metodo { get; private set; }
        public string? Ruta { get; private set; }
        public string? Firma { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (error is not null)
                throw error;

            Metodo = request.Method;
            Ruta = request.RequestUri!.AbsolutePath;
            Firma = request.Headers.TryGetValues(WebhookConstants.SignatureHeader, out var valores) ? valores.Single() : null;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json")
            };
        }
    }
}
