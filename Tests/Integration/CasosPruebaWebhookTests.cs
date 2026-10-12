using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using Shared.DTOs;
using Shared.WebhookModels;
using Usuario = recepcionEventosApi.Entities.Usuario;
using Evento = recepcionEventosApi.Entities.Evento;

namespace Tests.Integration;

/// <summary>Casos de prueba obligatorios CP-001 a CP-006 (ver CasosPrueba_Webhook_v2.xlsx).</summary>
public class CasosPruebaWebhookTests : IntegrationTestBase
{
    // ---------- CP-001: recepción exitosa de webhook (emisor -> receptor -> BD) ----------
    [Fact]
    public async Task CP001_RecepcionExitosaDeWebhook()
    {
        var emisor = CrearClienteEmisor();

        var respuesta = await emisor.PostAsJsonAsync("/crearUsuario", new { nombre = "abcde", email = "abcde@gmail.com" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var usuarioCreado = await respuesta.Content.ReadFromJsonAsync<UsuarioResponse>();
        usuarioCreado!.Nombre.Should().Be("abcde");
        usuarioCreado.Email.Should().Be("abcde@gmail.com");

        TotalUsuarios().Should().Be(1);
        TotalEventos().Should().Be(1);
        Usuarios().Single().Id.Should().Be(usuarioCreado.Id);

        // El emisor consulta la misma BD compartida y ve el usuario persistido por el receptor.
        var consulta = await emisor.GetFromJsonAsync<List<UsuarioResponse>>("/consultarUsuarios");
        consulta.Should().ContainSingle(u => u.Email == "abcde@gmail.com");
    }

    // ---------- CP-002: webhook inválido ----------
    [Fact]
    public async Task CP002_WebhookInvalido_Devuelve400YNoPersiste()
    {
        // Cuerpo del Excel: eventType vacío, nombre vacío y email inválido (correctamente firmado).
        var body = CrearCuerpoWebhook(nombre: "", email: "correo-invalido", eventType: "");

        var respuesta = await EnviarWebhookAsync(body, Firmar(body));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LeerErrorAsync(respuesta))!.Codigo.Should().Be(ErrorCodes.SolicitudInvalida);
        TotalUsuarios().Should().Be(0);
        TotalEventos().Should().Be(0);
    }

    [Fact]
    public async Task CP002_JsonMalformado_Devuelve400YNoPersiste()
    {
        const string body = "{ esto no es json";

        var respuesta = await EnviarWebhookAsync(body, Firmar(body));

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        TotalUsuarios().Should().Be(0);
        TotalEventos().Should().Be(0);
    }

    // ---------- CP-003: evento duplicado ----------
    [Fact]
    public async Task CP003_EventoDuplicado_PrimerEnvio200SegundoEnvio409()
    {
        var body = CrearCuerpoWebhook("Juan Pérez", "juan.perez@gmail.com");
        var firma = Firmar(body);

        var primero = await EnviarWebhookAsync(body, firma);
        var segundo = await EnviarWebhookAsync(body, firma);

        primero.StatusCode.Should().Be(HttpStatusCode.OK);
        segundo.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await LeerErrorAsync(segundo))!.Codigo.Should().Be(ErrorCodes.EventoDuplicado);

        TotalEventos().Should().Be(1, "solo se almacena el primer evento");
        TotalUsuarios().Should().Be(1);
    }

