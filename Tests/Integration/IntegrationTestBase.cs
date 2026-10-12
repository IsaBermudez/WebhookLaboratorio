using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using recepcionEventosApi.Configuration;
using recepcionEventosApi.Entities;
using Shared.Constants;
using Shared.DTOs;
using Shared.Helpers;
using Shared.WebhookModels;

namespace Tests.Integration;

/// <summary>
/// Cada prueba crea su propia BD en SQL Server (WebhookLab_Test_&lt;guid&gt;) y su propio receptor, y la elimina al
/// terminar: no hay estado compartido entre pruebas.
/// El servidor se toma de la variable de entorno WEBHOOK_TEST_SQLSERVER (cadena de conexión sin Database);
/// por defecto se usa LocalDB: Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    private readonly List<IDisposable> _recursos = new();

    public const string VariableEntorno = "WEBHOOK_TEST_SQLSERVER";
    private const string ServidorPorDefecto =
        "Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";

    /// <summary>Cadena de conexión de la BD temporal de esta prueba.</summary>
    protected string CadenaConexion { get; }
    protected ReceptorFactory Receptor { get; }
    protected HttpClient ReceptorClient { get; }

    /// <summary>Secreto HMAC efectivo del receptor (se lee de su configuración real, no se asume).</summary>
    protected string Secret { get; }

    protected IntegrationTestBase()
    {
        var servidor = Environment.GetEnvironmentVariable(VariableEntorno);
        var builder = new SqlConnectionStringBuilder(string.IsNullOrWhiteSpace(servidor) ? ServidorPorDefecto : servidor)
        {
            InitialCatalog = $"WebhookLab_Test_{Guid.NewGuid():N}"
        };
        CadenaConexion = builder.ConnectionString;

        Receptor = new ReceptorFactory(CadenaConexion);
        ReceptorClient = Receptor.CreateClient(); // arranca el host: crea la BD temporal y las tablas
        Secret = Receptor.Services.GetRequiredService<IOptions<WebhookOptions>>().Value.Secret;
    }

    /// <summary>Cliente de gestionUsuariosApi cuyo webhook va (por defecto) al receptor real en memoria.</summary>
    protected HttpClient CrearClienteEmisor(Func<HttpMessageHandler>? handlerReceptor = null)
    {
        var emisor = new EmisorFactory(CadenaConexion, handlerReceptor ?? (() => Receptor.Server.CreateHandler()));
        _recursos.Add(emisor);
        var cliente = emisor.CreateClient();
        _recursos.Add(cliente);
        return cliente;
    }

    protected T ConsultarBd<T>(Func<ApplicationDbContext, T> consulta)
    {
        using var scope = Receptor.Services.CreateScope();
        return consulta(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    protected int TotalUsuarios() => ConsultarBd(db => db.Usuarios.Count());
    protected int TotalEventos() => ConsultarBd(db => db.Eventos.Count());
    protected Usuario[] Usuarios() => ConsultarBd(db => db.Usuarios.OrderBy(u => u.Id).ToArray());
    protected Evento[] Eventos() => ConsultarBd(db => db.Eventos.OrderBy(e => e.Id).ToArray());

    protected static string CrearCuerpoWebhook(string nombre, string email, string eventType = "USUARIO_CREADO") =>
        JsonSerializer.Serialize(new
        {
            eventType,
            timestamp = "2026-10-02T10:00:00",
            data = new { nombre, email }
        });

    protected string Firmar(string body) => HmacHelper.CalcularFirma(body, Secret);

    /// <summary>Envía el body tal cual al receptor. Con firma null no se envía la cabecera X-Signature.</summary>
    protected async Task<HttpResponseMessage> EnviarWebhookAsync(string body, string? firma)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/" + WebhookConstants.RutaRecibirEventoUsuarioCreado)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        if (firma is not null)
            request.Headers.Add(WebhookConstants.SignatureHeader, firma);

        return await ReceptorClient.SendAsync(request);
    }

    protected static Task<ErrorResponse?> LeerErrorAsync(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<ErrorResponse>();

    public void Dispose()
    {
        foreach (var recurso in _recursos)
            recurso.Dispose();
        ReceptorClient.Dispose();
        Receptor.Dispose();

        // Elimina la BD temporal (EF libera el pool de conexiones antes de hacer DROP DATABASE).
        using var db = new DbContext(new DbContextOptionsBuilder().UseSqlServer(CadenaConexion).Options);
        db.Database.EnsureDeleted();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Simula recepcionEventosApi detenida: la conexión falla.</summary>
public sealed class ReceptorCaidoHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("No se puede establecer una conexión: el receptor está detenido.");
}
