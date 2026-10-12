using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using recepcionEventosApi.Security;
using recepcionEventosApi.Services;
using Shared.Constants;
using Shared.Helpers;
using Shared.WebhookModels;

namespace Tests.Unit;

public class ValidacionWebhookServiceTests
{
    private const string Secret = "secreto-unitario";
    private readonly ValidacionWebhookService _servicio =
        new(new HmacAuthenticationService(Options.Create(new WebhookOptions { Secret = Secret })));

    private static string Cuerpo(string eventType = "USUARIO_CREADO", string nombre = "Carolina", string email = "carolina@gmail.com") =>
        JsonSerializer.Serialize(new { eventType, timestamp = "2026-10-02T10:00:00", data = new { nombre, email } });

    private static string Firmar(string body) => HmacHelper.CalcularFirma(body, Secret);

    [Fact]
    public void WebhookValido_DevuelveDto()
    {
        var body = Cuerpo();

        var dto = _servicio.Validar(body, Firmar(body));

        dto.EventType.Should().Be(WebhookConstants.EventTypeUsuarioCreado);
        dto.Data.Nombre.Should().Be("Carolina");
        dto.Data.Email.Should().Be("carolina@gmail.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("firma-incorrecta")]
    public void FirmaAusenteOInvalida_Lanza401(string? firma)
    {
        var act = () => _servicio.Validar(Cuerpo(), firma);

        act.Should().Throw<ApiException>()
            .Where(e => e.StatusCode == 401 && e.Codigo == ErrorCodes.FirmaInvalida);
    }

    [Fact]
    public void BodyAlteradoDespuesDeFirmar_Lanza401()
    {
        var firma = Firmar(Cuerpo());

        var act = () => _servicio.Validar(Cuerpo(nombre: "Otra Persona"), firma);

        act.Should().Throw<ApiException>().Where(e => e.StatusCode == 401);
    }

    [Theory]
    [InlineData("")]
    [InlineData("OTRO_EVENTO")]
    public void EventTypeInvalido_Lanza400(string eventType)
    {
        var body = Cuerpo(eventType: eventType);

        var act = () => _servicio.Validar(body, Firmar(body));

        act.Should().Throw<ApiException>()
            .Where(e => e.StatusCode == 400 && e.Codigo == ErrorCodes.SolicitudInvalida);
    }

    [Fact]
    public void EmailInvalido_Lanza400()
    {
        var body = Cuerpo(email: "correo-invalido");

        var act = () => _servicio.Validar(body, Firmar(body));

        act.Should().Throw<ApiException>().Where(e => e.StatusCode == 400);
    }

    [Theory]
    [InlineData("{ esto no es json")]
    [InlineData("null")]
    [InlineData("{\"eventType\":\"USUARIO_CREADO\",\"timestamp\":\"2026-10-02T10:00:00\"}")]   // sin data
    [InlineData("{\"eventType\":\"USUARIO_CREADO\",\"data\":{\"nombre\":\"A\",\"email\":\"a@b.com\"}}")] // sin timestamp
    public void EstructuraInvalida_Lanza400(string body)
    {
        var act = () => _servicio.Validar(body, Firmar(body));

        act.Should().Throw<ApiException>().Where(e => e.StatusCode == 400);
    }
}