    // ---------- CP-004: receptor no disponible ----------
    [Fact]
    public async Task CP004_ReceptorNoDisponible_ErrorGestionadoCon503()
    {
        var emisor = CrearClienteEmisor(() => new ReceptorCaidoHandler());

        var respuesta = await emisor.PostAsJsonAsync("/crearUsuario",
            new { nombre = "Laura Martínez", email = "laura.martinez@gmail.com" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var error = await LeerErrorAsync(respuesta);
        error!.Codigo.Should().Be(ErrorCodes.ReceptorNoDisponible);
        error.Mensaje.Should().NotBeNullOrWhiteSpace();
        TotalUsuarios().Should().Be(0);
        TotalEventos().Should().Be(0);
    }

    // ---------- CP-005: persistencia correcta ----------
    [Fact]
    public async Task CP005_PersistenciaCorrecta_IdsGeneradosPorLaBaseDeDatos()
    {
        var body = CrearCuerpoWebhook("Carlos Rodríguez", "carlos.rodriguez@gmail.com");

        var respuesta = await EnviarWebhookAsync(body, Firmar(body));

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await respuesta.Content.ReadFromJsonAsync<WebhookProcesadoResponse>();

        var usuario = Usuarios().Should().ContainSingle().Subject;
        usuario.Id.Should().BeGreaterThan(0).And.Be(resultado!.UsuarioId);
        usuario.Nombre.Should().Be("Carlos Rodríguez");
        usuario.Email.Should().Be("carlos.rodriguez@gmail.com");
        usuario.FechaCreacion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        var evento = Eventos().Should().ContainSingle().Subject;
        evento.Id.Should().BeGreaterThan(0).And.Be(resultado.EventoId);
        evento.EventType.Should().Be(WebhookConstants.EventTypeUsuarioCreado);
        evento.Estado.Should().Be(WebhookConstants.EstadoProcesado);
        evento.FechaRecepcion.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CP005_LosIdsSonSecuencialesYLosAsignaLaBd()
    {
        foreach (var n in new[] { 1, 2, 3 })
        {
            var body = CrearCuerpoWebhook($"Usuario {n}", $"usuario{n}@test.com");
            (await EnviarWebhookAsync(body, Firmar(body))).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        Usuarios().Select(u => u.Id).Should().Equal(1, 2, 3);
        Eventos().Select(e => e.Id).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void CP005_EfCoreDelegaLaGeneracionDeIdsEnLaBaseDeDatos()
    {
        ConsultarBd(db =>
        {
            foreach (var tipo in new[] { typeof(Usuario), typeof(Evento) })
            {
                var propiedadId = db.Model.FindEntityType(tipo)!.FindProperty("Id")!;
                propiedadId.ValueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd,
                    $"{tipo.Name}.Id debe generarlo la base de datos (IDENTITY)");
            }
            return 0;
        });
    }

    [Fact]
    public void CP005_LasColumnasIdSonIdentityEnSqlServer()
    {
        var esIdentity = ConsultarBd(db => new[] { "Usuarios", "Eventos" }.ToDictionary(
            tabla => tabla,
            tabla => db.Database
                .SqlQueryRaw<int>("SELECT CAST(COLUMNPROPERTY(OBJECT_ID('dbo." + tabla + "'), 'Id', 'IsIdentity') AS int) AS Value")
                .AsEnumerable().Single()));

        esIdentity["Usuarios"].Should().Be(1, "Usuarios.Id debe ser IDENTITY(1,1)");
        esIdentity["Eventos"].Should().Be(1, "Eventos.Id debe ser IDENTITY(1,1)");
    }

    // ---------- CP-006: webhook sin autenticación ----------
    [Fact]
    public async Task CP006_SinCabeceraXSignature_Devuelve401YNoPersiste()
    {
        var body = CrearCuerpoWebhook("Usuario Seguridad", "seguridad@test.com");

        var respuesta = await EnviarWebhookAsync(body, firma: null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await LeerErrorAsync(respuesta))!.Codigo.Should().Be(ErrorCodes.FirmaInvalida);
        TotalUsuarios().Should().Be(0);
        TotalEventos().Should().Be(0);
    }

    [Fact]
    public async Task CP006_FirmaIncorrecta_Devuelve401YNoPersiste()
    {
        var body = CrearCuerpoWebhook("Usuario Seguridad", "seguridad@test.com");
        var firmaDeOtroCuerpo = Firmar(CrearCuerpoWebhook("Otro", "otro@test.com"));

        var respuesta = await EnviarWebhookAsync(body, firmaDeOtroCuerpo);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        TotalUsuarios().Should().Be(0);
        TotalEventos().Should().Be(0);
    }

    [Fact]
    public async Task CP006_FirmaNoHexadecimal_Devuelve401()
    {
        var body = CrearCuerpoWebhook("Usuario Seguridad", "seguridad@test.com");

        var respuesta = await EnviarWebhookAsync(body, "esto-no-es-una-firma");

        respuesta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        TotalUsuarios().Should().Be(0);
    }

    // ---------- Consultas y manejo global de errores ----------
    [Fact]
    public async Task ConsultarEventos_DevuelveLosEventosRegistrados()
    {
        var body = CrearCuerpoWebhook("Ana", "ana@test.com");
        await EnviarWebhookAsync(body, Firmar(body));

        var lista = await ReceptorClient.GetFromJsonAsync<List<recepcionEventosApi.DTOs.EventoResponse>>("/consultarEventos");
        var uno = await ReceptorClient.GetFromJsonAsync<recepcionEventosApi.DTOs.EventoResponse>("/consultarEventoId/1");

        lista.Should().ContainSingle().Which.Estado.Should().Be(WebhookConstants.EstadoProcesado);
        uno!.EventType.Should().Be(WebhookConstants.EventTypeUsuarioCreado);
    }

    [Fact]
    public async Task ConsultarEventoInexistente_Devuelve404ConFormatoDeError()
    {
        var respuesta = await ReceptorClient.GetAsync("/consultarEventoId/999");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LeerErrorAsync(respuesta))!.Codigo.Should().Be(ErrorCodes.NoEncontrado);
    }

    [Fact]
    public async Task ConsultarUsuarioInexistenteEnEmisor_Devuelve404()
    {
        var emisor = CrearClienteEmisor();

        var respuesta = await emisor.GetAsync("/consultarUsuarioId/999");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await LeerErrorAsync(respuesta))!.Codigo.Should().Be(ErrorCodes.NoEncontrado);
    }

    [Fact]
    public async Task CrearUsuarioConEmailInvalido_EmisorDevuelve400SinLlamarAlReceptor()
    {
        var emisor = CrearClienteEmisor();

        var respuesta = await emisor.PostAsJsonAsync("/crearUsuario", new { nombre = "Ana", email = "correo-invalido" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await LeerErrorAsync(respuesta))!.Codigo.Should().Be(ErrorCodes.SolicitudInvalida);
        TotalUsuarios().Should().Be(0);
    }

    [Fact]
    public async Task CrearUsuarioDosVecesConElMismoEmail_SegundaVezDevuelve409()
    {
        var emisor = CrearClienteEmisor();
        var usuario = new { nombre = "Ana", email = "ana@test.com" };

        var primera = await emisor.PostAsJsonAsync("/crearUsuario", usuario);
        var segunda = await emisor.PostAsJsonAsync("/crearUsuario", usuario);

        primera.StatusCode.Should().Be(HttpStatusCode.OK);
        segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);
        TotalUsuarios().Should().Be(1);
    }
}
